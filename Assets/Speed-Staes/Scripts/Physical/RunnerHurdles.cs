using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// A hurdle race on the runner's own lane: builds the hurdles and judges each one as the runner's distance
    /// passes it, from one fact only: is the runner in the air (a jump) there. In the air it clears the hurdle
    /// (green flash); on the ground it knocks it over (yellow flash, the sim loses speed). A jump too early or too
    /// late is simply on the ground at the hurdle. The test scene uses the Inspector's layout; the game sets the
    /// layout of the current event through <see cref="Configure"/>.
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
            if (_hurdles == null) Rebuild();
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
            Vector3 start = body.transform.position - body.transform.forward * body.Sim.Distance;
            _hurdles = new Transform[count];
            _fallStart = new float[count];
            for (int i = 0; i < count; i++)
            {
                _hurdles[i] = Build(start + body.transform.forward * (first + spacing * i));
                _fallStart[i] = -1f;
            }
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

        void Update()
        {
            if (_hurdles == null) return;
            var sim = body.Sim;
            while (_next < count && sim.Distance >= first + spacing * _next)
            {
                if (sim.JumpInAir)
                {
                    Cleared++;
                    Flash(clearFlash);
                }
                else
                {
                    Knocked++;
                    sim.HitHurdle();
                    _fallStart[_next] = Time.time;
                    Flash(hitFlash);
                }
                _next++;
            }

            for (int i = 0; i < count; i++)
            {
                if (_fallStart[i] < 0f) continue;
                float s = Mathf.Clamp01((Time.time - _fallStart[i]) / Mathf.Max(0.01f, fallTime));
                _hurdles[i].rotation = body.transform.rotation * Quaternion.Euler(90f * s * s, 0f, 0f);
            }
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
