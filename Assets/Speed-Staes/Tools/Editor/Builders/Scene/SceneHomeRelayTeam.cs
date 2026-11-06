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
    /// T-160: the relay team stage of the Home scene. Adds a "Relay Team" object with <see cref="MenuRelayTeam"/>, the runner prefab,
    /// the main character and three slot transforms standing beside the main character (placed in its own space; the designer moves
    /// them in the scene). Patch only: when "Relay Team" already exists nothing is touched, because the scene is hand-edited.
    /// Never runs in the game.
    /// </summary>
    public static class SceneHomeRelayTeam
    {
        private const string Source = nameof(SceneHomeRelayTeam);
        public const string OkMarker = "HOME_RELAY_TEAM_OK";
        public const string ObjectName = "Relay Team";

        /// <summary>Slot places in the main character's space (right, up, forward in metres): beside it and a little behind. A starting layout only.</summary>
        private static readonly Vector3[] SlotOffsets =
        {
            new Vector3(-0.9f, 0f, -0.5f),
            new Vector3(0.9f, 0f, -0.5f),
            new Vector3(0f, 0f, -1.1f)
        };

        [MenuItem("Tools/Speed Stars/Build/Home scene - add the relay team stage (patch)")]
        public static void PatchHomeScene()
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

                Scene scene = EditorSceneManager.OpenScene(SceneBuilder.HomeScenePath, OpenSceneMode.Single);
                if (Add(scene)) HandEditGuard.SaveScene(scene, SceneBuilder.HomeScenePath);
                UnityEngine.Debug.Log(OkMarker);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("HOME_RELAY_TEAM_FAILED: " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                Restore(before);
                HandEditGuard.ConvertPending();
            }
        }

        /// <summary>Adds the stage to an open Home scene; false when it was there already.</summary>
        internal static bool Add(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<MenuRelayTeam>(true) != null)
                {
                    Log(Source, $"'{scene.path}' already has a MenuRelayTeam; left as it is.");
                    return false;
                }

            MenuCharacter character = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<MenuCharacter>(true)).FirstOrDefault(c => c != null)
                ?? throw new InvalidOperationException($"No MenuCharacter in '{scene.path}': the main character is unknown.");
            Transform main = character.transform;
            var prefab = LoadRequired<GameObject>(SceneHomeCharacter.RunnerPrefabPath, "Malik's character prefab");

            var stage = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(stage, scene);
            var team = stage.AddComponent<MenuRelayTeam>();

            var slots = new Transform[SlotOffsets.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = new GameObject("Slot " + (i + 1)).transform;
                slot.SetParent(stage.transform, false);
                slot.SetPositionAndRotation(main.TransformPoint(SlotOffsets[i]), main.rotation);
                slots[i] = slot;
            }

            var so = new SerializedObject(team);
            Prop(so, "runnerPrefab").objectReferenceValue = prefab;
            Prop(so, "mainRunner").objectReferenceValue = main;
            SerializedProperty list = Prop(so, "slots");
            list.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            Apply(so);

            EditorSceneManager.MarkSceneDirty(scene);
            Log(Source, $"Home: '{ObjectName}' with {slots.Length} slots beside '{main.name}'.");
            return true;
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
