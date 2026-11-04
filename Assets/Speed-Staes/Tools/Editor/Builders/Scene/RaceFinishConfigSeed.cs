using System;
using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First creation of Data/Config/RaceFinishConfig.asset and its place on the Race scene's session (the field defaults are the first
    /// values). An existing asset is the user's and is never touched unless overwrite is asked for. <see cref="PatchRaceScene"/> sets only
    /// that one reference on an existing scene, so the scene does not have to be rebuilt.
    /// </summary>
    public static class RaceFinishConfigSeed
    {
        private const string Source = nameof(RaceFinishConfigSeed);
        public const string OkMarker = "RACE_FINISH_PATCH_OK";

        public const string Path = ConfigAssetSeeder.Folder + "/RaceFinishConfig.asset";

        internal static RaceFinishConfig Seed(bool overwrite)
        {
            EnsureFolder(ConfigAssetSeeder.Folder);
            var config = CreateOrReset<RaceFinishConfig>(Path, overwrite);
            if (config == null)
            {
                Skipped(Source, Path);
                return AssetDatabase.LoadAssetAtPath<RaceFinishConfig>(Path);
            }
            Log(Source, $"Created {Path} with the field defaults.");
            return config;
        }

        internal static void Wire(RaceSessionAdapter adapter)
        {
            var so = new SerializedObject(adapter);
            Prop(so, "finishConfig").objectReferenceValue = Seed(overwrite: false);
            Apply(so);
        }

        [MenuItem("Tools/Speed Stars/Build/Race scene - add finish config (patch, no rebuild)")]
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
                RaceSessionAdapter adapter = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    adapter = root.GetComponentInChildren<RaceSessionAdapter>(true);
                    if (adapter != null) break;
                }
                if (adapter == null) throw new InvalidOperationException($"No RaceSessionAdapter in '{scene.path}'.");
                Wire(adapter);
                HandEditGuard.SaveScene(scene, SceneBuilder.RaceScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("RACE_FINISH_PATCH_FAILED: " + e);
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
