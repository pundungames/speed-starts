using System;
using System.IO;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Costume;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The costume editor ("Edit racer") prefabs dressed with the designer's art (UI/Art/Costume): the screen
    /// (CosmeticsScreen.prefab: resource boxes, colour panel, stats, Random / Save, purchase button, wardrobe panel with
    /// the preset drop-down, nine tabs, item grid and scroll track), the item card and the preset row. Component rects
    /// come from Costume/layout.json (reference 1678x937 -> canvas); layers of one component share its art canvas at 100%
    /// scale (transparent edges are not cropped). Parts that move at runtime (colour cursor, hue handle, chevron, scroll
    /// handle) are nodes sized to the alpha bounds of their art, with the art layer stretched back out to the whole art
    /// canvas, so they move and turn around their own centre. Dynamic content stays empty: item cards and preset rows are
    /// prefabs the views create under the content parents. Never runs in the game.
    /// </summary>
    internal static class UiCostumePrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Costume";
        public const string ItemsFolder = ArtFolder + "/Items";
        public const string ItemCardPath = UiPrefabPaths.Root + "/Rows/CostumeItemCard.prefab";
        public const string PresetRowPath = UiPrefabPaths.Root + "/Rows/CostumePresetRow.prefab";

        private const string LayoutPath = ArtFolder + "/layout.json";
        private const string SampleItemArt = "Items/Tops/T01"; // shown on the card prefab so the picture slot is visible; the view replaces it

        private const float ValueSize = 48f;
        private const float PriceSize = 56f;
        private const float PresetSize = 44f;
        private const float HeightSize = 36f;
        private const float RowLabelSize = 40f;
        private const int Columns = 2;

        // Text slots as fractions of their art (min, max; y from the bottom).
        private static readonly Vector2 ValueMin = new Vector2(0.06f, 0.05f);
        private const float ValueTop = 0.04f; // gap between the value text and the icon above it (fraction of the box)
        private static readonly Vector2 PresetLabelMin = new Vector2(0.04f, 0.10f), PresetLabelMax = new Vector2(0.85f, 0.90f);
        private static readonly Vector2 RowLabelMin = new Vector2(0.05f, 0.10f), RowLabelMax = new Vector2(0.95f, 0.90f);
        private static readonly Vector2 ItemIconMin = new Vector2(0.06f, 0.06f), ItemIconMax = new Vector2(0.94f, 0.94f);
        private const float PriceGap = 0.03f; // between the gem and the price text
        private const float PriceEdge = 0.94f;
        private const float HeightInset = 0.03f;
        private const float TabLabelSize = 34f;
        private const float TitleSize = 40f;
        private const float StatLabelSize = 28f;
        private const float ActionLabelSize = 44f;
        private const float LabelGap = 0.03f;      // between an icon and its label (fraction of the component)
        private const float TabLabelRight = 0.96f;
        private const float TitleInset = 0.06f;
        private static readonly Color LabelWhite = Color.white;

        // Colour boxes under the colour square (placeholder look until the designer draws a selected frame): up to three
        // boxes (an item has 1-3 colour channels), side by side from the swatch frame's own spot; the selected one gets
        // a ring of this colour (the red of the selected item card) that sticks out of the frame by HighlightMargin.
        private const int ChannelBoxCount = CosmeticSlot.MaxChannels;
        private const float ChannelBoxGap = 0.03f;       // between boxes, fraction of the panel width
        private const float HighlightMargin = 0.14f;     // ring thickness, fraction of the box
        private static readonly Color HighlightColor = new Color32(255, 32, 45, 255);

        private static readonly (CostumeTab tab, string stem)[] TabArt =
        {
            (CostumeTab.Face, "category_face"),
            (CostumeTab.Head, "category_head"),
            (CostumeTab.Body, "category_body"),
            (CostumeTab.Shorts, "category_shorts"),
            (CostumeTab.Leggings, "category_leggings"),
            (CostumeTab.Hands, "category_hands"),
            (CostumeTab.Shoes, "category_shoes"),
            (CostumeTab.Socks, "category_socks"),
            (CostumeTab.Emote, "category_emote"),
        };

        /// <summary>
        /// Builds the item card, the preset row and the screen through the builder's own save routine (kept as it is when
        /// the prefab exists and overwrite is off). The config asset is seeded first (never overwritten here).
        /// </summary>
        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            CostumeEditorConfig config = CostumeConfigSeed.Seed(overwrite: false);
            UiArt.Layout layout = ReadLayout();

            GameObject card = build(ItemCardPath, root => ItemCard(root, layout));
            GameObject presetRow = build(PresetRowPath, root => PresetRow(root, layout));
            var cardView = UiViewBinder.RowComponent<CostumeItemCardView>(card, ItemCardPath);
            var rowView = UiViewBinder.RowComponent<CostumePresetRowView>(presetRow, PresetRowPath);
            build(UiPrefabPaths.CosmeticsScreen, root => BuildScreen(root, layout, config, cardView, rowView));
        }

        // ---------------------------------------------------------------- item card and preset row

        private static void ItemCard(RectTransform root, UiArt.Layout layout)
        {
            LayoutSize(root, layout.Get("item_ivory"), layout.Scale);
            var view = root.gameObject.AddComponent<CostumeItemCardView>();

            Image hit = HitArea(root.gameObject);
            Button button = UiArt.MakeButton(root.gameObject, hit, true, alphaHit: false);

            Image normal = UiArt.Layer(root, "Normal", Art("item_card_normal_background"));
            Image selected = UiArt.Layer(root, "Selected", Art("item_card_selected_background"));
            selected.gameObject.SetActive(false);

            RectTransform iconNode = Kit.Span(Kit.Node(root, "Icon"), ItemIconMin, ItemIconMax);
            Image icon = UiArt.Paint(iconNode.gameObject, Art(SampleItemArt), raycast: false);
            icon.preserveAspect = true;

            GameObject lockOverlay = UiArt.Layer(root, "Lock", Art("item_locked_icon")).gameObject;
            GameObject check = UiArt.Layer(root, "Check", Art("item_selected_check")).gameObject;
            lockOverlay.SetActive(false);
            check.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "normalBackground", normal);
            Ref(so, "selectedBackground", selected);
            Ref(so, "icon", icon);
            Ref(so, "check", check);
            Ref(so, "lockOverlay", lockOverlay);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PresetRow(RectTransform root, UiArt.Layout layout)
        {
            Sprite normalArt = Art("custom_dropdown_row_normal");
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, SpriteSize(normalArt, layout.Scale));
            Kit.PreferredHeight(root.gameObject, SpriteSize(normalArt, layout.Scale).y);
            var view = root.gameObject.AddComponent<CostumePresetRowView>();

            Image hit = HitArea(root.gameObject);
            Button button = UiArt.MakeButton(root.gameObject, hit, true, alphaHit: false);

            UiArt.Layer(root, "Normal", normalArt);
            GameObject selected = UiArt.Layer(root, "Selected", Art("custom_dropdown_row_selected")).gameObject;
            selected.SetActive(false);
            Text label = UiArt.Slot(root, "Label", "Custom 1", RowLabelSize, RowLabelMin, RowLabelMax, TextAnchor.MiddleLeft);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "selectedBackground", selected);
            Ref(so, "label", label);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- screen

        private static void BuildScreen(RectTransform root, UiArt.Layout layout, CostumeEditorConfig config,
            CostumeItemCardView cardPrefab, CostumePresetRowView rowPrefab)
        {
            if (config == null) throw new InvalidOperationException("UiCostumePrefabs: CostumeEditorConfig asset is missing (CostumeConfigSeed).");
            var view = root.gameObject.AddComponent<CostumeEditorScreenView>();

            // Tap catcher only: nothing reaches the scene behind the screen. The dimming itself is a scene object BEHIND the
            // 3D character (an overlay layer here would darken the character too), bound to the view's dimBackdrop.
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            HitArea(dim.gameObject);

            Button back = BackButton(root, layout);
            CostumeResourceView resources = ResourceColumn(root, layout, config);
            CostumeColorPickerView picker = ColorPanel(root, layout);
            Stats(root, layout, out Image powerFill, out Image staminaFill, out Text heightText, out Vector2 barSpan);
            Button random = ActionButton(root, layout, "Random", "random", CostumeTexts.Random, UiArt.Navy, out _);
            Button save = ActionButton(root, layout, "Save", "save", CostumeTexts.Save, LabelWhite, out Image saveBackground);
            Button purchase = PurchaseButton(root, layout, out Text price, out Image purchaseBackground);

            RectTransform wardrobe = Kit.Stretch(Kit.Node(root, "Wardrobe"));
            RectTransform panel = Kit.Node(wardrobe, "Panel");
            layout.PlaceOnScreen(panel, layout.Get("wardrobe"));
            UiArt.Paint(panel.gameObject, Art("wardrobe_background"), raycast: true);

            CostumeTabView[] tabs = Tabs(wardrobe, layout);
            ScrollRect itemsScroll = Items(wardrobe, layout, out RectTransform content);
            ScrollIndicatorView indicator = ScrollTrack(wardrobe, layout, itemsScroll);
            CostumePresetDropdownView dropdown = PresetDropdown(wardrobe, layout, rowPrefab);

            // ---- bind the view (all objects are inside this prefab; dimBackdrop is a scene object: the scene builder binds it)
            var so = new SerializedObject(view);
            Ref(so, "config", config);
            Ref(so, "backButton", back);
            Ref(so, "presetDropdown", dropdown);
            SerializedProperty tabList = Find(so, "tabs");
            tabList.arraySize = tabs.Length;
            for (int i = 0; i < tabs.Length; i++) tabList.GetArrayElementAtIndex(i).objectReferenceValue = tabs[i];
            Ref(so, "itemsScroll", itemsScroll);
            Ref(so, "itemsContent", content);
            Ref(so, "cardPrefab", cardPrefab);
            Ref(so, "scrollIndicator", indicator);
            Ref(so, "colorPicker", picker);
            Ref(so, "resources", resources);
            Ref(so, "randomButton", random);
            Ref(so, "saveButton", save);
            Ref(so, "saveBackground", saveBackground);
            Ref(so, "purchaseButton", purchase);
            Ref(so, "purchasePriceText", price);
            Ref(so, "purchaseBackground", purchaseBackground);
            Ref(so, "powerFill", powerFill);
            Ref(so, "staminaFill", staminaFill);
            Ref(so, "heightText", heightText);
            Find(so, "statBarSpan").vector2Value = barSpan;
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

        // ---------------------------------------------------------------- left side

        private static CostumeResourceView ResourceColumn(RectTransform root, UiArt.Layout layout, CostumeEditorConfig config)
        {
            RectTransform group = Kit.Stretch(Kit.Node(root, "Resources"));
            var view = group.gameObject.AddComponent<CostumeResourceView>();

            Text gem = ResourceBox(group, layout, "PurpleGem", "purple_gem");
            Text entry = ResourceBox(group, layout, "EntryTicket", "entry_ticket");
            Text timed = ResourceBox(group, layout, "TimedTicket", "timed_ticket");
            Text cash = ResourceBox(group, layout, "Cash", "diamond");

            var so = new SerializedObject(view);
            Ref(so, "purpleGemText", gem);
            Ref(so, "entryTicketText", entry);
            Ref(so, "timedTicketText", timed);
            Ref(so, "cashText", cash);
            Ref(so, "config", config);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>One resource box: its art, and the value text under the icon.</summary>
        private static Text ResourceBox(RectTransform parent, UiArt.Layout layout, string nodeName, string artName)
        {
            RectTransform box = Kit.Node(parent, nodeName);
            layout.PlaceOnScreen(box, layout.Get(artName));
            UiArt.Paint(box.gameObject, Art(artName + "_background"), raycast: false);
            UiArt.Layer(box, "Icon", Art(artName + "_icon"));
            Vector4 icon = AlphaBounds(artName + "_icon");
            return UiArt.Slot(box, "Value", "0", ValueSize, ValueMin, new Vector2(1f - ValueMin.x, icon.y - ValueTop));
        }

        private static CostumeColorPickerView ColorPanel(RectTransform root, UiArt.Layout layout)
        {
            UiArt.LayoutComponent component = layout.Get("color_panel");
            RectTransform panel = Kit.Node(root, "ColorPanel");
            layout.PlaceOnScreen(panel, component);
            var view = panel.gameObject.AddComponent<CostumeColorPickerView>();
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            Vector2 panelSize = new Vector2(component.rect[2], component.rect[3]) * layout.Scale;

            UiArt.Paint(panel.gameObject, Art("color_panel_background"), raycast: true);

            // The frame art is a solid navy plate (outer shape + border): it goes UNDER the gradient layers, which are smaller.
            UiArt.Layer(panel, "SvFrame", Art("color_sv_frame"));
            UiArt.Layer(panel, "HueFrame", Art("color_hue_frame"));

            // Square: invisible tap area over the inner area, the flat hue colour inside it, then the two overlays and the frame.
            Vector4 sv = AlphaBounds("color_sv_red_example");
            RectTransform svArea = Kit.Span(Kit.Node(panel, "SvArea"), new Vector2(sv.x, sv.y), new Vector2(sv.z, sv.w));
            HitArea(svArea.gameObject);
            ColorDragArea svDrag = svArea.gameObject.AddComponent<ColorDragArea>();
            Image hueBase = Kit.Fill(Kit.Stretch(Kit.Node(svArea, "Base")).gameObject, Color.red, raycast: false);
            UiArt.Layer(panel, "SvWhite", Art("color_sv_white_overlay"));
            UiArt.Layer(panel, "SvBlack", Art("color_sv_black_overlay"));

            // Title in the band above the square (from the panel top down to the square's top edge).
            UiArt.Slot(panel, "Title", CostumeTexts.ColorTitle, TitleSize, new Vector2(TitleInset, sv.w), new Vector2(1f - TitleInset, 0.98f),
                TextAnchor.MiddleLeft);

            // Hue strip.
            Vector4 hue = AlphaBounds("color_hue_spectrum");
            RectTransform hueArea = Kit.Span(Kit.Node(panel, "HueArea"), new Vector2(hue.x, hue.y), new Vector2(hue.z, hue.w));
            HitArea(hueArea.gameObject);
            ColorDragArea hueDrag = hueArea.gameObject.AddComponent<ColorDragArea>();
            UiArt.Layer(panel, "HueSpectrum", Art("color_hue_spectrum"));

            // Colour boxes: one per colour channel of the worn item; the view shows as many as the item has. Box 1 sits where the
            // single swatch used to; the rest follow to its right, still inside the panel.
            Vector4 frame = AlphaBounds("color_swatch_frame");
            Vector4 inner = AlphaBounds("color_swatch_red_example");
            float pitch = frame.z - frame.x + ChannelBoxGap;
            if (frame.z + (ChannelBoxCount - 1) * pitch > 1f)
                throw new InvalidOperationException("UiCostumePrefabs: the colour boxes do not fit in the colour panel.");
            RectTransform boxes = Kit.Stretch(Kit.Node(panel, "Channels"));
            var boxViews = new CostumeChannelBoxView[ChannelBoxCount];
            for (int i = 0; i < ChannelBoxCount; i++) boxViews[i] = ChannelBox(boxes, i, frame, inner, pitch * i);

            RectTransform cursor = MovingPart(panel, "SvCursor", "color_sv_cursor", panelSize);
            RectTransform handle = MovingPart(panel, "HueHandle", "color_hue_handle", panelSize);

            var so = new SerializedObject(view);
            Ref(so, "svArea", svDrag);
            Ref(so, "hueArea", hueDrag);
            Ref(so, "hueBase", hueBase);
            Ref(so, "svCursor", cursor);
            Ref(so, "hueHandle", handle);
            SerializedProperty boxList = Find(so, "channelBoxes");
            boxList.arraySize = boxViews.Length;
            for (int i = 0; i < boxViews.Length; i++) boxList.GetArrayElementAtIndex(i).objectReferenceValue = boxViews[i];
            Ref(so, "group", group);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// One colour box: a button over the swatch frame's rect (shifted right by <paramref name="shift"/>), a ring behind it
        /// that shows while selected, the flat colour inside, and the frame art on top (its border covers the colour's corners).
        /// </summary>
        private static CostumeChannelBoxView ChannelBox(RectTransform parent, int index, Vector4 frame, Vector4 inner, float shift)
        {
            float w = frame.z - frame.x;
            float h = frame.w - frame.y;
            RectTransform node = Kit.Node(parent, "Channel" + (index + 1));
            Kit.Span(node, new Vector2(frame.x + shift, frame.y), new Vector2(frame.z + shift, frame.w));
            var view = node.gameObject.AddComponent<CostumeChannelBoxView>();
            Image hit = HitArea(node.gameObject);
            Button button = UiArt.MakeButton(node.gameObject, hit, true, alphaHit: false);

            RectTransform ring = Kit.Span(Kit.Node(node, "Selected"), new Vector2(-HighlightMargin, -HighlightMargin),
                new Vector2(1f + HighlightMargin, 1f + HighlightMargin));
            Kit.Fill(ring.gameObject, HighlightColor, raycast: false);
            ring.gameObject.SetActive(false);

            RectTransform colorNode = Kit.Span(Kit.Node(node, "Color"), new Vector2((inner.x - frame.x) / w, (inner.y - frame.y) / h),
                new Vector2((inner.z - frame.x) / w, (inner.w - frame.y) / h));
            Image color = Kit.Fill(colorNode.gameObject, Color.white, raycast: false);
            CanvasLayer(node, "Frame", Art("color_swatch_frame"), frame);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "colorImage", color);
            Ref(so, "selectedHighlight", ring.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void Stats(RectTransform root, UiArt.Layout layout, out Image powerFill, out Image staminaFill,
            out Text heightText, out Vector2 barSpan)
        {
            RectTransform stats = Kit.Node(root, "Stats");
            layout.PlaceOnScreen(stats, layout.Get("stats"));
            UiArt.Paint(stats.gameObject, Art("stats_background"), raycast: false);
            UiArt.Layer(stats, "PowerIcon", Art("stats_power_icon"));
            UiArt.Layer(stats, "StaminaIcon", Art("stats_stamina_icon"));
            UiArt.Layer(stats, "HeightIcon", Art("stats_height_icon"));
            UiArt.Layer(stats, "PowerEmpty", Art("stats_power_empty"));
            UiArt.Layer(stats, "StaminaEmpty", Art("stats_stamina_empty"));
            UiArt.Layer(stats, "HeightField", Art("stats_height_field"));

            // The fill art is full on the whole stats canvas; a horizontal fill amount cuts it, and the view maps 0..1 of the
            // bar to the part of the canvas width the bar covers (the transparent left margin is cut with it).
            powerFill = FillLayer(stats, "PowerFill", "stats_power_fill");
            staminaFill = FillLayer(stats, "StaminaFill", "stats_stamina_fill");

            Vector4 power = AlphaBounds("stats_power_fill");
            Vector4 stamina = AlphaBounds("stats_stamina_fill");
            if (Mathf.Abs(power.x - stamina.x) > 0.002f || Mathf.Abs(power.z - stamina.z) > 0.002f)
                throw new InvalidOperationException("UiCostumePrefabs: the power and stamina bars do not span the same width; the view has one bar span.");
            barSpan = new Vector2(power.x, power.z);

            // Power / Stamina names: between the row icon and the bar (rows are the bar's own height band).
            StatLabel(stats, "PowerLabel", CostumeTexts.Power, AlphaBounds("stats_power_icon"), power);
            StatLabel(stats, "StaminaLabel", CostumeTexts.Stamina, AlphaBounds("stats_stamina_icon"), stamina);

            Vector4 field = AlphaBounds("stats_height_field");
            heightText = UiArt.Slot(stats, "HeightLabel", "Height", HeightSize,
                new Vector2(field.x + HeightInset, field.y), new Vector2(field.z - HeightInset, field.w));
        }

        private static void StatLabel(Transform stats, string name, string text, Vector4 icon, Vector4 bar)
        {
            UiArt.Slot(stats, name, text, StatLabelSize, new Vector2(icon.z + LabelGap * 0.5f, bar.y),
                new Vector2(bar.x - LabelGap * 0.5f, bar.w), TextAnchor.MiddleLeft);
        }

        private static Image FillLayer(Transform parent, string name, string art)
        {
            Image image = UiArt.Layer(parent, name, Art(art));
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
            return image;
        }

        private static Button ActionButton(RectTransform root, UiArt.Layout layout, string nodeName, string artName, string text, Color textColor,
            out Image background)
        {
            RectTransform node = Kit.Node(root, nodeName);
            layout.PlaceOnScreen(node, layout.Get(artName));
            background = UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: true);
            UiArt.Layer(node, "Icon", Art(artName + "_icon"));
            Vector4 icon = AlphaBounds(artName + "_icon");
            UiArt.Slot(node, "Label", text, ActionLabelSize, new Vector2(icon.z + LabelGap, 0.12f), new Vector2(0.96f, 0.88f),
                TextAnchor.MiddleLeft, textColor);
            return UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
        }

        private static Button PurchaseButton(RectTransform root, UiArt.Layout layout, out Text price, out Image background)
        {
            RectTransform node = Kit.Node(root, "Purchase");
            layout.PlaceOnScreen(node, layout.Get("purchase"));
            background = UiArt.Paint(node.gameObject, Art("purchase_background"), raycast: true);
            Button button = UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
            UiArt.Layer(node, "Icon", Art("purchase_icon"));
            Vector4 icon = AlphaBounds("purchase_icon");
            price = UiArt.Slot(node, "Price", "0", PriceSize, new Vector2(icon.z + PriceGap, 0.12f), new Vector2(PriceEdge, 0.88f));
            return button;
        }

        // ---------------------------------------------------------------- wardrobe

        private static CostumeTabView[] Tabs(RectTransform wardrobe, UiArt.Layout layout)
        {
            RectTransform group = Kit.Stretch(Kit.Node(wardrobe, "Tabs"));
            var tabs = new CostumeTabView[TabArt.Length];
            for (int i = 0; i < TabArt.Length; i++) tabs[i] = Tab(group, layout, TabArt[i].tab, TabArt[i].stem);
            return tabs;
        }

        private static CostumeTabView Tab(RectTransform parent, UiArt.Layout layout, CostumeTab tab, string stem)
        {
            RectTransform node = Kit.Node(parent, tab.ToString());
            layout.PlaceOnScreen(node, layout.Get(stem));
            Image background = UiArt.Paint(node.gameObject, Art(stem + "_normal_background"), raycast: true);
            Button button = UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);
            var view = node.gameObject.AddComponent<CostumeTabView>();

            // Body has two icons (navy normal, white selected); every other tab one icon that works on both backgrounds.
            bool twoIcons = tab == CostumeTab.Body;
            GameObject icon = null;
            if (twoIcons) icon = UiArt.Layer(node, "Icon", Art(stem + "_normal_icon")).gameObject;

            GameObject selected = UiArt.Layer(node, "Selected", Art(stem + "_selected_background")).gameObject;
            selected.SetActive(false);

            GameObject selectedIcon = null;
            if (twoIcons)
            {
                selectedIcon = UiArt.Layer(node, "IconSelected", Art(stem + "_icon")).gameObject;
                selectedIcon.SetActive(false);
            }
            else
            {
                icon = UiArt.Layer(node, "Icon", Art(stem + "_icon")).gameObject;
            }

            // Name right of the icon, drawn last so it is above every layer.
            Vector4 tabIcon = AlphaBounds(stem + (twoIcons ? "_normal_icon" : "_icon"));
            Text tabLabel = UiArt.Slot(node, "Label", CostumeTexts.TabName(tab), TabLabelSize,
                new Vector2(tabIcon.z + LabelGap, 0.1f), new Vector2(TabLabelRight, 0.9f), TextAnchor.MiddleLeft);

            var so = new SerializedObject(view);
            Find(so, "tab").intValue = (int)tab;
            Ref(so, "label", tabLabel);
            Ref(so, "button", button);
            Ref(so, "selectedBackground", selected);
            Ref(so, "icon", icon);
            if (selectedIcon != null) Ref(so, "selectedIcon", selectedIcon);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

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
            int rows = Mathf.RoundToInt((all.w - all.y + gapY) / (first.rect[3] + gapY)); // visible card rows of the art

            // Viewport = Columns cards wide and the visible rows tall.
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
            HitArea(list.gameObject); // drag area: empty space between cards must still scroll
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

        /// <summary>The thin track beside the grid and its handle (a position indicator that follows the list).</summary>
        private static ScrollIndicatorView ScrollTrack(RectTransform wardrobe, UiArt.Layout layout, ScrollRect scroll)
        {
            UiArt.LayoutComponent component = layout.Get("clothes_scroll");
            RectTransform track = Kit.Node(wardrobe, "ScrollTrack");
            layout.PlaceOnScreen(track, component);
            UiArt.Paint(track.gameObject, Art("clothes_scroll_empty"), raycast: false);
            var view = track.gameObject.AddComponent<ScrollIndicatorView>();

            Vector2 size = new Vector2(component.rect[2], component.rect[3]) * layout.Scale;
            RectTransform handle = MovingPart(track, "Handle", "clothes_scroll_handle", size);

            // The handle travels from the top of the track to the bottom of it, keeping the art's own margin at both ends.
            Vector4 trackBounds = AlphaBounds("clothes_scroll_empty");
            Vector4 handleBounds = AlphaBounds("clothes_scroll_handle");
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

        private static CostumePresetDropdownView PresetDropdown(RectTransform wardrobe, UiArt.Layout layout, CostumePresetRowView rowPrefab)
        {
            RectTransform group = Kit.Stretch(Kit.Node(wardrobe, "Presets"));
            var view = group.gameObject.AddComponent<CostumePresetDropdownView>();
            UiArt.LayoutComponent barRect = layout.Get("custom_dropdown");

            RectTransform bar = Kit.Node(group, "Bar");
            layout.PlaceOnScreen(bar, barRect);
            Image barImage = UiArt.Paint(bar.gameObject, Art("custom_dropdown_background"), raycast: true);
            Button barButton = UiArt.MakeButton(bar.gameObject, barImage, true, alphaHit: false);
            Vector4 chevronBounds = AlphaBounds("custom_dropdown_chevron");
            RectTransform chevron = MovingPart(bar, "Chevron", "custom_dropdown_chevron", new Vector2(barRect.rect[2], barRect.rect[3]) * layout.Scale);
            Text barLabel = UiArt.Slot(bar, "Label", "Custom 1", PresetSize, PresetLabelMin,
                new Vector2(Mathf.Min(PresetLabelMax.x, chevronBounds.x - PriceGap), PresetLabelMax.y), TextAnchor.MiddleLeft);

            // Open list: right under the closed bar, its rows inset by the art's own padding; starts closed.
            Sprite listArt = Art("custom_dropdown_list_background");
            Vector2 listSize = SpriteSize(listArt, 1f); // reference pixels
            Sprite rowArt = Art("custom_dropdown_row_normal");
            Vector2 rowSize = SpriteSize(rowArt, 1f);
            var listRect = new UiArt.LayoutComponent
            {
                rect = new[] { barRect.rect[0], barRect.rect[1] + barRect.rect[3], Mathf.RoundToInt(listSize.x), Mathf.RoundToInt(listSize.y) }
            };
            RectTransform list = Kit.Node(group, "List");
            layout.PlaceOnScreen(list, listRect);
            UiArt.Paint(list.gameObject, listArt, raycast: true);

            int presets = PresetCount();
            RectTransform rows = Kit.Stretch(Kit.Node(list, "Rows"));
            var rowLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            int sidePad = Mathf.RoundToInt(Mathf.Max(0f, (listSize.x - rowSize.x) * 0.5f) * layout.Scale);
            int endPad = Mathf.RoundToInt(Mathf.Max(0f, (listSize.y - presets * rowSize.y) * 0.5f) * layout.Scale);
            rowLayout.padding = new RectOffset(sidePad, sidePad, endPad, endPad);
            rowLayout.spacing = 0f;
            rowLayout.childAlignment = TextAnchor.UpperCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            list.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "bar", barButton);
            Ref(so, "barLabel", barLabel);
            Ref(so, "chevron", chevron);
            Ref(so, "list", list.gameObject);
            Ref(so, "rowsParent", rows);
            Ref(so, "rowPrefab", rowPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>Preset count of the catalog: the list is padded for exactly that many rows.</summary>
        private static int PresetCount()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(ConfigAssetSeeder.CosmeticCatalogPath);
            if (catalog == null)
                throw new InvalidOperationException($"UiCostumePrefabs: '{ConfigAssetSeeder.CosmeticCatalogPath}' not found (ConfigAssetSeeder); the preset list size comes from it.");
            return catalog.PresetCount;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// A part that moves or turns at runtime: a node anchored at one point, sized to the alpha bounds of its art, with the
        /// art layer stretched back out over the whole art canvas. The node starts where the art draws the part.
        /// </summary>
        private static RectTransform MovingPart(Transform parent, string name, string art, Vector2 canvasSize)
        {
            Vector4 b = AlphaBounds(art);
            var center = new Vector2((b.x + b.z) * 0.5f, (b.y + b.w) * 0.5f);
            RectTransform node = Kit.Node(parent, name);
            Kit.Place(node, center, center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2((b.z - b.x) * canvasSize.x, (b.w - b.y) * canvasSize.y));
            CanvasLayer(node, "Art", Art(art), b);
            return node;
        }

        /// <summary>
        /// A layer drawn on the whole art canvas while its parent covers only <paramref name="bounds"/> (fractions of that
        /// canvas): anchors extend past the parent by the same fractions, offsets stay zero.
        /// </summary>
        private static GameObject CanvasLayer(RectTransform parent, string name, Sprite sprite, Vector4 bounds)
        {
            float w = bounds.z - bounds.x;
            float h = bounds.w - bounds.y;
            RectTransform rt = Kit.Node(parent, name);
            Kit.Span(rt, new Vector2(-bounds.x / w, -bounds.y / h), new Vector2((1f - bounds.x) / w, (1f - bounds.y) / h));
            UiArt.Paint(rt.gameObject, sprite, raycast: false);
            return rt.gameObject;
        }

        /// <summary>Root size of a prefab that lives in a layout group: the art rect at canvas scale, centred.</summary>
        private static void LayoutSize(RectTransform root, UiArt.LayoutComponent component, float scale)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(component.rect[2], component.rect[3]) * scale);
        }

        private static Vector2 SpriteSize(Sprite sprite, float scale) => sprite.rect.size * scale;

        /// <summary>Invisible tap area (alpha 0): a Button needs a graphic that catches raycasts, the art layers do not.</summary>
        private static Image HitArea(GameObject go)
        {
            return Kit.Fill(go, new Color(1f, 1f, 1f, 0f), raycast: true);
        }

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiCostumePrefabs: sprite '{path}' not found or not imported as a Sprite.");
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
                    throw new InvalidOperationException($"UiCostumePrefabs: '{path}' could not be read.");

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
                if (maxX < 0) throw new InvalidOperationException($"UiCostumePrefabs: '{path}' is fully transparent.");
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
            if (!File.Exists(path)) throw new InvalidOperationException($"UiCostumePrefabs: '{LayoutPath}' not found.");
            var file = JsonUtility.FromJson<UiArt.LayoutFile>(File.ReadAllText(path));
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null)
                throw new InvalidOperationException($"UiCostumePrefabs: '{LayoutPath}' could not be read.");
            return new UiArt.Layout(file);
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiCostumePrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            return property;
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException($"UiCostumePrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            Find(so, field).objectReferenceValue = value;
        }
    }
}
