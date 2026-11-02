using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Physical
{
    /// <summary>Soft back-mounted airflow ribbons driven by actual running speed.</summary>
    [DefaultExecutionOrder(1250)]
    public sealed class MomentumWindFeedback : MonoBehaviour
    {
        const int Points = 20;
        readonly LineRenderer[] _ribbons = new LineRenderer[3];
        RunnerBody _body;
        Transform _chest;
        GameObject _root;
        float _strength;
        Vector3 _previous;
        bool _hasPrevious;

        void Awake() => _body = GetComponent<RunnerBody>();
        void OnEnable() => _body.RunReset += Clear;
        void Start()
        {
            var material = Resources.Load<Material>("MomentumWind");
            if (!material) { enabled = false; return; }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
                if (bone.name == "B-chest") { _chest = bone; break; }
            _root = new GameObject("Momentum Wind");
            _root.layer = gameObject.layer;
            _root.transform.SetParent(transform, false);
            for (int i = 0; i < _ribbons.Length; i++)
            {
                var go = new GameObject(i == 0 ? "Momentum Wind Ribbon" : "Momentum Wind Wisp");
                go.layer = gameObject.layer;
                go.transform.SetParent(_root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = Points;
                line.widthCurve = new AnimationCurve(new Keyframe(0, 0.35f),
                    new Keyframe(0.22f, 1f), new Keyframe(1, 0f));
                line.numCapVertices = 2;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(i == 0 ? new Color(0.55f, 0.85f, 1) : Color.white, 0),
                    new GradientColorKey(new Color(0.2f, 0.65f, 0.95f), 1) },
                    new[] { new GradientAlphaKey(0.85f, 0),
                        new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0, 1) });
                line.colorGradient = gradient;
                _ribbons[i] = line;
            }
            _root.SetActive(false);
        }
        void LateUpdate()
        {
            if (!_root || _body.Sim == null) return;
            Vector3 position = transform.position;
            float distance = _hasPrevious ? Vector3.Distance(position, _previous) : 0;
            bool teleport = distance > Mathf.Max(2f, 30f * Time.deltaTime);
            bool moving = _hasPrevious && Time.deltaTime > 0 && distance > 0.1f * Time.deltaTime;
            _previous = position;
            _hasPrevious = true;
            if (teleport) { Hide(); return; }
            float target = moving && _body.Sim.Speed > 0.8f
                ? Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(0.8f, Mathf.Max(1f, _body.tuning.maxSpeed * 0.65f), _body.Sim.Speed)) : 0;
            _strength = Mathf.Lerp(_strength, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            if (_strength < 0.015f) { _root.SetActive(false); return; }
            _root.SetActive(true);
            Vector3 forward = transform.forward;
            Vector3 anchor = (_chest ? _chest.position : position + Vector3.up * 1.35f)
                - forward * 0.23f + Vector3.up * 0.08f;
            for (int ribbon = 0; ribbon < _ribbons.Length; ribbon++)
            {
                var line = _ribbons[ribbon];
                float length = Mathf.Lerp(0.5f, 1.6f, _strength) * (ribbon == 0 ? 1 : 0.8f);
                line.widthMultiplier = (ribbon == 0 ? 0.22f : 0.04f) * _strength;
                for (int j = 0; j < Points; j++)
                {
                    float t = j / (float)(Points - 1);
                    float wave = Mathf.Sin(Time.time * 7f - t * 5f + ribbon * 1.7f) * 0.035f * t;
                    float spread = ribbon == 0 ? 0 : (ribbon == 1 ? 1 : -1) * 0.045f * t;
                    line.SetPosition(j, anchor - forward * (t * length)
                        + Vector3.up * (wave + spread));
                }
            }
        }
        void Hide()
        {
            _strength = 0;
            if (_root) _root.SetActive(false);
        }
        void Clear() { _hasPrevious = false; Hide(); }
        void OnDisable() { _body.RunReset -= Clear; Clear(); }
        void OnDestroy() { if (_root) Destroy(_root); }
    }
}
