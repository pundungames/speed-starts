using System;
using System.IO;
using SpeedStars.UI.Home;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// EventSelectScreen prefab dressed with the designer's art (UI/Art/EventSelect): dimscreen, Back, heading plate, three
    /// category cards (Sprints / Hurdles / Relays), Race of the Day, Practice and Continue. Component rects come from
    /// EventSelect/layout.json; layers of one component share its art canvas at 100% scale (transparent edges are not
    /// cropped). Distance buttons are smaller than their card canvas, so their node is the alpha bounds of the art (read
    /// from the PNG) and the two art layers inside it are stretched back out to the whole card canvas. Text slots are
    /// fractions of the art, read off the design preview. The prefab shows the Sprints card selected so the designer sees
    /// the selected look; the view switches states at runtime. Never runs in the game.
    /// </summary>
    internal static class UiEventSelectPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/EventSelect";
        private const string LayoutPath = ArtFolder + "/layout.json";

        private const float HeadingSize = 96f;
        private const float TitleSize = 64f;
        private const float DistanceSize = 56f;
        private const float BottomLabelSize = 48f;
        private const float ContinueSize = 64f;

        // Text slots as fractions of their art (min, max; y from the bottom).
        private static readonly Vector2 HeadingMin = new Vector2(0.04f, 0.10f), HeadingMax = new Vector2(0.96f, 0.90f);
        private static readonly Vector2 HeaderInset = new Vector2(0.03f, 0.02f); // inside the header's alpha bounds
        private static readonly Vector2 LabelMin = new Vector2(0.06f, 0.12f), LabelMax = new Vector2(0.94f, 0.88f); // inside a distance button
        private const float BottomLabelGap = 0.03f; // between a button's icon / arrow and its label
        private const float BottomLabelEdge = 0.96f;
        private const float ContinueLabelEdge = 0.04f;
        private const float MarkWidth = 6f;

        private static readonly Color DimmedTint = new Color(0.35f, 0.35f, 0.35f, 0.75f);

        public static void EventSelect(RectTransform root, EventSelectLayout content)
        {
            if (content == null) throw new InvalidOperationException("UiEventSelectPrefabs: EventSelectLayout asset is missing (EventSelectConfigSeed).");

            UiArt.Layout layout = ReadLayout();
            var view = root.gameObject.AddComponent<EventSelectScreenView>();

            // Dimscreen: catches taps so nothing reaches the 3D scene behind the screen.
            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dim"));
            UiArt.PaintDim(dim.gameObject, ArtFolder + "/dimscreen.png");

            RectTransform back = Kit.Node(root, "Back");
            layout.PlaceOnScreen(back, layout.Get("back"));
            Image backImage = UiArt.Paint(back.gameObject, Art("back_background"), raycast: true);
            UiArt.Layer(back, "Icon", Art("back_icon"));
            UiArt.MakeButton(back.gameObject, backImage, true, alphaHit: false);

            RectTransform heading = Kit.Node(root, "Heading");
            layout.PlaceOnScreen(heading, layout.Get("screen_heading"));
            UiArt.Paint(heading.gameObject, Art("screen_heading_background"), raycast: false);
            Text headingLabel = UiArt.Slot(heading, "Label", content.HeadingText, HeadingSize, HeadingMin, HeadingMax);

            var cards = new EventCategoryCardView[3];
            cards[0] = Card(root, layout, content, EventCategory.Sprints, "Sprints", "sprints", selected: true);
            cards[1] = Card(root, layout, content, EventCategory.Hurdles, "Hurdles", "hurdles", selected: false);
            cards[2] = Card(root, layout, content, EventCategory.Relays, "Relays", "relays", selected: false);

            RectTransform raceOfDay = BottomButton(root, layout, "RaceOfTheDay", "race_of_day", "race_of_day_icon",
                content.RaceOfTheDayLabel, out Text raceLabel, out Outline raceMark);
            RectTransform practice = BottomButton(root, layout, "Practice", "practice", "practice_icon",
                content.PracticeLabel, out Text practiceLabel, out Outline practiceMark);

            RectTransform cont = Kit.Node(root, "Continue");
            layout.PlaceOnScreen(cont, layout.Get("continue"));
            Image contImage = UiArt.Paint(cont.gameObject, Art("continue_background"), raycast: true);
            UiArt.MakeButton(cont.gameObject, contImage, true, alphaHit: false);
            Image dimmed = UiArt.Layer(cont, "Dimmed", Art("continue_background"));
            dimmed.color = DimmedTint;
            dimmed.gameObject.SetActive(false);
            UiArt.Layer(cont, "Arrow", Art("continue_arrow"));
            Vector4 arrow = AlphaBounds("continue_arrow");
            Text contLabel = UiArt.Slot(cont, "Label", content.ContinueLabel, ContinueSize,
                new Vector2(ContinueLabelEdge, 0.10f), new Vector2(arrow.x - BottomLabelGap, 0.90f), TextAnchor.MiddleCenter, Color.white);

            // ---- bind the view (all objects are inside this prefab; the layout asset is the config asset)
            var so = new SerializedObject(view);
            Ref(so, "layout", content);
            Ref(so, "headingText", headingLabel);
            SerializedProperty cardList = Find(so, "cards");
            cardList.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++) cardList.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            Ref(so, "raceOfTheDayButton", raceOfDay.GetComponent<Button>());
            Ref(so, "raceOfTheDayLabel", raceLabel);
            Ref(so, "raceOfTheDayMark", raceMark);
            Ref(so, "practiceButton", practice.GetComponent<Button>());
            Ref(so, "practiceLabel", practiceLabel);
            Ref(so, "practiceMark", practiceMark);
            Ref(so, "continueButton", cont.GetComponent<Button>());
            Ref(so, "continueLabel", contLabel);
            Ref(so, "continueDimmed", dimmed.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();

            UiViewBinder.BindClose(root, "Back"); // base UiPanel.closeButton: the same close rule as every screen
        }

        // ---------------------------------------------------------------- cards

        private static EventCategoryCardView Card(RectTransform root, UiArt.Layout layout, EventSelectLayout content,
            EventCategory category, string nodeName, string artName, bool selected)
        {
            RectTransform card = Kit.Node(root, nodeName);
            layout.PlaceOnScreen(card, layout.Get(artName));

            // Tap area of the whole card: invisible (the art layers below are what is drawn).
            Image hit = HitArea(card.gameObject);
            Button button = UiArt.MakeButton(card.gameObject, hit, true, alphaHit: false);
            var view = card.gameObject.AddComponent<EventCategoryCardView>();

            GameObject cardNormal = UiArt.Layer(card, "Normal", Art(artName + "_card_normal")).gameObject;
            GameObject cardSelected = UiArt.Layer(card, "Selected", Art(artName + "_card_selected")).gameObject;
            GameObject headerNormal = UiArt.Layer(card, "HeaderNormal", Art(artName + "_header_normal")).gameObject;
            GameObject headerSelected = UiArt.Layer(card, "HeaderSelected", Art(artName + "_header_selected")).gameObject;
            UiArt.Layer(card, "Illustration", Art(artName + "_illustration"));

            Vector4 header = AlphaBounds(artName + "_header_normal");
            Text title = UiArt.Slot(card, "Title", TitleOf(content, category), TitleSize,
                new Vector2(header.x + HeaderInset.x, header.y + HeaderInset.y), new Vector2(header.z - HeaderInset.x, header.w - HeaderInset.y));
            title.color = selected ? Color.white : UiArt.Navy;

            RectTransform distances = Kit.Stretch(Kit.Node(card, "Distances"));
            var slots = new RectTransform[3];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = DistanceButton(distances, artName, i + 1, selected && i == 0);

            cardNormal.SetActive(!selected);
            cardSelected.SetActive(selected);
            headerNormal.SetActive(!selected);
            headerSelected.SetActive(selected);
            distances.gameObject.SetActive(selected);

            var so = new SerializedObject(view);
            Find(so, "category").intValue = (int)category;
            Ref(so, "button", button);
            Ref(so, "cardNormal", cardNormal);
            Ref(so, "cardSelected", cardSelected);
            Ref(so, "headerNormal", headerNormal);
            Ref(so, "headerSelected", headerSelected);
            Ref(so, "titleText", title);
            Ref(so, "distanceRoot", distances.gameObject);
            SerializedProperty list = Find(so, "distances");
            list.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                SerializedProperty slot = list.GetArrayElementAtIndex(i);
                RectTransform node = slots[i];
                Rel(slot, "button").objectReferenceValue = node.GetComponent<Button>();
                Rel(slot, "normalLayer").objectReferenceValue = node.Find("Normal").gameObject;
                Rel(slot, "selectedLayer").objectReferenceValue = node.Find("Selected").gameObject;
                Rel(slot, "label").objectReferenceValue = node.Find("Label").GetComponent<Text>();
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static string TitleOf(EventSelectLayout content, EventCategory category)
        {
            foreach (EventCategoryEntry entry in content.Categories)
                if (entry.Category == category) return entry.Title;
            return category.ToString().ToUpperInvariant();
        }

        /// <summary>One distance button: node = alpha bounds of its art, layers = the whole card canvas seen from that node.</summary>
        private static RectTransform DistanceButton(Transform parent, string artName, int number, bool active)
        {
            string stem = $"{artName}_distance_{number}";
            Vector4 b = AlphaBounds(stem + "_normal");

            RectTransform node = Kit.Span(Kit.Node(parent, "Distance" + number), new Vector2(b.x, b.y), new Vector2(b.z, b.w));
            Image hit = HitArea(node.gameObject);
            UiArt.MakeButton(node.gameObject, hit, true, alphaHit: false);

            GameObject normal = CanvasLayer(node, "Normal", Art(stem + "_normal"), b);
            GameObject selected = CanvasLayer(node, "Selected", Art(stem + "_selected"), b);
            normal.SetActive(!active);
            selected.SetActive(active);

            Text label = UiArt.Slot(node, "Label", "100m", DistanceSize, LabelMin, LabelMax);
            label.color = active ? Color.white : UiArt.Navy;
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

        // ---------------------------------------------------------------- bottom row

        private static RectTransform BottomButton(Transform root, UiArt.Layout layout, string nodeName, string artName, string iconName,
            string labelText, out Text label, out Outline mark)
        {
            RectTransform node = Kit.Node(root, nodeName);
            layout.PlaceOnScreen(node, layout.Get(artName));
            Image background = UiArt.Paint(node.gameObject, Art(artName + "_background"), raycast: true);
            UiArt.MakeButton(node.gameObject, background, true, alphaHit: false);

            // Selected mark: the art has no selected state for these two, so a red outline (off until selected).
            mark = Kit.Frame(node.gameObject, MarkWidth);
            mark.effectColor = Kit.MarkerRed;
            mark.enabled = false;

            UiArt.Layer(node, "Icon", Art(iconName));
            Vector4 icon = AlphaBounds(iconName);
            label = UiArt.Slot(node, "Label", labelText, BottomLabelSize,
                new Vector2(icon.z + BottomLabelGap, 0.10f), new Vector2(BottomLabelEdge, 0.90f), TextAnchor.MiddleLeft);
            return node;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Invisible tap area (alpha 0): a Button needs a graphic that catches raycasts, the art layers do not.</summary>
        private static Image HitArea(GameObject go)
        {
            return Kit.Fill(go, new Color(1f, 1f, 1f, 0f), raycast: true);
        }

        private static Sprite Art(string file)
        {
            string path = ArtFolder + "/" + file + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException($"UiEventSelectPrefabs: sprite '{path}' not found or not imported as a Sprite.");
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
                    throw new InvalidOperationException($"UiEventSelectPrefabs: '{path}' could not be read.");

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
                if (maxX < 0) throw new InvalidOperationException($"UiEventSelectPrefabs: '{path}' is fully transparent.");
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
            if (!File.Exists(path)) throw new InvalidOperationException($"UiEventSelectPrefabs: '{LayoutPath}' not found.");
            var file = JsonUtility.FromJson<UiArt.LayoutFile>(File.ReadAllText(path));
            if (file == null || file.reference_size == null || file.reference_size.Length < 2 || file.components == null)
                throw new InvalidOperationException($"UiEventSelectPrefabs: '{LayoutPath}' could not be read.");
            return new UiArt.Layout(file);
        }

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiEventSelectPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            return property;
        }

        private static SerializedProperty Rel(SerializedProperty parent, string field)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            if (property == null)
                throw new InvalidOperationException($"UiEventSelectPrefabs: no serialized field '{field}' under '{parent.propertyPath}'.");
            return property;
        }

        /// <summary>Sets an object reference field; nothing silently stays unbound.</summary>
        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException($"UiEventSelectPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            Find(so, field).objectReferenceValue = value;
        }
    }
}
