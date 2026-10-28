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

            var so = new SerializedObject(homeView);
            Prop(so, "eventSelectScreen").objectReferenceValue = eventSelect;
            Prop(so, "entryGateModal").objectReferenceValue = entryGate;
            Prop(so, "cosmeticsScreen").objectReferenceValue = cosmetics;
            Prop(so, "highScoresScreen").objectReferenceValue = highScores;
            Prop(so, "sevenDayPopup").objectReferenceValue = sevenDay;
            Prop(so, "themeSelectScreen").objectReferenceValue = themeSelect;
            Apply(so);
            Log(nameof(SceneWiring), "Home: HomeScreenView screen/popup references wired.");
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

            var installerSo = new SerializedObject(installer);
            Prop(installerSo, "raceSession").objectReferenceValue = adapter;
            Prop(installerSo, "results").objectReferenceValue = results;
            Prop(installerSo, "rootScreen").objectReferenceValue = hudScreen;
            Apply(installerSo);
            Log(nameof(SceneWiring), "Race: RaceSession (adapter + DevRaceFinish) and RaceSceneInstaller fields wired.");
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
