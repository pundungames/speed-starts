using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Prototype measurement overlay (IMGUI). Shows speed/distance and the timing graph:
    /// the efficiency curve with the last taps plotted on it (blue = left, orange = right).
    /// </summary>
    public sealed class RunnerDebugHUD : MonoBehaviour
    {
        public RunnerStepController controller;
        public RunnerMovement movement;
        public RunnerGaitDriver visual;
        public KeyCode resetKey = KeyCode.R;

        static readonly Color LeftColor = new Color(0.35f, 0.65f, 1f);
        static readonly Color RightColor = new Color(1f, 0.6f, 0.2f);

        float _fps;
        GUIStyle _box, _big, _small;
        Texture2D _pixel;

        void Awake()
        {
            if (!visual) visual = GetComponent<RunnerGaitDriver>();
        }

        void Update()
        {
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame) controller.ResetRun();
#else
            if (Input.GetKeyDown(resetKey)) controller.ResetRun();
#endif
        }

        void OnGUI()
        {
            if (!controller || !movement) return;
            _box ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15, richText = true };
            _big ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold };
            _small ??= new GUIStyle(GUI.skin.label) { fontSize = 11 };
            if (!_pixel) { _pixel = new Texture2D(1, 1); _pixel.SetPixel(0, 0, Color.white); _pixel.Apply(); }

            var last = controller.Last;
            string side = controller.HistoryCount == 0 ? "-" : last.Leg == 0 ? "SOL" : "SAĞ";
            string text =
                $"Hız: <b>{movement.Speed:F2}</b> m/s\n" +
                $"Mesafe: {movement.Distance:F1} m   Süre: {controller.RaceTime:F2} s\n" +
                $"Sol: {controller.Legs[0].State}   Sağ: {controller.Legs[1].State}\n" +
                $"Son: {side} {last.Result}  verim={last.Efficiency:F2}  hata={last.Error * 1000f:F0} ms\n" +
                $"Dokunuş: {controller.Taps}   Sıklık: {controller.TapRate:F1}/s   Eğim: {(visual ? visual.Lean : 0f):+0;-0;0}°   FPS: {_fps:F0}\n" +
                "<size=12>← sol ayak, → sağ ayak, dokunmatik: sol/sağ yarı, R: sıfırla</size>";
            GUI.Box(new Rect(10, 10, 470, 135), text, _box);

            DrawTimingGraph(new Rect(10, 152, 470, 130));

            if (!controller.Started)
                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 50), "Koşmak için sol/sağ dokun", _big);
        }

        void DrawTimingGraph(Rect r)
        {
            GUI.Box(r, GUIContent.none);
            var timing = controller.timing;
            float x0 = timing.EarliestError, x1 = timing.LatestError;
            if (x1 <= x0) return;
            Rect plot = new Rect(r.x + 8, r.y + 18, r.width - 16, r.height - 34);

            GUI.Label(new Rect(r.x + 6, r.y, r.width, 18), "Zamanlama grafiği  (X: hazır olma anına göre ms, Y: verim)", _small);
            Fill(new Rect(plot.x, plot.yMax, plot.width, 1), new Color(1, 1, 1, 0.35f));
            float zeroX = Mathf.Lerp(plot.x, plot.xMax, Mathf.InverseLerp(x0, x1, 0f));
            Fill(new Rect(zeroX, plot.y, 1, plot.height), new Color(1, 1, 1, 0.35f));
            GUI.Label(new Rect(plot.x - 4, plot.yMax, 60, 16), $"{x0 * 1000f:F0}", _small);
            GUI.Label(new Rect(zeroX - 4, plot.yMax, 40, 16), "0", _small);
            GUI.Label(new Rect(plot.xMax - 30, plot.yMax, 60, 16), $"{x1 * 1000f:F0}", _small);

            const int samples = 120;
            for (int i = 0; i <= samples; i++)
            {
                float e = Mathf.Lerp(x0, x1, i / (float)samples);
                float y = Mathf.Lerp(plot.yMax, plot.y, timing.Evaluate(e));
                Fill(new Rect(Mathf.Lerp(plot.x, plot.xMax, i / (float)samples) - 1, y - 1, 2, 2), new Color(0.6f, 1f, 0.6f));
            }

            for (int i = controller.HistoryCount - 1; i >= 0; i--)
            {
                var rec = controller.History[i];
                if (rec.Result == RunnerStepController.TapResult.Start) continue;
                float px = Mathf.Lerp(plot.x, plot.xMax, Mathf.InverseLerp(x0, x1, Mathf.Clamp(rec.Error, x0, x1)));
                float py = Mathf.Lerp(plot.yMax, plot.y, rec.Efficiency);
                Color c = rec.Leg == 0 ? LeftColor : RightColor;
                c.a = i == 0 ? 1f : Mathf.Lerp(0.9f, 0.3f, i / (float)RunnerStepController.HistorySize);
                float size = i == 0 ? 10 : 7;
                Fill(new Rect(px - size / 2, py - size / 2, size, size), c);
            }
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _pixel);
            GUI.color = old;
        }
    }
}
