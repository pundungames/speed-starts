using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeedStars.Installers;
using SpeedStars.Physical;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Zenject;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Home and Race scenes and their Build Settings entries (contract §5). Home is built from scratch; Race is a
    /// copy of Malik's PhysicalTest scene (his file is only copied, never opened for writing) with the runner's debug
    /// tools switched off as scene overrides and our UI layer on top. Scenes open in Single mode one at a time;
    /// outside batchmode the builder refuses to run over unsaved scene or prefab work and puts the user's scenes
    /// back afterwards. An existing scene is skipped unless overwrite is asked for (then same path, same GUID).
    /// </summary>
    public static class SceneBuilder
    {
        private const string Source = nameof(SceneBuilder);

        public const string HomeScenePath = "Assets/Speed-Staes/Scenes/Menu/Home.unity";
        public const string RaceScenePath = "Assets/Speed-Staes/Scenes/Game/Race.unity";
        public const string PhysicalTestScenePath = "Assets/Speed-Staes/Scenes/Test/PhysicalTest.unity";

        public static string HomeSceneName => Path.GetFileNameWithoutExtension(HomeScenePath);
        public static string RaceSceneName => Path.GetFileNameWithoutExtension(RaceScenePath);

        // Flat menu background until the Home scene gets its 3D backdrop.
        private static readonly Color HomeBackground = new Color(0.27f, 0.56f, 0.86f);

        public static void BuildHome(bool overwrite)
        {
            if (FileExists(HomeScenePath) && !overwrite) { Skipped(Source, HomeScenePath); return; }

            // Every prefab first: a missing one stops the build before any scene file is written.
            GameObject[] screens = LoadPrefabs(UiPrefabPaths.HomeScreens);
            GameObject[] popups = LoadPrefabs(UiPrefabPaths.HomePopups);
            GameObject[] overlay = LoadPrefabs(new[] { UiPrefabPaths.ResourceBar, UiPrefabPaths.RewardFlight });

            SceneSetup[] userSetup = BeginSceneWork();
            try
            {
                EnsureParentFolder(HomeScenePath);
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                SceneUiLayout.CreateFlatCamera(scene, HomeBackground);
                SceneUiLayout.CreateEventSystem(scene);
                GameObject uiRoot = SceneUiLayout.CreateUiRoot(scene);

                Transform screenCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Screens", SceneUiLayout.BaseCanvasOrder);
                GameObject rootScreen = null;
                for (int i = 0; i < screens.Length; i++)
                {
                    GameObject instance = SceneUiLayout.Place(screens[i], screenCanvas, active: i == 0); // first = root screen
                    if (i == 0) rootScreen = instance;
                }

                Transform popupCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Popups", SceneUiLayout.PopupCanvasOrder);
                foreach (GameObject popup in popups) SceneUiLayout.Place(popup, popupCanvas, active: false);

                Transform overlayCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Overlay", SceneUiLayout.OverlayCanvasOrder);
                SceneUiLayout.Place(overlay[0], overlayCanvas, active: true);  // ResourceBar
                SceneUiLayout.Place(overlay[1], overlayCanvas, active: false); // RewardFlight
                SceneUiLayout.CreateTransitionBlocker(uiRoot, overlayCanvas);

                SceneUiLayout.CreateSceneContext<HomeSceneInstaller>(scene, SceneUiLayout.RootScreenOf(rootScreen, UiPrefabPaths.HomeScreens[0]));
                SceneWiring.WireHome(scene);
                Save(scene, HomeScenePath);
            }
            finally
            {
                EndSceneWork(userSetup);
            }
            Log(Source, $"Wrote {HomeScenePath}.");
        }

        public static void BuildRace(bool overwrite)
        {
            bool existed = FileExists(RaceScenePath);
            if (existed && !overwrite) { Skipped(Source, RaceScenePath); return; }
            if (!FileExists(PhysicalTestScenePath))
                throw new InvalidOperationException($"'{PhysicalTestScenePath}' not found; the Race scene is a copy of it.");

            GameObject[] hud = LoadPrefabs(new[] { UiPrefabPaths.RaceHud });
            GameObject[] popups = LoadPrefabs(new[] { UiPrefabPaths.ResultsScreen, UiPrefabPaths.EntryGateModal });

            SceneSetup[] userSetup = BeginSceneWork();
            bool copied = false;
            bool done = false;
            try
            {
                EnsureParentFolder(RaceScenePath);
                // Let go of every open scene first, so neither PhysicalTest nor an old Race is loaded while the file is copied.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CopyPhysicalTest(existed);
                copied = true;

                Scene scene = EditorSceneManager.OpenScene(RaceScenePath, OpenSceneMode.Single);
                DisableRunnerDebugTools(scene);
                if (FindInScene<SceneContext>(scene) != null)
                    throw new InvalidOperationException($"'{PhysicalTestScenePath}' already has a SceneContext; merge the Race scene by hand.");

                if (FindInScene<EventSystem>(scene) == null) SceneUiLayout.CreateEventSystem(scene);
                else Log(Source, "The copied scene already has an EventSystem; it is kept.");
                GameObject uiRoot = SceneUiLayout.CreateUiRoot(scene);

                Transform hudCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Hud", SceneUiLayout.BaseCanvasOrder);
                GameObject raceHud = SceneUiLayout.Place(hud[0], hudCanvas, active: true);

                Transform popupCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Popups", SceneUiLayout.PopupCanvasOrder);
                foreach (GameObject popup in popups) SceneUiLayout.Place(popup, popupCanvas, active: false);

                Transform overlayCanvas = SceneUiLayout.CreateCanvas(uiRoot.transform, "Canvas Overlay", SceneUiLayout.OverlayCanvasOrder);
                SceneUiLayout.CreateTransitionBlocker(uiRoot, overlayCanvas);

                SceneUiLayout.CreateSceneContext<RaceSceneInstaller>(scene, SceneUiLayout.RootScreenOf(raceHud, UiPrefabPaths.RaceHud));
                SceneWiring.WireRace(scene);
                Save(scene, RaceScenePath);
                done = true;
            }
            finally
            {
                EndSceneWork(userSetup);
                // A bare copy left behind would be skipped as "existing" by the next run: remove what this run created.
                if (!done && copied && !existed) RemoveFailedCopy();
                else if (!done && copied)
                    Warn(Source, $"Race build failed after the copy; '{RaceScenePath}' now holds a bare PhysicalTest copy. Fix the error and rebuild with overwrite.");
            }
            Log(Source, $"Wrote {RaceScenePath} (copy of {PhysicalTestScenePath} + UI).");
        }

        /// <summary>Home at 0 and Race at 1, both enabled. Entries the user added before are kept after them.</summary>
        public static void SetBuildSettings()
        {
            foreach (string path in new[] { HomeScenePath, RaceScenePath })
                LoadRequired<SceneAsset>(path, Source);

            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(HomeScenePath, true),
                new EditorBuildSettingsScene(RaceScenePath, true),
            };
            var kept = EditorBuildSettings.scenes.Where(s => s.path != HomeScenePath && s.path != RaceScenePath).ToList();
            if (kept.Count > 0)
                Warn(Source, $"Build Settings keeps {kept.Count} other scene(s) after Home/Race: {string.Join(", ", kept.Select(s => s.path))}.");
            scenes.AddRange(kept);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();

            // A build-settings change can stay in memory only (docs/TUZAKLAR.md §3): check the file, save the project once more if needed.
            if (!BuildSettingsFileHas(HomeScenePath, RaceScenePath))
            {
                EditorApplication.ExecuteMenuItem("File/Save Project");
                if (!BuildSettingsFileHas(HomeScenePath, RaceScenePath))
                    Warn(Source, $"'{BuildSettingsFile}' does not list Home/Race on disk yet; check it after the editor closes.");
            }
            Log(Source, $"Build Settings: 0 = {HomeScenePath}, 1 = {RaceScenePath}.");
        }

        private const string BuildSettingsFile = "ProjectSettings/EditorBuildSettings.asset";

        private static bool BuildSettingsFileHas(params string[] scenePaths)
        {
            string fullPath = FullPath(BuildSettingsFile);
            if (!File.Exists(fullPath)) return false;
            string text = File.ReadAllText(fullPath);
            return scenePaths.All(p => text.Contains("path: " + p));
        }

        private static void CopyPhysicalTest(bool overwriteExisting)
        {
            if (!overwriteExisting)
            {
                if (!AssetDatabase.CopyAsset(PhysicalTestScenePath, RaceScenePath))
                    throw new InvalidOperationException($"Could not copy '{PhysicalTestScenePath}' to '{RaceScenePath}'.");
                return;
            }
            // Overwrite: copy the file over the old one and keep Race.unity.meta, so the scene keeps its GUID.
            File.Copy(FullPath(PhysicalTestScenePath), FullPath(RaceScenePath), true);
            AssetDatabase.ImportAsset(RaceScenePath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// The runner prefab brings its IMGUI tuning panel and the 1-4 / Esc replay keys (contract §5); in the race
        /// they are off. Disabled on the scene instance (a prefab override), the prefab file is not touched.
        /// </summary>
        private static void DisableRunnerDebugTools(Scene scene)
        {
            var bodies = FindAllInScene<RunnerBody>(scene);
            if (bodies.Count == 0)
                throw new InvalidOperationException($"No RunnerBody in the copy of '{PhysicalTestScenePath}'; the Race scene needs Malik's runner.");
            if (bodies.Count > 1)
                Warn(Source, $"{bodies.Count} RunnerBody objects in the Race scene; the mechanic's fallback lookups expect one (contract §4).");

            var tools = new List<Behaviour>();
            tools.AddRange(FindAllInScene<RunnerBodyHUD>(scene));
            tools.AddRange(FindAllInScene<RunnerReplay>(scene));
            if (tools.Count == 0) Warn(Source, "No RunnerBodyHUD / RunnerReplay found on the runner; nothing to switch off.");

            foreach (Behaviour tool in tools)
            {
                var so = new SerializedObject(tool);
                Prop(so, "m_Enabled").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                Log(Source, $"Race: {tool.GetType().Name} on '{tool.gameObject.name}' switched off (scene override).");
            }
        }

        private static GameObject[] LoadPrefabs(IReadOnlyList<string> paths)
        {
            var prefabs = new GameObject[paths.Count];
            var missing = new List<string>();
            for (int i = 0; i < paths.Count; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefabs[i] == null) missing.Add(paths[i]);
            }
            if (missing.Count > 0)
                throw new InvalidOperationException($"UI prefab(s) missing (UiPrefabBuilder, T-007, runs first): {string.Join(", ", missing)}.");
            return prefabs;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            var all = FindAllInScene<T>(scene);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<T> FindAllInScene<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(true)); // inactive objects too
            return result;
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Could not save scene '{path}'.");
        }

        /// <summary>Refuses to replace open scenes that hold unsaved work; returns the open scenes to restore later.</summary>
        private static SceneSetup[] BeginSceneWork()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before building scenes.");

            if (!Application.isBatchMode)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null && stage.scene.isDirty)
                    throw new InvalidOperationException($"Prefab '{stage.assetPath}' is open with unsaved changes; save or close it first.");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene open = SceneManager.GetSceneAt(i);
                    if (open.isDirty)
                        throw new InvalidOperationException($"Scene '{(string.IsNullOrEmpty(open.path) ? open.name : open.path)}' has unsaved changes; save it first.");
                }
            }
            return EditorSceneManager.GetSceneManagerSetup();
        }

        /// <summary>Reopens the scenes that were open before (saved ones only; an untitled scene cannot be reopened).</summary>
        private static void EndSceneWork(SceneSetup[] setup)
        {
            if (setup == null) return;
            SceneSetup[] saved = setup.Where(s => !string.IsNullOrEmpty(s.path) && FileExists(s.path)).ToArray();
            if (saved.Length == 0) return;
            if (!saved.Any(s => s.isActive)) saved[0].isActive = true;
            if (!saved.Any(s => s.isLoaded)) saved[0].isLoaded = true;
            EditorSceneManager.RestoreSceneManagerSetup(saved);
        }

        private static void RemoveFailedCopy()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).path == RaceScenePath)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // let go of the half-built scene
                    break;
                }
            }
            AssetDatabase.DeleteAsset(RaceScenePath);
            Warn(Source, $"Race build failed; the copy '{RaceScenePath}' made by this run was removed.");
        }
    }
}
