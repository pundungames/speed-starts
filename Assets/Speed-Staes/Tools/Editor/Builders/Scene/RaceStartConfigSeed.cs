using System;
using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First creation of Data/Config/RaceStartConfig.asset (T-039). The field defaults of RaceStartConfig are the first values (READY
    /// 0.8 s, SET wait 0.6-1.4 s are placeholders to tune by feel; the motion numbers are the art's), so nothing is written over them.
    /// An existing asset is the user's and is never touched unless overwrite is asked for; the existing asset is returned.
    /// </summary>
    internal static class RaceStartConfigSeed
    {
        private const string Source = nameof(RaceStartConfigSeed);

        public const string Path = ConfigAssetSeeder.Folder + "/RaceStartConfig.asset";

        public static RaceStartConfig Seed(bool overwrite)
        {
            EnsureFolder(ConfigAssetSeeder.Folder);
            var config = CreateOrReset<RaceStartConfig>(Path, overwrite);
            if (config == null)
            {
                Skipped(Source, Path);
                return AssetDatabase.LoadAssetAtPath<RaceStartConfig>(Path);
            }
            Log(Source, $"Created {Path} with the field defaults.");
            return config;
        }
    }

    /// <summary>
    /// Scene side of the race start (T-039): the adapter gets its config asset, and the RaceStart prefab is placed on the HUD canvas
    /// (above the HUD, inactive group inside, raycast off). Called by SceneWiring.WireRace; re-running it on an existing Race scene
    /// adds the missing piece. Without the prefab (prefab builder not run) the scene keeps working with no start words.
    /// </summary>
    internal static class RaceStartWiring
    {
        private const string Source = nameof(RaceStartWiring);

        public static void Wire(Scene scene, RaceSessionAdapter adapter, Transform hudCanvas)
        {
            RaceStartConfig config = RaceStartConfigSeed.Seed(overwrite: false);
            var adapterSo = new SerializedObject(adapter);
            Prop(adapterSo, "startConfig").objectReferenceValue = config;
            Apply(adapterSo);

            RaceStartView view = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                view = root.GetComponentInChildren<RaceStartView>(true);
                if (view != null) break;
            }

            if (view == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPaths.RaceStart);
                if (prefab == null)
                {
                    Warn(Source, $"Race: '{UiPrefabPaths.RaceStart}' not found (run the UI prefab builder); the start words are not placed.");
                    return;
                }
                if (hudCanvas == null) throw new InvalidOperationException("The Race HUD has no parent canvas; the start words cannot be placed over it.");

                GameObject instance = SceneUiLayout.Place(prefab, hudCanvas, active: true);
                instance.transform.SetAsLastSibling(); // drawn above the HUD
                if (instance.GetComponent<RaceStartView>() == null)
                    throw new InvalidOperationException($"'{UiPrefabPaths.RaceStart}' has no RaceStartView on its root.");
            }
            Log(Source, "Race: adapter config set, start words placed over the HUD.");
        }
    }
}
