using System;
using System.Linq;
using SpeedStars.Bridge;
using SpeedStars.Installers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The race emotes of the Race scene (T-089): <see cref="RaceEmotes"/> on the player's runner root (the object that carries
    /// RaceRunnerOutfit, which SceneCosmetics put there), its <c>bots</c> field on the scene's RaceBots, and
    /// <c>RaceSceneInstaller.raceEmotes</c> on it. <see cref="Add"/> is idempotent and is called by <c>SceneBuilder.BuildRace</c>
    /// (after the bots exist). <see cref="PatchRaceScene"/> does the same on the SAVED Race scene without rebuilding it, because a
    /// rebuild drops the saved buildings. Never runs in the game.
    /// </summary>
    public static class SceneRaceEmotes
    {
        private const string Source = nameof(SceneRaceEmotes);
        public const string OkMarker = "RACE_EMOTES_PATCH_OK";

        [MenuItem("Tools/Speed Stars/Build/Race scene - add race emotes (patch, no rebuild)")]
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
                Add(scene);
                if (!EditorSceneManager.SaveScene(scene, SceneBuilder.RaceScenePath))
                    throw new InvalidOperationException($"Could not save scene '{SceneBuilder.RaceScenePath}'.");
                Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                Debug.LogError("RACE_EMOTES_PATCH_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                Restore(before);
            }
        }

        /// <summary>Adds / wires the race emotes in an open Race scene; safe to run again.</summary>
        internal static RaceEmotes Add(Scene scene)
        {
            RaceRunnerOutfit outfit = Find<RaceRunnerOutfit>(scene)
                ?? throw new InvalidOperationException($"No RaceRunnerOutfit in '{scene.path}': the player's runner root is unknown (SceneCosmetics runs first).");
            GameObject runner = outfit.gameObject;
            var emotes = runner.GetComponent<RaceEmotes>() ?? runner.AddComponent<RaceEmotes>();

            var emoteSo = new SerializedObject(emotes);
            RaceBots bots = Find<RaceBots>(scene);
            if (bots != null) Prop(emoteSo, "bots").objectReferenceValue = bots;
            else Warn(Source, "No RaceBots in the scene; the opponents will not emote.");
            Apply(emoteSo);

            RaceSceneInstaller installer = Find<RaceSceneInstaller>(scene)
                ?? throw new InvalidOperationException($"No RaceSceneInstaller in '{scene.path}'.");
            var installerSo = new SerializedObject(installer);
            Prop(installerSo, "raceEmotes").objectReferenceValue = emotes;
            Apply(installerSo);

            EditorSceneManager.MarkSceneDirty(scene);
            Log(Source, $"Race: RaceEmotes on '{runner.name}' (bots {(bots != null ? "wired" : "missing")}), RaceSceneInstaller.raceEmotes set.");
            return emotes;
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
