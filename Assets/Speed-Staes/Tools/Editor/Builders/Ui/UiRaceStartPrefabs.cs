using System;
using SpeedStars.Core.Race;
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
    /// the 700x200 word box (legacy Text, bold italic, Outline; IMPLEMENTATION.md says TMP, this project has none). No panel background.
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
        private const float WordSize = 150f;          // canvas points inside the 700x200 box (best fit shrinks it if it ever overflows)
        private const float GroupCenterFromTop = 330f; // the group's centre, below the HUD's top row and above the runner
        private static readonly Vector2 OutlineDistance = new Vector2(6f, -6f);

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
            Text word = Kit.Label(textRoot, "Word", "READY", WordSize, config.WordColor);
            var outline = AddRequired<Outline>(word.gameObject);
            outline.effectColor = config.OutlineColor;
            outline.effectDistance = OutlineDistance;
            outline.useGraphicAlpha = false; // the view fades fill and outline together through their own colours

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
