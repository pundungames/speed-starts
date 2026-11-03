using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// A hurdle race on the runner's own lane: builds the hurdles and judges each one by what is seen. Each leg
    /// (thigh, shin, foot, toe) is read off the drawn body as it goes over the hurdle's line: any of it going through
    /// below the top of the bar knocks the hurdle over (yellow flash, the sim loses speed); every part over it
    /// clears it (green flash). So a jump timed right but too low still hits, and no jump hits too. The pose is
    /// only read, never changed. Without leg bones it falls back to the sim: in the air at the line = cleared.
    /// The test scene uses the Inspector's layout; the game sets the layout of the current event through
    /// <see cref="Configure"/>.
    /// </summary>
    public sealed class RunnerHurdles : MonoBehaviour
    {
        [Tooltip("The runner racing the hurdles.")]
        public RunnerBody body;
        [Tooltip("Its race length is set to raceDistance.")]
        public RaceController race;
        [Tooltip("Race length with hurdles (m). 0 = leave the race length alone (the game takes it from the event).")]
        public float raceDistance = 110f;
        [Tooltip("The hurdle object, facing along the lane with its pivot on the ground. Empty = simple built-in hurdles.")]
        public GameObject hurdlePrefab;
        [Tooltip("Number of hurdles.")]
        public int count = 10;
        [Tooltip("Distance of the first hurdle from the start (m).")]
        public float first = 13.72f;
        [Tooltip("Distance between hurdles (m).")]
        public float spacing = 9.14f;
        [Tooltip("Height of the bar (m).")]
        public float height = 0.8f;
        [Tooltip("Width of the hurdle across the lane (m).")]
        public float width = 1f;
        [Tooltip("Colour of the bar (the runner's own lane).")]
        public Color barColor = new Color(0.55f, 0.35f, 0.85f);
        [Tooltip("Colour of the frame.")]
        public Color frameColor = Color.white;
        [Tooltip("How far a leg may dip under the top of the bar and still clear it (m). Higher = easier.")]
        public float forgiveness = 0.15f;
        [Tooltip("Write each hurdle's lowest leg height against the bar to the console (for tuning forgiveness).")]
        public bool logClearance;
        [Tooltip("How long a knocked hurdle takes to lie flat (s).")]
        public float fallTime = 0.2f;
        [Tooltip("Flash the screen on each hurdle. Only for the player's lane.")]
        public bool screenFlash = true;
        [Tooltip("Screen flash for a cleared hurdle.")]
        public Color clearFlash = new Color(0.2f, 0.9f, 0.2f, 0.4f);
        [Tooltip("Screen flash for a knocked hurdle.")]
        public Color hitFlash = new Color(1f, 0.75f, 0.1f, 0.45f);
        [Tooltip("How long the screen flash fades out (s).")]
        public float flashTime = 0.3f;

        Transform[] _hurdles;
        float[] _fallStart;
        float[] _barTop;                  // world height of each hurdle's top, over the middle of the lane
        Transform[] _legChain;            // per leg: thigh, shin, foot, toe (empty without bones)
        float[] _lastAlong;               // each point's distance past the next hurdle's line last frame (NaN = none yet)
        float[] _lastY;
        float _lowest;                    // the lowest any leg went through the next hurdle's line so far (world height)
        int _next;
        Color _flash;
        float _flashStart = -10f;

        /// <summary>Hurdles cleared and knocked this run.</summary>
        public int Cleared { get; private set; }
        public int Knocked { get; private set; }

        /// <summary>Distance of the hurdle still ahead; false when none is left. Its index tells hurdles apart.</summary>
        public bool TryGetNext(out float distance, out int index)
        {
            index = _next;
            distance = first + spacing * _next;
            return _hurdles != null && _next < count;
        }

        void Awake()
        {
            if (!body) body = FindObjectOfType<RunnerBody>();
            if (!race) race = FindObjectOfType<RaceController>();
            if (race && raceDistance > 0f) race.raceDistance = raceDistance;
        }

        void OnEnable()
        {
            if (!body) return;
            body.RunReset += OnRunReset;
        }

        void OnDisable()
        {
            if (!body) return;
            body.RunReset -= OnRunReset;
        }

        void Start()
        {
            FindLegs();
            if (_hurdles == null) Rebuild();
        }

        void FindLegs()
        {
            if (!body) return;
            var chain = new Transform[8];
            int i = 0;
            foreach (string side in new[] { "L", "R" })
            {
                Transform shin = FindDeep(body.transform, "B-shin." + side);
                chain[i++] = shin ? shin.parent : null;
                chain[i++] = shin;
                chain[i++] = FindDeep(body.transform, "B-foot." + side);
                chain[i++] = FindDeep(body.transform, "B-toe." + side);
            }
            foreach (Transform bone in chain)
                if (!bone) { _legChain = new Transform[0]; return; }
            _legChain = chain;
            _lastAlong = new float[chain.Length];
            _lastY = new float[chain.Length];
            ForgetLegs();
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        void ForgetLegs()
        {
            _lowest = float.PositiveInfinity;
            if (_lastAlong == null) return;
            for (int i = 0; i < _lastAlong.Length; i++) _lastAlong[i] = float.NaN;
        }

        /// <summary>A new layout, standing and uncounted. A count of 0 leaves the lane empty.</summary>
        public void Configure(int hurdleCount, float firstMeters, float spacingMeters)
        {
            count = hurdleCount;
            first = firstMeters;
            spacing = spacingMeters;
            Rebuild();
        }

        void Rebuild()
        {
            if (!body || body.Sim == null) return;
            if (_hurdles != null)
            {
                foreach (Transform hurdle in _hurdles) Destroy(hurdle.gameObject);
            }
            _next = 0;
            Cleared = Knocked = 0;
            ForgetLegs();
            Vector3 start = body.transform.position - body.transform.forward * body.Sim.Distance;
            _hurdles = new Transform[count];
            _fallStart = new float[count];
            _barTop = new float[count];
            for (int i = 0; i < count; i++)
            {
                _hurdles[i] = Build(start + body.transform.forward * (first + spacing * i));
                _fallStart[i] = -1f;
                _barTop[i] = BarTop(_hurdles[i]);
            }
        }

        /// <summary>The highest part of the hurdle over the middle of the lane (the side posts do not count).</summary>
        float BarTop(Transform hurdle)
        {
            Vector3 side = hurdle.right;
            float top = hurdle.position.y + height;
            bool found = false;
            foreach (Renderer r in hurdle.GetComponentsInChildren<Renderer>())
            {
                Bounds b = r.bounds;
                float a = Vector3.Dot(b.min - hurdle.position, side), c = Vector3.Dot(b.max - hurdle.position, side);
                if (Mathf.Min(a, c) > 0f || Mathf.Max(a, c) < 0f) continue;
                top = found ? Mathf.Max(top, b.max.y) : b.max.y;
                found = true;
            }
            return top;
        }

        Transform Build(Vector3 position)
        {
            if (hurdlePrefab) return Instantiate(hurdlePrefab, position, body.transform.rotation, transform).transform;

            var root = new GameObject("Hurdle").transform;
            root.SetParent(transform, false);
            root.SetPositionAndRotation(position, body.transform.rotation);
            float half = width * 0.5f;
            Part(root, new Vector3(0f, height - 0.03f, 0f), new Vector3(width, 0.1f, 0.03f), barColor);
            foreach (float x in new[] { -half, half })
            {
                Part(root, new Vector3(x, height * 0.5f, 0f), new Vector3(0.03f, height, 0.03f), frameColor);
                Part(root, new Vector3(x, 0.015f, -0.15f), new Vector3(0.03f, 0.03f, 0.4f), frameColor);
            }
            return root;
        }

        static void Part(Transform root, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material.color = color;
        }

        // After the rig has posed the body this frame: the legs are where they are drawn.
        void LateUpdate()
        {
            if (_hurdles == null) return;
            if (_legChain == null) FindLegs();
            var sim = body.Sim;
            if (_legChain.Length == 0)
            {
                while (_next < count && sim.Distance >= first + spacing * _next)
                {
                    if (sim.JumpInAir) Clear();
                    else Knock();
                }
            }
            else if (_next < count)
            {
                JudgeLegs();
            }

            for (int i = 0; i < count; i++)
            {
                if (_fallStart[i] < 0f) continue;
                float s = Mathf.Clamp01((Time.time - _fallStart[i]) / Mathf.Max(0.01f, fallTime));
                _hurdles[i].rotation = body.transform.rotation * Quaternion.Euler(90f * s * s, 0f, 0f);
            }
        }

        /// <summary>
        /// Each leg point's path since last frame, and each bone between two points now, is checked where it goes
        /// through the next hurdle's line: lower than forgiveness under the bar's top is a touch. Cleared once every
        /// point is past the line without one.
        /// </summary>
        void JudgeLegs()
        {
            Transform hurdle = _hurdles[_next];
            Vector3 forward = body.transform.forward;
            float top = _barTop[_next] - forgiveness;
            bool allPast = true;
            int n = _legChain.Length;
            var along = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 p = _legChain[i].position;
                along[i] = Vector3.Dot(p - hurdle.position, forward);
                if (along[i] < 0f) allPast = false;
                float last = _lastAlong[i];
                if (!float.IsNaN(last) && last < 0f && along[i] >= 0f) _lowest = Mathf.Min(_lowest, Crossing(last, _lastY[i], along[i], p.y));
                _lastAlong[i] = along[i];
                _lastY[i] = p.y;
            }
            for (int i = 0; i < n; i++)
            {
                if (i % 4 == 3) continue;   // the toe ends its leg's chain
                float a = along[i], b = along[i + 1];
                if ((a < 0f) == (b < 0f)) continue;
                _lowest = Mathf.Min(_lowest, Crossing(a, _legChain[i].position.y, b, _legChain[i + 1].position.y));
            }

            bool touched = _lowest < top;
            if (touched || allPast) LogClearance(touched);
            if (touched) Knock();
            else if (allPast) Clear();
            else if (body.Sim.Distance > first + spacing * _next + 3f) Clear();   // the legs never reached the line (teleport, reset)
        }

        void LogClearance(bool touched)
        {
            if (!logClearance) return;
            Debug.Log($"[Hurdles] {name} #{_next + 1}: lowest leg {(_lowest - _barTop[_next]) * 100f:+0;-0} cm against the bar top " +
                      $"(forgiveness {forgiveness * 100f:0} cm) -> {(touched ? "knocked" : "cleared")}");
        }

        /// <summary>Height where a line from (along a, height ya) to (along b, height yb) meets along 0.</summary>
        static float Crossing(float a, float ya, float b, float yb) =>
            Mathf.Approximately(a, b) ? Mathf.Min(ya, yb) : Mathf.Lerp(ya, yb, -a / (b - a));

        void Clear()
        {
            Cleared++;
            Flash(clearFlash);
            _next++;
            ForgetLegs();
        }

        void Knock()
        {
            Knocked++;
            body.Sim.HitHurdle();
            _fallStart[_next] = Time.time;
            Flash(hitFlash);
            _next++;
            ForgetLegs();
        }

        void Flash(Color color)
        {
            if (!screenFlash) return;
            _flash = color;
            _flashStart = Time.time;
        }

        void OnRunReset()
        {
            _next = 0;
            Cleared = Knocked = 0;
            _flashStart = -10f;
            ForgetLegs();
            if (_hurdles == null) return;
            for (int i = 0; i < count; i++)
            {
                _fallStart[i] = -1f;
                _hurdles[i].rotation = body.transform.rotation;
            }
        }

        void OnGUI()
        {
            float s = (Time.time - _flashStart) / Mathf.Max(0.01f, flashTime);
            if (s >= 1f) return;
            Color c = _flash;
            c.a *= 1f - s * s;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
