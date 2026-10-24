using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// IMGUI tuning panel: race clock, speed, energy, last press, and live graphs of the physical
    /// state (speed, lean, leg extension and hold, hop) over the last seconds. Press markers are
    /// coloured by result. Buttons replay the reference sequences. H hides it.
    /// </summary>
    public sealed class RunnerBodyHUD : MonoBehaviour
    {
        public RunnerBody body;
        public RaceController race;
        public RunnerReplay replay;
        [Tooltip("Seconds of history in the graphs.")]
        public float window = 6f;
        public bool visible = true;

        const int Samples = 240;
        const float MphPerMs = 2.23694f;

        static readonly Color LeftColor = new Color(0.3f, 0.6f, 1f);
        static readonly Color RightColor = new Color(1f, 0.6f, 0.2f);

        readonly float[] _speed = new float[Samples], _lean = new float[Samples], _hop = new float[Samples];
        readonly float[] _extL = new float[Samples], _extR = new float[Samples];
        readonly float[] _holdL = new float[Samples], _holdR = new float[Samples];
        readonly int[] _press = new int[Samples];   // 0 none, else (int)PressResult
        readonly int[] _pressLeg = new int[Samples];
        int _head, _lastPresses;
        float _sampleClock;
        Texture2D _pixel;
        GUIStyle _label;

        void Awake()
        {
            if (!body) body = GetComponent<RunnerBody>();
            if (!race) race = FindObjectOfType<RaceController>();
            if (!replay) replay = FindObjectOfType<RunnerReplay>();
        }

        void Update()
        {
            if (DebugKeys.Down(KeyCode.H)) visible = !visible;
            if (!body || body.Sim == null) return;
            var sim = body.Sim;

            int newPress = 0;
            if (sim.Presses != _lastPresses)
            {
                if (sim.Presses > _lastPresses) newPress = (int)sim.LastResult;
                _lastPresses = sim.Presses;
            }

            _sampleClock += Time.deltaTime;
            float step = window / Samples;
            if (_sampleClock < step && newPress == 0) return;
            _sampleClock = 0f;
            _head = (_head + 1) % Samples;
            _speed[_head] = sim.Speed / sim.T.maxSpeed;
            _lean[_head] = Mathf.InverseLerp(-sim.T.maxBackLean, sim.T.maxForwardLean, sim.Lean);
            _hop[_head] = Mathf.Clamp01(sim.HopY / 0.1f);
            _extL[_head] = sim.Legs[0].ExtValue;
            _extR[_head] = sim.Legs[1].ExtValue;
            _holdL[_head] = sim.Legs[0].HoldValue;
            _holdR[_head] = sim.Legs[1].HoldValue;
            _press[_head] = newPress;
            _pressLeg[_head] = sim.LastLeg;
        }

        void OnGUI()
        {
            if (!visible || !body || body.Sim == null) return;
            if (!_pixel)
            {
                _pixel = new Texture2D(1, 1);
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply();
            }
            if (_label == null) _label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };

            var sim = body.Sim;
            float x = 10f, y = 10f, w = 440f;
            Box(new Rect(x - 5f, y - 5f, w + 10f, 520f), new Color(0f, 0f, 0f, 0.6f));

            string state = race ? race.Current.ToString() : "-";
            string last = race && race.LastRaceTime > 0f ? $"   son: <b>{race.LastRaceTime:F2} s</b>" : "";
            Line(ref y, x, $"<b>{state}</b>   süre <b>{(race ? race.RaceTime : 0f):F2} s</b>   mesafe {sim.Distance:F1} / {(race ? race.raceDistance : 0f):F0} m{last}");
            Line(ref y, x, $"hız <b>{sim.Speed:F2} m/s</b>  ({sim.Speed * MphPerMs:F1} mph)   enerji {sim.Energy:F0}");
            Line(ref y, x, $"eğim {sim.Lean:F0}°   hop {sim.HopY * 100f:F1} cm   {(sim.Airborne ? "havada" : "yerde")}");
            Line(ref y, x, $"son basış: <b>{ResultText(sim.LastResult)}</b>  verim {sim.LastEfficiency:F2}  ({LegName(sim.LastLeg)})");
            Line(ref y, x, $"sol  ext {sim.Legs[0].ExtValue:F2} hold {sim.Legs[0].HoldValue:F2} {(sim.Legs[0].Pressed ? "■" : "□")}" +
                           $"    sağ  ext {sim.Legs[1].ExtValue:F2} hold {sim.Legs[1].HoldValue:F2} {(sim.Legs[1].Pressed ? "■" : "□")}");
            y += 6f;

            Graph(ref y, x, w, 70f, "hız", Color.green, _speed, null, true);
            Graph(ref y, x, w, 70f, "eğim (0° çizgisi)", Color.yellow, _lean, null, false,
                Mathf.InverseLerp(-sim.T.maxBackLean, sim.T.maxForwardLean, 0f));
            Graph(ref y, x, w, 50f, "ext (mavi sol / turuncu sağ)", LeftColor, _extL, _extR, false);
            Graph(ref y, x, w, 50f, "hold", LeftColor, _holdL, _holdR, false);
            Graph(ref y, x, w, 40f, "hop", Color.cyan, _hop, null, false);

            y += 4f;
            if (replay)
            {
                string playing = replay.Playing != null ? $"oynatılıyor: {replay.Playing.Name} ({replay.Elapsed:F1} s) — hedef: {replay.Playing.Expect}" : "tekrar oynat (1–4, Esc durdur):";
                GUI.Label(new Rect(x, y, w, 40f), playing, _label);
                y += 22f;
                float bw = (w - 15f) / 4f;
                for (int i = 0; i < ReferenceSequences.All.Length; i++)
                    if (GUI.Button(new Rect(x + i * (bw + 5f), y, bw, 22f), $"{i + 1}. {ReferenceSequences.All[i].Name}"))
                        replay.Play(i);
                y += 26f;
            }
            GUI.Label(new Rect(x, y, w, 20f), "←/→ veya ekranın sol/sağ yarısı · bitişte başa döner · H gizle", _label);
        }

        void Line(ref float y, float x, string text)
        {
            GUI.Label(new Rect(x, y, 460f, 22f), text, _label);
            y += 20f;
        }

        void Graph(ref float y, float x, float w, float h, string title, Color c, float[] a, float[] b, bool markers, float zero = -1f)
        {
            GUI.Label(new Rect(x, y, w, 18f), title, _label);
            y += 18f;
            var r = new Rect(x, y, w, h);
            Box(r, new Color(1f, 1f, 1f, 0.08f));
            if (zero >= 0f) Box(new Rect(x, y + h * (1f - zero), w, 1f), new Color(1f, 1f, 1f, 0.35f));

            for (int i = 0; i < Samples; i++)
            {
                int idx = (_head + 1 + i) % Samples;
                float px = x + w * i / (Samples - 1);
                Box(new Rect(px, y + h * (1f - Mathf.Clamp01(a[idx])) - 1f, 2f, 2f), c);
                if (b != null) Box(new Rect(px, y + h * (1f - Mathf.Clamp01(b[idx])) - 1f, 2f, 2f), RightColor);
                if (markers && _press[idx] != 0)
                    Box(new Rect(px, y, 2f, h), ResultColor((PressResult)_press[idx], _pressLeg[idx]));
            }
            y += h + 4f;
        }

        void Box(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _pixel);
            GUI.color = old;
        }

        static Color ResultColor(PressResult r, int leg)
        {
            switch (r)
            {
                case PressResult.Good: return new Color(0.2f, 1f, 0.3f, 0.6f);
                case PressResult.Late: return new Color(1f, 0.9f, 0.2f, 0.6f);
                case PressResult.Early: return new Color(1f, 0.2f, 0.2f, 0.7f);
                default: return leg == 0 ? new Color(0.3f, 0.6f, 1f, 0.6f) : new Color(1f, 0.6f, 0.2f, 0.6f);
            }
        }

        static string ResultText(PressResult r)
        {
            switch (r)
            {
                case PressResult.Start: return "çıkış";
                case PressResult.Good: return "<color=#4f4>tam zamanında</color>";
                case PressResult.Late: return "<color=#ff4>geç</color>";
                case PressResult.Early: return "<color=#f44>erken (havada)</color>";
                default: return "-";
            }
        }

        static string LegName(int leg) => leg == 0 ? "sol" : leg == 1 ? "sağ" : "-";
    }
}
