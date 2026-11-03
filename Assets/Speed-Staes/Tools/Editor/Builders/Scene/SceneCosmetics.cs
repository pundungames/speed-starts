using System;
using System.Linq;
using SpeedStars.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Cosmetic components for the scenes' runner instances (T-025). The runner prefab is Malik's and stays untouched: the
    /// components are added to the scene instance. Called from <c>SceneBuilder.BuildRace</c> (one line, after
    /// <c>DisableRunnerDebugTools</c> and before <c>Save</c>): <c>SceneCosmetics.AddToRaceRunner(scene);</c>
    /// The Home runner needs nothing from the builder: <c>MenuCharacter</c> adds the applier itself;
    /// <c>AddApplier</c> is there if the Home builder wants it visible in the Inspector.
    /// </summary>
    public static class SceneCosmetics
    {
        private const string Source = nameof(SceneCosmetics);
        private const string WardrobeName = "Accessories";
        public const string RaceOfTheDayOkMarker = "RACE_OF_THE_DAY_PATCH_OK";
        public const string RaceOfTheDayUiOkMarker = "RACE_OF_THE_DAY_UI_OK";

        /// <summary>Applier + RaceRunnerOutfit on the runner root of the Race scene (the object that owns the Accessories wardrobe).</summary>
        public static void AddToRaceRunner(Scene scene)
        {
            GameObject runner = FindRunnerRoot(scene)
                ?? throw new InvalidOperationException($"Scene '{scene.path}': no object with an '{WardrobeName}' child; the runner prefab is not in the scene.");
            AddApplier(runner);
            if (runner.GetComponent<RaceRunnerOutfit>() == null) runner.AddComponent<RaceRunnerOutfit>();
            Log(Source, $"Race: CosmeticApplier + RaceRunnerOutfit added to '{runner.name}'.");
        }

        /// <summary>
        /// T-087: <see cref="RaceOfTheDayOutfit"/> on the player's runner root (next to RaceRunnerOutfit), wired to the scene's race session
        /// adapter and RaceBots. Idempotent. <c>SceneBuilder.BuildRace</c> should call it after <c>SceneRaceEmotes.Add</c> (the bots must
        /// exist); <see cref="PatchRaceOfTheDay"/> does it on the SAVED Race scene without rebuilding it.
        /// </summary>
        internal static RaceOfTheDayOutfit AddRaceOfTheDay(Scene scene)
        {
            RaceRunnerOutfit outfit = FindIn<RaceRunnerOutfit>(scene)
                ?? throw new InvalidOperationException($"No RaceRunnerOutfit in '{scene.path}': the player's runner root is unknown (AddToRaceRunner runs first).");
            GameObject runner = outfit.gameObject;
            var daily = runner.GetComponent<RaceOfTheDayOutfit>() ?? runner.AddComponent<RaceOfTheDayOutfit>();

            RaceSessionAdapter session = FindIn<RaceSessionAdapter>(scene)
                ?? throw new InvalidOperationException($"No RaceSessionAdapter in '{scene.path}'.");
            var so = new SerializedObject(daily);
            Prop(so, "session").objectReferenceValue = session;
            RaceBots bots = FindIn<RaceBots>(scene);
            if (bots != null) Prop(so, "bots").objectReferenceValue = bots;
            else Warn(Source, "No RaceBots in the scene; only the player's runner will wear the day's outfit.");
            Prop(so, "playerApplier").objectReferenceValue = AddApplier(runner);
            Apply(so);

            EditorSceneManager.MarkSceneDirty(scene);
            Log(Source, $"Race: RaceOfTheDayOutfit on '{runner.name}' (session wired, bots {(bots != null ? "wired" : "missing")}).");
            return daily;
        }

        /// <summary>Adds / wires Race of the Day's outfit in the saved Race scene without rebuilding it (a rebuild drops the saved buildings).</summary>
        [MenuItem("Tools/Speed Stars/Build/Race scene - add Race of the Day outfit (patch, no rebuild)")]
        public static void PatchRaceOfTheDay()
        {
            SceneSetup[] before = null;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
                if (!Application.isBatchMode)
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                        if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                            throw new InvalidOperationException("An open scene has unsaved changes; save it first.");
                before = EditorSceneManager.GetSceneManagerSetup();

                Scene scene = EditorSceneManager.OpenScene(SceneBuilder.RaceScenePath, OpenSceneMode.Single);
                AddRaceOfTheDay(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneBuilder.RaceScenePath))
                    throw new InvalidOperationException($"Could not save scene '{SceneBuilder.RaceScenePath}'.");
                Debug.Log(RaceOfTheDayOkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("RACE_OF_THE_DAY_PATCH_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                RestoreSetup(before);
            }
        }

        /// <summary>
        /// T-087, steps 2 and 3 in one: the UI prefabs (HomeScreen with the Racer of the day card, ResultsScreen with the daily cards) and the
        /// Home scene are rebuilt (overwrite, no config is seeded or touched), then the saved Race scene gets RaceOfTheDayOutfit by the
        /// targeted patch. The Race scene itself is NOT rebuilt (a rebuild drops its saved buildings, docs/TUZAKLAR.md). Parameterless, so it
        /// runs with <c>-executeMethod SpeedStars.Tools.Builders.SceneCosmetics.RebuildRaceOfTheDayUi</c> in a verification copy.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Build/Race of the Day - UI prefabs + Home + Race outfit patch (no config, no Race rebuild)")]
        public static void RebuildRaceOfTheDayUi()
        {
            try
            {
                UiPrefabBuilder.BuildAll(true);
                SceneBuilder.BuildHome(true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                PatchRaceOfTheDay();
                Debug.Log(RaceOfTheDayUiOkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("RACE_OF_THE_DAY_UI_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static T FindIn<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static void RestoreSetup(SceneSetup[] setup)
        {
            if (setup == null) return;
            SceneSetup[] saved = setup.Where(s => !string.IsNullOrEmpty(s.path) && FileExists(s.path)).ToArray();
            if (saved.Length == 0) return;
            if (!saved.Any(s => s.isActive)) saved[0].isActive = true;
            if (!saved.Any(s => s.isLoaded)) saved[0].isLoaded = true;
            EditorSceneManager.RestoreSceneManagerSetup(saved);
        }

        /// <summary>A <c>CosmeticApplier</c> on the runner root (kept if there is one).</summary>
        public static CosmeticApplier AddApplier(GameObject runnerRoot)
        {
            var applier = runnerRoot.GetComponent<CosmeticApplier>();
            return applier != null ? applier : runnerRoot.AddComponent<CosmeticApplier>();
        }

        private static GameObject FindRunnerRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == WardrobeName && t.parent != null) return t.parent.gameObject;
            return null;
        }
    }
}
