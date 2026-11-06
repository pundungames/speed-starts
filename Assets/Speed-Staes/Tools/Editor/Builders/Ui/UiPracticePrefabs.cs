using System;
using SpeedStars.Meta.Practice;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The Practice panel prefab of the race HUD (T-090) and its config asset. Top-right under the HUD buttons: a "Start:" dropdown
    /// (button + list of option buttons), a Reset energy button and three check rows (Lock energy, Hurdles, Standing start). Every
    /// widget is a prefab object bound to <see cref="PracticePanelView"/>; the view fills the values at run time. <see cref="Build"/> keeps
    /// an existing prefab / asset (the user may have edited them). Never runs in the game.
    /// </summary>
    public static class UiPracticePrefabs
    {
        public const string PrefabPath = UiPrefabPaths.Root + "/Widgets/PracticePanel.prefab";
        public const string ConfigPath = "Assets/Speed-Staes/Data/Config/PracticeConfig.asset";

        private const int OptionSlots = 6;           // dropdown entries the prefab holds; the config may use fewer
        private const float PanelWidth = 560f;
        private const float Pad = 24f;
        private const float RowHeight = 88f;
        private const float RowGap = 16f;
        private const float CaptionWidth = 170f;
        private const float DropdownWidth = 340f;
        private const float OptionHeight = 76f;
        private const float OptionGap = 6f;
        private const float BoxSize = 60f;
        private const float RightInset = -50f;       // the right margin of the HUD's button row
        private const float TopInset = -587f;        // under the right HUD column (Telemetry, Energy, Cadence: bottom at -563) with a 24 gap
        private const float ContentScale = 0.85f;    // so the panel still fits under that column on the shortest HUD (1080 high)
        private const float FontSize = 40f;
        private const float CaptionSize = 38f;

        [MenuItem("Tools/Speed Stars/Build/Practice panel - prefab and config (keeps existing)")]
        public static void Build()
        {
            EnsureConfig();
            BuildPrefab();
        }

        /// <summary>The config asset with the code defaults; an existing one is left as it is.</summary>
        public static PracticeConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PracticeConfig>(ConfigPath);
            if (existing != null) return existing;
            EnsureFolder(System.IO.Path.GetDirectoryName(ConfigPath).Replace('\\', '/'));
            var created = PracticeConfig.CreateDefault();
            AssetDatabase.CreateAsset(created, ConfigPath);
            AssetDatabase.SaveAssets();
            Log(nameof(UiPracticePrefabs), $"Created '{ConfigPath}'.");
            return created;
        }

        /// <summary>The panel prefab; an existing one is loaded and returned unchanged.</summary>
        public static GameObject BuildPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            Kit.Begin();
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Kit.EnsureFolder(System.IO.Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
                RectTransform root = Kit.NewRoot("PracticePanel", scene);
                Fill(root);
                GameObject saved = HandEditGuard.SaveNewPrefab(root.gameObject, PrefabPath);
                UnityEngine.Object.DestroyImmediate(root.gameObject);
                return saved;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                HandEditGuard.ConvertPending(); // a fresh panel holds legacy Text: TextMeshPro like the other prefabs (T-100)
            }
        }

        private static void Fill(RectTransform root)
        {
            var view = root.gameObject.AddComponent<PracticePanelView>();

            var topRight = Vector2.one;
            float rows = 5f;
            float height = Pad * 2f + rows * RowHeight + (rows - 1f) * RowGap;
            RectTransform content = Kit.Node(root, "Content");
            Kit.Anchor(content, topRight, topRight, new Vector2(RightInset, TopInset), new Vector2(PanelWidth, height));
            content.localScale = new Vector3(ContentScale, ContentScale, 1f);
            Kit.Panel(content.gameObject, Kit.SoftColor, frame: true, raycast: true);

            float innerWidth = PanelWidth - Pad * 2f;

            // Row 0: "Start:" caption + the dropdown button.
            RectTransform row0 = Row(content, "StartRow", 0, innerWidth);
            Text caption = Kit.Label(row0, "Caption", "Start:", CaptionSize, Kit.InkColor, TextAnchor.MiddleLeft);
            caption.rectTransform.offsetMax = new Vector2(-(innerWidth - CaptionWidth), 0f);
            RectTransform dropdown = Kit.TextButton(row0, "DistanceButton", "100m", FontSize, Kit.InkColor, out Text distanceLabel);
            Kit.Anchor(dropdown, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(DropdownWidth, RowHeight));

            // Rows 1-4 are built first so the list (drawn last) lies over them.
            RectTransform reset = Kit.TextButton(Row(content, "ResetEnergyRow", 1, innerWidth), "ResetEnergyButton", "Reset energy", FontSize, Kit.InkColor);
            Kit.Stretch(reset);
            reset.gameObject.AddComponent<CanvasGroup>();

            CheckRowParts lockRow = CheckRow(Row(content, "LockEnergyRow", 2, innerWidth), "Lock energy");
            CheckRowParts hurdlesRow = CheckRow(Row(content, "HurdlesRow", 3, innerWidth), "Hurdles");
            CheckRowParts standingRow = CheckRow(Row(content, "StandingStartRow", 4, innerWidth), "Standing start");

            // The open list: under the dropdown button, over the rows below.
            float listHeight = OptionSlots * OptionHeight + (OptionSlots - 1) * OptionGap;
            RectTransform list = Kit.Node(content, "DistanceList");
            Kit.Anchor(list, topRight, topRight, new Vector2(-Pad, -(Pad + RowHeight + OptionGap)), new Vector2(DropdownWidth, listHeight));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = OptionGap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var optionButtons = new Button[OptionSlots];
            var optionLabels = new Text[OptionSlots];
            for (int i = 0; i < OptionSlots; i++)
            {
                RectTransform option = Kit.TextButton(list, "Option" + (i + 1), "100m", FontSize, Kit.InkColor, out optionLabels[i]);
                Kit.PreferredHeight(option.gameObject, OptionHeight);
                optionButtons[i] = option.GetComponent<Button>();
            }

            var so = new SerializedObject(view);
            Prop(so, "content").objectReferenceValue = content.gameObject;
            Prop(so, "distanceButton").objectReferenceValue = dropdown.GetComponent<Button>();
            Prop(so, "distanceLabel").objectReferenceValue = distanceLabel;
            Prop(so, "distanceList").objectReferenceValue = list.gameObject;
            SetObjectList(Prop(so, "distanceOptions"), optionButtons);
            SetObjectList(Prop(so, "distanceOptionLabels"), optionLabels);
            Prop(so, "resetEnergyButton").objectReferenceValue = reset.GetComponent<Button>();
            BindRow(so, "lockEnergy", lockRow);
            BindRow(so, "hurdles", hurdlesRow);
            BindRow(so, "standingStart", standingRow);
            Apply(so);
            list.gameObject.SetActive(false);    // closed until Start is pressed
            content.gameObject.SetActive(false); // the view shows it in Practice only
        }

        private struct CheckRowParts
        {
            public Button button;
            public Image check;
        }

        private static void BindRow(SerializedObject so, string field, CheckRowParts parts)
        {
            SerializedProperty row = Prop(so, field);
            Rel(row, "button").objectReferenceValue = parts.button;
            Rel(row, "check").objectReferenceValue = parts.check;
        }

        /// <summary>A row slot inside the panel: <paramref name="index"/> rows down from the top, full inner width.</summary>
        private static RectTransform Row(RectTransform content, string name, int index, float innerWidth)
        {
            RectTransform row = Kit.Node(content, name);
            Kit.Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(Pad + index * (RowHeight + RowGap))),
                new Vector2(innerWidth, RowHeight));
            return row;
        }

        /// <summary>A pressable row: caption on the left, a box on the right that shows a check mark while on.</summary>
        private static CheckRowParts CheckRow(RectTransform row, string caption)
        {
            Image background = Kit.Panel(row.gameObject, Kit.PanelColor, frame: true, raycast: true);
            Button button = Kit.Clickable(row.gameObject, background);
            row.gameObject.AddComponent<CanvasGroup>();

            Text label = Kit.Label(row, "Label", caption, FontSize, Kit.InkColor, TextAnchor.MiddleLeft);
            Kit.Margin(label, 20f, 4f, BoxSize + 40f, 4f);

            RectTransform box = Kit.Node(row, "Box");
            Kit.Anchor(box, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(BoxSize, BoxSize));
            Kit.Panel(box.gameObject, Kit.SoftColor, frame: true, raycast: false);
            Image check = Kit.Icon(box, "Check", UiShape.Check, Kit.GreenText);
            Kit.Stretch(check.rectTransform, 6f, 6f, 6f, 6f);
            return new CheckRowParts { button = button, check = check };
        }
    }
}
