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
    /// The race Analysis screen dressed with the designer's art (UI/Art/Analysis, Analiz_UI_v1): Cash box and Back on the
    /// left, the red ANALYSIS header, the chart on the left, the ranking on the right, a dimscreen under all of it.
    /// Component rects come from Analysis/layout.json (reference 1678x937 -> canvas); layers of one component share its art
    /// canvas at 100% scale. The chart holds a fixed pool of tick labels, the cursor art (guide, marker, balloon), a mesh
    /// graphic for grid and lines (AnalysisChartGraphic) and an invisible touch surface; the ranking is a scroll list whose
    /// row is its own prefab (AnalysisRankingRow) that the view creates. Text slots are fractions of the art, read off
    /// the approved design. Never runs in the game. IMPLEMENTATION.md of the art says TMP; this project uses legacy Text.
    /// </summary>
    internal static class UiAnalysisPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Analysis";
        public const string RowPath = UiPrefabPaths.Root + "/Rows/AnalysisRankingRow.prefab";

        private const string LayoutPath = ArtFolder + "/layout.json";

        // Chart art canvas (px) and the cursor art as drawn (IMPLEMENTATION.md: marker (607,249), balloon pointer (607,236)).
        private static readonly Vector2 ChartArt = new Vector2(1038f, 502f);
        private static readonly Vector2 MarkerArt = new Vector2(607f, 249f);
        private const float BalloonBodyHeightArt = 38f; // the balloon body without its pointer, read off the approved design

        // Tick label pool: more than any axis needs (about 5 intervals + the end tick).
        private const int TickPool = 8;

        // Text sizes in canvas points (design reference px x ~1.15), best fit shrinks them to the slot.
        private const float CashSize = 54f;
        private const float HeaderSize = 60f;
        private const float AxisTitleSize = 30f;
        private const float TickSize = 28f;
        private const float BalloonSize = 28f;
        private const float EmptySize = 46f;
        private const float RankTitleSize = 52f;
        private const float ColumnSize = 30f;
        private const float RowSize = 44f;

        private static readonly Color White = new Color(1f, 1f, 1f, 1f);
        private static readonly string[] Swatches = { "teal", "orange", "violet", "blue", "ochre", "slate" };

        // Text slots as fractions of their art (min, max; y from the bottom).
        private static readonly Vector2 CashMin = new Vector2(0.46f, 0.08f), CashMax = new Vector2(0.95f, 0.92f);
        private static readonly Vector2 HeaderMin = new Vector2(0.04f, 0.10f), HeaderMax = new Vector2(0.96f, 0.90f);
        private static readonly Vector2 TimeTitleMin = new Vector2(0.025f, 0.875f), TimeTitleMax = new Vector2(0.160f, 0.952f);
        private static readonly Vector2 DistanceTitleMin = new Vector2(0.410f, 0.016f), DistanceTitleMax = new Vector2(0.630f, 0.092f);
        private static readonly Vector2 PlotEmptyMin = new Vector2(0.25f, 0.30f), PlotEmptyMax = new Vector2(0.75f, 0.60f);
        private static readonly Vector2 RankTitleMin = new Vector2(0.06f, 0.878f), RankTitleMax = new Vector2(0.94f, 0.990f);
        private static readonly Vector2 PosColMin = new Vector2(0.07f, 0.790f), PosColMax = new Vector2(0.21f, 0.862f);
        private static readonly Vector2 PlayerColMin = new Vector2(0.27f, 0.790f), PlayerColMax = new Vector2(0.62f, 0.862f);
        private static readonly Vector2 TimeColMin = new Vector2(0.72f, 0.790f), TimeColMax = new Vector2(0.93f, 0.862f);
        private static readonly Vector2 RowPosMin = new Vector2(0.03f, 0.08f), RowPosMax = new Vector2(0.16f, 0.92f);
        private static readonly Vector2 RowNameMin = new Vector2(0.31f, 0.08f), RowNameMax = new Vector2(0.70f, 0.92f);
        private static readonly Vector2 RowTimeMin = new Vector2(0.68f, 0.08f), RowTimeMax = new Vector2(0.95f, 0.92f);

        // Tick label boxes (reference px): X labels hang under the axis, Y labels stand left of it.
        private const float XTickWidth = 100f, XTickHeight = 36f, XTickGap = 8f;
        private const float YTickWidth = 80f, YTickHeight = 36f, YTickGap = 15f;

        // Dots at the plot's edge may leave the plot rectangle by this many canvas pixels before the mask cuts them.
        private const float MaskReach = 12f;

        // ---------------------------------------------------------------- layout.json extras

        [Serializable]
        private sealed class ExtraFile
        {
            public int[] chart_plot_local; // left, top, right, bottom in chart art pixels
            public RowInfo[] components;   // same list UiArt reads; here only the repeat fields of ranking_row
        }

        [Serializable]
        private sealed class RowInfo
        {
            public string name;
            public int[] rect;
            public int repeat_y_step;
            public int count;
        }

        private sealed class Sheet
        {
            public UiArt.Layout Layout;
            public int[] Plot;
            public int[] FirstRow;  // x, y, w, h of the first ranking row (reference px)
            public int RowStep;
            public int RowCount;
            public int[] RankingsRect;
        }

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            Sheet sheet = ReadSheet();
            AnalysisConfig config = AnalysisConfigSeed.Seed(ConfigAssetSeeder.AnalysisPath, overwrite: false); // never overwrites the user's asset
            if (config == null) throw new InvalidOperationException($"UiAnalysisPrefabs: '{ConfigAssetSeeder.AnalysisPath}' could not be loaded.");

            GameObject row = build(RowPath, root => Row(root, sheet));
            var rowView = UiViewBinder.RowComponent<AnalysisRowView>(row, RowPath);
            build(UiPrefabPaths.AnalysisScreen, root => BuildScreen(root, sheet, rowView, config));
        }

        // ---------------------------------------------------------------- screen

        private static void BuildScreen(RectTransform root, Sheet sheet, AnalysisRowView rowPrefab, AnalysisConfig config)
        {
            UiArt.Layout layout = sheet.Layout;
            var view = root.gameObject.AddComponent<AnalysisScreenView>();

            // Dimscreen: over the scene, under the UI; catches taps so the screens below stay untouched.
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dimscreen"));
            UiArt.Paint(dim.gameObject, Art("dimscreen"), raycast: true);

            // ---- top left: Cash box, Back, header
            RectTransform diamond = Place(root, "Diamond", layout, layout.Get("diamond"));
            UiArt.Paint(diamond.gameObject, Art("diamond_background"), raycast: false);
            UiArt.Layer(diamond, "Icon", Art("diamond_icon"));
            Text cash = UiArt.Slot(diamond, "Value", "0", CashSize, CashMin, CashMax);

            RectTransform back = Place(root, "Back", layout, layout.Get("back"));
            Image backBackground = UiArt.Paint(back.gameObject, Art("back_background"), raycast: true);
            UiArt.Layer(back, "Icon", Art("back_icon"));
            Button backButton = UiArt.MakeButton(back.gameObject, backBackground, true, alphaHit: false);
            backButton.transition = Selectable.Transition.SpriteSwap;
            backButton.spriteState = new SpriteState { pressedSprite = Art("back_pressed_background") };

            RectTransform header = Place(root, "Header", layout, layout.Get("analysis_header"));
            UiArt.Paint(header.gameObject, Art("analysis_header_background"), raycast: false);
            UiArt.Slot(header, "Title", "ANALYSIS", HeaderSize, HeaderMin, HeaderMax, TextAnchor.MiddleCenter, White);

            // ---- chart
            RectTransform chart = Place(root, "Chart", layout, layout.Get("chart"));
            UiArt.Paint(chart.gameObject, Art("chart_background"), raycast: false);
            UiArt.Layer(chart, "Axes", Art("chart_axes"));

            // Plot: exactly the plot rectangle of the art. Only distance/time tick labels hang from it; the mesh sits in its own masked child.
            Vector2 plotMin = new Vector2(sheet.Plot[0] / ChartArt.x, 1f - sheet.Plot[3] / ChartArt.y);
            Vector2 plotMax = new Vector2(sheet.Plot[2] / ChartArt.x, 1f - sheet.Plot[1] / ChartArt.y);
            RectTransform plot = Kit.Span(Kit.Node(chart, "Plot"), plotMin, plotMax);

            RectTransform lines = Kit.Stretch(Kit.Node(plot, "Lines"));
            var graphic = lines.gameObject.AddComponent<AnalysisChartGraphic>();
            graphic.raycastTarget = false; // the Touch surface above takes the pointer
            var mask = lines.gameObject.AddComponent<RectMask2D>();
            float reach = -MaskReach * layout.Scale; // negative padding: the mask is a little bigger than the plot
            mask.padding = new Vector4(reach, reach, reach, reach);

            var xTicks = new List<Text>();
            var yTicks = new List<Text>();
            RectTransform xGroup = Kit.Stretch(Kit.Node(plot, "XTicks"));
            RectTransform yGroup = Kit.Stretch(Kit.Node(plot, "YTicks"));
            float scale = layout.Scale;
            for (int i = 0; i < TickPool; i++)
            {
                Text x = Kit.Label(xGroup, "XTick" + i, i.ToString(), TickSize, UiArt.Navy, TextAnchor.UpperCenter);
                Kit.Anchor(x.rectTransform, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(0f, -XTickGap * scale), new Vector2(XTickWidth * scale, XTickHeight * scale));
                xTicks.Add(x);

                Text y = Kit.Label(yGroup, "YTick" + i, i.ToString(), TickSize, UiArt.Navy, TextAnchor.MiddleRight);
                Kit.Anchor(y.rectTransform, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(-YTickGap * scale, 0f), new Vector2(YTickWidth * scale, YTickHeight * scale));
                yTicks.Add(y);
            }

            UiArt.Slot(chart, "TimeTitle", "TIME (s)", AxisTitleSize, TimeTitleMin, TimeTitleMax, TextAnchor.MiddleLeft);
            UiArt.Slot(chart, "DistanceTitle", "DISTANCE (m)", AxisTitleSize, DistanceTitleMin, DistanceTitleMax);
            Text empty = UiArt.Slot(chart, "EmptyText", "No race data", EmptySize, PlotEmptyMin, PlotEmptyMax);
            empty.gameObject.SetActive(false); // the view shows it only when the race has nothing to draw

            // Cursor art: each layer is the full art canvas, the view moves it by the distance between the drawn and the wanted position.
            RectTransform guide = Kit.Stretch(Kit.Node(chart, "Guide"));
            UiArt.Paint(guide.gameObject, Art("chart_cursor_guide"), raycast: false);
            RectTransform marker = Kit.Stretch(Kit.Node(chart, "Marker"));
            UiArt.Paint(marker.gameObject, Art("chart_cursor_marker"), raycast: false);
            RectTransform balloon = Kit.Stretch(Kit.Node(chart, "Balloon"));
            UiArt.Paint(balloon.gameObject, Art("chart_tooltip_background"), raycast: false);

            Vector4 body = UiArt.AlphaBounds(ArtFolder + "/chart_tooltip_background.png"); // x, y (from bottom), x2, y2 as fractions
            Vector2 textMin = new Vector2(body.x + 0.012f, body.w - BalloonBodyHeightArt / ChartArt.y);
            Vector2 textMax = new Vector2(body.z - 0.012f, body.w);
            Text balloonText = UiArt.Slot(balloon, "Text", "0 m / 0.00 s", BalloonSize, textMin, textMax);
            var balloonBodyArt = new Vector4(body.x * ChartArt.x, (1f - body.w) * ChartArt.y, body.z * ChartArt.x, (1f - body.y) * ChartArt.y);
            guide.gameObject.SetActive(false);
            marker.gameObject.SetActive(false);
            balloon.gameObject.SetActive(false);

            // Touch surface over the plot: transparent, takes presses and drags. Last child = on top of the cursor art.
            RectTransform touch = Kit.Span(Kit.Node(chart, "Touch"), plotMin, plotMax);
            Image touchImage = touch.gameObject.AddComponent<Image>();
            touchImage.color = new Color(1f, 1f, 1f, 0f);
            touchImage.raycastTarget = true;
            touchImage.canvasRenderer.cullTransparentMesh = false; // a culled (fully transparent) mesh is skipped by the raycaster
            var input = touch.gameObject.AddComponent<AnalysisChartInput>();

            // ---- ranking
            RectTransform rankings = Place(root, "Rankings", layout, layout.Get("rankings"));
            UiArt.Paint(rankings.gameObject, Art("rankings_background"), raycast: false);
            UiArt.Layer(rankings, "Header", Art("rankings_header"));
            Text eventTitle = UiArt.Slot(rankings, "Title", "Event", RankTitleSize, RankTitleMin, RankTitleMax, TextAnchor.MiddleCenter, White);
            UiArt.Layer(rankings, "Columns", Art("rankings_columns_background"));
            UiArt.Slot(rankings, "Pos", "POS", ColumnSize, PosColMin, PosColMax);
            UiArt.Slot(rankings, "Player", "PLAYER", ColumnSize, PlayerColMin, PlayerColMax, TextAnchor.MiddleLeft);
            UiArt.Slot(rankings, "Time", "TIME", ColumnSize, TimeColMin, TimeColMax);

            // Rows: the viewport shows what the art has room for (six lines at the art's step); more runners scroll.
            int[] firstRow = sheet.FirstRow;
            int rowStep = sheet.RowStep;
            int[] viewportRect = { firstRow[0], firstRow[1], firstRow[2], rowStep * (sheet.RowCount - 1) + firstRow[3] };
            RectTransform scroll = Inside(rankings, "Scroll", layout, viewportRect, sheet.RankingsRect[0], sheet.RankingsRect[1]);
            RectTransform viewport = Kit.Stretch(Kit.Node(scroll, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Kit.Node(viewport, "Content");
            Kit.Place(content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 0f));
            VerticalLayoutGroup stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = (rowStep - firstRow[3]) * layout.Scale; // the row canvas is 1 px taller than the step
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = false;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // ---- bind the view (every object is inside this prefab)
            var so = new SerializedObject(view);
            Ref(so, "config", config);
            Ref(so, "cashText", cash);
            Ref(so, "eventTitleText", eventTitle);
            Ref(so, "emptyText", empty);
            Ref(so, "chartRoot", chart);
            Ref(so, "plotRect", plot);
            Ref(so, "chartGraphic", graphic);
            Ref(so, "chartInput", input);
            so.FindProperty("chartArtSize").vector2Value = ChartArt;
            so.FindProperty("plotArt").vector4Value = new Vector4(sheet.Plot[0], sheet.Plot[1], sheet.Plot[2], sheet.Plot[3]);
            RefArray(so, "xTickLabels", xTicks);
            RefArray(so, "yTickLabels", yTicks);
            Ref(so, "guideNode", guide);
            Ref(so, "markerNode", marker);
            Ref(so, "balloonNode", balloon);
            Ref(so, "balloonText", balloonText);
            so.FindProperty("markerArt").vector2Value = MarkerArt;
            so.FindProperty("balloonBodyArt").vector4Value = balloonBodyArt;
            Ref(so, "rowsContent", content);
            Ref(so, "rowsScroll", scrollRect);
            Ref(so, "rowPrefab", rowPrefab);
            Ref(so, "closeButton", backButton); // UiPanel's Back: returns to the screen under (Results)
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- row

        /// <summary>Ranking row: layers share the 453x83 row canvas; the view shows the YOU highlight and the runner's own colour bar.</summary>
        private static void Row(RectTransform root, Sheet sheet)
        {
            UiArt.Layout layout = sheet.Layout;
            int[] firstRow = sheet.FirstRow;
            var view = root.gameObject.AddComponent<AnalysisRowView>();
            Vector2 size = new Vector2(firstRow[2] * layout.Scale, firstRow[3] * layout.Scale);
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);

            Image background = UiArt.Paint(root.gameObject, Art("ranking_row_background"), raycast: true);
            Image highlight = UiArt.Layer(root, "YouHighlight", Art("ranking_row_you_highlight"));
            UiArt.Layer(root, "Separator", Art("ranking_row_separator"));
            highlight.gameObject.SetActive(false);

            var swatches = new List<GameObject>();
            foreach (string colour in Swatches)
            {
                Image swatch = UiArt.Layer(root, "Swatch_" + colour, Art("ranking_swatch_" + colour));
                swatch.gameObject.SetActive(false);
                swatches.Add(swatch.gameObject);
            }

            Text rank = UiArt.Slot(root, "Rank", "1", RowSize, RowPosMin, RowPosMax);
            Text playerName = UiArt.Slot(root, "Name", "YOU", RowSize, RowNameMin, RowNameMax, TextAnchor.MiddleLeft);
            Text time = UiArt.Slot(root, "Time", "0.00s", RowSize, RowTimeMin, RowTimeMax, TextAnchor.MiddleRight);

            Button button = UiArt.MakeButton(root.gameObject, background, true, alphaHit: false);
            button.transition = Selectable.Transition.None; // tint would colour the whole row art (Image.color x tint)

            var element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = size.y;
            element.preferredHeight = size.y;
            element.minWidth = size.x;
            element.preferredWidth = size.x;

            var so = new SerializedObject(view);
            Ref(so, "rankText", rank);
            Ref(so, "nameText", playerName);
            Ref(so, "timeText", time);
            Ref(so, "youHighlight", highlight.gameObject);
            RefArray(so, "swatches", swatches);
            Ref(so, "selectButton", button);
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
        private static RectTransform Inside(Transform parent, string name, UiArt.Layout layout, int[] rect, float originX, float originY)
        {
            RectTransform node = Kit.Node(parent, name);
            layout.PlaceInside(node, new UiArt.LayoutComponent { name = name, rect = rect, pivot = new[] { 0.5f, 0.5f } }, originX, originY);
            return node;
        }

        /// <summary>Moves the pivot to the centre without moving the rect, so open-animation scaling grows from the middle.</summary>
        private static RectTransform Recenter(RectTransform rt)
        {
            Vector2 shift = new Vector2(0.5f, 0.5f) - rt.pivot;
            rt.anchoredPosition += new Vector2(shift.x * rt.sizeDelta.x, shift.y * rt.sizeDelta.y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        // ---------------------------------------------------------------- helpers

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiAnalysisPrefabs: sprite '{path}' not found or not imported as a Sprite.");
            return sprite;
        }

        private static Sheet ReadSheet()
        {
            string path = Kit.FullPath(LayoutPath);
            if (!File.Exists(path)) throw new InvalidOperationException($"UiAnalysisPrefabs: '{LayoutPath}' not found.");
            string json = File.ReadAllText(path);

            var file = JsonUtility.FromJson<UiArt.LayoutFile>(json);
            var extra = JsonUtility.FromJson<ExtraFile>(json);
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null
                || extra == null || extra.chart_plot_local == null || extra.chart_plot_local.Length < 4)
                throw new InvalidOperationException($"UiAnalysisPrefabs: '{LayoutPath}' could not be read.");

            RowInfo row = null;
            RowInfo rankings = null;
            if (extra.components != null)
            {
                foreach (RowInfo component in extra.components)
                {
                    if (component.name == "ranking_row") row = component;
                    else if (component.name == "rankings") rankings = component;
                }
            }
            if (row == null || row.rect == null || row.rect.Length < 4 || row.repeat_y_step <= 0 || row.count <= 0 || rankings == null || rankings.rect == null)
                throw new InvalidOperationException($"UiAnalysisPrefabs: '{LayoutPath}' has no usable ranking_row (rect, repeat_y_step, count) / rankings.");

            return new Sheet
            {
                Layout = new UiArt.Layout(file),
                Plot = extra.chart_plot_local,
                FirstRow = row.rect,
                RowStep = row.repeat_y_step,
                RowCount = row.count,
                RankingsRect = rankings.rect
            };
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiAnalysisPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiAnalysisPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }

        private static void RefArray<T>(SerializedObject so, string field, List<T> items) where T : UnityEngine.Object
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"UiAnalysisPrefabs: {so.targetObject.GetType().Name} has no serialized array '{field}'.");
            property.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void RefArray(SerializedObject so, string field, List<GameObject> items)
        {
            RefArray<GameObject>(so, field, items);
        }
    }
}
