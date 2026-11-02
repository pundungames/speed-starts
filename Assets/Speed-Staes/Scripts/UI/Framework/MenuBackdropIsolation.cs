using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>Dim every background depth; redraw only the preview character and its platform above the dim.</summary>
    public sealed class MenuBackdropIsolation : MonoBehaviour
    {
        const int PreviewLayer = 31;
        readonly Dictionary<GameObject, int> _layers = new Dictionary<GameObject, int>();
        Camera _main, _foreground;
        Canvas _canvas;
        int _originalMask;
        float _originalDistance;
        public Camera ForegroundCamera => _foreground;

        public void Configure()
        {
            if (_foreground) return;
            _canvas = GetComponent<Canvas>();
            _main = _canvas ? _canvas.worldCamera : null;
            if (!_main) return;
            Transform platform = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
                    if (candidate.name == "Foreground Character Platform") { platform = candidate; break; }
            if (!platform) return;
            _originalMask = _main.cullingMask;
            _originalDistance = _canvas.planeDistance;
            foreach (var child in platform.GetComponentsInChildren<Transform>(true))
            {
                _layers[child.gameObject] = child.gameObject.layer;
                child.gameObject.layer = PreviewLayer;
            }
            var cameraObject = new GameObject("Menu Preview Camera");
            cameraObject.transform.SetParent(_main.transform, false);
            _foreground = cameraObject.AddComponent<Camera>();
            _foreground.CopyFrom(_main);
            _foreground.clearFlags = CameraClearFlags.Depth;
            _foreground.cullingMask = 1 << PreviewLayer;
            _foreground.depth = _main.depth + 1;
            _foreground.tag = "Untagged";
            _main.cullingMask &= ~(1 << PreviewLayer);
            // In front of all background geometry, rather than halfway through the scene.
            _canvas.planeDistance = _main.nearClipPlane + 0.05f;
        }
        void LateUpdate()
        {
            if (!_foreground || !_main) return;
            _foreground.fieldOfView = _main.fieldOfView;
            _foreground.orthographic = _main.orthographic;
            _foreground.orthographicSize = _main.orthographicSize;
            _foreground.rect = _main.rect;
        }
        void OnDisable()
        {
            if (_main && _layers.Count > 0) _main.cullingMask = _originalMask;
            if (_canvas && _layers.Count > 0) _canvas.planeDistance = _originalDistance;
            foreach (var pair in _layers) if (pair.Key) pair.Key.layer = pair.Value;
            _layers.Clear();
            if (_foreground)
            {
                _foreground.enabled = false;
                Destroy(_foreground.gameObject);
            }
            _foreground = null;
            _main = null;
        }
    }
}
