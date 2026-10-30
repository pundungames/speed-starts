using System;
using SpeedStars.Core.Race;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The READY / SET / GO! overlay (UI/Art/RaceStart, Yaris_Baslangic_UI_v1). One prefab: RaceStartRoot (view + a CanvasGroup that blocks
    /// nothing) over a Group of the art's own 1024x576 canvas at the top centre; inside it EffectsRoot holds the 13 line layers, all
    /// stretched over the same canvas at 100% scale (never cropped or moved: they sit on top of each other as drawn), and TextRoot holds
    /// the 700x200 word box (legacy Text in Kanit Black Italic with a ThickOutline; IMPLEMENTATION.md says TMP, this project has none). No panel
    /// background. Size and outline thickness come from the RaceStartConfig (a change needs a rebuild of this prefab).
    /// The group starts hidden; the view shows it. Layer file names are the art's. Never runs in the game.
    /// </summary>
    internal static class UiRaceStartPrefabs
    {
        public const string ArtFolder = UiArt.ArtRoot + "/RaceStart";

        private static readonly string[] Impact = { "impact_01", "impact_02", "impact_03" };
        private static readonly string[] Tension = { "tension_left_01", "tension_left_02", "tension_right_01", "tension_right_02" };
        private static readonly string[] Release = { "release_01", "release_02", "release_03", "release_04", "release_05", "release_06" };

        private static readonly Vector2 ArtCanvas = new Vector2(1024f, 576f);
        private static readonly Vector2 WordBox = new Vector2(700f, 200f);
        public const string FontPath = "Assets/Speed-Staes/UI/Fonts/Kanit/Kanit-BlackItalic.ttf"; // SIL OFL 1.1, OFL.txt beside it
        private const float GroupCenterFromTop = 250f; // the group's centre: under the HUD's top row, the word clear of the runner's head

        public static void BuildAll(Func<string, Action<RectTransform>, GameObject> build)
        {
            RaceStartConfig config = RaceStartConfigSeed.Seed(overwrite: false); // the view reads this asset; seeded when missing, never overwritten
            build(UiPrefabPaths.RaceStart, root => Build(root, config));
        }

        private static void Build(RectTransform root, RaceStartConfig config)
        {
            var view = root.gameObject.AddComponent<RaceStartView>();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform groupRt = Kit.Node(root, "Group");
            Kit.Anchor(groupRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -GroupCenterFromTop), ArtCanvas);
            groupRt.localScale = Vector3.one * config.GroupScale;

            RectTransform effects = Kit.Stretch(Kit.Node(groupRt, "EffectsRoot"));
            Image[] impact = Layers(effects, Impact);
            Image[] tension = Layers(effects, Tension);
            Image[] release = Layers(effects, Release);

            RectTransform textRoot = Kit.Node(groupRt, "TextRoot");
            Kit.Anchor(textRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WordBox);
            Text word = Kit.Label(textRoot, "Word", "READY", config.WordFontSize, config.WordColor);
            StyleWord(word, config);
            var outline = AddRequired<ThickOutline>(word.gameObject);
            outline.Configure(config.OutlineThicknessRatio * config.WordFontSize, config.OutlineColor, config.OutlineDirections);

            groupRt.gameObject.SetActive(false); // the view shows it with the first phase

            var so = new SerializedObject(view);
            Prop(so, "config").objectReferenceValue = config;
            Prop(so, "group").objectReferenceValue = groupRt;
            Prop(so, "effectsRoot").objectReferenceValue = effects;
            Prop(so, "textRoot").objectReferenceValue = textRoot;
            Prop(so, "wordText").objectReferenceValue = word;
            Prop(so, "wordOutline").objectReferenceValue = outline;
            SetObjectList(Prop(so, "impactImages"), impact);
            SetObjectList(Prop(so, "tensionImages"), tension);
            SetObjectList(Prop(so, "releaseImages"), release);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The word is one line that must never wrap, shrink or be cut: fixed size, no best fit, overflow on. The font already is the
        /// black italic cut, so the style stays Normal (Bold/Italic on a dynamic font would fake a second weight and slant).
        /// Without the font file the built-in font stays (a warning says so) and the style falls back to bold italic.
        /// </summary>
        private static void StyleWord(Text word, RaceStartConfig config)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font != null)
            {
                word.font = font;
                word.fontStyle = FontStyle.Normal;
            }
            else
            {
                Warn(nameof(UiRaceStartPrefabs), $"Font '{FontPath}' not found: READY / SET / GO! fall back to the built-in font.");
            }
            word.fontSize = Mathf.RoundToInt(config.WordFontSize);
            word.resizeTextForBestFit = false;
            word.horizontalOverflow = HorizontalWrapMode.Overflow;
            word.verticalOverflow = VerticalWrapMode.Overflow;
            word.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>One full-canvas Image per art file (stretched over the shared 1024x576 canvas, no tap catching).</summary>
        private static Image[] Layers(RectTransform parent, string[] files)
        {
            var images = new Image[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string path = ArtFolder + "/" + files[i] + ".png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) throw new InvalidOperationException($"UiRaceStartPrefabs: sprite '{path}' not found or not imported as a Sprite.");
                images[i] = UiArt.Layer(parent, files[i], sprite);
            }
            return images;
        }
    }
}
