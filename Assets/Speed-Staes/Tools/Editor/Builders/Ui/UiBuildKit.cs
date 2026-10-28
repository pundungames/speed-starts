using System;
using System.IO;
using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tools.Builders
{
    /// <summary>Which built-in UI sprite a placeholder graphic uses.</summary>
    internal enum UiShape
    {
        Panel,
        Round,
        Check,
        Arrow
    }

    /// <summary>
    /// Shared pieces of the placeholder UI builder: nodes, layout setters, panels, labels, buttons and scroll lists.
    /// Editor time only. The look follows the original game (white panels, dark frame, bold italic text) with Unity's
    /// built-in UI sprites, so the art team can swap sprites later without touching the hierarchy.
    /// Every Button gets a ButtonFeedback; labels and decorations never catch raycasts, so taps reach the button.
    /// </summary>
    internal static class UiBuildKit
    {
        public static readonly Color PanelColor = Color.white;
        public static readonly Color FrameColor = new Color(0.13f, 0.13f, 0.15f, 1f);
        public static readonly Color InkColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        public static readonly Color SoftColor = new Color(0.93f, 0.94f, 0.95f, 1f);
        public static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        public static readonly Color ShadeColor = new Color(0.1f, 0.1f, 0.12f, 0.5f);
        public static readonly Color IconGrey = new Color(0.74f, 0.75f, 0.78f, 1f);
        public static readonly Color HighlightColor = new Color(1f, 0.87f, 0.32f, 1f);
        public static readonly Color MarkerRed = new Color(0.9f, 0.16f, 0.16f, 1f);
        public static readonly Color BlueText = new Color(0.16f, 0.64f, 0.84f, 1f);
        public static readonly Color GoldText = new Color(0.78f, 0.6f, 0.08f, 1f);
        public static readonly Color PinkText = new Color(0.92f, 0.38f, 0.5f, 1f);
        public static readonly Color GreenText = new Color(0.1f, 0.66f, 0.3f, 1f);
        public static readonly Color OrangeText = new Color(0.94f, 0.48f, 0.1f, 1f);
        public static readonly Color GemPurple = new Color(0.62f, 0.3f, 0.9f, 1f);
        public static readonly Color TicketOrange = new Color(0.98f, 0.62f, 0.18f, 1f);
        public static readonly Color TimedCyan = new Color(0.2f, 0.78f, 0.86f, 1f);
        public static readonly Color CashGreen = new Color(0.24f, 0.74f, 0.36f, 1f);

        /// <summary>Thickness of the dark frame (Outline effect distance, px).</summary>
        public const float FrameWidth = 5f;

        private const string PanelSpritePath = "UI/Skin/UISprite.psd";
        private const string RoundSpritePath = "UI/Skin/Knob.psd";
        private const string CheckSpritePath = "UI/Skin/Checkmark.psd";
        private const string ArrowSpritePath = "UI/Skin/DropdownArrow.psd";
        private const string FontResource = "LegacyRuntime.ttf";

        private static Font s_font;
        private static Sprite s_panelSprite;
        private static Sprite s_roundSprite;
        private static Sprite s_checkSprite;
        private static Sprite s_arrowSprite;
        private static int s_uiLayer;

        /// <summary>
        /// Loads the font and sprites once per build. Legacy Text uses the built-in dynamic font, which falls back to the
        /// operating system font for CJK, so the same prefabs work for the Chinese market without font assets.
        /// </summary>
        public static void Begin()
        {
            s_font = Resources.GetBuiltinResource<Font>(FontResource);
            if (s_font == null)
            {
                throw new InvalidOperationException($"UiPrefabBuilder: built-in font '{FontResource}' not found.");
            }

            s_panelSprite = LoadBuiltinSprite(PanelSpritePath);
            s_roundSprite = LoadBuiltinSprite(RoundSpritePath);
            s_checkSprite = LoadBuiltinSprite(CheckSpritePath);
            s_arrowSprite = LoadBuiltinSprite(ArrowSpritePath);

            int layer = LayerMask.NameToLayer("UI");
            s_uiLayer = layer >= 0 ? layer : 0;
        }

        // ---------------------------------------------------------------- nodes

        /// <summary>A prefab root: RectTransform stretched over its parent, living in the builder's preview scene.</summary>
        public static RectTransform NewRoot(string name, UnityEngine.SceneManagement.Scene scene)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = s_uiLayer;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            var rt = (RectTransform)go.transform;
            Stretch(rt);
            return rt;
        }

        /// <summary>An empty child with a RectTransform (centered, 100×100 until a layout setter places it).</summary>
        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = s_uiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>
        /// Puts an instance of a prefab under <paramref name="parent"/> (nested prefab, keeps its own layout).
        /// A missing prefab is warned about and skipped: the parent is still built, just without that sample.
        /// </summary>
        public static RectTransform Nest(GameObject prefab, Transform parent, string name, UnityEngine.SceneManagement.Scene scene)
        {
            if (prefab == null)
            {
                UnityEngine.Debug.LogWarning($"UiPrefabBuilder: '{name}' under '{parent.name}' skipped, its prefab is missing.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                UnityEngine.Debug.LogWarning($"UiPrefabBuilder: '{prefab.name}' could not be instantiated under '{parent.name}'.");
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.name = name;
            Record(instance);
            Record(instance.transform);
            return instance.transform as RectTransform;
        }

        /// <summary>
        /// Keeps a change made by script on a nested prefab instance (name, text, ...): without this call Unity drops
        /// script edits of prefab instances when the outer prefab is saved. No-op on plain objects.
        /// </summary>
        public static void Record(UnityEngine.Object target)
        {
            if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        // ---------------------------------------------------------------- layout

        /// <summary>Stretches over the parent with insets (px, positive = inward).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Fixed size at one anchor point.</summary>
        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            return Place(rt, anchor, anchor, pivot, position, size);
        }

        /// <summary>Full control: anchors, pivot, anchored position and size delta.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        /// <summary>Fills a fraction of the parent (anchors only, zero offsets): follows the parent's size.</summary>
        public static RectTransform Span(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>A band along the parent's top edge: <paramref name="top"/> px down, <paramref name="height"/> tall, inset left/right.</summary>
        public static RectTransform TopBand(RectTransform rt, float top, float height, float inset = 0f)
        {
            return Place(rt, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -top), new Vector2(-2f * inset, height));
        }

        /// <summary>A band along the parent's bottom edge.</summary>
        public static RectTransform BottomBand(RectTransform rt, float bottom, float height, float inset = 0f)
        {
            return Place(rt, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, bottom), new Vector2(-2f * inset, height));
        }

        /// <summary>Row height for vertical list layout groups.</summary>
        public static void PreferredHeight(GameObject go, float height)
        {
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        // ---------------------------------------------------------------- graphics

        /// <summary>A panel image (sliced white sprite) on an existing object, optionally with the dark frame.</summary>
        public static Image Panel(GameObject go, Color color, bool frame, bool raycast)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = s_panelSprite;
            image.type = s_panelSprite != null && s_panelSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            if (frame) Frame(go);
            return image;
        }

        /// <summary>A flat fill without a sprite (backdrops, dividers): no sprite means a plain quad, which is the intent here.</summary>
        public static Image Fill(GameObject go, Color color, bool raycast)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>The original's thick dark edge, as an Outline effect so it stays editable in the Inspector.</summary>
        public static Outline Frame(GameObject go, float width = FrameWidth)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = FrameColor;
            outline.effectDistance = new Vector2(width, -width);
            outline.useGraphicAlpha = true;
            return outline;
        }

        /// <summary>A placeholder icon child (built-in sprite, tinted). Never catches raycasts.</summary>
        public static Image Icon(Transform parent, string name, UiShape shape, Color color)
        {
            RectTransform rt = Node(parent, name);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = SpriteOf(shape);
            image.type = shape == UiShape.Panel && image.sprite != null && image.sprite.border != Vector4.zero
                ? Image.Type.Sliced
                : Image.Type.Simple;
            image.preserveAspect = shape == UiShape.Check || shape == UiShape.Arrow;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Bold italic legacy Text label stretched over its parent. Best fit shrinks long text to the box (max = given
        /// size); one or two character captions ("X", "+", "<") keep a fixed size. Placeholder text only: views fill the
        /// real values. Text is English.
        /// </summary>
        public static Text Label(Transform parent, string name, string text, float size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            RectTransform rt = Node(parent, name);
            Text label = rt.gameObject.AddComponent<Text>();
            Stretch(rt);
            label.font = s_font;
            label.text = text;
            label.fontStyle = FontStyle.BoldAndItalic;
            label.fontSize = Mathf.RoundToInt(size);
            // Always best fit (never above the design size): runtime numbers grow ("0" -> "1234") and a label whose line does
            // not fit its rect vertically is not drawn at all with Truncate (Race HUD cash showed nothing).
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = Mathf.RoundToInt(size);
            label.resizeTextMinSize = Mathf.RoundToInt(Mathf.Max(12f, size * 0.4f));
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Insets a label's rect (left, top, right, bottom px) on top of its current placement: legacy Text has no margin,
        /// so the rect itself shrinks. Call after the label is placed.
        /// </summary>
        public static void Margin(Text label, float left, float top, float right, float bottom)
        {
            RectTransform rt = label.rectTransform;
            rt.offsetMin += new Vector2(left, bottom);
            rt.offsetMax -= new Vector2(right, top);
        }

        // ---------------------------------------------------------------- buttons

        /// <summary>Makes an existing object tappable: Button (tinting <paramref name="target"/>) + ButtonFeedback.</summary>
        public static Button Clickable(GameObject go, Graphic target)
        {
            Button button = go.AddComponent<Button>();
            button.targetGraphic = target;
            go.AddComponent<ButtonFeedback>();
            return button;
        }

        /// <summary>White framed button with a centered "Label". Place it with a layout setter afterwards.</summary>
        public static RectTransform TextButton(Transform parent, string name, string caption, float fontSize, Color textColor)
        {
            return TextButton(parent, name, caption, fontSize, textColor, out _);
        }

        /// <summary>TextButton that also hands back its label (to move it or add a second line).</summary>
        public static RectTransform TextButton(Transform parent, string name, string caption, float fontSize, Color textColor,
            out Text label)
        {
            RectTransform rt = Node(parent, name);
            Image background = Panel(rt.gameObject, PanelColor, frame: true, raycast: true);
            Clickable(rt.gameObject, background);
            label = Label(rt, "Label", caption, fontSize, textColor);
            Margin(label, 16f, 4f, 16f, 4f);
            return rt;
        }

        /// <summary>Button with a square "Icon" on the left and the "Label" after it (the original's icon + word buttons).</summary>
        public static RectTransform IconButton(Transform parent, string name, string caption, float fontSize, Color textColor,
            Color iconColor, float iconSize, UiShape shape = UiShape.Panel)
        {
            return IconButton(parent, name, caption, fontSize, textColor, iconColor, iconSize, shape, out _);
        }

        /// <summary>IconButton that also hands back its label.</summary>
        public static RectTransform IconButton(Transform parent, string name, string caption, float fontSize, Color textColor,
            Color iconColor, float iconSize, UiShape shape, out Text label)
        {
            RectTransform rt = TextButton(parent, name, caption, fontSize, textColor, out label);
            Image icon = Icon(rt, "Icon", shape, iconColor);
            Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(iconSize, iconSize));
            icon.rectTransform.SetAsFirstSibling();
            Margin(label, iconSize + 16f, 0f, 0f, 0f); // on top of the 16 px TextButton inset
            return rt;
        }

        /// <summary>The "Close" X of a popup window, at its top-right corner.</summary>
        public static RectTransform CloseButton(Transform window)
        {
            RectTransform close = TextButton(window, "Close", "X", 56f, InkColor);
            return Anchor(close, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(96f, 96f));
        }

        /// <summary>The top-left "Back" of a screen (the original's back arrow corner).</summary>
        public static RectTransform BackButton(Transform screen)
        {
            RectTransform back = TextButton(screen, "Back", "Back", 52f, PinkText);
            return Anchor(back, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(220f, 110f));
        }

        // ---------------------------------------------------------------- lists

        /// <summary>
        /// Vertical scroll list: ScrollRect → Viewport (RectMask2D) → Content (VerticalLayoutGroup + ContentSizeFitter).
        /// Rows control their height with a LayoutElement; width follows the list. Content starts empty: the view
        /// instantiates row prefabs into it.
        /// </summary>
        public static RectTransform VerticalList(Transform parent, string name, float spacing)
        {
            RectTransform list = ScrollList(parent, name, out RectTransform content);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return list;
        }

        /// <summary>Same as VerticalList, Content laid out as a fixed-column grid of equal cells.</summary>
        public static RectTransform GridList(Transform parent, string name, Vector2 cellSize, Vector2 spacing, int columns)
        {
            RectTransform list = ScrollList(parent, name, out RectTransform content);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            return list;
        }

        private static RectTransform ScrollList(Transform parent, string name, out RectTransform content)
        {
            RectTransform list = Node(parent, name);
            Panel(list.gameObject, SoftColor, frame: false, raycast: true); // drag area: empty space must still scroll
            ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = Stretch(Node(list, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Place(Node(viewport, "Content"), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            return list;
        }

        /// <summary>Row of fixed-size children (they keep their own size), centered.</summary>
        public static HorizontalLayoutGroup Row(GameObject go, float spacing, TextAnchor alignment)
        {
            HorizontalLayoutGroup row = go.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = alignment;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        // ---------------------------------------------------------------- assets

        /// <summary>Creates every missing folder of an asset folder path ("Assets/A/B"), importing ones already on disk.</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            int slash = folder.LastIndexOf('/');
            if (slash <= 0) throw new InvalidOperationException($"UiPrefabBuilder: '{folder}' is not under Assets.");

            string parent = folder.Substring(0, slash);
            string leaf = folder.Substring(slash + 1);
            EnsureFolder(parent);

            if (Directory.Exists(FullPath(folder)))
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceSynchronousImport); // on disk, not yet known
            else
                AssetDatabase.CreateFolder(parent, leaf);

            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException($"UiPrefabBuilder: folder '{folder}' could not be created.");
        }

        /// <summary>Absolute disk path of a project-relative asset path (never resolved against the process CWD).</summary>
        public static string FullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }

        /// <summary>The built-in sprite behind a shape (null if the editor did not provide it).</summary>
        public static Sprite SpriteOf(UiShape shape)
        {
            switch (shape)
            {
                case UiShape.Round: return s_roundSprite;
                case UiShape.Check: return s_checkSprite;
                case UiShape.Arrow: return s_arrowSprite;
                case UiShape.Panel: return s_panelSprite;
                default: return s_panelSprite;
            }
        }

        private static Sprite LoadBuiltinSprite(string path)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
            if (sprite == null) UnityEngine.Debug.LogWarning($"UiPrefabBuilder: built-in sprite '{path}' not found; graphics fall back to plain quads.");
            return sprite;
        }
    }
}
