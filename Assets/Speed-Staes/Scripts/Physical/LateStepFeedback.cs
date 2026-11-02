using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Physical
{
    /// <summary>A readable chest-height label for late presses. Presentation only.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed class LateStepFeedback : MonoBehaviour
    {
        const float Duration = 0.4f;
        const float RepeatDelay = 2f;
        RunnerBody _body;
        Transform _head;
        Camera _camera;
        GameObject _root;
        TextMesh _label, _shadow;
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
            foreach (var bone in GetComponentsInChildren<Transform>(true))
                if (bone.name == "B-head") { _head = bone; break; }
            _camera = Camera.main;
            _root = new GameObject("Late Step Feedback");
            _root.layer = gameObject.layer;
            _root.transform.SetParent(transform, false);
            _shadow = CreateText("Late Step Text Shadow", 0);
            _shadow.transform.localPosition = new Vector3(0.007f, -0.007f, 0.002f);
            _label = CreateText("Late Step Label", 1);
            _root.SetActive(false);
        }
        TextMesh CreateText(string name, int order)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(_root.transform, false);
            var text = go.AddComponent<TextMesh>();
            text.text = "LATE";
            text.anchor = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.fontSize = 64;
            text.characterSize = 0.024f;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }
        void OnTapped(int leg, bool pressed, double at)
        {
            if (pressed && _body.Sim.LastResult == PressResult.Good && _body.Sim.LastEfficiency >= 0.7f)
            {
                // A successful recovery replaces the old warning; retain its repeat cooldown.
                _pending = false;
                _age = Duration;
                if (_root) _root.SetActive(false);
                return;
            }
            if (!pressed || _body.Sim.LastResult != PressResult.Late || at - _lastShown < RepeatDelay) return;
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
            float side = Vector3.Dot(transform.forward, right) < 0 ? -1 : 1;
            Vector3 chest = _head ? _head.position - Vector3.up * 0.28f
                : transform.position + Vector3.up * 1.3f;
            float t = _age / Duration;
            _root.transform.position = chest - right * (side * 0.7f) + up * (0.28f + t * 0.12f);
            _root.transform.rotation = _camera ? _camera.transform.rotation : Quaternion.identity;
            // Keep the first part fully readable, then fade smoothly.
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t));
            _label.color = new Color(0.86f, 0.25f, 0.27f, alpha * 0.8f);
            _shadow.color = new Color(0.12f, 0.02f, 0.03f, alpha * 0.5f);
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
