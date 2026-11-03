using System;
using System.Linq;
using SpeedStars.Bridge;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The Practice panel of the Race scene (T-090): the panel prefab as a child of the race HUD (so it hides with the HUD), a
    /// <see cref="PracticeSession"/> next to the session adapter, and its references: the session, the panel, the config asset, plus
    /// <c>RaceSessionAdapter.practice</c> and <c>RaceHurdles.practice</c>. <see cref="Add"/> is idempotent.
    /// <see cref="PatchRaceScene"/> does it on the SAVED Race scene without rebuilding it, because a rebuild drops the saved
    /// buildings (SceneRaceEmotes does the same for the emotes). Never runs in the game.
    /// </summary>
    public static class ScenePracticePanel
    {
        private const string Source = nameof(ScenePracticePanel);
        public const string OkMarker = "PRACTICE_PANEL_PATCH_OK";

        [MenuItem("Tools/Speed Stars/Build/Race scene - add Practice panel (patch, no rebuild)")]
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

                UiPracticePrefabs.Build(); // the config asset and the prefab, kept when they exist
                Scene scene = EditorSceneManager.OpenScene(SceneBuilder.RaceScenePath, OpenSceneMode.Single);
                Add(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneBuilder.RaceScenePath))
                    throw new InvalidOperationException($"Could not save scene '{SceneBuilder.RaceScenePath}'.");
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("PRACTICE_PANEL_PATCH_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                Restore(before);
            }
        }

        /// <summary>Adds / wires the Practice panel in an open Race scene; safe to run again.</summary>
        internal static PracticeSession Add(Scene scene)
        {
            RaceSessionAdapter adapter = Find<RaceSessionAdapter>(scene)
                ?? throw new InvalidOperationException($"No RaceSessionAdapter in '{scene.path}'.");
            RaceHudView hud = Find<RaceHudView>(scene)
                ?? throw new InvalidOperationException($"No RaceHudView in '{scene.path}': the panel has no HUD to sit in.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPracticePrefabs.PrefabPath)
                ?? throw new InvalidOperationException($"'{UiPracticePrefabs.PrefabPath}' is missing; run the Practice panel prefab build first.");
            var config = AssetDatabase.LoadAssetAtPath<SpeedStars.Meta.Practice.PracticeConfig>(UiPracticePrefabs.ConfigPath)
                ?? throw new InvalidOperationException($"'{UiPracticePrefabs.ConfigPath}' is missing; run the Practice panel prefab build first.");

            PracticePanelView panel = Find<PracticePanelView>(scene);
            if (panel == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud.transform);
                instance.name = prefab.name;
                panel = instance.GetComponent<PracticePanelView>()
                    ?? throw new InvalidOperationException("The Practice panel prefab has no PracticePanelView.");
            }
            panel.transform.SetAsLastSibling(); // over the other HUD parts

            var session = adapter.GetComponent<PracticeSession>() ?? adapter.gameObject.AddComponent<PracticeSession>();
            var sessionSo = new SerializedObject(session);
            Prop(sessionSo, "session").objectReferenceValue = adapter;
            Prop(sessionSo, "panel").objectReferenceValue = panel;
            Prop(sessionSo, "config").objectReferenceValue = config;
            Apply(sessionSo);

            var adapterSo = new SerializedObject(adapter);
            Prop(adapterSo, "practice").objectReferenceValue = session;
            Apply(adapterSo);

            RaceHurdles hurdles = Find<RaceHurdles>(scene);
            if (hurdles != null)
            {
                var hurdlesSo = new SerializedObject(hurdles);
                Prop(hurdlesSo, "practice").objectReferenceValue = session;
                Apply(hurdlesSo);
            }
            else Warn(Source, "No RaceHurdles in the scene; the Hurdles box will not lay hurdles.");

            EditorSceneManager.MarkSceneDirty(scene);
            Log(Source, $"Race: Practice panel under '{hud.name}', PracticeSession on '{adapter.name}' (hurdles {(hurdles != null ? "wired" : "missing")}).");
            return session;
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static void Restore(SceneSetup[] setup)
        {
            if (setup == null) return;
            SceneSetup[] saved = setup.Where(s => !string.IsNullOrEmpty(s.path) && FileExists(s.path)).ToArray();
            if (saved.Length == 0) return;
            if (!saved.Any(s => s.isActive)) saved[0].isActive = true;
            if (!saved.Any(s => s.isLoaded)) saved[0].isLoaded = true;
            EditorSceneManager.RestoreSceneManagerSetup(saved);
        }
    }
}
