using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Framework
{
    /// <summary>A non-interactive finger that follows the current guide target through UI transitions.</summary>
    internal sealed class FirstRunPointer : MonoBehaviour
    {
        private RectTransform _area;
        private RectTransform _marker;
        private RectTransform _target;
        private Canvas _root;

        public static FirstRunPointer Create(Canvas root)
        {
            Texture2D texture = Resources.Load<Texture2D>("FirstRun/index-finger");
            if (!root || !texture) return null;

            var overlay = new GameObject("First Run Pointer", typeof(RectTransform), typeof(Canvas), typeof(FirstRunPointer));
            var cue = overlay.GetComponent<FirstRunPointer>();
            cue._root = root.rootCanvas;
            cue._area = (RectTransform)overlay.transform;
            cue._area.SetParent(cue._root.transform, false);
            cue._area.anchorMin = Vector2.zero;
            cue._area.anchorMax = Vector2.one;
            cue._area.offsetMin = cue._area.offsetMax = Vector2.zero;
            var canvas = overlay.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = cue._root.sortingLayerID;
            canvas.sortingOrder = 30001;

            var finger = new GameObject("Index Finger", typeof(RectTransform), typeof(RawImage));
            var icon = finger.GetComponent<RawImage>();
            cue._marker = icon.rectTransform;
            cue._marker.SetParent(cue._area, false);
            cue._marker.anchorMin = cue._marker.anchorMax = new Vector2(0.5f, 0.5f);
            cue._marker.sizeDelta = new Vector2(60f, 82f);
            icon.texture = texture;
            icon.uvRect = new Rect(326f / 1254f, 229f / 1254f, 575f / 1254f, 790f / 1254f);
            icon.raycastTarget = false;
            cue.PointAt(null);
            return cue;
        }

        public void PointAt(RectTransform target)
        {
            _target = target;
            _marker.gameObject.SetActive(target);
        }

        private void LateUpdate()
        {
            if (!_target || !_target.gameObject.activeInHierarchy)
            {
                _marker.gameObject.SetActive(false);
                return;
            }
            _marker.gameObject.SetActive(true);
            Canvas source = _target.GetComponentInParent<Canvas>().rootCanvas;
            Camera from = source.renderMode == RenderMode.ScreenSpaceOverlay ? null : source.worldCamera;
            Camera to = _root.renderMode == RenderMode.ScreenSpaceOverlay ? null : _root.worldCamera;
            Vector2 pixel = RectTransformUtility.WorldToScreenPoint(from, _target.TransformPoint(_target.rect.center));
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, pixel, to, out Vector2 point)) return;

            float bob = 8f * Mathf.Sin(Time.unscaledTime * 8f);
            Rect bounds = _area.rect;
            _marker.anchoredPosition = new Vector2(
                Mathf.Clamp(point.x + 18f, bounds.xMin + 44f, bounds.xMax - 44f),
                Mathf.Clamp(point.y - 48f + bob, bounds.yMin + 51f, bounds.yMax - 51f));
        }
    }
}