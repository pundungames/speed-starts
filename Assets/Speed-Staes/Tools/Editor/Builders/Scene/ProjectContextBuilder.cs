using System;
using SpeedStars.Installers;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Emotes;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Settings;
using SpeedStars.Meta.Themes;
using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Writes Assets/Resources/ProjectContext.prefab, the path Zenject loads the session context from (contract §4):
    /// a ProjectContext with the ProjectInstaller in its installer list and every config asset referenced in the
    /// installer's Inspector slots, so the numbers stay in their assets. Built in a preview scene, so no open scene
    /// is touched. Skips an existing prefab unless overwrite is asked for (then the same path, same GUID).
    /// </summary>
    public static class ProjectContextBuilder
    {
        private const string Source = nameof(ProjectContextBuilder);

        public const string PrefabPath = "Assets/Resources/" + ProjectContext.ProjectContextResourcePath + ".prefab";

        public static void Build(bool overwrite)
        {
            if (FileExists(PrefabPath) && !overwrite) { Skipped(Source, PrefabPath); return; }

            const string seeder = nameof(ConfigAssetSeeder);
            var economy = LoadRequired<EconomyConfig>(ConfigAssetSeeder.EconomyPath, seeder);
            var raceEvents = LoadRequired<RaceEventCatalog>(ConfigAssetSeeder.RaceEventCatalogPath, seeder);
            var gemRewards = LoadRequired<GemRewardConfig>(ConfigAssetSeeder.GemRewardPath, seeder);
            var sevenDayRewards = LoadRequired<SevenDayRewardConfig>(ConfigAssetSeeder.SevenDayRewardPath, seeder);
            var cosmetics = LoadRequired<CosmeticCatalog>(ConfigAssetSeeder.CosmeticCatalogPath, seeder);
            var uiMotion = LoadRequired<UiMotionConfig>(ConfigAssetSeeder.UiMotionPath, seeder);
            var themes = LoadRequired<ThemeCatalog>(ConfigAssetSeeder.ThemeCatalogPath, seeder);
            var settings = LoadRequired<SettingsConfig>(ConfigAssetSeeder.SettingsPath, seeder);
            var leaderboard = LoadRequired<LeaderboardConfig>(ConfigAssetSeeder.LeaderboardPath, seeder);
            var emotes = LoadRequired<EmoteCatalog>(EmoteCatalogSeed.CatalogPath, seeder);

            EnsureParentFolder(PrefabPath);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = ObjectFactory.CreateGameObject(preview, HideFlags.None, ProjectContext.ProjectContextResourcePath);
                var context = AddRequired<ProjectContext>(root);
                var installer = AddRequired<ProjectInstaller>(root);

                var installerSo = new SerializedObject(installer);
                Prop(installerSo, "economy").objectReferenceValue = economy;
                Prop(installerSo, "raceEvents").objectReferenceValue = raceEvents;
                Prop(installerSo, "gemRewards").objectReferenceValue = gemRewards;
                Prop(installerSo, "sevenDayRewards").objectReferenceValue = sevenDayRewards;
                Prop(installerSo, "cosmetics").objectReferenceValue = cosmetics;
                Prop(installerSo, "uiMotion").objectReferenceValue = uiMotion;
                Prop(installerSo, "themes").objectReferenceValue = themes;
                Prop(installerSo, "settings").objectReferenceValue = settings;
                Prop(installerSo, "leaderboard").objectReferenceValue = leaderboard;
                Prop(installerSo, "emotes").objectReferenceValue = emotes;
                // Scene names must match Build Settings (SceneFlow loads by name); taken from the scene builder's paths.
                Prop(installerSo, "scenes.homeScene").stringValue = SceneBuilder.HomeSceneName;
                Prop(installerSo, "scenes.raceScene").stringValue = SceneBuilder.RaceSceneName;
                Apply(installerSo);

                var contextSo = new SerializedObject(context);
                SetObjectList(Prop(contextSo, "_monoInstallers"), new UnityEngine.Object[] { installer });
                Apply(contextSo);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException($"Could not save '{PrefabPath}'.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            Log(Source, $"Wrote {PrefabPath} (ProjectContext + ProjectInstaller, 10 config references).");
        }
    }
}
