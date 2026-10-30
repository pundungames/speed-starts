using System;
using System.Collections.Generic;
using SpeedStars.Bridge;
using SpeedStars.Installers;
using SpeedStars.Physical;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Scene-internal references of Faz 2 (T-014b): fields that point from one scene object to another and so cannot
    /// live in a prefab. Every field is set by name through SerializedObject; a missing field, component or scene
    /// object stops the build with a message naming it. Called by SceneBuilder after the scene objects are placed.
    /// </summary>
    internal static class SceneWiring
    {
        private const string RaceSessionName = "RaceSession";

        /// <summary>Home: HomeScreenView's scene-object fields (other screens, entry gate, 7-day popup).</summary>
        public static void WireHome(Scene scene)
        {
            var homeView = RequireOne<HomeScreenView>(scene, "Home screen (HomeScreen prefab instance)");
            var eventSelect = RequireOne<EventSelectScreenView>(scene, "EventSelect screen (EventSelectScreen prefab instance)");
            var entryGate = RequireOne<EntryGateModalView>(scene, "EntryGate modal (EntryGateModal prefab instance)");
            var cosmetics = RequireInstance<UiScreen>(scene, UiPrefabPaths.CosmeticsScreen);
            var highScores = RequireInstance<UiScreen>(scene, UiPrefabPaths.HighScoresScreen);
            var sevenDay = RequireInstance<UiPopup>(scene, UiPrefabPaths.SevenDayCheckInPanel);
            var themeSelect = RequireInstance<UiScreen>(scene, UiPrefabPaths.ThemeSelectScreen);
            var settings = RequireInstance<UiPopup>(scene, UiPrefabPaths.SettingsPopup);
            WireEmoteScreen(scene, homeView); // T-040

            var so = new SerializedObject(homeView);
            Prop(so, "eventSelectScreen").objectReferenceValue = eventSelect;
            Prop(so, "entryGateModal").objectReferenceValue = entryGate;
            Prop(so, "cosmeticsScreen").objectReferenceValue = cosmetics;
            Prop(so, "highScoresScreen").objectReferenceValue = highScores;
            Prop(so, "sevenDayPopup").objectReferenceValue = sevenDay;
            Prop(so, "themeSelectScreen").objectReferenceValue = themeSelect;
            Prop(so, "settingsPopup").objectReferenceValue = settings;
            Apply(so);
            var resourceBar = RequireOne<ResourceBarView>(scene, "ResourceBar widget (ResourceBar prefab instance)");
            var barSo = new SerializedObject(resourceBar);
            Prop(barSo, "homeScreen").objectReferenceValue = homeView; // bar shows only while Home is the top screen
            Apply(barSo);
            var costume = RequireOne<SpeedStars.UI.Costume.CostumeEditorScreenView>(scene, "Costume editor screen (CosmeticsScreen prefab instance)");
            var costumeSo = new SerializedObject(costume);
            Prop(costumeSo, "dimBackdrop").objectReferenceValue = SceneHomeDim.Find(scene);
            Apply(costumeSo);
            Log(nameof(SceneWiring), "Home: HomeScreenView screen/popup references wired.");
        }

        /// <summary>T-040: Home's emote button target and the emote screen's dim backdrop (both scene objects).</summary>
        private static void WireEmoteScreen(Scene scene, HomeScreenView homeView)
        {
            var emote = RequireOne<SpeedStars.UI.Emotes.EmoteScreenView>(scene, "Emote screen (EmoteScreen prefab instance)");
            var homeSo = new SerializedObject(homeView);
            Prop(homeSo, "emoteScreen").objectReferenceValue = emote;
            Apply(homeSo);
            var emoteSo = new SerializedObject(emote);
            Prop(emoteSo, "dimBackdrop").objectReferenceValue = SceneHomeDim.Find(scene);
            Apply(emoteSo);
        }

        /// <summary>Race: RaceSession object (adapter + dev finish) and the RaceSceneInstaller fields.</summary>
        public static void WireRace(Scene scene)
        {
            var race = RequireOne<RaceController>(scene, "RaceController (Malik's runner)");
            var body = RequireOne<RunnerBody>(scene, "RunnerBody (Malik's runner)");
            var results = RequireOne<ResultsScreenView>(scene, "Results screen (ResultsScreen prefab instance)");
            var installer = RequireOne<RaceSceneInstaller>(scene, "RaceSceneInstaller (SceneContext)");
            var raceHud = RequireOne<RaceHudView>(scene, "Race HUD (RaceHud prefab instance)");
            var hudScreen = raceHud.GetComponent<UiScreen>();
            if (hudScreen == null) throw new InvalidOperationException("RaceHudView has no UiScreen; cannot be the Race root screen.");

            var go = SceneUiLayout.CreateRoot(scene, RaceSessionName);
            var adapter = AddRequired<RaceSessionAdapter>(go);
            var adapterSo = new SerializedObject(adapter);
            Prop(adapterSo, "race").objectReferenceValue = race;
            Prop(adapterSo, "body").objectReferenceValue = body;
            Apply(adapterSo);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var dev = AddRequired<DevRaceFinish>(go);
            var devSo = new SerializedObject(dev);
            Prop(devSo, "adapter").objectReferenceValue = adapter;
            Apply(devSo);
#endif

            // T-038: the leaderboard screen sits next to the Results screen (same popup canvas, drawn after it); the Results
            // High Scores button pushes it and its Back pops to the results again.
            var resultsSo = new SerializedObject(results);
            Prop(resultsSo, "highScoresScreen").objectReferenceValue = EnsureLeaderboardScreen(scene, results);
            Apply(resultsSo);

            var installerSo = new SerializedObject(installer);
            Prop(installerSo, "raceSession").objectReferenceValue = adapter;
            Prop(installerSo, "results").objectReferenceValue = results;
            Prop(installerSo, "rootScreen").objectReferenceValue = hudScreen;
            Apply(installerSo);
            Log(nameof(SceneWiring), "Race: RaceSession (adapter + DevRaceFinish) and RaceSceneInstaller fields wired.");

            WireAnalysis(scene, results);
            RaceStartWiring.Wire(scene, adapter, raceHud.transform.parent); // T-039: adapter config + start words over the HUD
        }

        /// <summary>
        /// T-036: the Analysis screen sits next to Results (same popup canvas, just above it, inactive) and Results' Stats button
        /// opens it. The prefab instance is placed here when the scene has none yet, so an existing Race scene gets it by
        /// re-running the wiring. Without the prefab (prefab builder not run) the scene stays as it was: Stats stays disabled.
        /// </summary>
        private static void WireAnalysis(Scene scene, ResultsScreenView results)
        {
            AnalysisScreenView analysis = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                analysis = root.GetComponentInChildren<AnalysisScreenView>(true);
                if (analysis != null) break;
            }

            if (analysis == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPaths.AnalysisScreen);
                if (prefab == null)
                {
                    Warn(nameof(SceneWiring), $"Race: '{UiPrefabPaths.AnalysisScreen}' not found (run the UI prefab builder); the Analysis screen is not placed and Stats stays disabled.");
                    return;
                }
                Transform canvas = results.transform.parent;
                if (canvas == null) throw new InvalidOperationException("The Results screen has no parent canvas; the Analysis screen cannot be placed beside it.");

                GameObject instance = SceneUiLayout.Place(prefab, canvas, active: false);
                instance.transform.SetSiblingIndex(results.transform.GetSiblingIndex() + 1); // drawn above Results
                analysis = instance.GetComponent<AnalysisScreenView>();
                if (analysis == null) throw new InvalidOperationException($"'{UiPrefabPaths.AnalysisScreen}' has no AnalysisScreenView on its root.");
            }

            var resultsSo = new SerializedObject(results);
            Prop(resultsSo, "analysisScreen").objectReferenceValue = analysis;
            Apply(resultsSo);
            Log(nameof(SceneWiring), "Race: Analysis screen placed beside Results and wired to its Stats button.");
        }

        /// <summary>The HighScoresScreen instance of the Race scene: an existing one, else a new inactive one under the Results screen's canvas.</summary>
        private static UiScreen EnsureLeaderboardScreen(Scene scene, ResultsScreenView results)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPaths.HighScoresScreen);
            if (prefab == null) throw new InvalidOperationException($"Prefab '{UiPrefabPaths.HighScoresScreen}' not found.");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                    if (PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != prefab) continue;
                    return t.GetComponent<UiScreen>();
                }
            }
            GameObject instance = SceneUiLayout.Place(prefab, results.transform.parent, active: false);
            var screen = instance.GetComponent<UiScreen>();
            if (screen == null) throw new InvalidOperationException("The HighScoresScreen prefab has no UiScreen on its root.");
            return screen;
        }

        private static T RequireOne<T>(Scene scene, string what) where T : Component
        {
            var all = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                all.AddRange(root.GetComponentsInChildren<T>(true));
            if (all.Count == 0)
                throw new InvalidOperationException($"Scene '{scene.path}': {what} not found ({typeof(T).FullName}). Is the prefab builder (T-014a) up to date?");
            if (all.Count > 1)
                Warn(nameof(SceneWiring), $"{all.Count} x {typeof(T).Name} in '{scene.path}' ({what}); the first one is used.");
            return all[0];
        }

        /// <summary>The component of type T on the scene instance of a prefab, found by the prefab's source asset.</summary>
        private static T RequireInstance<T>(Scene scene, string prefabPath) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException($"Prefab '{prefabPath}' not found.");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                    if (PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != prefab) continue;
                    var component = t.GetComponent<T>();
                    if (component == null)
                        throw new InvalidOperationException($"Instance of '{prefabPath}' in '{scene.path}' has no {typeof(T).FullName} on its root.");
                    return component;
                }
            }
            throw new InvalidOperationException($"No instance of '{prefabPath}' in scene '{scene.path}'.");
        }
    }
}
