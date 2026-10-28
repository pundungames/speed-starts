using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// The colour panel: a saturation / value square over the chosen hue (flat hue colour + white and black overlay art),
    /// the hue strip with its handle, the cursor and the swatch. Everything is a prefab object bound in the Inspector; this
    /// view only moves the cursor and handle, tints the flat hue layer and the swatch, and reports the colour as the
    /// "RRGGBB" text outfits store. Maths in <see cref="HsvColor"/>. Square: left = no saturation, top = full brightness;
    /// strip: red at the top, running down and back to red at the bottom.
    /// </summary>
    public sealed class CostumeColorPickerView : MonoBehaviour
    {
        [Tooltip("ColorPanel/SvArea: invisible tap area over the saturation / value square (inner area of the frame).")]
        [SerializeField] private ColorDragArea svArea;

        [Tooltip("ColorPanel/HueArea: invisible tap area over the hue strip.")]
        [SerializeField] private ColorDragArea hueArea;

        [Tooltip("ColorPanel/SvArea/Base: flat colour of the chosen hue, under the white and black overlays.")]
        [SerializeField] private Image hueBase;

        [Tooltip("ColorPanel/SvCursor: the ring; moves inside the square. Anchored at one point (anchors = position).")]
        [SerializeField] private RectTransform svCursor;

        [Tooltip("ColorPanel/HueHandle: moves up and down the strip. Anchored at one point (anchors = position).")]
        [SerializeField] private RectTransform hueHandle;

        [Tooltip("ColorPanel/SwatchColor: flat colour of the chosen colour.")]
        [SerializeField] private Image swatchColor;

        [Tooltip("Whole panel: switched off (greyed, no taps) while there is no item to recolour.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Alpha of the panel while it is switched off.")]
        [Range(0f, 1f)]
        [SerializeField] private float disabledAlpha = 0.5f;

        private HsvColor _color = new HsvColor(0f, 0f, 1f);

        /// <summary>The player picked a colour ("RRGGBB").</summary>
        public event Action<string> ColorPicked;

        private void Awake()
        {
            svArea.Picked += OnSvPicked;
            hueArea.Picked += OnHuePicked;
        }

        private void OnDestroy()
        {
            if (svArea != null) svArea.Picked -= OnSvPicked;
            if (hueArea != null) hueArea.Picked -= OnHuePicked;
        }

        /// <summary>Shows a colour without reporting it. Text that is not a colour is ignored.</summary>
        public void SetColor(string hex)
        {
            if (!HsvColor.TryFromHex(hex, _color.Hue, out HsvColor parsed))
            {
                Redraw(); // not a colour: still place the parts from the current one so nothing sits at its prefab default
                return;
            }
            _color = parsed;
            Redraw();
        }

        public void SetInteractable(bool interactable)
        {
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
            group.alpha = interactable ? 1f : disabledAlpha;
        }

        private void OnSvPicked(float x, float y)
        {
            _color = _color.WithSaturationValue(x, y);
            Report();
        }

        private void OnHuePicked(float x, float y)
        {
            _color = _color.WithHue(1f - y);
            Report();
        }

        private void Report()
        {
            Redraw();
            ColorPicked?.Invoke(_color.ToHex());
        }

        private void Redraw()
        {
            _color.HueToRgb(out float hr, out float hg, out float hb);
            hueBase.color = new Color(hr, hg, hb, 1f);

            _color.ToRgb(out float r, out float g, out float b);
            swatchColor.color = new Color(r, g, b, 1f);

            Place(svCursor, Mid(svArea, _color.Saturation, _color.Value), true, true);
            Place(hueHandle, Mid(hueArea, 0.5f, 1f - _color.Hue), false, true);
        }

        /// <summary>Point inside an area as a fraction of the parent (the panel): the area's own anchors span its rect.</summary>
        private static Vector2 Mid(ColorDragArea area, float x, float y)
        {
            var rect = (RectTransform)area.transform;
            return new Vector2(Mathf.Lerp(rect.anchorMin.x, rect.anchorMax.x, x), Mathf.Lerp(rect.anchorMin.y, rect.anchorMax.y, y));
        }

        private static void Place(RectTransform node, Vector2 anchor, bool moveX, bool moveY)
        {
            Vector2 current = node.anchorMin;
            var target = new Vector2(moveX ? anchor.x : current.x, moveY ? anchor.y : current.y);
            node.anchorMin = target;
            node.anchorMax = target;
            node.anchoredPosition = Vector2.zero;
        }
    }
}
