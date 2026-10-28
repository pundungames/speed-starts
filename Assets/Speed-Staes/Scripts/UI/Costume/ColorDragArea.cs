using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// A rect that reports where it was pressed or dragged, as 0..1 on both axes (x to the right, y upwards), clamped to
    /// the rect. Put it on an object that has an invisible raycast Image (the colour picker's square and hue strip).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ColorDragArea : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform _rect;

        /// <summary>Normalised x, y inside the rect.</summary>
        public event Action<float, float> Picked;

        public void OnPointerDown(PointerEventData eventData) => Report(eventData);

        public void OnDrag(PointerEventData eventData) => Report(eventData);

        private void Report(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_rect == null) _rect = (RectTransform)transform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;

            Rect rect = _rect.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            float x = Mathf.Clamp01((local.x - rect.xMin) / rect.width);
            float y = Mathf.Clamp01((local.y - rect.yMin) / rect.height);
            Picked?.Invoke(x, y);
        }
    }
}
