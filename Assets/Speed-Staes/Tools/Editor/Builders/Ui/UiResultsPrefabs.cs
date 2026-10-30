using System;
using System.Collections.Generic;
using System.IO;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The post-race Results screen dressed with the designer's art (UI/Art/Results, Oyun_Bitis_UI_v1): Cash box and the
    /// six-button column on the left, the Personal Best / World Position / Percentile cards on top, the results table and
    /// the Best of the day panel on the right, a dimscreen under all of it. Component rects come from Results/layout.json
    /// (reference 1678x937 -> canvas); layers of one component share its art canvas at 100% scale. Groups keep their
    /// internal distances: the top cards hang from the screen centre, the right column from the right edge, the left
    /// column from the left edge. The table row is its own prefab (ResultsTableRow) the view creates under "Rows".
    /// Text slots are fractions of the art, read off the approved design. Never runs in the game.
    /// </summary>
    internal static class UiResultsPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Results";
        public const string RowPath = UiPrefabPaths.Root + "/Rows/ResultsTableRow.prefab";

        private const string LayoutPath = ArtFolder + "/layout.json";
        private const int TableRows = 6; // what the panel art has room for (ResultsModel.MaxRows)

        // Text sizes in canvas points (design reference px x ~1.15), best fit shrinks them to the slot.
        private const float CashSize = 54f;
        private const float NavSize = 38f; // "Watch Replay" is the longest caption and must stay on one line
        private const float CardTitleSize = 30f;
        private const float CardValueSize = 60f;
        private const float CardEventSize = 28f;
        private const float HeaderSize = 48f;
        private const float ColumnSize = 28f;
        private const float RowSize = 44f;
        private const float DailyHeaderSize = 38f;
        private const float DailyRowSize = 42f;
        private const float GemSize = 40f;

        private static readonly Color White = new Color(1f, 1f, 1f, 1f);

        // Text slots as fractions of their art (min, max; y from the bottom).
        private static readonly Vector2 CashMin = new Vector2(0.46f, 0.08f), CashMax = new Vector2(0.95f, 0.92f);
        private static readonly Vector2 NavMin = new Vector2(0.35f, 0.08f), NavMax = new Vector2(0.97f, 0.92f);
        private static readonly Vector2 PbTitleMin = new Vector2(0.34f, 0.64f), PbTitleMax = new Vector2(0.88f, 0.84f);
        private static readonly Vector2 PbValueMin = new Vector2(0.34f, 0.33f), PbValueMax = new Vector2(0.88f, 0.63f);
        private static readonly Vector2 PbEventMin = new Vector2(0.34f, 0.10f), PbEventMax = new Vector2(0.88f, 0.31f);
        private static readonly Vector2 WorldTitleMin = new Vector2(0.39f, 0.64f), WorldTitleMax = new Vector2(0.92f, 0.84f);
        private static readonly Vector2 WorldValueMin = new Vector2(0.39f, 0.18f), WorldValueMax = new Vector2(0.92f, 0.62f);
        private static readonly Vector2 PercentTitleMin = new Vector2(0.35f, 0.64f), PercentTitleMax = new Vector2(0.85f, 0.84f);
        private static readonly Vector2 PercentValueMin = new Vector2(0.35f, 0.18f), PercentValueMax = new Vector2(0.85f, 0.62f);
        private static readonly Vector2 HeaderMin = new Vector2(0.04f, 0.10f), HeaderMax = new Vector2(0.96f, 0.90f);
        private static readonly Vector2 PosMin = new Vector2(0.03f, 0.10f), PosMax = new Vector2(0.20f, 0.90f);
        private static readonly Vector2 PlayerColMin = new Vector2(0.27f, 0.10f), PlayerColMax = new Vector2(0.60f, 0.90f);
        private static readonly Vector2 TimeColMin = new Vector2(0.70f, 0.10f), TimeColMax = new Vector2(0.99f, 0.90f);
        private static readonly Vector2 RankMin = new Vector2(0.17f, 0.08f), RankMax = new Vector2(0.26f, 0.92f);
        private static readonly Vector2 NameMin = new Vector2(0.29f, 0.08f), NameMax = new Vector2(0.64f, 0.92f);
        private static readonly Vector2 TimeMin = new Vector2(0.64f, 0.08f), TimeMax = new Vector2(0.94f, 0.92f);
        private static readonly Vector2 PbBadgeMin = new Vector2(0.56f, 0.2f), PbBadgeMax = new Vector2(0.66f, 0.8f);
        private static readonly Vector2 DbBadgeMin = new Vector2(0.66f, 0.2f), DbBadgeMax = new Vector2(0.76f, 0.8f);
        private static readonly Color PbBadgeColor = new Color32(0x00, 0xB6, 0xB0, 0xFF);
        private static readonly Color DbBadgeColor = new Color32(0xFF, 0x20, 0x2D, 0xFF);
        private static readonly Vector2 DailyHeaderTextMin = new Vector2(0.17f, 0.08f), DailyHeaderTextMax = new Vector2(0.95f, 0.92f);
        private static readonly Vector2 DailyRowMin = new Vector2(0.05f, 0.08f), DailyRowMax = new Vector2(0.95f, 0.92f);

        // ---------------------------------------------------------------- layout.json extras (table, daily best)

        [Serializable]
        private sealed class ExtraFile
        {
            public TableInfo table;
            public DailyInfo daily_best;
        }

        [Serializable]
        private sealed class TableInfo
        {
            public int[] header_rect;
            public int[] columns_rect;
            public int[] row_canvas;
            public int[] first_row_position;
            public float row_step_y;
        }

        [Serializable]
        private sealed class DailyInfo
        {
            public int[] header_rect;
            public int[] row_rect;
        }

        private sealed class Sheet
        {
            public UiArt.Layout Layout;
            public TableInfo Table;
            public DailyInfo Daily;
        }

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            Sheet sheet = ReadSheet();
            GameObject row = build(RowPath, root => Row(root, sheet));
            var rowView = UiViewBinder.RowComponent<ResultsTableRowView>(row, RowPath);
            build(UiPrefabPaths.ResultsScreen, root => BuildScreen(root, sheet, rowView));
        }

        // ---------------------------------------------------------------- screen

        private static void BuildScreen(RectTransform root, Sheet sheet, ResultsTableRowView rowPrefab)
        {
            UiArt.Layout layout = sheet.Layout;
            var view = root.gameObject.AddComponent<ResultsScreenView>();
            var reveal = new List<CanvasGroup>();

            // Dimscreen: over the scene, under the UI; catches taps so the HUD below stays untouched.
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dimscreen"));
            UiArt.Paint(dim.gameObject, Art("dimscreen"), raycast: true);

            // ---- left column: Cash box, then the six buttons
            RectTransform diamond = Place(root, "Diamond", layout, layout.Get("diamond"));
            UiArt.Paint(diamond.gameObject, Art("diamond_background"), raycast: false);
            UiArt.Layer(diamond, "Icon", Art("diamond_icon"));
            Text cash = UiArt.Slot(diamond, "Value", "0", CashSize, CashMin, CashMax);
            reveal.Add(Reveal(diamond));

            RectTransform nav = Kit.Node(root, "Nav");
            Kit.Stretch(nav);
            Button raceAgain = NavButton(nav, layout, "RaceAgain", "race_again", "Race Again", true, "nav_primary_pressed_background", reveal, White);
            Button watchReplay = NavButton(nav, layout, "WatchReplay", "watch_replay", "Watch Replay", false, null, reveal, UiArt.Navy);
            Button highScores = NavButton(nav, layout, "HighScores", "high_scores", "High Scores", false, null, reveal, UiArt.Navy);
            Button stats = NavButton(nav, layout, "Stats", "stats", "Stats", false, null, reveal, UiArt.Navy);
            Button mainMenu = NavButton(nav, layout, "MainMenu", "main_menu", "Main Menu", true, "nav_secondary_pressed_background", reveal, UiArt.Navy);
            Button practice = NavButton(nav, layout, "Practice", "practice", "Practice", false, null, reveal, UiArt.Navy);

            // ---- top cards, hanging from the screen centre
            Vector4 cardBounds = layout.Bounds("personal_best", "world_position", "percentile");
            RectTransform top = Kit.Node(root, "TopCards");
            Kit.Place(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
                new Vector2((cardBounds.x - layout.RefWidth * 0.5f) * layout.Scale, -cardBounds.y * layout.Scale),
                new Vector2((cardBounds.z - cardBounds.x) * layout.Scale, (cardBounds.w - cardBounds.y) * layout.Scale));

            RectTransform pbCard = Card(top, layout, "PersonalBest", "personal_best", cardBounds, reveal);
            Text pbTitle = UiArt.Slot(pbCard, "Title", "PERSONAL BEST", CardTitleSize, PbTitleMin, PbTitleMax);
            Text pbValue = UiArt.Slot(pbCard, "Value", "0.00s", CardValueSize, PbValueMin, PbValueMax);
            Text pbEvent = UiArt.Slot(pbCard, "Event", "Event", CardEventSize, PbEventMin, PbEventMax);

            RectTransform worldCard = Card(top, layout, "WorldPosition", "world_position", cardBounds, reveal);
            UiArt.Slot(worldCard, "Title", "WORLD POSITION", CardTitleSize, WorldTitleMin, WorldTitleMax);
            Text worldValue = UiArt.Slot(worldCard, "Value", "--", CardValueSize, WorldValueMin, WorldValueMax);

            RectTransform percentCard = Card(top, layout, "Percentile", "percentile", cardBounds, reveal);
            UiArt.Slot(percentCard, "Title", "PERCENTILE", CardTitleSize, PercentTitleMin, PercentTitleMax);
            Text percentValue = UiArt.Slot(percentCard, "Value", "--", CardValueSize, PercentValueMin, PercentValueMax);

            // ---- right column, hanging from the right edge: results table, best of the day, gem line
            Vector4 columnBounds = layout.Bounds("results_panel", "daily_best_panel");
            const float gemGap = 11f;
            const float gemHeight = 56f;
            float gemTop = columnBounds.w + gemGap;
            var columnRect = new UiArt.LayoutComponent
            {
                name = "right_column",
                rect = new[] { (int)columnBounds.x, (int)columnBounds.y, (int)(columnBounds.z - columnBounds.x), (int)(gemTop + gemHeight - columnBounds.y) },
                pivot = new[] { 0.5f, 0.5f }
            };
            RectTransform column = Kit.Node(root, "RightColumn");
            layout.PlaceOnScreen(column, columnRect);
            float originX = columnBounds.x, originY = columnBounds.y;

            RectTransform panel = Inside(column, "ResultsPanel", layout, layout.Get("results_panel").rect, originX, originY);
            UiArt.Paint(panel.gameObject, Art("results_panel_background"), raycast: false);
            reveal.Add(Reveal(panel));

            RectTransform header = Inside(panel, "Header", layout, sheet.Table.header_rect, layout.Get("results_panel").rect[0], layout.Get("results_panel").rect[1], recenter: false);
            UiArt.Paint(header.gameObject, Art("results_event_header"), raycast: false);
            Text eventTitle = UiArt.Slot(header, "Title", "Event", HeaderSize, HeaderMin, HeaderMax, TextAnchor.MiddleCenter, White);

            RectTransform columns = Inside(panel, "Columns", layout, sheet.Table.columns_rect, layout.Get("results_panel").rect[0], layout.Get("results_panel").rect[1], recenter: false);
            UiArt.Paint(columns.gameObject, Art("results_columns_background"), raycast: false);
            UiArt.Slot(columns, "Pos", "POS", ColumnSize, PosMin, PosMax);
            UiArt.Slot(columns, "Player", "PLAYER", ColumnSize, PlayerColMin, PlayerColMax, TextAnchor.MiddleLeft);
            UiArt.Slot(columns, "Time", "TIME", ColumnSize, TimeColMin, TimeColMax);

            // Rows content: first row's top-left, rows stack at the art's step (the row canvas is 1 px taller than the step).
            float rowHeight = sheet.Table.row_canvas[1];
            int[] rowsRect =
            {
                sheet.Table.first_row_position[0], sheet.Table.first_row_position[1], sheet.Table.row_canvas[0],
                (int)(sheet.Table.row_step_y * (TableRows - 1) + rowHeight)
            };
            RectTransform rows = Inside(panel, "Rows", layout, rowsRect, layout.Get("results_panel").rect[0], layout.Get("results_panel").rect[1], recenter: false);
            VerticalLayoutGroup stack = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = (sheet.Table.row_step_y - rowHeight) * layout.Scale;
            stack.childAlignment = TextAnchor.UpperLeft;
            stack.childControlWidth = false;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;

            RectTransform daily = Inside(column, "DailyBest", layout, layout.Get("daily_best_panel").rect, originX, originY);
            UiArt.Paint(daily.gameObject, Art("daily_best_panel_background"), raycast: false);
            reveal.Add(Reveal(daily));
            float dailyX = layout.Get("daily_best_panel").rect[0], dailyY = layout.Get("daily_best_panel").rect[1];
            RectTransform dailyHeader = Inside(daily, "Header", layout, sheet.Daily.header_rect, dailyX, dailyY, recenter: false);
            UiArt.Paint(dailyHeader.gameObject, Art("daily_best_header"), raycast: false);
            UiArt.Layer(dailyHeader, "Crown", Art("daily_best_crown"));
            UiArt.Slot(dailyHeader, "Title", "BEST OF THE DAY", DailyHeaderSize, DailyHeaderTextMin, DailyHeaderTextMax, TextAnchor.MiddleLeft, White);
            RectTransform dailyRow = Inside(daily, "Row", layout, sheet.Daily.row_rect, dailyX, dailyY, recenter: false);
            UiArt.Paint(dailyRow.gameObject, Art("daily_best_row_background"), raycast: false);
            Text dailyText = UiArt.Slot(dailyRow, "Label", "No races today", DailyRowSize, DailyRowMin, DailyRowMax);

            RectTransform gem = Inside(column, "GemReward", layout, new[] { (int)columnBounds.x, (int)gemTop, (int)(columnBounds.z - columnBounds.x), (int)gemHeight }, originX, originY);
            Text gemText = UiArt.Slot(gem, "Label", "+3 Purple Gems", GemSize, new Vector2(0f, 0f), new Vector2(1f, 1f), TextAnchor.MiddleCenter, White);
            Kit.Frame(gemText.gameObject, 3f); // white text over the scenery: navy outline keeps it readable
            reveal.Add(Reveal(gem));
            gem.gameObject.SetActive(false); // the view shows it only when the race paid Purple Gems

            // ---- bind the view (every object is inside this prefab)
            var so = new SerializedObject(view);
            Ref(so, "cashText", cash);
            Ref(so, "personalBestCard", pbCard);
            Ref(so, "personalBestTitleText", pbTitle);
            Ref(so, "personalBestValueText", pbValue);
            Ref(so, "personalBestEventText", pbEvent);
            Ref(so, "worldPositionText", worldValue);
            Ref(so, "percentileText", percentValue);
            Ref(so, "eventTitleText", eventTitle);
            Ref(so, "rowsContent", rows);
            Ref(so, "rowPrefab", rowPrefab);
            Ref(so, "dailyBestText", dailyText);
            Ref(so, "gemRewardRoot", gem.gameObject);
            Ref(so, "gemRewardText", gemText);
            Ref(so, "raceAgainButton", raceAgain);
            Ref(so, "mainMenuButton", mainMenu);
            Ref(so, "watchReplayButton", watchReplay);
            Ref(so, "highScoresButton", highScores);
            Ref(so, "statsButton", stats);
            Ref(so, "practiceButton", practice);
            RefArray(so, "revealItems", reveal);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- pieces

        /// <summary>Nav button: state background (root Image, sprite swap for pressed / disabled), icon layer, left-aligned label.</summary>
        private static Button NavButton(Transform parent, UiArt.Layout layout, string nodeName, string artName, string caption,
            bool interactable, string pressedSprite, List<CanvasGroup> reveal, Color textColor)
        {
            RectTransform node = Place(parent, nodeName, layout, layout.Get(artName));
            Image background = UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: true);
            UiArt.Layer(node, "Icon", Art(artName + "_icon"));
            // One fixed size for every nav label (best fit would shrink only the long ones, and wrap "Watch Replay" to two lines).
            Text label = UiArt.Slot(node, "Label", caption, NavSize, NavMin, NavMax, TextAnchor.MiddleLeft, textColor);
            label.resizeTextForBestFit = false;
            label.fontSize = Mathf.RoundToInt(NavSize);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            Button button = UiArt.MakeButton(node.gameObject, background, interactable, alphaHit: false);
            button.transition = Selectable.Transition.SpriteSwap;
            var states = new SpriteState { disabledSprite = Art("nav_disabled_background") };
            if (pressedSprite != null) states.pressedSprite = Art(pressedSprite);
            button.spriteState = states;
            reveal.Add(Reveal(node));
            return button;
        }

        /// <summary>Info card: background + icon on the card's own art canvas, placed inside the top-cards group.</summary>
        private static RectTransform Card(Transform group, UiArt.Layout layout, string nodeName, string artName, Vector4 bounds, List<CanvasGroup> reveal)
        {
            RectTransform node = Inside(group, nodeName, layout, layout.Get(artName).rect, bounds.x, bounds.y);
            UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: false);
            UiArt.Layer(node, "Icon", Art(artName + "_icon"));
            reveal.Add(Reveal(node));
            return node;
        }

        /// <summary>Table row: layers share the 566x64 row canvas; the view shows the highlight and one cup.</summary>
        private static void Row(RectTransform root, Sheet sheet)
        {
            UiArt.Layout layout = sheet.Layout;
            var view = root.gameObject.AddComponent<ResultsTableRowView>();
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(sheet.Table.row_canvas[0] * layout.Scale, sheet.Table.row_canvas[1] * layout.Scale));

            UiArt.Layer(root, "Background", Art("result_row_background"));
            UiArt.Layer(root, "Divider", Art("result_row_divider"));
            Image highlight = UiArt.Layer(root, "PlayerHighlight", Art("result_row_player_highlight"));
            Image gold = UiArt.Layer(root, "TrophyGold", Art("result_row_trophy_gold"));
            Image silver = UiArt.Layer(root, "TrophySilver", Art("result_row_trophy_silver"));
            Image bronze = UiArt.Layer(root, "TrophyBronze", Art("result_row_trophy_bronze"));
            highlight.gameObject.SetActive(false);
            gold.gameObject.SetActive(false);
            silver.gameObject.SetActive(false);
            bronze.gameObject.SetActive(false);

            Text rank = UiArt.Slot(root, "Rank", "1", RowSize, RankMin, RankMax);
            Text playerName = UiArt.Slot(root, "Name", "YOU", RowSize, NameMin, NameMax, TextAnchor.MiddleLeft);
            Text time = UiArt.Slot(root, "Time", "0.00s", RowSize, TimeMin, TimeMax, TextAnchor.MiddleRight);

            // Badges stay off until the model says so; no art, just small coloured text after the name.
            Text pb = UiArt.Slot(root, "PersonalBestBadge", "PB", RowSize * 0.6f, PbBadgeMin, PbBadgeMax, TextAnchor.MiddleCenter, PbBadgeColor);
            Text db = UiArt.Slot(root, "DayBestBadge", "DB", RowSize * 0.6f, DbBadgeMin, DbBadgeMax, TextAnchor.MiddleCenter, DbBadgeColor);
            pb.gameObject.SetActive(false);
            db.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "personalBestBadge", pb);
            Ref(so, "dayBestBadge", db);
            Ref(so, "rankText", rank);
            Ref(so, "nameText", playerName);
            Ref(so, "timeText", time);
            Ref(so, "playerHighlight", highlight.gameObject);
            Ref(so, "goldTrophy", gold.gameObject);
            Ref(so, "silverTrophy", silver.gameObject);
            Ref(so, "bronzeTrophy", bronze.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- placement

        /// <summary>A node placed on the screen by the edge its component sits near (like the other art screens).</summary>
        private static RectTransform Place(Transform parent, string name, UiArt.Layout layout, UiArt.LayoutComponent component)
        {
            RectTransform node = Kit.Node(parent, name);
            layout.PlaceOnScreen(node, component);
            return Recenter(node);
        }

        /// <summary>A node placed inside a group by its reference rect, relative to the group's own origin (reference px).</summary>
        private static RectTransform Inside(Transform parent, string name, UiArt.Layout layout, int[] rect, float originX, float originY, bool recenter = true)
        {
            RectTransform node = Kit.Node(parent, name);
            layout.PlaceInside(node, new UiArt.LayoutComponent { name = name, rect = rect, pivot = new[] { 0.5f, 0.5f } }, originX, originY);
            return recenter ? Recenter(node) : node;
        }

        /// <summary>Moves the pivot to the centre without moving the rect, so reveal scaling grows from the middle.</summary>
        private static RectTransform Recenter(RectTransform rt)
        {
            Vector2 shift = (new Vector2(0.5f, 0.5f) - rt.pivot);
            rt.anchoredPosition += new Vector2(shift.x * rt.sizeDelta.x, shift.y * rt.sizeDelta.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        /// <summary>Group for the open animation: the view fades and scales the node through this CanvasGroup.</summary>
        private static CanvasGroup Reveal(RectTransform node) => node.gameObject.AddComponent<CanvasGroup>();

        // ---------------------------------------------------------------- helpers

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiResultsPrefabs: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        private static Sheet ReadSheet()
        {
            string path = Kit.FullPath(LayoutPath);
            if (!File.Exists(path)) throw new InvalidOperationException($"UiResultsPrefabs: '{LayoutPath}' not found.");
            string json = File.ReadAllText(path);

            var file = JsonUtility.FromJson<UiArt.LayoutFile>(json);
            var extra = JsonUtility.FromJson<ExtraFile>(json);
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null
                || extra == null || extra.table == null || extra.table.row_canvas == null || extra.table.first_row_position == null
                || extra.table.header_rect == null || extra.table.columns_rect == null
                || extra.daily_best == null || extra.daily_best.header_rect == null || extra.daily_best.row_rect == null)
                throw new InvalidOperationException($"UiResultsPrefabs: '{LayoutPath}' could not be read.");

            return new Sheet { Layout = new UiArt.Layout(file), Table = extra.table, Daily = extra.daily_best };
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiResultsPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiResultsPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }

        private static void RefArray(SerializedObject so, string field, List<CanvasGroup> items)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"UiResultsPrefabs: {so.targetObject.GetType().Name} has no serialized array '{field}'.");
            property.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
