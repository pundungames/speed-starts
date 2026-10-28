using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// The thin scroll track beside the item grid: a handle that follows a ScrollRect (a position indicator, not a control:
    /// the list is dragged by its content). The handle node is anchored at one point; it travels between the two anchor
    /// heights the builder read off the art. Hidden while the content fits.
    /// </summary>
    public sealed class ScrollIndicatorView : MonoBehaviour
    {
        [Tooltip("The list this indicator follows.")]
        [SerializeField] private ScrollRect scroll;

        [Tooltip("Handle node: anchored at one point; only its anchor height changes.")]
        [SerializeField] private RectTransform handle;

        [Tooltip("Anchor height (0 = bottom of the track, 1 = top) of the handle when the list is at the top.")]
        [Range(0f, 1f)]
        [SerializeField] private float topAnchorY = 1f;

        [Tooltip("Anchor height of the handle when the list is at the bottom.")]
        [Range(0f, 1f)]
        [SerializeField] private float bottomAnchorY;

        private void OnEnable()
        {
            scroll.onValueChanged.AddListener(OnScrolled);
            Redraw();
        }

        private void OnDisable() => scroll.onValueChanged.RemoveListener(OnScrolled);

        private void OnScrolled(Vector2 position) => Redraw();

        /// <summary>Call after the content changed (a new category): the handle shows only when there is something to scroll.</summary>
        public void Redraw()
        {
            RectTransform content = scroll.content;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            bool scrollable = content != null && content.rect.height > viewport.rect.height + 0.5f;
            handle.gameObject.SetActive(scrollable);
            if (!scrollable) return;

            float y = Mathf.Lerp(bottomAnchorY, topAnchorY, Mathf.Clamp01(scroll.verticalNormalizedPosition));
            handle.anchorMin = new Vector2(handle.anchorMin.x, y);
            handle.anchorMax = new Vector2(handle.anchorMax.x, y);
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
