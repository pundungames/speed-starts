using System;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Emotes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The emote screen prefabs (T-040), dressed with the costume editor's art (UI/Art/Costume): no new art. The screen
    /// (EmoteScreen.prefab) has Back, the Cash box, the wardrobe panel with the Emote tab as its title, the two-column list
    /// of cards and the scroll track, an Equip button (the Save button art) and the price button (the Purchase art) under the
    /// pedestal; the 3D character stands in the middle exactly as in the costume editor. The card (EmoteCard.prefab) is the
    /// wardrobe item card plus a name and a price. Rects come from Costume/layout.json; layers of one component share its art
    /// canvas at 100% scale. Dynamic content stays empty: the view creates the cards. Never runs in the game.
    /// </summary>
    internal static class UiEmotePrefabs
    {
        public const string EmoteScreenPath = UiPrefabPaths.Root + "/Screens/EmoteScreen.prefab";
        public const string EmoteCardPath = UiPrefabPaths.Root + "/Rows/EmoteCard.prefab";

        private const string ArtFolder = UiCostumePrefabs.ArtFolder;
        private const int Columns = 2;

        private const float NameSize = 34f;
        private const float PriceSize = 34f;
        private const float ValueSize = 48f;
        private const float PurchasePriceSize = 56f;
        private const float ActionLabelSize = 44f;
        private const float TabLabelSize = 34f;
        private const float LabelGap = 0.03f;
        private const float PriceGap = 0.03f;
        private const float PriceEdge = 0.94f;

        // Text slots as fractions of their art (min, max; y from the bottom).
        private static readonly Vector2 NameMin = new Vector2(0.05f, 0.03f), NameMax = new Vector2(0.95f, 0.25f);
        private static readonly Vector2 PriceMin = new Vector2(0.15f, 0.25f), PriceMax = new Vector2(0.85f, 0.42f);
        private static readonly Vector2 IconMin = new Vector2(0.2f, 0.4f), IconMax = new Vector2(0.8f, 0.94f);
        private static readonly Vector2 ValueMin = new Vector2(0.06f, 0.05f);
        private const float ValueTop = 0.04f;

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            EmoteScreenConfig config = EmoteCatalogSeed.SeedScreenConfig(overwrite: false);
            UiArt.Layout layout = UiArt.ReadLayout();

            GameObject card = build(EmoteCardPath, root => Card(root, layout));
            var cardView = UiViewBinder.RowComponent<EmoteCardView>(card, EmoteCardPath);
            build(EmoteScreenPath, root => BuildScreen(root, layout, config, cardView));
        }

        // ---------------------------------------------------------------- card

        private static void Card(RectTransform root, UiArt.Layout layout)
        {
            UiArt.LayoutComponent size = layout.Get("item_ivory");
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.rect[2], size.rect[3]) * layout.Scale);
            var view = root.gameObject.AddComponent<EmoteCardView>();

            Image hit = Kit.Fill(root.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);
            Button button = UiArt.MakeButton(root.gameObject, hit, true, alphaHit: false);

            Image normal = UiArt.Layer(root, "Normal", Art("item_card_normal_background"));
            Image selected = UiArt.Layer(root, "Selected", Art("item_card_selected_background"));
            selected.gameObject.SetActive(false);

            RectTransform iconNode = Kit.Span(Kit.Node(root, "Icon"), IconMin, IconMax);
            Image icon = UiArt.Paint(iconNode.gameObject, Art("category_emote_icon"), raycast: false);
            icon.preserveAspect = true;

            GameObject lockOverlay = UiArt.Layer(root, "Lock", Art("item_locked_icon")).gameObject;
            GameObject check = UiArt.Layer(root, "Check", Art("item_selected_check")).gameObject;
            lockOverlay.SetActive(false);
            check.SetActive(false);

            Text nameText = UiArt.Slot(root, "Name", "Emote", NameSize, NameMin, NameMax);
            Text priceText = UiArt.Slot(root, "Price", "0", PriceSize, PriceMin, PriceMax);
            priceText.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "normalBackground", normal);
            Ref(so, "selectedBackground", selected);
            Ref(so, "check", check);
            Ref(so, "lockOverlay", lockOverlay);
            Ref(so, "nameText", nameText);
            Ref(so, "priceText", priceText);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- screen

        private static void BuildScreen(RectTransform root, UiArt.Layout layout, EmoteScreenConfig config, EmoteCardView cardPrefab)
        {
            if (config == null) throw new InvalidOperationException("UiEmotePrefabs: EmoteScreenConfig asset is missing (EmoteCatalogSeed).");
            var view = root.gameObject.AddComponent<EmoteScreenView>();

            // Tap catcher only: nothing reaches the scene behind the screen. The dimming itself is the scene's 'Canvas Dim'
            // BEHIND the 3D character (an overlay layer here would darken the character too), bound to dimBackdrop.
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            Kit.Fill(dim.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true);

            Button back = BackButton(root, layout);
            Text cash = CashBox(root, layout);

            RectTransform wardrobe = Kit.Stretch(Kit.Node(root, "Wardrobe"));
            RectTransform panel = Kit.Node(wardrobe, "Panel");
            layout.PlaceOnScreen(panel, layout.Get("wardrobe"));
            UiArt.Paint(panel.gameObject, Art("wardrobe_background"), raycast: true);
            TabTitle(wardrobe, layout);
            ScrollRect itemsScroll = Items(wardrobe, layout, out RectTransform content);
            ScrollIndicatorView indicator = ScrollTrack(wardrobe, layout, itemsScroll);

            Button equip = EquipButton(root, layout, out Text equipLabel);
            Button purchase = PurchaseButton(root, layout, out Text price, out Image purchaseBackground);

            var so = new SerializedObject(view);
            Ref(so, "config", config);
            Ref(so, "backButton", back);
            Ref(so, "itemsScroll", itemsScroll);
            Ref(so, "itemsContent", content);
            Ref(so, "cardPrefab", cardPrefab);
            Ref(so, "scrollIndicator", indicator);
            Ref(so, "cashText", cash);
            Ref(so, "equipButton", equip);
            Ref(so, "equipLabel", equipLabel);
            Ref(so, "purchaseButton", purchase);
            Ref(so, "purchasePriceText", price);
            Ref(so, "purchaseBackground", purchaseBackground);
            so.ApplyModifiedPropertiesWithoutUndo();

            UiViewBinder.BindClose(root, "Back"); // base UiPanel.closeButton: the same close rule as every screen
        }

        private static Button BackButton(RectTransform root, UiArt.Layout layout)
        {
            RectTransform back = Kit.Node(root, "Back");
            layout.PlaceOnScreen(back, layout.Get("back"));
            Image image = UiArt.Paint(back.gameObject, Art("back_background"), raycast: true);
            UiArt.Layer(back, "Icon", Art("back_icon"));
            return UiArt.MakeButton(back.gameObject, image, true, alphaHit: false);
        }

        /// <summary>The Cash box of the costume editor: its art and the value text under the icon.</summary>
        private static Text CashBox(RectTransform root, UiArt.Layout layout)
        {
            RectTransform box = Kit.Node(root, "Cash");
            layout.PlaceOnScreen(box, layout.Get("diamond"));
            UiArt.Paint(box.gameObject, Art("diamond_background"), raycast: false);
            UiArt.Layer(box, "Icon", Art("diamond_icon"));
            Vector4 icon = UiArt.AlphaBounds(ArtPath("diamond_icon"));
            return UiArt.Slot(box, "Value", "0", ValueSize, ValueMin, new Vector2(1f - ValueMin.x, icon.y - ValueTop));
        }

        /// <summary>The Emote tab art (selected look) as the panel's title: art layers only, not a button.</summary>
        private static void TabTitle(RectTransform wardrobe, UiArt.Layout layout)
        {
            RectTransform node = Kit.Node(wardrobe, "Title");
            layout.PlaceOnScreen(node, layout.Get("category_emote"));
            UiArt.Paint(node.gameObject, Art("category_emote_selected_background"), raycast: false);
            UiArt.Layer(node, "Icon", Art("category_emote_icon"));
            Vector4 icon = UiArt.AlphaBounds(ArtPath("category_emote_icon"));
            UiArt.Slot(node, "Label", EmoteTexts.Title, TabLabelSize, new Vector2(icon.z + LabelGap, 0.1f), new Vector2(0.96f, 0.9f), TextAnchor.MiddleLeft);
        }

        /// <summary>Equip: the Save button art, centred on the Purchase spot under the pedestal (only one of the two shows).</summary>
        private static Button EquipButton(RectTransform root, UiArt.Layout layout, out Text label)
        {
            UiArt.LayoutComponent save = layout.Get("save");
            UiArt.LayoutComponent spot = layout.Get("purchase");
            var rect = new UiArt.LayoutComponent
            {
                rect = new[]
                {
                    Mathf.RoundToInt(spot.rect[0] + spot.rect[2] * 0.5f - save.rect[2] * 0.5f),
                    Mathf.RoundToInt(spot.rect[1] + spot.rect[3] * 0.5f - save.rect[3] * 0.5f),
                    save.rect[2], save.rect[3]
                }
            };
            RectTransform node = Kit.Node(root, "Equip");
            layout.PlaceOnScreen(node, rect);
            Image background = UiArt.Paint(node.gameObject, Art("save_background"), raycast: true);
            UiArt.Layer(node, "Icon", Art("save_icon"));
            Vector4 icon = UiArt.AlphaBounds(ArtPath("save_icon"));
            label = UiArt.Slot(node, "Label", EmoteTexts.Equip, ActionLabelSize, new Vector2(icon.z + LabelGap, 0.12f), new Vector2(0.96f, 0.88f),
                TextAnchor.MiddleLeft, Color.white);
            return UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
        }

        private static Button PurchaseButton(RectTransform root, UiArt.Layout layout, out Text price, out Image background)
        {
            RectTransform node = Kit.Node(root, "Purchase");
            layout.PlaceOnScreen(node, layout.Get("purchase"));
            background = UiArt.Paint(node.gameObject, Art("purchase_background"), raycast: true);
            Button button = UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
            UiArt.Layer(node, "Icon", Art("purchase_icon"));
            Vector4 icon = UiArt.AlphaBounds(ArtPath("purchase_icon"));
            price = UiArt.Slot(node, "Price", "0", PurchasePriceSize, new Vector2(icon.z + PriceGap, 0.12f), new Vector2(PriceEdge, 0.88f));
            return button;
        }

        // ---------------------------------------------------------------- list (the costume editor's grid recipe)

        /// <summary>Scroll list over the two-column card area: the cell size and gaps are the art's own item rects.</summary>
        private static ScrollRect Items(RectTransform wardrobe, UiArt.Layout layout, out RectTransform content)
        {
            UiArt.LayoutComponent first = layout.Get("item_ivory");
            UiArt.LayoutComponent right = layout.Get("item_red");
            UiArt.LayoutComponent below = layout.Get("item_black");
            float s = layout.Scale;
            float gapX = right.rect[0] - (first.rect[0] + first.rect[2]);
            float gapY = below.rect[1] - (first.rect[1] + first.rect[3]);
            Vector4 all = layout.Bounds("item_ivory", "item_red", "item_black", "item_gray", "item_blue", "item_green", "item_yellow", "item_purple");
            int rows = Mathf.RoundToInt((all.w - all.y + gapY) / (first.rect[3] + gapY));

            var viewportRect = new UiArt.LayoutComponent
            {
                rect = new[]
                {
                    first.rect[0], first.rect[1],
                    Columns * first.rect[2] + Mathf.RoundToInt((Columns - 1) * gapX),
                    rows * first.rect[3] + Mathf.RoundToInt((rows - 1) * gapY)
                }
            };

            RectTransform list = Kit.Node(wardrobe, "Items");
            layout.PlaceOnScreen(list, viewportRect);
            Kit.Fill(list.gameObject, new Color(1f, 1f, 1f, 0f), raycast: true); // drag area: empty space between cards must still scroll
            var scroll = list.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = Kit.Stretch(Kit.Node(list, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Kit.Place(Kit.Node(viewport, "Content"), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(first.rect[2], first.rect[3]) * s;
            grid.spacing = new Vector2(gapX, gapY) * s;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            return scroll;
        }

        private static ScrollIndicatorView ScrollTrack(RectTransform wardrobe, UiArt.Layout layout, ScrollRect scroll)
        {
            UiArt.LayoutComponent component = layout.Get("clothes_scroll");
            RectTransform track = Kit.Node(wardrobe, "ScrollTrack");
            layout.PlaceOnScreen(track, component);
            UiArt.Paint(track.gameObject, Art("clothes_scroll_empty"), raycast: false);
            var view = track.gameObject.AddComponent<ScrollIndicatorView>();

            Vector2 size = new Vector2(component.rect[2], component.rect[3]) * layout.Scale;
            RectTransform handle = MovingPart(track, "Handle", "clothes_scroll_handle", size);

            Vector4 trackBounds = UiArt.AlphaBounds(ArtPath("clothes_scroll_empty"));
            Vector4 handleBounds = UiArt.AlphaBounds(ArtPath("clothes_scroll_handle"));
            float half = (handleBounds.w - handleBounds.y) * 0.5f;
            float margin = trackBounds.w - handleBounds.w;

            var so = new SerializedObject(view);
            Ref(so, "scroll", scroll);
            Ref(so, "handle", handle);
            Find(so, "topAnchorY").floatValue = handleBounds.w - half;
            Find(so, "bottomAnchorY").floatValue = trackBounds.y + margin + half;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>A part that moves at runtime: a node anchored at one point, sized to the alpha bounds of its art, the art stretched back over the canvas.</summary>
        private static RectTransform MovingPart(Transform parent, string name, string art, Vector2 canvasSize)
        {
            Vector4 b = UiArt.AlphaBounds(ArtPath(art));
            var center = new Vector2((b.x + b.z) * 0.5f, (b.y + b.w) * 0.5f);
            RectTransform node = Kit.Node(parent, name);
            Kit.Place(node, center, center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2((b.z - b.x) * canvasSize.x, (b.w - b.y) * canvasSize.y));
            UiArt.CanvasLayer(node, "Art", Art(art), b);
            return node;
        }

        // ---------------------------------------------------------------- helpers

        private static string ArtPath(string file) => ArtFolder + "/" + file + ".png";

        private static Sprite Art(string file)
        {
            string path = ArtPath(file);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiEmotePrefabs: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiEmotePrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            return property;
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException($"UiEmotePrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            Find(so, field).objectReferenceValue = value;
        }
    }
}
