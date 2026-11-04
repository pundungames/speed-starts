using System;
using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kit = SpeedStars.Tools.Builders.UiBuildKit;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The first race's tutorial: its config asset, its prefab (two half-screen dark layers, a TAP! word on each half, a banner with the
    /// sentence; RaceTutorialView and RaceTutorial on the root; nothing catches a touch) and its place on the Race scene's HUD canvas, over
    /// the HUD and the countdown's dim, so WAIT FOR GO! reads. Everything is kept when it exists: the user may have edited it. Never runs in the game.
    /// </summary>
    public static class UiRaceTutorialPrefabs
    {
        public const string OkMarker = "RACE_TUTORIAL_PATCH_OK";
        public const string ConfigPath = ConfigAssetSeeder.Folder + "/RaceTutorialConfig.asset";

        private const string Source = nameof(UiRaceTutorialPrefabs);
        private const float DimAlpha = 0.55f;
        private const float HintFontSize = 170f;
        private const float BannerScale = 1f;
        private const float BannerFromBottomShare = 0.115f;
        private const float SentenceFontSize = 62f;
        private static readonly Vector2 LeftHintAnchor = new Vector2(0.25f, 0.5f);
        private static readonly Vector2 RightHintAnchor = new Vector2(0.75f, 0.5f);
        private static readonly Vector2 HintSize = new Vector2(520f, 240f);
        private static readonly Vector2 BannerCanvas = new Vector2(864f, 184f); // the art's canvas, as in UiRaceStartPrefabs
        private static readonly Vector2 SentenceInset = new Vector2(70f, 20f);  // inside the banner's pointed ends

        public static RaceTutorialConfig EnsureConfig()
        {
            EnsureFolder(ConfigAssetSeeder.Folder);
            var config = CreateOrReset<RaceTutorialConfig>(ConfigPath, false);
            if (config == null)
            {
                Skipped(Source, ConfigPath);
                return AssetDatabase.LoadAssetAtPath<RaceTutorialConfig>(ConfigPath);
            }
            Log(Source, $"Created {ConfigPath} with the field defaults.");
            return config;
        }

        /// <summary>The prefab; an existing one is loaded and returned unchanged.</summary>
        public static GameObject BuildPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPaths.RaceTutorial);
            if (existing != null) return existing;

            RaceTutorialConfig config = EnsureConfig();
            RaceStartConfig look = RaceStartConfigSeed.Seed(overwrite: false); // the same red, navy and banner as the countdown
            Kit.Begin();
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                Kit.EnsureFolder(System.IO.Path.GetDirectoryName(UiPrefabPaths.RaceTutorial).Replace('\\', '/'));
                RectTransform root = Kit.NewRoot("RaceTutorial", scene);
                Fill(root, config, look);
                GameObject saved = HandEditGuard.SaveNewPrefab(root.gameObject, UiPrefabPaths.RaceTutorial);
                UnityEngine.Object.DestroyImmediate(root.gameObject);
                return saved;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Fill(RectTransform root, RaceTutorialConfig config, RaceStartConfig look)
        {
            var view = root.gameObject.AddComponent<RaceTutorialView>();
            var tutorial = root.gameObject.AddComponent<RaceTutorial>();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            var dim = new Color(0f, 0f, 0f, DimAlpha);
            RectTransform leftShade = Kit.Span(Kit.Node(root, "LeftShade"), Vector2.zero, new Vector2(0.5f, 1f));
            Kit.Fill(leftShade.gameObject, dim, raycast: false);
            RectTransform rightShade = Kit.Span(Kit.Node(root, "RightShade"), new Vector2(0.5f, 0f), Vector2.one);
            Kit.Fill(rightShade.gameObject, dim, raycast: false);

            RectTransform leftHint = Hint(root, "LeftHint", LeftHintAnchor, look);
            RectTransform rightHint = Hint(root, "RightHint", RightHintAnchor, look);

            RectTransform box = Kit.Node(root, "TextBox");
            Kit.Anchor(box, new Vector2(0.5f, BannerFromBottomShare), new Vector2(0.5f, 0.5f), Vector2.zero, BannerCanvas * BannerScale);
            string bannerPath = UiRaceStartPrefabs.BannerFolder + "/start_banner_background.png";
            var banner = AssetDatabase.LoadAssetAtPath<Sprite>(bannerPath);
            if (banner == null) throw new InvalidOperationException($"{Source}: sprite '{bannerPath}' not found or not imported as a Sprite.");
            UiArt.Layer(box, "Banner", banner);
            RectTransform sentenceRt = Kit.Stretch(Kit.Node(box, "Sentence"), SentenceInset.x, SentenceInset.x, SentenceInset.y, SentenceInset.y);
            TextMeshProUGUI sentence = Label(sentenceRt, RaceTutorialTexts.WaitForGo, SentenceFontSize, look.ReadySetColor);
            sentence.enableAutoSizing = true;
            sentence.fontSizeMin = SentenceFontSize * 0.5f;
            sentence.fontSizeMax = SentenceFontSize;

            leftShade.gameObject.SetActive(false);
            rightShade.gameObject.SetActive(false);
            leftHint.gameObject.SetActive(false);
            rightHint.gameObject.SetActive(false);
            box.gameObject.SetActive(false);

            var viewSo = new SerializedObject(view);
            Prop(viewSo, "config").objectReferenceValue = config;
            Prop(viewSo, "leftShade").objectReferenceValue = leftShade.gameObject;
            Prop(viewSo, "rightShade").objectReferenceValue = rightShade.gameObject;
            Prop(viewSo, "leftHint").objectReferenceValue = leftHint;
            Prop(viewSo, "rightHint").objectReferenceValue = rightHint;
            Prop(viewSo, "textBox").objectReferenceValue = box;
            Prop(viewSo, "sentence").objectReferenceValue = sentence;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            var tutorialSo = new SerializedObject(tutorial);
            Prop(tutorialSo, "config").objectReferenceValue = config;
            Prop(tutorialSo, "view").objectReferenceValue = view;
            tutorialSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Hint(RectTransform root, string name, Vector2 anchor, RaceStartConfig look)
        {
            RectTransform hint = Kit.Node(root, name);
            Kit.Anchor(hint, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, HintSize);
            Label(hint, "TAP!", HintFontSize, look.GoColor); // the view gives it its outline
            return hint;
        }

        private static TextMeshProUGUI Label(RectTransform rt, string text, float size, Color color)
        {
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPaths.AmericanCaptainSdf);
            if (font != null) label.font = font;
            else Warn(Source, $"'{UiFontPaths.AmericanCaptainSdf}' not found: the tutorial falls back to the default TMP font.");
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>The prefab instance on the HUD canvas, drawn last; kept when it is already there.</summary>
        internal static void Place(Scene scene)
        {
            RaceTutorialView placed = FindInScene<RaceTutorialView>(scene);
            if (placed != null) return;
            RaceStartView words = FindInScene<RaceStartView>(scene)
                ?? throw new InvalidOperationException($"No RaceStartView in '{scene.path}': the tutorial has no canvas to sit on.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(BuildPrefab(), words.transform.parent);
            instance.name = "RaceTutorial";
            Kit.Stretch((RectTransform)instance.transform);
            instance.transform.SetAsLastSibling();
            Log(Source, "Race: tutorial placed over the HUD and the start words.");
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        [MenuItem("Tools/Speed Stars/Build/Race scene - add tutorial (patch, no rebuild)")]
        public static void PatchRaceScene()
        {
            SceneSetup[] before = null;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
                if (!Application.isBatchMode)
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        if (SceneManager.GetSceneAt(i).isDirty)
                            throw new InvalidOperationException("An open scene has unsaved changes; save it first.");
                before = EditorSceneManager.GetSceneManagerSetup();

                Scene scene = EditorSceneManager.OpenScene(SceneBuilder.RaceScenePath, OpenSceneMode.Single);
                Place(scene);
                HandEditGuard.SaveScene(scene, SceneBuilder.RaceScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("RACE_TUTORIAL_PATCH_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (before != null && before.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(before);
            }
        }
    }
}
