using System;
using System.IO;
using SpeedStars.UI.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Settings window prefabs dressed with the designer's art (UI/Art/Settings, package Settings_Moduler_UI_v1):
    /// SettingsRow (icon + label + control slot, copied for every setting), LanguageRow, FlagCell and the SettingsPopup
    /// that nests four SettingsRow instances (Music, Language, Flag, Player Name) and holds the two list windows on its own
    /// top layer. Sizes are the art's native pixels times <see cref="Scale"/> (the approved reference was drawn for a
    /// 937 px tall screen; the canvas is 1080 tall). 9-slice borders are import settings written here from the package's
    /// manifest.json. Never runs in the game.
    /// </summary>
    internal static class UiSettingsPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/Settings";

        private const float ReferenceHeight = 937f; // height of approved_design_reference.jpg (the art's native scale)
        private static readonly float Scale = UiArt.CanvasHeight / ReferenceHeight;

        // Package geometry, art pixels (IMPLEMENTATION.md).
        private const float PanelWidth = 832f, HeaderHeight = 128f, FooterHeight = 112f, BodyHeight = 540f;
        private const float RowWidth = 752f, RowHeight = 112f, RowSpacing = 8f;
        private const float ViewportLeft = 24f, ViewportRight = 56f, ErrorStrip = 52f;
        private const float ScrollbarWidth = 32f, ScrollbarInset = 16f;
        private const float SlotRight = 30f, SlotWidth = 356f, SlotHeight = 88f;
        private const float ToggleWidth = 192f, ToggleHeight = 88f, FieldHeight = 80f;
        private const float ListRowWidth = 368f, ListRowHeight = 64f;
        private const float FlagCellWidth = 160f, FlagCellHeight = 112f;
        private const float LabelSize = 36f, TitleSize = 64f, FieldTextSize = 34f, ErrorSize = 30f, DoneSize = 46f;
        private const int FlagColumns = 4;

        // 9-slice borders from manifest.json "slicing" as left, top, right, bottom (Unity wants left, bottom, right, top).
        private static readonly (string File, int L, int T, int R, int B)[] Slices =
        {
            ("panel_header", 28, 24, 28, 12),
            ("panel_body_tile", 12, 0, 12, 0),
            ("panel_footer", 28, 12, 28, 24),
            ("row_normal", 22, 22, 22, 22),
            ("row_hover", 22, 22, 22, 22),
            ("row_selected", 22, 22, 22, 22),
            ("row_disabled", 22, 22, 22, 22),
            ("field_normal", 20, 20, 20, 20),
            ("field_focused", 20, 20, 20, 20),
            ("field_disabled", 20, 20, 20, 20),
            ("field_error", 20, 20, 20, 20),
        };

        private const string TrackTight = "scrollbar_track_tight";
        private const string HandleTight = "scrollbar_handle_tight";

        public static readonly string[] FlagCodes = { "TR", "US", "GB", "DE", "FR", "IT", "NL", "BE", "PL", "UA", "JP", "SE", "NO", "DK", "FI", "CH" };

        // ---------------------------------------------------------------- art

        /// <summary>Imports the settings art as sprites, writes the 9-slice borders and the two cropped scrollbar sprites. Idempotent.</summary>
        public static void EnsureArt()
        {
            CropToAlpha("scrollbar_empty", TrackTight);
            CropToAlpha("scrollbar_handle", HandleTight);
            UiArt.ImportAll(); // sprite import settings for every PNG under UI/Art, the new ones included

            foreach ((string file, int l, int t, int r, int b) in Slices) SetBorder(file, new Vector4(l, b, r, t));
            foreach (string file in new[] { TrackTight, HandleTight })
            {
                int half = Mathf.Max(1, Mathf.RoundToInt(SpriteWidth(file) * 0.5f) - 1);
                SetBorder(file, new Vector4(half, half, half, half));
            }
        }

        public static Sprite FlagSprite(string code)
        {
            EnsureArt();
            return Art("flag_" + code);
        }

        private static void SetBorder(string file, Vector4 border)
        {
            string path = ArtPath(file);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException($"UiSettingsPrefabs: '{path}' is not imported as a texture.");
            if (importer.spriteBorder == border) return;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static float SpriteWidth(string file) => Art(file).rect.width;

        /// <summary>Writes a copy of an art PNG cropped to its visible pixels (the scrollbar art sits on a 32x512 canvas that is mostly empty).</summary>
        private static void CropToAlpha(string source, string target)
        {
            string targetPath = ArtPath(target);
            if (File.Exists(Kit.FullPath(targetPath))) return;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(ArtPath(source)))))
                    throw new InvalidOperationException($"UiSettingsPrefabs: '{ArtPath(source)}' could not be read.");

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
                if (maxX < 0) throw new InvalidOperationException($"UiSettingsPrefabs: '{source}' is fully transparent.");

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

        /// <summary>Visible-pixel bounds of an art PNG as fractions of its canvas (x, y from bottom, x2, y2).</summary>
        private static Vector4 AlphaBounds(string file)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Kit.FullPath(ArtPath(file)))))
                    throw new InvalidOperationException($"UiSettingsPrefabs: '{ArtPath(file)}' could not be read.");
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
                if (maxX < 0) throw new InvalidOperationException($"UiSettingsPrefabs: '{file}' is fully transparent.");
                return new Vector4(minX / (float)width, minY / (float)height, (maxX + 1) / (float)width, (maxY + 1) / (float)height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ArtPath(string file) => ArtFolder + "/" + file + ".png";

        private static Sprite Art(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath(file));
            if (sprite == null) throw new InvalidOperationException($"UiSettingsPrefabs: sprite '{ArtPath(file)}' not found or not imported as a Sprite.");
            return sprite;
        }

        // ---------------------------------------------------------------- entry

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build, UnityEngine.SceneManagement.Scene scene)
        {
            EnsureArt();
            GameObject row = build(UiPrefabPaths.SettingsRow, BuildRow);
            GameObject languageRow = build(UiPrefabPaths.LanguageRow, BuildLanguageRow);
            GameObject flagCell = build(UiPrefabPaths.FlagCell, BuildFlagCell);
            var languageView = UiViewBinder.RowComponent<LanguageRowView>(languageRow, UiPrefabPaths.LanguageRow);
            var flagView = UiViewBinder.RowComponent<FlagCellView>(flagCell, UiPrefabPaths.FlagCell);
            build(UiPrefabPaths.SettingsPopup, root => BuildPopup(root, row, languageView, flagView, scene));
        }

        // ---------------------------------------------------------------- row prefabs

        private static void BuildRow(RectTransform root)
        {
            Kit.PreferredHeight(root.gameObject, P(RowHeight));
            Sliced(root.gameObject, Art("row_normal"), raycast: false);
            UiArt.Layer(root, "Icon", Art("row_music_icon"));

            Text label = Label(root, "Label", "Setting", LabelSize, TextAnchor.MiddleLeft);
            Kit.Span(label.rectTransform, new Vector2(128f / RowWidth, 0f), new Vector2((RowWidth - SlotRight - SlotWidth - 10f) / RowWidth, 1f));

            RectTransform slot = Kit.Node(root, "ControlSlot");
            Kit.Anchor(slot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-P(SlotRight), 0f), V(SlotWidth, SlotHeight));
        }

        private static void BuildLanguageRow(RectTransform root)
        {
            Kit.PreferredHeight(root.gameObject, P(ListRowHeight));
            var view = root.gameObject.AddComponent<LanguageRowView>();
            Image background = UiArt.Paint(root.gameObject, Art("list_row_normal"), raycast: true);
            Button button = Clickable(root.gameObject, background);

            Text label = Label(root, "Label", "English", 30f, TextAnchor.MiddleLeft);
            Kit.Span(label.rectTransform, new Vector2(0.07f, 0f), new Vector2(0.80f, 1f));
            Image check = UiArt.Layer(root, "Check", Art("list_selected_check"));
            check.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "background", background);
            Ref(so, "label", label);
            Ref(so, "checkMark", check.gameObject);
            Ref(so, "normalSprite", Art("list_row_normal"));
            Ref(so, "selectedSprite", Art("list_row_selected"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildFlagCell(RectTransform root)
        {
            Kit.Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(FlagCellWidth, FlagCellHeight));
            var view = root.gameObject.AddComponent<FlagCellView>();
            Image frame = UiArt.Paint(root.gameObject, Art("flag_cell_normal"), raycast: true);
            Button button = Clickable(root.gameObject, frame);

            Image flag = UiArt.Layer(root, "Flag", Art("flag_TR")); // sample picture; the view fills the real one
            flag.enabled = false;
            Image check = UiArt.Layer(root, "Check", Art("flag_cell_selected_check"));
            check.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "frame", frame);
            Ref(so, "flag", flag);
            Ref(so, "checkMark", check.gameObject);
            Ref(so, "normalSprite", Art("flag_cell_normal"));
            Ref(so, "selectedSprite", Art("flag_cell_selected"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------------------------------------------------------------- popup

        private static void BuildPopup(RectTransform root, GameObject rowPrefab, LanguageRowView languageRowPrefab,
            FlagCellView flagCellPrefab, UnityEngine.SceneManagement.Scene scene)
        {
            var view = root.gameObject.AddComponent<SettingsPopupView>();

            RectTransform dim = Kit.Stretch(Kit.Node(root, "Dimscreen"));
            UiArt.Paint(dim.gameObject, Art("dimscreen"), raycast: true); // full screen, swallows taps meant for the menu

            float panelHeight = HeaderHeight + BodyHeight + FooterHeight;
            RectTransform panel = Kit.Node(root, "Panel");
            Kit.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(PanelWidth, panelHeight));

            // ---- header
            RectTransform header = Kit.Node(panel, "Header");
            Kit.TopBand(header, 0f, P(HeaderHeight));
            Sliced(header.gameObject, Art("panel_header"), raycast: false);
            UiArt.Layer(header, "GearIcon", Art("header_gear_icon"));
            Text title = Label(header, "Title", "SETTINGS", TitleSize, TextAnchor.MiddleLeft);
            Kit.Span(title.rectTransform, new Vector2(150f / PanelWidth, 0f), new Vector2(620f / PanelWidth, 0.96f));

            RectTransform close = Kit.Node(header, "Close");
            Kit.Anchor(close, Vector2.one, Vector2.one, new Vector2(-P(36f), -P(12f)), V(104f, 104f));
            Image closeImage = UiArt.Paint(close.gameObject, Art("close_background"), raycast: true);
            UiArt.Layer(close, "Icon", Art("close_icon"));
            UiArt.MakeButton(close.gameObject, closeImage, true, alphaHit: false);

            // ---- body: background, the scrolling rows, the error line
            RectTransform body = Kit.Node(panel, "Body");
            Kit.Stretch(body, 0f, 0f, P(HeaderHeight), P(FooterHeight));
            Sliced(body.gameObject, Art("panel_body_tile"), raycast: true);

            ScrollArea(body, "Scroll", out RectTransform viewport, out RectTransform content, out ScrollRect scroll);
            Kit.Stretch(viewport, P(ViewportLeft), P(ViewportRight), 0f, P(ErrorStrip));
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, Mathf.RoundToInt(P(RowSpacing)), Mathf.RoundToInt(P(RowSpacing)));
            layout.spacing = P(RowSpacing);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            AddScrollbar(body, scroll);

            RectTransform musicRow = Row(rowPrefab, content, "MusicRow", "row_music_icon", "Music", scene);
            RectTransform languageRow = Row(rowPrefab, content, "LanguageRow", "row_language_icon", "Language", scene);
            RectTransform flagRow = Row(rowPrefab, content, "FlagRow", "row_flag_icon", "Flag", scene);
            RectTransform nameRow = Row(rowPrefab, content, "NameRow", "row_player_icon", "Player Name", scene);

            SettingsToggleView toggle = BuildToggle(Slot(musicRow));
            BuildDropdownField(Slot(languageRow), "LanguageField", withFlag: false, out Button languageButton, out Text languageValue,
                out Image languageArrow, out _);
            BuildDropdownField(Slot(flagRow), "FlagField", withFlag: true, out Button flagButton, out _, out Image flagArrow, out Image flagValue);
            BuildNameField(Slot(nameRow), out InputField input, out Image inputImage, out Text placeholder, out RectTransform inputRect);

            Text error = Label(body, "ErrorText", string.Empty, ErrorSize, TextAnchor.MiddleCenter);
            error.color = Kit.MarkerRed;
            Kit.Place(error.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, P(4f)), new Vector2(-2f * P(ViewportLeft), P(ErrorStrip - 8f)));

            // ---- footer
            RectTransform footer = Kit.Node(panel, "Footer");
            Kit.BottomBand(footer, 0f, P(FooterHeight));
            Sliced(footer.gameObject, Art("panel_footer"), raycast: true);
            RectTransform done = Kit.Node(footer, "Done");
            Kit.Anchor(done, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(320f, 88f));
            Image doneImage = UiArt.Paint(done.gameObject, Art("done_normal_background"), raycast: true);
            Button doneButton = UiArt.MakeButton(done.gameObject, doneImage, true, alphaHit: false);
            doneButton.transition = Selectable.Transition.SpriteSwap;
            doneButton.spriteState = new SpriteState
            {
                pressedSprite = Art("done_pressed_background"),
                disabledSprite = Art("done_disabled_background")
            };
            Text doneLabel = Label(done, "Label", "Done", DoneSize, TextAnchor.MiddleCenter);
            doneLabel.color = Color.white;
            Kit.Margin(doneLabel, 16f, 8f, 16f, 12f);

            // ---- the two list windows on their own top layer (never clipped by the body's viewport)
            RectTransform layer = Kit.Stretch(Kit.Node(root, "PopupLayer"));
            BuildLanguageWindow(layer, panelHeight, out GameObject languageWindow, out Button languageCatcher,
                out RectTransform languageContent);
            BuildFlagWindow(layer, out GameObject flagWindow, out Button flagCatcher, out RectTransform flagContent);

            var so = new SerializedObject(view);
            Ref(so, "titleText", title);
            Ref(so, "musicLabel", musicRow.Find("Label").GetComponent<Text>());
            Ref(so, "languageLabel", languageRow.Find("Label").GetComponent<Text>());
            Ref(so, "flagLabel", flagRow.Find("Label").GetComponent<Text>());
            Ref(so, "nameLabel", nameRow.Find("Label").GetComponent<Text>());
            Ref(so, "doneLabel", doneLabel);
            Ref(so, "musicToggle", toggle);
            Ref(so, "languageButton", languageButton);
            Ref(so, "languageValueText", languageValue);
            Ref(so, "languageArrow", languageArrow);
            Ref(so, "languageWindow", languageWindow);
            Ref(so, "languageCatcher", languageCatcher);
            Ref(so, "languageContent", languageContent);
            Ref(so, "languageRowPrefab", languageRowPrefab);
            Ref(so, "flagButton", flagButton);
            Ref(so, "flagValueImage", flagValue);
            Ref(so, "flagArrow", flagArrow);
            Ref(so, "flagWindow", flagWindow);
            Ref(so, "flagCatcher", flagCatcher);
            Ref(so, "flagContent", flagContent);
            Ref(so, "flagCellPrefab", flagCellPrefab);
            Ref(so, "arrowDown", Art("dropdown_arrow_down"));
            Ref(so, "arrowUp", Art("dropdown_arrow_up"));
            Ref(so, "nameInput", input);
            Ref(so, "nameFieldImage", inputImage);
            Ref(so, "namePlaceholder", placeholder);
            Ref(so, "nameErrorText", error);
            Ref(so, "nameShakeTarget", inputRect);
            Ref(so, "nameNormalSprite", Art("field_normal"));
            Ref(so, "nameFocusedSprite", Art("field_focused"));
            Ref(so, "nameErrorSprite", Art("field_error"));
            Ref(so, "doneButton", doneButton);
            so.ApplyModifiedPropertiesWithoutUndo();

            UiViewBinder.BindClose(root, "Panel/Header/Close"); // base UiPanel.closeButton: closes this popup through the navigator
        }

        /// <summary>One SettingsRow instance with its icon and label set (recorded as overrides of the nested prefab).</summary>
        private static RectTransform Row(GameObject prefab, Transform parent, string name, string iconFile, string caption,
            UnityEngine.SceneManagement.Scene scene)
        {
            RectTransform row = Kit.Nest(prefab, parent, name, scene);
            if (row == null) throw new InvalidOperationException($"UiSettingsPrefabs: '{UiPrefabPaths.SettingsRow}' could not be nested.");

            Image icon = row.Find("Icon").GetComponent<Image>();
            icon.sprite = Art(iconFile);
            Kit.Record(icon);
            Text label = row.Find("Label").GetComponent<Text>();
            label.text = caption;
            Kit.Record(label);
            return row;
        }

        private static RectTransform Slot(RectTransform row) => (RectTransform)row.Find("ControlSlot");

        // ---------------------------------------------------------------- controls

        private static SettingsToggleView BuildToggle(RectTransform slot)
        {
            RectTransform node = Kit.Node(slot, "MusicToggle");
            Kit.Anchor(node, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, V(ToggleWidth, ToggleHeight));
            var view = node.gameObject.AddComponent<SettingsToggleView>();

            Image track = UiArt.Paint(node.gameObject, Art("toggle_on_track"), raycast: true);
            Button button = Clickable(node.gameObject, track);
            Image thumb = UiArt.Layer(node, "Thumb", Art("toggle_off_thumb")); // rests at the off side, the view slides it right
            Text state = Label(node, "StateLabel", "ON", 30f, TextAnchor.MiddleLeft);
            state.color = Color.white;

            Vector4 off = AlphaBounds("toggle_off_thumb");
            Vector4 on = AlphaBounds("toggle_on_thumb");
            float travel = ((on.x + on.z) - (off.x + off.z)) * 0.5f * P(ToggleWidth); // centre of the on thumb minus the off thumb, canvas px

            var so = new SerializedObject(view);
            Ref(so, "button", button);
            Ref(so, "track", track);
            Ref(so, "thumb", thumb.rectTransform);
            Ref(so, "stateLabel", state);
            Ref(so, "onTrack", Art("toggle_on_track"));
            Ref(so, "offTrack", Art("toggle_off_track"));
            so.FindProperty("thumbTravel").floatValue = travel;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void BuildDropdownField(RectTransform slot, string name, bool withFlag, out Button button, out Text value,
            out Image arrow, out Image flag)
        {
            RectTransform field = Kit.Node(slot, name);
            Kit.Anchor(field, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(SlotWidth, FieldHeight));
            Image background = Sliced(field.gameObject, Art("field_normal"), raycast: true);
            button = Clickable(field.gameObject, background);

            value = Label(field, "Value", "English", FieldTextSize, TextAnchor.MiddleLeft);
            Kit.Span(value.rectTransform, new Vector2(24f / SlotWidth, 0f), new Vector2(280f / SlotWidth, 1f));
            arrow = UiArt.Layer(field, "Arrow", Art("dropdown_arrow_down"));

            flag = null;
            if (withFlag)
            {
                value.gameObject.SetActive(false);
                RectTransform flagRect = Kit.Node(field, "Flag");
                Kit.Anchor(flagRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(P(14f), 0f), V(FlagCellWidth * 0.7f, FlagCellHeight * 0.7f));
                flag = flagRect.gameObject.AddComponent<Image>();
                flag.sprite = Art("flag_TR"); // sample; the view shows the chosen flag
                flag.preserveAspect = true;
                flag.raycastTarget = false;
            }
        }

        private static void BuildNameField(RectTransform slot, out InputField input, out Image background, out Text placeholder,
            out RectTransform rect)
        {
            rect = Kit.Node(slot, "NameField");
            Kit.Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(SlotWidth, FieldHeight));
            background = Sliced(rect.gameObject, Art("field_normal"), raycast: true);
            input = rect.gameObject.AddComponent<InputField>();

            Text text = Label(rect, "Text", string.Empty, FieldTextSize, TextAnchor.MiddleLeft);
            text.resizeTextForBestFit = false;
            text.fontSize = Mathf.RoundToInt(P(FieldTextSize));
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            Kit.Margin(text, P(24f), 0f, P(24f), 0f);

            placeholder = Label(rect, "Placeholder", "Enter name", FieldTextSize, TextAnchor.MiddleLeft);
            placeholder.resizeTextForBestFit = false;
            placeholder.fontSize = Mathf.RoundToInt(P(FieldTextSize));
            placeholder.verticalOverflow = VerticalWrapMode.Overflow;
            placeholder.color = new Color(UiArt.Navy.r, UiArt.Navy.g, UiArt.Navy.b, 0.4f);
            Kit.Margin(placeholder, P(24f), 0f, P(24f), 0f);

            input.targetGraphic = background;
            input.transition = Selectable.Transition.None; // the view swaps the field sprite (normal / typing / refused)
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.selectionColor = new Color(UiArt.Navy.r, UiArt.Navy.g, UiArt.Navy.b, 0.25f);
            input.caretColor = UiArt.Navy;
        }

        // ---------------------------------------------------------------- list windows

        private static void BuildLanguageWindow(RectTransform layer, float panelHeight,
            out GameObject window, out Button catcher, out RectTransform content)
        {
            RectTransform root = Catcher(layer, "LanguagePopup", out catcher);
            window = root.gameObject;

            // Under the Language field: the field's centre is 152 art px right of the panel centre, its bottom edge 356 px below the panel top.
            float fieldCentre = ViewportLeft + RowWidth - SlotRight - SlotWidth * 0.5f - PanelWidth * 0.5f;
            float below = HeaderHeight + RowSpacing + (RowHeight + RowSpacing) + (RowHeight + FieldHeight) * 0.5f + 4f;
            RectTransform win = Kit.Node(root, "Window");
            Kit.Anchor(win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(P(fieldCentre), P(panelHeight * 0.5f - below)), V(400f, 336f));
            UiArt.Paint(win.gameObject, Art("language_popup_background"), raycast: true);

            ScrollArea(win, "Scroll", out RectTransform viewport, out content, out _);
            Kit.Stretch(viewport, P(16f), P(16f), P(16f), P(16f));
            VerticalLayoutGroup list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = P(RowSpacing);
            list.childAlignment = TextAnchor.UpperCenter;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            root.gameObject.SetActive(false);
        }

        private static void BuildFlagWindow(RectTransform layer, out GameObject window, out Button catcher, out RectTransform content)
        {
            RectTransform root = Catcher(layer, "FlagPopup", out catcher);
            window = root.gameObject;

            RectTransform win = Kit.Node(root, "Window"); // centred on the screen: the grid is taller than the room under the Flag field
            Kit.Anchor(win, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(704f, 576f));
            UiArt.Paint(win.gameObject, Art("flag_popup_background"), raycast: true);

            ScrollArea(win, "Scroll", out RectTransform viewport, out content, out _);
            Kit.Stretch(viewport);
            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(Mathf.RoundToInt(P(20f)), Mathf.RoundToInt(P(20f)), Mathf.RoundToInt(P(36f)), Mathf.RoundToInt(P(36f)));
            grid.cellSize = V(FlagCellWidth, FlagCellHeight);
            grid.spacing = V(8f, 16f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = FlagColumns;
            root.gameObject.SetActive(false);
        }

        /// <summary>A full-screen invisible button: a tap outside the window under it closes the list.</summary>
        private static RectTransform Catcher(RectTransform layer, string name, out Button button)
        {
            RectTransform root = Kit.Stretch(Kit.Node(layer, name));
            Image image = Kit.Fill(root.gameObject, new Color(0f, 0f, 0f, 0f), raycast: true);
            button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return root;
        }

        // ---------------------------------------------------------------- scroll and graphics helpers

        /// <summary>ScrollRect > Viewport (RectMask2D) > Content (top-anchored, grows with ContentSizeFitter). Layout group goes on the content afterwards.</summary>
        private static void ScrollArea(Transform parent, string name, out RectTransform viewport, out RectTransform content, out ScrollRect scroll)
        {
            RectTransform area = Kit.Stretch(Kit.Node(parent, name));
            scroll = area.gameObject.AddComponent<ScrollRect>();
            viewport = Kit.Stretch(Kit.Node(area, "Viewport"));
            viewport.gameObject.AddComponent<RectMask2D>();
            Kit.Fill(viewport.gameObject, new Color(0f, 0f, 0f, 0f), raycast: true); // empty space must still drag the list

            content = Kit.Place(Kit.Node(viewport, "Content"), new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
        }

        /// <summary>Vertical scrollbar at the body's right edge; hidden while the rows fit (AutoHide).</summary>
        private static void AddScrollbar(RectTransform body, ScrollRect scroll)
        {
            RectTransform bar = Kit.Node(body, "Scrollbar");
            bar.anchorMin = new Vector2(1f, 0f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(1f, 0.5f);
            bar.offsetMin = new Vector2(-P(ScrollbarInset + ScrollbarWidth), P(ErrorStrip));
            bar.offsetMax = new Vector2(-P(ScrollbarInset), -P(8f));
            Image track = Sliced(bar.gameObject, Art(TrackTight), raycast: true);

            RectTransform sliding = Kit.Stretch(Kit.Node(bar, "SlidingArea"));
            RectTransform handle = Kit.Stretch(Kit.Node(sliding, "Handle"));
            Image handleImage = Sliced(handle.gameObject, Art(HandleTight), raycast: true);

            Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.transition = Selectable.Transition.None;
            track.raycastTarget = true;

            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        /// <summary>Navy bold label (the art's look; the kit label is bold italic).</summary>
        private static Text Label(Transform parent, string name, string text, float artSize, TextAnchor alignment)
        {
            Text label = Kit.Label(parent, name, text, P(artSize), UiArt.Navy, alignment);
            label.fontStyle = FontStyle.Bold;
            return label;
        }

        /// <summary>Sliced image with the sprite's 9-slice border drawn at the same scale as the rect (art px times Scale).</summary>
        private static Image Sliced(GameObject go, Sprite sprite, bool raycast)
        {
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 1f / Scale;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Button without colour tint (the art carries its own states) + the shared press feedback.</summary>
        private static Button Clickable(GameObject go, Graphic target)
        {
            Button button = Kit.Clickable(go, target);
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static float P(float artPixels) => artPixels * Scale;

        private static Vector2 V(float x, float y) => new Vector2(P(x), P(y));

        private static void Ref(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"UiSettingsPrefabs: {so.targetObject.GetType().Name} has no serialized field '{field}'.");
            if (value == null)
                throw new InvalidOperationException($"UiSettingsPrefabs: {so.targetObject.GetType().Name}.{field}: nothing to assign.");
            property.objectReferenceValue = value;
        }
    }
}
