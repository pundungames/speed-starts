using System;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// One entry point for the Faz 1 content: config assets, UI prefabs, ProjectContext, Home/Race scenes, Build Settings.
    /// Runs from the menu or in batchmode (-executeMethod SpeedStars.Tools.Builders.Faz1Builder.BuildAll) in a
    /// verification copy, so the user's open editor is never touched. Every step skips what already exists unless
    /// "overwrite" is asked for: assets the user edited by hand are never replaced silently.
    /// </summary>
    public static class Faz1Builder
    {
        public const string OkMarker = "FAZ1_BUILD_OK";

        [MenuItem("Tools/Speed Stars/Build/Faz 1 - all (skip existing)")]
        public static void BuildAll() => Run(false);

        [MenuItem("Tools/Speed Stars/Build/Faz 1 - all (overwrite)")]
        public static void RebuildAll() => Run(true);

        private static void Run(bool overwrite)
        {
            try
            {
                ConfigAssetSeeder.SeedAll(overwrite);
                UiPrefabBuilder.BuildAll(overwrite);
                ProjectContextBuilder.Build(overwrite);
                SceneBuilder.BuildHome(overwrite);
                SceneBuilder.BuildRace(overwrite);
                SceneBuilder.SetBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("FAZ1_BUILD_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
