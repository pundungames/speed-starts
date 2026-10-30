using System;
using System.Collections.Generic;
using System.IO;
using SpeedStars.UI.Home;
using SpeedStars.UI.Leaderboard;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The leaderboard screen dressed with the designer's art (UI/Art/Leaderboard, package Leaderboard_UI_v1): the HighScoresScreen
    /// prefab and the three prefabs it instantiates (table row, country row, distance option). Every placement is in the
    /// art's reference pixels (the approved 1678x937 drawings, see layout.json canvases) times <see cref="S"/> to the 1080 high
    /// canvas; art layers sit on their own canvas at 100% (the layer node has the canvas size), so nothing is trimmed.
    /// The three windows (region, period, event) are popup objects inside the screen prefab (inactive); the view binds them.
    /// Never runs in the game.
    /// </summary>
    internal static class UiLeaderboardPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Leaderboard";
        public const string RowPath = UiPrefabPaths.Root + "/Rows/LeaderboardRow.prefab";
        public const string CountryRowPath = UiPrefabPaths.Root + "/Rows/LeaderboardCountryRow.prefab";
        public const string DistanceOptionPath = UiPrefabPaths.Root + "/Rows/LeaderboardDistanceOption.prefab";

        private const float RefHeight = 937f;
        private const float RefWidth = 1678f;
        private static readonly float S = UiArt.CanvasHeight / RefHeight;

        private const string TrackTight = "scrollbar_track_tight";
        private const string ThumbTight = "scrollbar_thumb_tight";

        // Text sizes in canvas points; best fit shrinks them to their slot.
        private const float TitleSize = 72f, HeaderSize = 34f, RowSize = 40f, FilterSize = 40f, WindowTitleSize = 44f;
        private const float ListSize = 36f, ButtonSize = 38f, StatusSize = 42f, HintSize = 30f, CashSize = 54f;

        // Screen geometry (reference px, top-left origin): the table panel and what sits on it.
        private const float TableX = 176f, TableY = 140f, TableW = 1468f, TableH = 760f;
        private const float RowW = 1400f, RowH = 58f, RowInset = 34f;
        private const float ListY = 165f, ListH = 443f;
        private const float YouY = 618f, FindMeY = 680f;

        // Row text slots as fractions of the 1400 px row (x from the left).
        private static readonly Vector2 RankMin = new Vector2(0.029f, 0.08f), RankMax = new Vector2(0.100f, 0.92f);
        private static readonly Vector2 NameMin = new Vector2(0.223f, 0.08f), NameMax = new Vector2(0.571f, 0.92f);
        private static readonly Vector2 TimeMin = new Vector2(0.579f, 0.08f), TimeMax = new Vector2(0.714f, 0.92f);
        private static readonly Vector2 DateMin = new Vector2(0.786f, 0.08f), DateMax = new Vector2(0.929f, 0.92f);

        private static readonly Color Grey = new Color(0.5f, 0.55f, 0.65f, 1f);
        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        // ---------------------------------------------------------------- entry

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build, UnityEngine.SceneManagement.Scene scene,
            EventSelectLayout eventLayout)
        {
            EnsureArt();
            GameObject rowGo = build(RowPath, Row);
            GameObject countryGo = build(CountryRowPath, CountryRow);
            GameObject distanceGo = build(DistanceOptionPath, DistanceOption);

            var rowView = UiViewBinder.RowComponent<LeaderboardRowView>(rowGo, RowPath);
            var countryView = UiViewBinder.RowComponent<LeaderboardChoiceView>(countryGo, CountryRowPath);
            var distanceView = UiViewBinder.RowComponent<LeaderboardChoiceView>(distanceGo, DistanceOptionPath);
            build(UiPrefabPaths.HighScoresScreen, root => BuildScreen(root, rowView, countryView, distanceView, scene, eventLayout));
        }

        // ---------------------------------------------------------------- art

        /// <summary>Crops the two scrollbar sprites to their visible pixels and sets their 9-slice borders. Idempotent.</summary>
        private static void EnsureArt()
        {
            CropToAlpha("scrollbar_track", TrackTight);
            CropToAlpha("scrollbar_thumb", ThumbTight);
            UiArt.ImportAll(); // sprite import settings for every PNG under UI/Art, the new ones included
            foreach (string file in new[] { TrackTight, ThumbTight })
            {
                int half = Mathf.Max(1, Mathf.RoundToInt(Art(file).rect.width * 0.5f) - 1);
                SetBorder(file, new Vector4(half, half, half, half));
            }
        }

        private static void SetBorder(string file, Vector4 border)
        {
            string path = ArtPath(file);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"UiLeaderboardPrefabs: '{path}' is not imported as a texture.");
            if (importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static void CropToAlpha(string source, string target)
        {
            string targetPath = ArtPath(target);
            if (File.Exists(Kit.FullPath(targetPath))) return;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(ArtPath(source)))))
                    throw new InvalidOperationException($"UiLeaderboardPrefabs: '{ArtPath(source)}' could not be read.");

                int width = texture.width, height = texture.height;
                Color32[] pixels = texture.GetPixels32();
                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[y * width + x].a == 0) continue;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
                if (maxX < 0) throw new InvalidOperationException($"UiLeaderboardPrefabs: '{source}' is fully transparent.");

                int w = maxX - minX + 1, h = maxY - minY + 1;
                var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
                cropped.SetPixels(texture.GetPixels(minX, minY, w, h));
                cropped.Apply();
                File.WriteAllBytes(Kit.FullPath(targetPath), cropped.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(cropped);
                AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ArtPath(string file) => ArtFolder + "/" + file + ".png";

        private static Sprite Art(string file)
        {
            string path = ArtPath(file);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiLeaderboardPrefabs: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        // ---------------------------------------------------------------- prefabs: row, country row, distance option

        /// <summary>Table row: layers share the 1400x58 row canvas (flag layer prepositioned by the art); the view swaps its sprite.</summary>
        private static void Row(RectTransform root)
        {
            var view = root.gameObject.AddComponent<LeaderboardRowView>();
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(RowW * S, RowH * S));

            UiArt.Layer(root, "Background", Art("ranking_row_background"));
            Image separator = UiArt.Layer(root, "Separator", Art("ranking_row_separator"));
            Image highlight = UiArt.Layer(root, "YouHighlight", Art("ranking_row_you_highlight"));
            Image flag = UiArt.Layer(root, "FlagLayer", Art("ranking_row_flag_unknown"));
            highlight.gameObject.SetActive(false);
            flag.enabled = false;

            Text rank = UiArt.Slot(root, "Rank", "1", RowSize, RankMin, RankMax);
            Text name = UiArt.Slot(root, "Name", "Name", RowSize, NameMin, NameMax, TextAnchor.MiddleLeft);
            Text time = UiArt.Slot(root, "Time", "0.00 s", RowSize, TimeMin, TimeMax);
            Text date = UiArt.Slot(root, "Date", "01 JAN 2026", RowSize, DateMin, DateMax);

            var so = new SerializedObject(view);
            Ref(so, "rankText", rank);
            Ref(so, "nameText", name);
            Ref(so, "timeText", time);
            Ref(so, "dateText", date);
            Ref(so, "flagLayer", flag);
            Ref(so, "youHighlight", highlight.gameObject);
            Ref(so, "separator", separator.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CountryRow(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(596f * S, 54f * S));
            Choice(root, "country_row_normal", "country_row_selected", "country_row_disabled", ListSize,
                new Vector2(0.12f, 0.06f), new Vector2(0.90f, 0.94f), TextAnchor.MiddleLeft, "country_row_check", flagLayer: "country_row_flag_unknown", globeLayer: "country_row_globe");
        }

        private static void DistanceOption(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226f * S, 76f * S));
            Choice(root, "distance_option_normal", "distance_option_selected", "distance_option_disabled", ListSize,
                new Vector2(0.05f, 0.08f), new Vector2(0.80f, 0.92f), TextAnchor.MiddleCenter, "distance_option_check");
        }

        /// <summary>
        /// A selectable entry on an art canvas: state background on the root Image (plain Button, no press punch: choosing a row
        /// is not a button press moment), extra layers, check, label. Returns the bound LeaderboardChoiceView.
        /// </summary>
        private static LeaderboardChoiceView Choice(RectTransform node, string normal, string selected, string disabled, float size,
            Vector2 labelMin, Vector2 labelMax, TextAnchor align, string checkLayer, string flagLayer = null, string globeLayer = null,
            string[] extraLayers = null)
        {
            var view = node.gameObject.AddComponent<LeaderboardChoiceView>();
            Image background = UiArt.Paint(node.gameObject, Art(normal), raycast: true);
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            Image flag = null;
            GameObject globe = null;
            if (flagLayer != null)
            {
                flag = UiArt.Layer(node, "FlagLayer", Art(flagLayer));
                flag.enabled = false;
            }
            if (globeLayer != null)
            {
                globe = UiArt.Layer(node, "GlobeLayer", Art(globeLayer)).gameObject;
                globe.SetActive(false);
            }
            if (extraLayers != null)
            {
                foreach (string layer in extraLayers) UiArt.Layer(node, "Icon", Art(layer));
            }
            GameObject check = null;
            if (checkLayer != null)
            {
                check = UiArt.Layer(node, "CheckMark", Art(checkLayer)).gameObject;
                check.SetActive(false);
            }
            Text label = UiArt.Slot(node, "Label", "Label", size, labelMin, labelMax, align);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "background", background);
            Ref(so, "label", label);
            Ref(so, "normalSprite", Art(normal));
            Ref(so, "selectedSprite", Art(selected));
            Ref(so, "disabledSprite", Art(disabled));
            if (check != null) Ref(so, "checkMark", check);
            if (flag != null) Ref(so, "flagLayer", flag);
            if (globe != null) Ref(so, "globeLayer", globe);
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---------------------------------------------------------------- screen

        private static void BuildScreen(RectTransform root, LeaderboardRowView rowPrefab, LeaderboardChoiceView countryPrefab,
            LeaderboardChoiceView distancePrefab, UnityEngine.SceneManagement.Scene scene, EventSelectLayout eventLayout)
        {
            var view = root.gameObject.AddComponent<LeaderboardScreenView>();

            // ---- top left: Cash box and Back
            RectTransform diamond = AtLeft(root, "Diamond", 42f, 47f, 236f, 92f);
            UiArt.Paint(diamond.gameObject, Art("diamond_background"), raycast: false);
            UiArt.Layer(diamond, "Icon", Art("diamond_icon"));
            Text cash = UiArt.Slot(diamond, "Value", "0", CashSize, new Vector2(0.42f, 0.08f), new Vector2(0.95f, 0.92f));

            RectTransform back = AtLeft(root, "Back", 45f, 153f, 80f, 80f);
            Button backButton = StateButton(back, "small_button_normal", "small_button_hover", "small_button_pressed", "small_button_disabled");
            UiArt.Layer(back, "Icon", Art("small_button_back"));

            // ---- title
            RectTransform title = AtScreen(root, "Title", 435f, 54f, 810f, 105f);
            UiArt.Paint(title.gameObject, Art("title_background"), raycast: false);
            Text titleText = UiArt.Slot(title, "Label", LeaderboardTexts.Title, TitleSize, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.90f), TextAnchor.MiddleCenter, Color.white);

            // ---- table panel
            RectTransform table = AtScreen(root, "Table", TableX, TableY, TableW, TableH);
            UiArt.Layer(table, "Background", Art("table_background"));
            UiArt.Layer(table, "Columns", Art("table_columns"));
            UiArt.Layer(table, "Footer", Art("table_footer"));
            ColumnHeader(table, LeaderboardTexts.ColumnRank, RankMin, RankMax, TextAnchor.MiddleCenter);
            ColumnHeader(table, LeaderboardTexts.ColumnPlayer, NameMin, NameMax, TextAnchor.MiddleLeft);
            ColumnHeader(table, LeaderboardTexts.ColumnTime, TimeMin, TimeMax, TextAnchor.MiddleCenter);
            ColumnHeader(table, LeaderboardTexts.ColumnDate, DateMin, DateMax, TextAnchor.MiddleCenter);

            // filter fields
            RectTransform regionField = Filter(table, "RegionField", 50f, true, out Text regionLabel, out GameObject regionGlobe, out Image regionFlag);
            RectTransform periodField = Filter(table, "PeriodField", 514f, false, out Text periodLabel, out _, out _);
            RectTransform eventField = Filter(table, "EventField", 978f, false, out Text eventLabel, out _, out _);

            // list
            RectTransform list = At(table, "List", RowInset, ListY, RowW, ListH);
            ScrollRect scroll = Scroll(list, 0f, out RectTransform content, out RectTransform viewport);
            ScrollbarOn(table, scroll, "Scrollbar", RowInset + RowW - 26f, ListY + 6f, ListH - 12f);

            // status: one 600x240 art canvas centred on the list
            RectTransform status = At(table, "Status", RowInset + RowW * 0.5f - 300f, ListY + ListH * 0.5f - 120f, 600f, 240f);
            RectTransform loading = Group(status, "Loading");
            RectTransform spinner = Kit.Node(loading, "Spinner");
            SpinnerNode(spinner, "status_loading");
            UiArt.Slot(loading, "Label", LeaderboardTexts.Loading, StatusSize, new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.52f));

            RectTransform empty = Group(status, "Empty");
            UiArt.Layer(empty, "Icon", Art("status_empty"));
            UiArt.Slot(empty, "Title", LeaderboardTexts.EmptyTitle, StatusSize, new Vector2(0.03f, 0.33f), new Vector2(0.97f, 0.55f));
            UiArt.Slot(empty, "Hint", LeaderboardTexts.EmptyHint, HintSize, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.31f));
            empty.gameObject.SetActive(false);

            RectTransform error = Group(status, "Error");
            UiArt.Layer(error, "Icon", Art("status_warning"));
            UiArt.Slot(error, "Title", LeaderboardTexts.ErrorTitle, StatusSize, new Vector2(0f, 0.43f), new Vector2(1f, 0.60f));
            RectTransform retry = At(error, "Retry", 160f, 150f, 280f, 68f);
            Button retryButton = ActionButton(retry, "action_button_selected", "action_button_selected", "action_button_selected", "action_button_disabled",
                LeaderboardTexts.Retry, Color.white, out Text retryLabel);
            error.gameObject.SetActive(false);

            RectTransform more = At(table, "LoadMore", RowInset + RowW * 0.5f - 300f, ListY + ListH - 88f, 600f, 240f);
            RectTransform moreSpin = Kit.Node(more, "Spinner");
            SpinnerNode(moreSpin, "status_loading");
            more.localScale = new Vector3(0.6f, 0.6f, 1f);
            SetPivotKeepRect(more, new Vector2(0.5f, 1f - 65f / 240f));
            more.gameObject.SetActive(false);

            // YOU line + FIND ME
            RectTransform you = Kit.Nest(rowPrefab.gameObject, table, "YouRow", scene);
            PlaceTopLeft(you, RowInset, YouY, RowW, RowH);
            Kit.Record(you);
            var youView = you.GetComponent<LeaderboardRowView>();
            RectTransform find = At(table, "FindMe", RowInset, FindMeY, 280f, 68f);
            Button findButton = ActionButton(find, "action_button_normal", "action_button_hover", "action_button_pressed", "action_button_disabled",
                LeaderboardTexts.FindMe, UiArt.Navy, out Text findLabel);

            // ---- windows (popups of the navigator, inactive)
            Windows(root, out WindowRefs w);

            // ---- bind the view
            var so = new SerializedObject(view);
            Ref(so, "closeButton", backButton);
            Ref(so, "cashText", cash);
            Ref(so, "titleText", titleText);
            Ref(so, "regionButton", regionField.GetComponent<Button>());
            Ref(so, "regionLabel", regionLabel);
            Ref(so, "regionGlobe", regionGlobe);
            Ref(so, "regionFlag", regionFlag);
            Ref(so, "periodButton", periodField.GetComponent<Button>());
            Ref(so, "periodLabel", periodLabel);
            Ref(so, "eventButton", eventField.GetComponent<Button>());
            Ref(so, "eventLabel", eventLabel);
            Ref(so, "scroll", scroll);
            Ref(so, "rowsContent", content);
            Ref(so, "rowPrefab", rowPrefab);
            Ref(so, "loadMoreIndicator", more.gameObject);
            Ref(so, "youRow", youView);
            Ref(so, "findMeButton", findButton);
            Ref(so, "loadingGroup", loading.gameObject);
            Ref(so, "spinner", spinner);
            Ref(so, "emptyGroup", empty.gameObject);
            Ref(so, "errorGroup", error.gameObject);
            Ref(so, "retryButton", retryButton);
            Ref(so, "regionPopup", w.Region);
            Ref(so, "searchInput", w.Search);
            Ref(so, "searchBackground", w.SearchBackground);
            Ref(so, "searchNormalSprite", Art("search_field_normal"));
            Ref(so, "searchFocusedSprite", Art("search_field_focused"));
            Ref(so, "searchPlaceholder", w.SearchPlaceholder);
            Ref(so, "searchClearButton", w.SearchClear);
            Ref(so, "countriesContent", w.CountriesContent);
            Ref(so, "countryRowPrefab", countryPrefab);
            Ref(so, "noCountriesText", w.NoCountries);
            Ref(so, "regionHeaderText", w.RegionHeader);
            Ref(so, "periodPopup", w.Period);
            RefArray(so, "periodRows", w.PeriodRows);
            Ref(so, "periodHeaderText", w.PeriodHeader);
            Ref(so, "periodHintText", w.PeriodHint);
            Ref(so, "eventModal", w.Event);
            RefArray(so, "categoryTabs", w.Tabs);
            Ref(so, "distancesContent", w.DistancesContent);
            Ref(so, "distanceOptionPrefab", distancePrefab);
            Ref(so, "noEventsText", w.NoEvents);
            Ref(so, "applyButton", w.Apply);
            Ref(so, "cancelButton", w.Cancel);
            Ref(so, "modalCloseButton", w.ModalClose);
            Ref(so, "eventHeaderText", w.EventHeader);
            RefArray(so, "buttonLabels", new List<Text> { w.ApplyLabel, w.CancelLabel, retryLabel, findLabel });
            if (eventLayout != null) Ref(so, "eventLayout", eventLayout);
            BindFlags(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindFlags(SerializedObject so)
        {
            SerializedProperty array = so.FindProperty("flags");
            string[] codes = UiSettingsPrefabs.FlagCodes;
            array.arraySize = codes.Length;
            for (int i = 0; i < codes.Length; i++) FillFlag(array.GetArrayElementAtIndex(i), codes[i], codes[i]);
            FillFlag(so.FindProperty("unknownFlag"), string.Empty, "unknown");
        }

        private static void FillFlag(SerializedProperty entry, string code, string art)
        {
            entry.FindPropertyRelative("code").stringValue = code;
            entry.FindPropertyRelative("tableRow").objectReferenceValue = Art("ranking_row_flag_" + art);
            entry.FindPropertyRelative("listRow").objectReferenceValue = Art("country_row_flag_" + art);
            entry.FindPropertyRelative("icon").objectReferenceValue = Art("flag_" + art);
        }

        // ---------------------------------------------------------------- table pieces

        private static void ColumnHeader(RectTransform table, string text, Vector2 min, Vector2 max, TextAnchor align)
        {
            // Row slots are fractions of the 1400 px row; the header sits in the same columns, in the header band (y 106..165).
            float x0 = RowInset + min.x * RowW;
            float x1 = RowInset + max.x * RowW;
            Text label = Kit.Label(table, "Header" + text.Replace(" ", string.Empty), text, HeaderSize, UiArt.Navy, align);
            PlaceTopLeft(label.rectTransform, x0, 108f, x1 - x0, 56f);
        }

        /// <summary>Filter field (440x72 art): state sprites on the root Image, chevron layer, optional globe and flag, label.</summary>
        private static RectTransform Filter(RectTransform table, string name, float x, bool region, out Text label, out GameObject globe, out Image flag)
        {
            RectTransform node = At(table, name, x, 25f, 440f, 72f);
            StateButton(node, "filter_normal", "filter_hover", "filter_pressed", "filter_disabled");
            UiArt.Layer(node, "ChevronDown", Art("filter_down"));
            globe = null;
            flag = null;
            if (region)
            {
                globe = UiArt.Layer(node, "Globe", Art("filter_globe")).gameObject;
                RectTransform flagNode = At(node, "Flag", 14f, 17f, 52f, 36f);
                flag = UiArt.Paint(flagNode.gameObject, Art("flag_unknown"), raycast: false);
                flag.preserveAspect = true;
                flag.enabled = false;
            }
            label = UiArt.Slot(node, "Label", name, FilterSize, new Vector2(region ? 0.17f : 0.06f, 0.08f), new Vector2(0.88f, 0.92f), TextAnchor.MiddleLeft);
            return node;
        }

        /// <summary>ScrollRect on a node (transparent catcher so empty space drags): Viewport (RectMask2D) + Content (vertical stack).</summary>
        private static ScrollRect Scroll(RectTransform node, float spacing, out RectTransform content, out RectTransform viewport)
        {
            Kit.Fill(node.gameObject, Clear, raycast: true);
            ScrollRect scroll = node.gameObject.AddComponent<ScrollRect>();

            viewport = Kit.Stretch(Kit.Node(node, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Kit.Place(Kit.Node(viewport, "Content"), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = spacing * S;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = false;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
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

        /// <summary>Vertical scrollbar from the package's track and thumb (cropped, 9-sliced), wired to the ScrollRect.</summary>
        private static void ScrollbarOn(RectTransform parent, ScrollRect scroll, string name, float x, float y, float height)
        {
            float width = Art(TrackTight).rect.width; // art px
            RectTransform bar = At(parent, name, x, y, width, height);
            Image track = UiArt.Paint(bar.gameObject, Art(TrackTight), raycast: true);
            track.type = Image.Type.Sliced;
            Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            RectTransform area = Kit.Stretch(Kit.Node(bar, "SlidingArea"), 2f * S, 2f * S, 5f * S, 5f * S);
            RectTransform handle = Kit.Stretch(Kit.Node(area, "Handle"));
            Image thumb = UiArt.Paint(handle.gameObject, Art(ThumbTight), raycast: true);
            thumb.type = Image.Type.Sliced;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = thumb;
            scrollbar.transition = Selectable.Transition.None;

            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        /// <summary>Loading indicator: the whole 600x240 status canvas, pivot on the icon's centre (300, 65) so only the icon turns.</summary>
        private static void SpinnerNode(RectTransform node, string sprite)
        {
            Kit.Stretch(node);
            UiArt.Paint(node.gameObject, Art(sprite), raycast: false);
            SetPivotKeepRect(node, new Vector2(0.5f, 1f - 65f / 240f));
        }

        private static void SetPivotKeepRect(RectTransform rt, Vector2 pivot)
        {
            Vector2 min = rt.anchorMin, max = rt.anchorMax;
            Vector2 size = rt.sizeDelta;
            Vector2 before = rt.anchoredPosition;
            Vector2 shift = new Vector2((pivot.x - rt.pivot.x) * rt.rect.width, (pivot.y - rt.pivot.y) * rt.rect.height);
            rt.pivot = pivot;
            if (min == max) rt.anchoredPosition = before + shift; // fixed-size node: keep the rect where it was
            else
            {
                rt.offsetMin = Vector2.zero; // stretched node: still fills its parent
                rt.offsetMax = Vector2.zero;
            }
            rt.anchorMin = min;
            rt.anchorMax = max;
            if (min == max) rt.sizeDelta = size;
        }

        private static RectTransform Group(RectTransform parent, string name)
        {
            return Kit.Stretch(Kit.Node(parent, name));
        }

        // ---------------------------------------------------------------- windows

        private sealed class WindowRefs
        {
            public LeaderboardPopupPanel Region, Period, Event;
            public InputField Search;
            public Image SearchBackground;
            public Text SearchPlaceholder, NoCountries, RegionHeader, PeriodHeader, PeriodHint, NoEvents, EventHeader, ApplyLabel, CancelLabel;
            public Button SearchClear, Apply, Cancel, ModalClose;
            public RectTransform CountriesContent, DistancesContent;
            public List<LeaderboardChoiceView> PeriodRows = new List<LeaderboardChoiceView>();
            public List<LeaderboardChoiceView> Tabs = new List<LeaderboardChoiceView>();
        }

        private static void Windows(RectTransform screen, out WindowRefs w)
        {
            w = new WindowRefs();
            RegionWindow(screen, w);
            PeriodWindow(screen, w);
            EventWindow(screen, w);
        }

        /// <summary>Popup root: stretched over the screen, a catcher button behind the window (the base close button), starts inactive.</summary>
        private static LeaderboardPopupPanel PopupRoot(RectTransform screen, string name, Sprite catcherSprite, out RectTransform catcher)
        {
            RectTransform root = Kit.Stretch(Kit.Node(screen, name));
            var panel = root.gameObject.AddComponent<LeaderboardPopupPanel>();
            catcher = Kit.Stretch(Kit.Node(root, "Catcher"));
            Image image = catcherSprite != null ? UiArt.Paint(catcher.gameObject, catcherSprite, raycast: true) : Kit.Fill(catcher.gameObject, Clear, raycast: true);
            Button button = catcher.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var so = new SerializedObject(panel);
            Ref(so, "closeButton", button);
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        private static void RegionWindow(RectTransform screen, WindowRefs w)
        {
            w.Region = PopupRoot(screen, "RegionPopup", null, out _);
            RectTransform window = AtScreen(w.Region.transform, "Window", 218f, 157f, 640f, 600f);
            WindowFace(window, "popup_region_background", LeaderboardTexts.RegionHeader, out w.RegionHeader);

            RectTransform search = At(window, "Search", 22f, 70f, 596f, 56f);
            w.SearchBackground = UiArt.Paint(search.gameObject, Art("search_field_normal"), raycast: true);
            UiArt.Layer(search, "SearchIcon", Art("search_field_search"));
            Text input = Kit.Label(search, "Text", string.Empty, ListSize, UiArt.Navy, TextAnchor.MiddleLeft);
            input.supportRichText = false;
            PlaceTopLeft(input.rectTransform, 58f, 6f, 470f, 44f);
            w.SearchPlaceholder = Kit.Label(search, "Placeholder", LeaderboardTexts.SearchPlaceholder, ListSize, Grey, TextAnchor.MiddleLeft);
            PlaceTopLeft(w.SearchPlaceholder.rectTransform, 58f, 6f, 470f, 44f);
            w.Search = search.gameObject.AddComponent<InputField>();
            w.Search.targetGraphic = w.SearchBackground;
            w.Search.textComponent = input;
            w.Search.placeholder = w.SearchPlaceholder;
            w.Search.lineType = InputField.LineType.SingleLine;
            w.Search.characterLimit = 24;

            RectTransform clear = At(search, "Clear", 544f, 4f, 48f, 48f);
            Image clearHit = Kit.Fill(clear.gameObject, Clear, raycast: true);
            w.SearchClear = clear.gameObject.AddComponent<Button>();
            w.SearchClear.targetGraphic = clearHit;
            w.SearchClear.transition = Selectable.Transition.None;
            UiArt.Paint(At(clear, "Icon", -544f, -4f, 596f, 56f).gameObject, Art("search_field_clear"), raycast: false);
            clear.gameObject.SetActive(false);

            RectTransform list = At(window, "List", 22f, 136f, 596f, 452f);
            ScrollRect scroll = Scroll(list, 2f, out w.CountriesContent, out _);
            ScrollbarOn(window, scroll, "Scrollbar", 606f, 140f, 444f);

            w.NoCountries = Kit.Label(window, "NoCountries", LeaderboardTexts.NoCountries, ListSize, UiArt.Navy);
            PlaceTopLeft(w.NoCountries.rectTransform, 22f, 290f, 596f, 70f);
            w.NoCountries.gameObject.SetActive(false);
            w.Region.gameObject.SetActive(false);
        }

        private static void PeriodWindow(RectTransform screen, WindowRefs w)
        {
            w.Period = PopupRoot(screen, "PeriodPopup", null, out _);
            RectTransform window = AtScreen(w.Period.transform, "Window", 682f, 157f, 640f, 430f);
            WindowFace(window, "popup_period_background", LeaderboardTexts.PeriodHeader, out w.PeriodHeader);

            string[] icons = { "period_row_infinity", "period_row_calendar", "period_row_calendar", "period_row_clock" };
            for (int i = 0; i < icons.Length; i++)
            {
                RectTransform row = At(window, "Period" + i, 22f, 64f + i * 74f, 596f, 74f);
                w.PeriodRows.Add(Choice(row, "period_row_normal", "period_row_selected", "period_row_disabled", ListSize,
                    new Vector2(0.12f, 0.08f), new Vector2(0.86f, 0.92f), TextAnchor.MiddleLeft, "period_row_check", extraLayers: new[] { icons[i] }));
            }

            w.PeriodHint = Kit.Label(window, "Hint", LeaderboardTexts.PeriodHint, HintSize, Grey);
            PlaceTopLeft(w.PeriodHint.rectTransform, 22f, 372f, 596f, 44f);
            w.Period.gameObject.SetActive(false);
        }

        private static void EventWindow(RectTransform screen, WindowRefs w)
        {
            w.Event = PopupRoot(screen, "EventModal", Art("modal_dimscreen"), out _);
            RectTransform window = AtScreen(w.Event.transform, "Window", RefWidth * 0.5f - 380f, (RefHeight - 440f) * 0.5f, 760f, 440f);
            UiArt.Paint(window.gameObject, Art("modal_event_background"), raycast: true); // the window itself swallows taps (the dim behind closes)
            w.EventHeader = Kit.Label(window, "Title", LeaderboardTexts.EventHeader, WindowTitleSize, UiArt.Navy, TextAnchor.MiddleLeft);
            PlaceTopLeft(w.EventHeader.rectTransform, 30f, 10f, 400f, 52f);

            RectTransform close = At(window, "Close", 760f - 70f, 6f, 56f, 56f);
            w.ModalClose = StateButton(close, "small_button_normal", "small_button_hover", "small_button_pressed", "small_button_disabled");
            UiArt.Layer(close, "Icon", Art("small_button_close"));

            for (int i = 0; i < 3; i++)
            {
                RectTransform tab = At(window, "Tab" + i, 29f + i * 238f, 70f, 226f, 64f);
                w.Tabs.Add(Choice(tab, "category_tab_normal", "category_tab_selected", "category_tab_disabled", ListSize,
                    new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), TextAnchor.MiddleCenter, null));
            }

            RectTransform distances = At(window, "Distances", 29f, 150f, 702f, 176f);
            w.DistancesContent = distances;
            GridLayoutGroup grid = distances.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(226f * S, 76f * S);
            grid.spacing = new Vector2(12f * S, 12f * S);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            w.NoEvents = Kit.Label(window, "NoEvents", LeaderboardTexts.NoEvents, ListSize, UiArt.Navy);
            PlaceTopLeft(w.NoEvents.rectTransform, 29f, 190f, 702f, 70f);
            w.NoEvents.gameObject.SetActive(false);

            RectTransform cancel = At(window, "Cancel", 29f, 350f, 280f, 68f);
            w.Cancel = ActionButton(cancel, "action_button_normal", "action_button_hover", "action_button_pressed", "action_button_disabled",
                LeaderboardTexts.Cancel, UiArt.Navy, out w.CancelLabel);
            RectTransform apply = At(window, "Apply", 760f - 29f - 280f, 350f, 280f, 68f);
            w.Apply = ActionButton(apply, "action_button_selected", "action_button_selected", "action_button_selected", "action_button_disabled",
                LeaderboardTexts.Apply, Color.white, out w.ApplyLabel);
            w.Event.gameObject.SetActive(false);
        }

        /// <summary>Window background, a header label and the up chevron (open windows point up, closed fields down).</summary>
        private static void WindowFace(RectTransform window, string background, string header, out Text headerText)
        {
            UiArt.Paint(window.gameObject, Art(background), raycast: true); // taps on the window must not reach the catcher
            headerText = Kit.Label(window, "Header", header, WindowTitleSize, UiArt.Navy, TextAnchor.MiddleLeft);
            PlaceTopLeft(headerText.rectTransform, 30f, 8f, 400f, 52f);
            RectTransform chevron = At(window, "ChevronUp", 200f, -1f, 440f, 72f);
            UiArt.Paint(chevron.gameObject, Art("filter_up"), raycast: false);
        }

        // ---------------------------------------------------------------- buttons

        /// <summary>Button with the art's four states as sprite swap on its own Image; punch feedback like every button.</summary>
        private static Button StateButton(RectTransform node, string normal, string hover, string pressed, string disabled)
        {
            Image background = UiArt.Paint(node.gameObject, Art(normal), raycast: true);
            Button button = UiArt.MakeButton(node.gameObject, background, true, false);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Art(hover),
                pressedSprite = Art(pressed),
                selectedSprite = Art(normal),
                disabledSprite = Art(disabled)
            };
            return button;
        }

        private static Button ActionButton(RectTransform node, string normal, string hover, string pressed, string disabled, string caption,
            Color textColor, out Text label)
        {
            Button button = StateButton(node, normal, hover, pressed, disabled);
            label = UiArt.Slot(node, "Label", caption, ButtonSize, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), TextAnchor.MiddleCenter, textColor);
            return button;
        }

        // ---------------------------------------------------------------- placement (reference px -> canvas)

        /// <summary>Top-left anchored in the parent: x, y from its top-left corner, in reference px.</summary>
        private static RectTransform At(Transform parent, string name, float x, float y, float w, float h)
        {
            return PlaceTopLeft(Kit.Node(parent, name), x, y, w, h);
        }

        private static RectTransform PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            var corner = new Vector2(0f, 1f);
            return Kit.Anchor(rt, corner, corner, new Vector2(x * S, -y * S), new Vector2(w * S, h * S));
        }

        /// <summary>Hangs from the left screen edge (reference x from the edge, y from the top).</summary>
        private static RectTransform AtLeft(Transform parent, string name, float x, float y, float w, float h)
        {
            var corner = new Vector2(0f, 1f);
            return Kit.Anchor(Kit.Node(parent, name), corner, corner, new Vector2(x * S, -y * S), new Vector2(w * S, h * S));
        }

        /// <summary>Hangs from the screen's horizontal centre by its distance to the reference centre (the Expand scaler widens the screen).</summary>
        private static RectTransform AtScreen(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rt = Kit.Node(parent, name);
            return Kit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2((x - RefWidth * 0.5f) * S, -y * S), new Vector2(w * S, h * S));
        }

        // ---------------------------------------------------------------- serialized references

        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiLeaderboardPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiLeaderboardPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }

        private static void RefArray<T>(SerializedObject so, string field, List<T> items) where T : UnityEngine.Object
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"UiLeaderboardPrefabs: {so.targetObject.GetType().Name} has no serialized array '{field}'.");
            property.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
