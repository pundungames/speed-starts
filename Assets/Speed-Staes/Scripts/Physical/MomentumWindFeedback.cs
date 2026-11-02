using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Physical
{
    /// <summary>Short separated air strokes reward rhythm, never speed alone.</summary>
    [DefaultExecutionOrder(1250)]
    public sealed class MomentumWindFeedback : MonoBehaviour
    {
        const float Duration = 0.32f, Cooldown = 1.8f;
        readonly LineRenderer[] _strokes = new LineRenderer[3];
        RunnerBody _body;
        Transform _chest;
        GameObject _root;
        int _goodSteps, _lastLeg = -1;
        double _lastGood = -10, _lastShown = -10;
        float _age = Duration;
        bool _pending, _hasPrevious;
        Vector3 _previous;
        void Awake() => _body = GetComponent<RunnerBody>();
        void OnEnable() { _body.Tapped += OnTapped; _body.RunReset += Clear; }
        void Start()
        {
            var material = Resources.Load<Material>("MomentumWind");
            if (!material) { enabled = false; return; }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
                if (bone.name == "B-chest") { _chest = bone; break; }
            _root = new GameObject("Momentum Air Strokes");
            _root.layer = gameObject.layer;
            _root.transform.SetParent(transform, false);
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Momentum Air Stroke");
                go.layer = gameObject.layer;
                go.transform.SetParent(_root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = 3;
                line.widthMultiplier = 0.025f;
                line.widthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
                line.numCapVertices = 2;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                _strokes[i] = line;
            }
            _root.SetActive(false);
        }
        void OnTapped(int leg, bool pressed, double at)
        {
            if (!pressed) return;
            var sim = _body.Sim;
            if (sim.LastResult != PressResult.Good || sim.LastEfficiency < 0.8f)
            {
                _goodSteps = 0; _lastLeg = -1; _pending = false; Hide(); return;
            }
            _goodSteps = at - _lastGood <= 0.8 && leg != _lastLeg ? _goodSteps + 1 : 1;
            _lastGood = at; _lastLeg = leg;
            if (_goodSteps < 3 || sim.Speed < 2.5f || at - _lastShown < Cooldown) return;
            _pending = true; _lastShown = at; _goodSteps = 0;
        }
        void LateUpdate()
        {
            if (!_root) return;
            Vector3 position = transform.position;
            bool teleport = _hasPrevious && Vector3.Distance(position, _previous) > Mathf.Max(2, 30 * Time.deltaTime);
            _previous = position; _hasPrevious = true;
            if (teleport || _body.Sim.Speed < 0.8f) { _pending = false; Hide(); return; }
            if (_pending) { _pending = false; _age = 0; _root.SetActive(true); }
            if (!_root.activeSelf) return;
            if (_age >= Duration) { Hide(); return; }
            float t = _age / Duration;
            Vector3 anchor = _chest ? _chest.position : position + Vector3.up * 1.35f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 start = anchor - transform.forward * (0.45f + i * 0.13f + t * 0.25f)
                    + Vector3.up * ((i - 1) * 0.1f);
                float length = i == 1 ? 0.28f : 0.18f;
                for (int j = 0; j < 3; j++) _strokes[i].SetPosition(j, start - transform.forward * (j * 0.5f * length));
                var color = new Color(0.75f, 0.88f, 0.96f, 0.65f * (1f - t));
                _strokes[i].startColor = _strokes[i].endColor = color;
            }
            _age += Time.deltaTime;
        }
        void Hide() { _age = Duration; if (_root) _root.SetActive(false); }
        void Clear()
        {
            _goodSteps = 0; _lastLeg = -1; _lastGood = _lastShown = -10;
            _pending = false; _hasPrevious = false; Hide();
        }
        void OnDisable() { _body.Tapped -= OnTapped; _body.RunReset -= Clear; Clear(); }
        void OnDestroy() { if (_root) Destroy(_root); }
    }
}
