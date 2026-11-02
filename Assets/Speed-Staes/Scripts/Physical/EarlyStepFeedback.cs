using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Physical
{
    /// <summary>Two quiet head-side marks for an early press. No change to pose or physics.</summary>
    [DefaultExecutionOrder(1150)]
    public sealed class EarlyStepFeedback : MonoBehaviour
    {
        const float Duration = 0.2f;
        const float RepeatDelay = 0.3f;
        readonly LineRenderer[] _marks = new LineRenderer[2];
        RunnerBody _body;
        Transform _head;
        GameObject _root;
        Camera _camera;
        float _age = Duration;
        double _lastShown = -10;
        bool _pending;

        void Awake() => _body = GetComponent<RunnerBody>();
        void OnEnable()
        {
            _body.Tapped += OnTapped;
            _body.RunReset += Clear;
        }
        void Start()
        {
            var material = Resources.Load<Material>("EarlyStep");
            if (!material) { enabled = false; return; }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
                if (bone.name == "B-head") { _head = bone; break; }
            _camera = Camera.main;
            _root = new GameObject("Early Step Wobble");
            _root.layer = gameObject.layer;
            _root.transform.SetParent(transform, false);
            for (int side = 0; side < 2; side++)
            {
                var go = new GameObject("Early Step Arc");
                go.layer = gameObject.layer;
                go.transform.SetParent(_root.transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = true;
                line.positionCount = 9;
                line.widthMultiplier = 0.012f;
                line.numCapVertices = 3;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                _marks[side] = line;
            }
            _root.SetActive(false);
        }
        void OnTapped(int leg, bool pressed, double at)
        {
            if (!pressed || _body.Sim.LastResult != PressResult.Early) return;
            // Mashing cannot stack the marks or keep them permanently visible.
            if (at - _lastShown < RepeatDelay) return;
            _lastShown = at;
            _pending = true;
        }
        void LateUpdate()
        {
            if (!_root) return;
            if (_pending)
            {
                _pending = false;
                _age = 0;
                _root.SetActive(true);
            }
            if (!_root.activeSelf) return;
            if (_age >= Duration) { _root.SetActive(false); return; }
            if (!_camera) _camera = Camera.main;
            Vector3 right = _camera ? _camera.transform.right : transform.forward;
            Vector3 up = _camera ? _camera.transform.up : Vector3.up;
            Vector3 towardsCamera = _camera ? -_camera.transform.forward * 0.035f : Vector3.zero;
            Vector3 center = (_head ? _head.position + Vector3.up * 0.07f
                : transform.position + Vector3.up * 1.65f) + towardsCamera;
            float t = _age / Duration;
            float wobble = Mathf.Sin(t * Mathf.PI * 4f) * 0.008f * (1f - t);
            var color = new Color(0.82f, 0.72f, 0.44f, 0.6f * (1f - t));
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                var mark = _marks[side];
                mark.startColor = mark.endColor = color;
                for (int j = 0; j < 9; j++)
                {
                    float angle = Mathf.Lerp(-0.45f, 0.45f, j / 8f);
                    mark.SetPosition(j, center + right * (sign * (0.17f + Mathf.Cos(angle) * 0.035f) + wobble)
                        + up * (Mathf.Sin(angle) * 0.09f + sign * wobble));
                }
            }
            _age += Time.deltaTime;
        }
        void Clear()
        {
            _pending = false;
            _age = Duration;
            _lastShown = -10;
            if (_root) _root.SetActive(false);
        }
        void OnDisable()
        {
            _body.Tapped -= OnTapped;
            _body.RunReset -= Clear;
            Clear();
        }
        void OnDestroy() { if (_root) Destroy(_root); }
    }
}
