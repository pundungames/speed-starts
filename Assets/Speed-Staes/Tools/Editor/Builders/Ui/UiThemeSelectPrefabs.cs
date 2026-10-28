using System;
using System.IO;
using SpeedStars.UI.Themes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// ThemeSelectScreen prefab dressed with the designer's art (UI/Art/ThemeSelect): Back, the four resource boxes on the
    /// left, theme name with the two arrows and the action button on the lower right. No dimscreen and no character: the
    /// 3D world behind is the content. Component rects come from ThemeSelect/layout.json; layers of one component share its
    /// art canvas at 100% scale. The action button has one background Image (the view swaps its sprite per state) plus
    /// the currency icon and two labels (price / USE-IN USE). Text slots are fractions of the art, read off the design
    /// preview. Never runs in the game.
    /// </summary>
    internal static class UiThemeSelectPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/ThemeSelect";
        private const string LayoutPath = ArtFolder + "/layout.json";

        private const float ValueSize = 48f;
        private const float NameSize = 56f;
        private const float ActionSize = 64f;

        // Text slots as fractions of their art (min, max; y from the bottom).
        private const float ValueGap = 0.04f; // between a resource icon and its value
        private const float ValueEdge = 0.93f;
        // Theme name: the plate's inner area, read off layout.json's theme_name_rect (231x78 at 1267,579 inside the 277x90 plate at 1243,572).
        private static readonly Vector2 NameMin = new Vector2(0.087f, 0.056f), NameMax = new Vector2(0.921f, 0.922f);
        private const float ActionGap = 0.03f; // between the currency icon and the price
        private const float ActionEdge = 0.95f;

        public static void ThemeSelect(RectTransform root)
        {
            UiArt.Layout layout = ReadLayout();
            var view = root.gameObject.AddComponent<ThemeSelectScreenView>();

            RectTransform back = Kit.Node(root, "Back");
            layout.PlaceOnScreen(back, layout.Get("back"));
            Image backImage = UiArt.Paint(back.gameObject, Art("back_background"), raycast: true);
            UiArt.Layer(back, "Icon", Art("back_icon"));
            UiArt.MakeButton(back.gameObject, backImage, true, alphaHit: false);

            Text purpleGem = ResourceItem(root, layout, "PurpleGem", "purple_gem", "0");
            Text entryTicket = ResourceItem(root, layout, "EntryTicket", "entry_ticket", "0/0");
            Text timedTicket = ResourceItem(root, layout, "TimedTicket", "timed_ticket", "0");
            Text cash = ResourceItem(root, layout, "Cash", "diamond", "0");

            RectTransform nameNode = Kit.Node(root, "ThemeName");
            layout.PlaceOnScreen(nameNode, layout.Get("theme_name"));
            UiArt.Paint(nameNode.gameObject, Art("theme_name_background"), raycast: false);
            Text nameLabel = UiArt.Slot(nameNode, "Label", "CITY", NameSize, NameMin, NameMax);

            Button previous = ArrowButton(root, layout, "ThemePrevious", "theme_previous");
            Button next = ArrowButton(root, layout, "ThemeNext", "theme_next");

            // Action button: root Image = the state background, the view swaps its sprite.
            RectTransform action = Kit.Node(root, "ThemeAction");
            layout.PlaceOnScreen(action, layout.Get("theme_action"));
            Image actionImage = UiArt.Paint(action.gameObject, Art("theme_action_background"), raycast: true);
            Button actionButton = UiArt.MakeButton(action.gameObject, actionImage, true, alphaHit: false);
            actionButton.transition = Selectable.Transition.SpriteSwap; // pressed USE shows its own art; the view fills the SpriteState
            Image icon = UiArt.Layer(action, "CurrencyIcon", Art("theme_action_icon"));
            Vector4 iconBounds = AlphaBounds("theme_action_icon");
            Text price = UiArt.Slot(action, "PriceLabel", "0", ActionSize,
                new Vector2(iconBounds.z + ActionGap, 0.10f), new Vector2(ActionEdge, 0.90f), TextAnchor.MiddleCenter);
            Text state = UiArt.Slot(action, "StateLabel", "USE", ActionSize,
                new Vector2(1f - ActionEdge, 0.10f), new Vector2(ActionEdge, 0.90f), TextAnchor.MiddleCenter);
            state.gameObject.SetActive(false);

            // ---- bind the view (all objects are inside this prefab)
            var so = new SerializedObject(view);
            Ref(so, "nameText", nameLabel);
            Ref(so, "previousButton", previous);
            Ref(so, "nextButton", next);
            Ref(so, "actionButton", actionButton);
            Ref(so, "actionBackground", actionImage);
            Ref(so, "currencyIcon", icon.gameObject);
            Ref(so, "priceText", price);
            Ref(so, "stateText", state);
            Ref(so, "purchaseSprite", Art("theme_action_background"));
            Ref(so, "useSprite", Art("theme_use_background"));
            Ref(so, "usePressedSprite", Art("theme_use_pressed_background"));
            Ref(so, "inUseSprite", Art("theme_in_use_background"));
            Ref(so, "disabledSprite", Art("theme_disabled_background"));
            Ref(so, "purpleGemText", purpleGem);
            Ref(so, "entryTicketText", entryTicket);
            Ref(so, "timedTicketText", timedTicket);
            Ref(so, "cashText", cash);
            so.ApplyModifiedPropertiesWithoutUndo();

            UiViewBinder.BindClose(root, "Back"); // base UiPanel.closeButton: the same close rule as every screen
        }

        /// <summary>Resource box: background, icon, and a left-aligned value text right of the icon.</summary>
        private static Text ResourceItem(Transform root, UiArt.Layout layout, string nodeName, string artName, string placeholder)
        {
            RectTransform node = Kit.Node(root, nodeName);
            layout.PlaceOnScreen(node, layout.Get(artName));
            UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: false);
            UiArt.Layer(node, "Icon", Art(artName + "_icon"));
            Vector4 icon = AlphaBounds(artName + "_icon");
            return UiArt.Slot(node, "Value", placeholder, ValueSize,
                new Vector2(icon.z + ValueGap, 0.05f), new Vector2(ValueEdge, 0.95f), TextAnchor.MiddleLeft);
        }

        private static Button ArrowButton(Transform root, UiArt.Layout layout, string nodeName, string artName)
        {
            RectTransform node = Kit.Node(root, nodeName);
            layout.PlaceOnScreen(node, layout.Get(artName));
            Image background = UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: true);
            UiArt.Layer(node, "Icon", Art(artName + "_icon"));
            return UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
        }

        // ---------------------------------------------------------------- helpers

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiThemeSelectPrefabs: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        /// <summary>Bounds of the visible (alpha &gt; 0) pixels of an art PNG as fractions of its canvas: x, y (from bottom), x2, y2.</summary>
        private static Vector4 AlphaBounds(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(path))))
                    throw new InvalidOperationException($"UiThemeSelectPrefabs: '{path}' could not be read.");

                int width = texture.width, height = texture.height;
                Color32[] pixels = texture.GetPixels32();
                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a == 0) continue;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
                if (maxX < 0) throw new InvalidOperationException($"UiThemeSelectPrefabs: '{path}' is fully transparent.");
                return new Vector4(minX / (float)width, minY / (float)height, (maxX + 1) / (float)width, (maxY + 1) / (float)height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static UiArt.Layout ReadLayout()
        {
            string path = Kit.FullPath(LayoutPath);
            if (!File.Exists(path)) throw new InvalidOperationException($"UiThemeSelectPrefabs: '{LayoutPath}' not found.");
            var file = JsonUtility.FromJson<UiArt.LayoutFile>(File.ReadAllText(path));
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null)
                throw new InvalidOperationException($"UiThemeSelectPrefabs: '{LayoutPath}' could not be read.");
            return new UiArt.Layout(file);
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiThemeSelectPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiThemeSelectPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }
    }
}
