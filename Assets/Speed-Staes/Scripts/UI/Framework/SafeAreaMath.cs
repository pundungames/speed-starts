using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Pure safe-area rules (no scene, no Screen access): a device cut-out is turned into canvas anchors and an authored
    /// anchor is squeezed into them. Kept free of components so the rules are testable in EditMode.
    /// </summary>
    public static class SafeAreaMath
    {
        /// <summary>
        /// Anchors (0..1 of the screen) of the safe rectangle. Insets of at most <paramref name="tolerancePx"/> count as none,
        /// so rounding noise of a notch-less phone leaves the layout exactly as authored. The vertical axis only changes
        /// when <paramref name="includeVertical"/> is set (the game is landscape-only; the notch sits on a side edge).
        /// </summary>
        public static void SafeAnchors(Vector2 screenSize, Rect safe, bool includeVertical, float tolerancePx,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
            if (screenSize.x <= 0f || screenSize.y <= 0f) return;

            AxisAnchors(screenSize.x, safe.xMin, safe.xMax, tolerancePx, out anchorMin.x, out anchorMax.x);
            if (includeVertical) AxisAnchors(screenSize.y, safe.yMin, safe.yMax, tolerancePx, out anchorMin.y, out anchorMax.y);
        }

        private static void AxisAnchors(float length, float safeMin, float safeMax, float tolerancePx, out float min, out float max)
        {
            float lo = Mathf.Clamp(safeMin, 0f, length);
            float hi = Mathf.Clamp(safeMax, 0f, length);
            min = 0f;
            max = 1f;
            if (hi <= lo) return; // nonsense rectangle: show everything rather than nothing

            float loInset = lo;
            float hiInset = length - hi;
            if (loInset > tolerancePx) min = loInset / length;
            if (hiInset > tolerancePx) max = 1f - hiInset / length;
        }

        /// <summary>
        /// Maps an authored anchor into the safe rectangle (0 stays at the safe edge, 1 at the other safe edge, 0.5 at the safe
        /// centre), so a corner item moves to the safe corner and a stretched item shrinks to the safe width.
        /// </summary>
        public static Vector2 Remap(Vector2 authored, Vector2 safeMin, Vector2 safeMax)
        {
            return new Vector2(
                safeMin.x + authored.x * (safeMax.x - safeMin.x),
                safeMin.y + authored.y * (safeMax.y - safeMin.y));
        }

        /// <summary>True when the anchors span the whole parent (0,0)-(1,1).</summary>
        public static bool IsFullStretch(Vector2 anchorMin, Vector2 anchorMax)
        {
            const float eps = 1e-4f;
            return anchorMin.sqrMagnitude < eps && (anchorMax - Vector2.one).sqrMagnitude < eps;
        }

        /// <summary>
        /// A child that must reach the screen edges: it stretches over the whole parent with no offsets and draws something
        /// (dim, background, tap catcher). A stretched container without a graphic only holds content and is fitted.
        /// </summary>
        public static bool LooksFullBleed(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool hasGraphic, float offsetTolerancePx)
        {
            if (!hasGraphic || !IsFullStretch(anchorMin, anchorMax)) return false;
            return Mathf.Abs(offsetMin.x) <= offsetTolerancePx && Mathf.Abs(offsetMin.y) <= offsetTolerancePx
                && Mathf.Abs(offsetMax.x) <= offsetTolerancePx && Mathf.Abs(offsetMax.y) <= offsetTolerancePx;
        }
    }
}
