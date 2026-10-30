using SpeedStars.Meta.Contracts;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace SpeedStars.Physical
{
    /// <summary>Presentation only. Pools short world-space contact bursts; never changes the simulation.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class PerfectStepFeedback : MonoBehaviour
    {
        public float duration = 0.6f;
        public float groundOffset = 0.055f;
        static readonly Color Cyan = new Color(0.12f, 1f, 0.92f);
        sealed class Burst
        {
            public GameObject root;
            public LineRenderer ring, flash;
            public readonly LineRenderer[] sparks = new LineRenderer[5];
            public readonly LineRenderer[] trails = new LineRenderer[2];
            public TextMesh label;
            public float age;
            public bool showLabel;
            public bool recovery;
        }
        readonly Burst[] _pool = new Burst[3];
        readonly Transform[] _feet = new Transform[2];
        RunnerBody _body;
        Material _material;
        AudioSource _audio;
        AudioClip _tick;
        IPlayerSettings _settings;
        int _next, _pendingLeg = -1;
        bool _pendingLabel;
        bool _pendingRecovery;
        double _lastBad = -10;
        Transform _head;
        double _lastGood = -10;
        float _ground;
        Camera _camera;

        void Awake() => _body = GetComponent<RunnerBody>();
        void OnEnable()
        {
            _body.Tapped += OnTapped;
            _body.RunReset += Clear;
        }
        void Start()
        {
            // A Resources material retains the shader in player builds.
            _material = Resources.Load<Material>("PerfectStep");
            if (!_material) { enabled = false; return; }
            _ground = transform.position.y;
            foreach (var bone in GetComponentsInChildren<Transform>(true))
            {
                if (bone.name == "B-foot.L") _feet[0] = bone;
                if (bone.name == "B-foot.R") _feet[1] = bone;
                if (bone.name == "B-head") _head = bone;
            }
            _camera = Camera.main;
            var context = FindObjectOfType<SceneContext>();
            if (context) _settings = context.Container.TryResolve<IPlayerSettings>();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _tick = AudioClip.Create("Perfect Step Tick", 1764, 1, 44100, false);
            var samples = new float[1764];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / 44100f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * (1500f * t - 6500f * t * t))
                    * Mathf.Exp(-120f * t) * Mathf.Min(1f, i / 30f);
            }
            _tick.SetData(samples, 0);
            for (int i = 0; i < _pool.Length; i++) _pool[i] = CreateBurst();
        }

        LineRenderer Line(Transform parent, string name, int points, float width)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        Burst CreateBurst()
        {
            var b = new Burst { root = new GameObject("Perfect Step Burst") };
            b.root.layer = gameObject.layer;
            // Unparented: contact stays on the track instead of following the runner.
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b.root, gameObject.scene);
            b.ring = Line(b.root.transform, "Contact Ring", 33, 0.04f);
            b.flash = Line(b.root.transform, "Contact Flash", 2, 0.055f);
            b.flash.SetPosition(0, new Vector3(0, 0.008f, -0.09f));
            b.flash.SetPosition(1, new Vector3(0, 0.008f, 0.09f));
            for (int j = 0; j < 5; j++) b.sparks[j] = Line(b.root.transform, "Spark", 2, 0.018f);
            for (int j = 0; j < 2; j++)
            {
                b.trails[j] = Line(b.root.transform, "Push Streak", 2, 0.025f);
                b.trails[j].widthCurve = AnimationCurve.Linear(0, 0, 1, 1);
            }
            var text = new GameObject("Perfect Label");
            text.layer = gameObject.layer;
            text.transform.SetParent(b.root.transform, false);
            b.label = text.AddComponent<TextMesh>();
            b.label.text = "PERFECT";
            b.label.fontSize = 48;
            b.label.characterSize = 0.035f;
            b.label.anchor = TextAnchor.MiddleCenter;
            b.label.fontStyle = FontStyle.Bold;
            b.root.SetActive(false);
            return b;
        }

        void OnTapped(int leg, bool pressed, double at)
        {
            if (!pressed) return;
            var sim = _body.Sim;
            if (sim.LastResult != PressResult.Good || sim.LastEfficiency < 0.7f)
            {
                if (sim.LastResult == PressResult.Early || sim.LastResult == PressResult.Late)
                    _lastBad = at;
                else if (sim.LastResult == PressResult.Start) _lastBad = -10;
                _lastGood = -10;
                return;
            }
            _pendingRecovery = at - _lastBad <= 1.5;
            _lastBad = -10; // The first efficient good step consumes recovery.
            _pendingLabel = _pendingRecovery || at - _lastGood > 0.8;
            _lastGood = at;
            _pendingLeg = leg;
        }

        void LateUpdate()
        {
            if (_pool[0] == null) return;
            if (_pendingLeg >= 0)
            {
                var b = _pool[_next++ % _pool.Length];
                var foot = _feet[_pendingLeg];
                Vector3 p = foot ? foot.position : transform.position;
                p.y = _ground + groundOffset;
                b.root.transform.SetPositionAndRotation(p, Quaternion.Euler(0, transform.eulerAngles.y, 0));
                b.age = 0;
                b.showLabel = _pendingLabel;
                b.recovery = _pendingRecovery;
                b.label.text = b.recovery ? "RECOVER!" : "PERFECT";
                b.label.characterSize = b.recovery ? 0.04f : 0.035f;
                b.flash.gameObject.SetActive(!b.recovery);
                foreach (var spark in b.sparks) spark.gameObject.SetActive(!b.recovery);
                foreach (var trail in b.trails) trail.gameObject.SetActive(!b.recovery);
                b.label.gameObject.SetActive(b.showLabel);
                b.root.SetActive(true);
                if (_settings == null || _settings.SfxOn) _audio.PlayOneShot(_tick, 0.16f);
                _pendingLeg = -1;
            }
            foreach (var b in _pool)
            {
                if (!b.root.activeSelf) continue;
                float lifetime = Mathf.Max(0.05f, duration);
                if (b.age >= lifetime) { b.root.SetActive(false); continue; }
                float t = Mathf.Clamp01(b.age / Mathf.Max(0.05f, duration));
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t));
                for (int j = 0; j < 33; j++)
                {
                    float angle = j * Mathf.PI * 2f / 32;
                    b.ring.SetPosition(j, new Vector3(Mathf.Cos(angle) * 0.29f, 0,
                        Mathf.Sin(angle) * 0.43f) * (0.95f + t * 0.25f));
                }
                Tint(b.ring, Cyan, fade);
                Tint(b.flash, Color.white, Mathf.Max(0, 1f - t * 3));
                for (int j = 0; j < 5; j++)
                {
                    float angle = j * 2.39996f;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), 0.65f + j * 0.12f, Mathf.Sin(angle));
                    Vector3 p = direction * (0.045f + t * 0.22f);
                    b.sparks[j].SetPosition(0, p);
                    b.sparks[j].SetPosition(1, p + direction * 0.025f);
                    Tint(b.sparks[j], j % 2 == 0 ? new Color(1, 0.9f, 0.55f) : Cyan, fade);
                }
                for (int j = 0; j < 2; j++)
                {
                    b.trails[j].SetPosition(0, new Vector3((j == 0 ? -1 : 1) * 0.08f, 0.025f, -0.4f - t * 0.18f));
                    b.trails[j].SetPosition(1, new Vector3(0, 0.025f, -0.02f));
                    Tint(b.trails[j], Cyan, fade);
                }
                if (b.showLabel)
                {
                    float labelT = b.age / lifetime;
                    {
                        Vector3 right = _camera ? _camera.transform.right : transform.forward;
                        float side = Vector3.Dot(transform.forward, right) < 0 ? -1 : 1;
                        Vector3 chest = _head ? _head.position - Vector3.up * 0.28f
                            : transform.position + Vector3.up * 1.3f;
                        b.label.transform.position = chest - right * (side * (b.recovery ? 0.9f : 0.75f))
                            + Vector3.up * (0.28f + labelT * 0.14f);
                    }
                    if (_camera) b.label.transform.rotation = _camera.transform.rotation;
                    b.label.color = b.recovery ? new Color(0.2f, 1, 0.8f, fade)
                        : new Color(0.65f, 1, 0.96f, fade);
                }
                b.age += Time.deltaTime;
            }
        }

        static void Tint(LineRenderer line, Color color, float alpha)
        {
            color.a = alpha;
            line.startColor = line.endColor = color;
        }
        void Clear()
        {
            _pendingLeg = -1;
            _lastGood = -10;
            _lastBad = -10;
            _pendingRecovery = false;
            foreach (var b in _pool) if (b != null && b.root) b.root.SetActive(false);
            if (_audio) _audio.Stop();
        }
        void OnDisable()
        {
            _body.Tapped -= OnTapped;
            _body.RunReset -= Clear;
            Clear();
        }
        void OnDestroy()
        {
            foreach (var b in _pool) if (b != null && b.root) Destroy(b.root);
            if (_tick) Destroy(_tick);
            if (_audio) Destroy(_audio);
        }
    }
}
