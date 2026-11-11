using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SpeedStars.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.Physical.EditorTools
{
    /// <summary>
    /// The rhythm probe (Tools → Speed Stars → Rhythm Probe): the same measurement after every tuning step, so before and after
    /// compare by numbers. It opens the Race scene in play mode (the scenes in the editor stay as they are), waits for GO (a press
    /// before it is a false start), then presses the keys in turn like fingers (each held HoldTime) at four rhythms — slow, good,
    /// fast, mashing — and reads, over the last seconds of each: the trunk's angle (hips → neck) and how far it swings, the whole
    /// body's line (the lowest foot → the head), the speed, the sim's head angle and the biggest one-frame jump of a leg at a
    /// touchdown (the twitch). The table goes to the console and to Temp/RhythmProbe (csv per frame, summary, a picture atlas),
    /// next to the reference game's targets measured from finger play on 2026-11-11.
    /// </summary>
    [InitializeOnLoad]
    public static class RhythmProbe
    {
        const string RaceScene = "Assets/Speed-Staes/Scenes/Game/Race.unity";
        const string PendingKey = "SpeedStars.RhythmProbe.Pending";
        const double HoldTime = 0.08;     // a finger keeps a key down about this long (measured on the phone: 0.07-0.12 s)
        const double WarmGap = 0.34, WarmTime = 1.6;
        const double Window = 2.0;        // the last seconds of each rhythm are read
        const double ShotEvery = 0.2;
        const int Cell = 260;

        struct Segment
        {
            public string Name; public double Gap, Time; public string Target; public bool FromStart;
            public Segment(string name, double gap, double time, string target, bool fromStart = false)
            { Name = name; Gap = gap; Time = time; Target = target; FromStart = fromStart; }
        }

        static readonly Segment[] Segments =
        {
            new Segment("seyrek", 0.40, 3.4, "govde ~0, adim basina 25-35 derece geri sallanma"),
            new Segment("iyi", 0.23, 3.0, "govde 0-5, sallanma <=5, hiz en yuksek (~10 m/s)"),
            new Segment("gecis", 0.13, 2.6, "iyi ritimden sik basmaya ilk saniye: govde yatarken arka bacak govde cizgisinde (fark ~0)", true),
            new Segment("sik", 0.13, 3.0, "govde yatik ve sabitken bacaklar kalcanin altinda (on bacak altta %30+), hiz ~3.5 m/s"),
            new Segment("cok sik", 0.10, 3.0, "sik gibi, biraz daha yatay, hiz 2-4 m/s"),
        };

        static Probe _probe;

        static RhythmProbe() => EditorApplication.playModeStateChanged += OnPlayMode;

        [MenuItem("Tools/Speed Stars/Rhythm Probe (Ritim Olcumu)")]
        static void Run()
        {
            if (EditorApplication.isPlaying) { Begin(); return; }
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                Begin();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && _probe != null)
            {
                EditorApplication.update -= Tick;
                _probe = null;
            }
        }

        static void Begin()
        {
            Application.runInBackground = true;
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            EditorSceneManager.LoadSceneInPlayMode(RaceScene, new LoadSceneParameters(LoadSceneMode.Single));
            EditorApplication.update -= Tick;
            _probe = new Probe();
            EditorApplication.update += Tick;
            Debug.Log("[RhythmProbe] Race loading, waiting for GO.");
        }

        static void Tick()
        {
            if (_probe == null || !EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
            if (_probe.Step()) { EditorApplication.update -= Tick; _probe = null; }
        }

        sealed class Stats
        {
            public readonly List<float> Trunk = new List<float>(), Line = new List<float>(), Speed = new List<float>(), Head = new List<float>();
            public float ThighJump, KneeJump;
            public readonly List<float> BackLine = new List<float>();   // the back thigh against the trunk's line (deg, 0 = in line)
            public int Under, Count;                                   // frames with a thigh forward under the body (over 20 deg)
        }

        sealed class Probe
        {
            RunnerBody _body;
            RunnerPoseDriver _driver;
            RaceSessionAdapter _race;
            Transform _hips, _neck, _head, _thL, _shL, _ftL, _thR, _shR, _ftR;
            readonly List<double[]> _events = new List<double[]>();
            readonly double[] _segStart = new double[Segments.Length + 1];
            readonly Stats[] _stats = new Stats[Segments.Length];
            readonly StringBuilder _csv = new StringBuilder("segment,gap,t,speed,head,trunk,bodyLine,hipY,Lthigh,Lknee,Rthigh,Rknee\n");
            readonly List<Color32[]> _shots = new List<Color32[]>();
            double _t0 = -1, _waitFrom = -1, _nextShot, _end;
            int _next, _fails;
            readonly bool[] _wasGrounded = new bool[2];
            readonly float[] _lastThigh = new float[2], _lastKnee = new float[2];
            RenderTexture _rt;
            Texture2D _grab;
            static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

            public Probe()
            {
                for (int i = 0; i < _stats.Length; i++) _stats[i] = new Stats();
                double t = 0.3;
                int leg = 0;
                for (double w = 0; w < WarmTime; w += WarmGap) { Press(t, leg, WarmGap); leg = 1 - leg; t += WarmGap; }
                for (int s = 0; s < Segments.Length; s++)
                {
                    _segStart[s] = t;
                    int n = Mathf.RoundToInt((float)(Segments[s].Time / Segments[s].Gap));
                    for (int i = 0; i < n; i++) { Press(t, leg, Segments[s].Gap); leg = 1 - leg; t += Segments[s].Gap; }
                }
                _segStart[Segments.Length] = t;
                _end = t;
                _events.Sort((a, b) => a[0].CompareTo(b[0]));
            }

            void Press(double t, int leg, double gap)
            {
                _events.Add(new[] { t, leg, 1.0 });
                _events.Add(new[] { t + System.Math.Min(HoldTime, gap * 0.9), leg, 0.0 });
            }

            /// <summary>One editor frame; true when the probe is over.</summary>
            public bool Step()
            {
                if (_t0 < 0) return WaitForGo();
                if (_race && _race.IsFailing) _fails++;
                double now = Time.timeAsDouble - _t0;
                while (_next < _events.Count && _events[_next][0] <= now)
                {
                    _body.input.Inject((int)_events[_next][1], _events[_next][2] > 0.5, _t0 + _events[_next][0]);
                    _next++;
                }
                if (now < 0) return false;
                int seg = -1;
                for (int s = 0; s < Segments.Length; s++)
                    if (Segments[s].FromStart ? now >= _segStart[s] && now < _segStart[s + 1]
                                              : now >= _segStart[s + 1] - Window && now < _segStart[s + 1]) seg = s;
                if (seg >= 0) Read(seg, now);
                if (now < _end) return false;
                Finish();
                return true;
            }

            bool WaitForGo()
            {
                if (_waitFrom < 0) _waitFrom = Time.realtimeSinceStartupAsDouble;
                if (!_body)
                {
                    foreach (var b in Object.FindObjectsOfType<RunnerBody>())
                        if (b.isActiveAndEnabled && b.input) _body = b;
                    if (!_body) return Timeout();
                    _driver = _body.GetComponent<RunnerPoseDriver>();
                    _race = Object.FindObjectOfType<RaceSessionAdapter>();
                    Transform root = _body.transform;
                    _hips = Bone(root, "B-hips"); _neck = Bone(root, "B-neck"); _head = Bone(root, "B-head");
                    _thL = Bone(root, "B-thigh.L"); _shL = Bone(root, "B-shin.L"); _ftL = Bone(root, "B-foot.L");
                    _thR = Bone(root, "B-thigh.R"); _shR = Bone(root, "B-shin.R"); _ftR = Bone(root, "B-foot.R");
                }
                if (_race && (_race.IsCountingDown || _race.IsFailing || _race.StartPhase.ToString() != "Go"))
                    return Timeout();
                _t0 = Time.timeAsDouble + 0.3;
                Debug.Log("[RhythmProbe] GO: pressing.");
                return false;
            }

            bool Timeout()
            {
                if (Time.realtimeSinceStartupAsDouble - _waitFrom < 30.0) return false;
                Debug.LogWarning("[RhythmProbe] No runner or no GO within 30 s: stopped.");
                return true;
            }

            static Transform Bone(Transform root, string name)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                return null;
            }

            Vector3 Local(Transform t) => _body.transform.InverseTransformPoint(t.position);

            float Up(Transform a, Transform b)
            {
                Vector3 d = _body.transform.InverseTransformDirection(b.position - a.position);
                return Mathf.Atan2(d.z, d.y) * Mathf.Rad2Deg;
            }

            float Down(Transform a, Transform b)
            {
                Vector3 d = _body.transform.InverseTransformDirection(b.position - a.position);
                return Mathf.Atan2(d.z, -d.y) * Mathf.Rad2Deg;
            }

            static float Knee(Transform a, Transform b, Transform c) => Vector3.Angle(a.position - b.position, c.position - b.position);

            void Read(int seg, double now)
            {
                Stats st = _stats[seg];
                Transform lowFoot = Local(_ftL).y < Local(_ftR).y ? _ftL : _ftR;
                Vector3 h = Local(_head), f = Local(lowFoot);
                float line = Mathf.Atan2(h.z - f.z, h.y - f.y) * Mathf.Rad2Deg;
                float trunk = Up(_hips, _neck);
                st.Trunk.Add(trunk); st.Line.Add(line); st.Speed.Add(_body.Sim.Speed); st.Head.Add(_body.Sim.HeadAngle);
                float[] thigh = { Down(_thL, _shL), Down(_thR, _shR) };
                st.BackLine.Add(Mathf.Min(thigh[0], thigh[1]) + trunk);
                st.Count++;
                if (Mathf.Max(thigh[0], thigh[1]) > 20f) st.Under++;
                float[] knee = { Knee(_thL, _shL, _ftL), Knee(_thR, _shR, _ftR) };
                for (int i = 0; i < 2; i++)
                {
                    bool grounded = (i == 0 ? _driver.leftLeg : _driver.rightLeg).grounded;
                    if (grounded && !_wasGrounded[i] && st.Trunk.Count > 1)
                    {
                        st.ThighJump = Mathf.Max(st.ThighJump, Mathf.Abs(thigh[i] - _lastThigh[i]));
                        st.KneeJump = Mathf.Max(st.KneeJump, Mathf.Abs(knee[i] - _lastKnee[i]));
                    }
                    _wasGrounded[i] = grounded; _lastThigh[i] = thigh[i]; _lastKnee[i] = knee[i];
                }
                _csv.AppendLine(string.Format(Ci, "{0},{1},{2:F3},{3:F2},{4:F1},{5:F1},{6:F1},{7:F3},{8:F1},{9:F1},{10:F1},{11:F1}",
                    Segments[seg].Name, Segments[seg].Gap, now, _body.Sim.Speed, _body.Sim.HeadAngle, trunk, line, Local(_hips).y,
                    thigh[0], knee[0], thigh[1], knee[1]));
                if (now >= _nextShot) { _nextShot = now + ShotEvery; Shot(); }
            }

            void Shot()
            {
                Camera cam = Camera.main;
                if (!cam) return;
                if (!_rt) { _rt = new RenderTexture(960, 540, 24); _grab = new Texture2D(Cell, Cell, TextureFormat.RGB24, false); }
                Vector3 vp = cam.WorldToViewportPoint(_hips.position);
                int cx = Mathf.Clamp((int)(vp.x * 960) - Cell / 2, 0, 960 - Cell), cy = Mathf.Clamp((int)(vp.y * 540) - Cell / 2 + 30, 0, 540 - Cell);
                RenderTexture prev = cam.targetTexture;
                cam.targetTexture = _rt; cam.Render(); cam.targetTexture = prev;
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = _rt;
                _grab.ReadPixels(new Rect(cx, cy, Cell, Cell), 0, 0); _grab.Apply();
                RenderTexture.active = active;
                _shots.Add(_grab.GetPixels32());
            }

            void Finish()
            {
                var sb = new StringBuilder();
                sb.AppendLine("Rhythm probe " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", Ci) + (_fails > 0 ? "  (FALSE START: invalid)" : ""));
                sb.AppendLine("ritim     aralik  hiz m/s ort (min-max)  bas (min..max)  govde ort (min..max)  salinim  vucut cizgisi ort (min..max)  basista sicrama uyluk/diz");
                for (int s = 0; s < Segments.Length; s++)
                {
                    Stats st = _stats[s];
                    if (st.Trunk.Count == 0) continue;
                    sb.AppendLine(string.Format(Ci,
                        "{0,-9} {1,5:F2}   {2,4:F1} ({3:F1}-{4:F1})        {5,4:F0}..{6,3:F0}      {7,5:F1} ({8,5:F1}..{9,5:F1})  {10,5:F1}    {11,5:F1} ({12,5:F1}..{13,5:F1})        {14,4:F1} / {15,4:F1}",
                        Segments[s].Name, Segments[s].Gap, Avg(st.Speed), Min(st.Speed), Max(st.Speed), Min(st.Head), Max(st.Head),
                        Avg(st.Trunk), Min(st.Trunk), Max(st.Trunk), Max(st.Trunk) - Min(st.Trunk),
                        Avg(st.Line), Min(st.Line), Max(st.Line), st.ThighJump, st.KneeJump));
                    sb.AppendLine(string.Format(Ci, "          arka bacak - govde cizgisi farki ort {0:F0} (min {1:F0}), on bacak altta %{2:F0}",
                        Avg(st.BackLine), Min(st.BackLine), 100f * st.Under / Mathf.Max(1, st.Count)));
                    sb.AppendLine("          hedef: " + Segments[s].Target);
                }
                string dir = Path.Combine(Application.dataPath, "../Temp/RhythmProbe");
                Directory.CreateDirectory(dir);
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", Ci);
                File.WriteAllText(Path.Combine(dir, "probe_" + stamp + ".txt"), sb.ToString());
                File.WriteAllText(Path.Combine(dir, "probe_" + stamp + ".csv"), _csv.ToString());
                if (_shots.Count > 0)
                {
                    int cols = 10, rows = (_shots.Count + cols - 1) / cols;
                    var atlas = new Texture2D(Cell * cols, Cell * rows, TextureFormat.RGB24, false);
                    var fill = new Color32[Cell * cols * Cell * rows];
                    for (int k = 0; k < fill.Length; k++) fill[k] = new Color32(30, 30, 30, 255);
                    atlas.SetPixels32(fill);
                    for (int k = 0; k < _shots.Count; k++) atlas.SetPixels32((k % cols) * Cell, (rows - 1 - k / cols) * Cell, Cell, Cell, _shots[k]);
                    atlas.Apply();
                    File.WriteAllBytes(Path.Combine(dir, "probe_" + stamp + ".png"), atlas.EncodeToPNG());
                    Object.DestroyImmediate(atlas);
                }
                if (_rt) { Object.DestroyImmediate(_rt); Object.DestroyImmediate(_grab); }
                Debug.Log("[RhythmProbe] " + Path.GetFullPath(Path.Combine(dir, "probe_" + stamp + ".txt")) + "\n" + sb);
                EditorApplication.isPaused = true;
            }

            static float Avg(List<float> v) { float s = 0f; foreach (float x in v) s += x; return v.Count > 0 ? s / v.Count : 0f; }
            static float Min(List<float> v) { float m = float.MaxValue; foreach (float x in v) m = Mathf.Min(m, x); return m; }
            static float Max(List<float> v) { float m = float.MinValue; foreach (float x in v) m = Mathf.Max(m, x); return m; }
        }
    }
}
