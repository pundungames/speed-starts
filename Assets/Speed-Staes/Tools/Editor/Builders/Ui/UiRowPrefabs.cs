using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Widget prefabs: the resource bar (fixed indicators, not rows), and the red dot.
    /// The resource bar lives on the scene's overlay canvas; screens only keep an empty slot for it.
    /// </summary>
    internal static class UiWidgetPrefabs
    {
        /// <summary>UI v2 resource bar: left vertical column from layout.json (see UiV2Prefabs).</summary>
        public static void ResourceBar(RectTransform root)
        {
            UiV2Prefabs.ResourceBar(root);
        }

        /// <summary>Small "there is something new" dot; sits at its parent's top-right corner.</summary>
        public static void RedDot(RectTransform root)
        {
            Kit.Anchor(root, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(38f, 38f));
            Image dot = root.gameObject.AddComponent<Image>();
            dot.sprite = Kit.SpriteOf(UiShape.Round);
            dot.color = Kit.MarkerRed;
            dot.raycastTarget = false;
            Kit.Frame(root.gameObject, 2f);
        }
    }
}
