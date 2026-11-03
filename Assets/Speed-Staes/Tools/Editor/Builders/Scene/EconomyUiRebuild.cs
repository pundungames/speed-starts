using System;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// T-085: rebuilds only the UI prefabs and the Home / Race scenes (overwrite), WITHOUT seeding or overwriting any config asset
    /// (Faz1Builder.RebuildAll would). Parameterless so it can run with -executeMethod in a verification copy:
    /// <c>SpeedStars.Tools.Builders.EconomyUiRebuild.Run</c>. Writes the marker below on success.
    /// </summary>
    public static class EconomyUiRebuild
    {
        public const string OkMarker = "ECONOMY_UI_REBUILD_OK";

        [MenuItem("Tools/Speed Stars/Build/Economy UI - prefabs + Home + Race (overwrite, no configs)")]
        public static void Run()
        {
            try
            {
                UiPrefabBuilder.BuildAll(true);
                SceneBuilder.BuildHome(true);
                SceneBuilder.BuildRace(true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("ECONOMY_UI_REBUILD_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
