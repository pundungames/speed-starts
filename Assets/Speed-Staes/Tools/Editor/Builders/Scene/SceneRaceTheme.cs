using System;
using SpeedStars.Bridge;
using SpeedStars.Physical;
using SpeedStars.Runner;
using SpeedStars.Themes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The theme side of the Race scene (T-072), which is a copy of Malik's PhysicalTest: Serhat's streaming worlds are brought in
    /// the way his ThemeRunPlayground holds them (the configured LoopWay prefab for the standard city, one ThemedLoopWay per
    /// Definition.asset for the other four) and <see cref="RaceThemeView"/> opens the one the player picked. The flat Track
    /// (ground, lane lines) would cover or fight the worlds' roads, so it is switched off; FinishLine stays. Serhat's scene and
    /// prefabs are only read/instantiated, never written.
    /// </summary>
    internal static class SceneRaceTheme
    {
        private const string Source = nameof(SceneRaceTheme);

        public const string StandardWorldPrefab = "Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab";
        public const string TrackName = "Track";

        /// <summary>Themed worlds in showcase order 1..4 (same order as ThemeRunPlayground and MainMenuThemeShowcase).</summary>
        private static readonly string[] ThemedDefinitionPaths =
        {
            "Assets/Speed-Staes/Prefabs/Themes/Egypt/Definition.asset",
            "Assets/Speed-Staes/Prefabs/Themes/RomeByzantium/Definition.asset",
            "Assets/Speed-Staes/Prefabs/Themes/PirateSea/Definition.asset",
            "Assets/Speed-Staes/Prefabs/Themes/FloatingSky/Definition.asset",
        };

        private static readonly EnvironmentConcept[] ThemedConcepts =
        {
            EnvironmentConcept.Egypt, EnvironmentConcept.RomeByzantium, EnvironmentConcept.PirateSea, EnvironmentConcept.FloatingSky,
        };

        public static RaceThemeView Add(Scene scene)
        {
            GameObject standardPrefab = LoadRequired<GameObject>(StandardWorldPrefab, "Serhat's LoopWay prefab");
            var definitions = new LoopThemeDefinition[ThemedDefinitionPaths.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i] = LoadRequired<LoopThemeDefinition>(ThemedDefinitionPaths[i], "Serhat's theme definitions");
                if (definitions[i].concept != ThemedConcepts[i])
                    throw new InvalidOperationException($"'{ThemedDefinitionPaths[i]}' is {definitions[i].concept}, expected {ThemedConcepts[i]}; the showcase order changed.");
            }

            RunnerBody body = RequireBody(scene);

            var root = new GameObject("Race Worlds");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            var view = AddRequired<RaceThemeView>(root);

            // Standard city: an instance of the configured prefab, three loops like in his playground, closed until the view opens it.
            var standardGo = (GameObject)PrefabUtility.InstantiatePrefab(standardPrefab, scene);
            standardGo.name = "Standard City";
            standardGo.transform.SetParent(root.transform, true);
            var standard = standardGo.GetComponent<LoopWayGenerator>()
                ?? throw new InvalidOperationException($"'{StandardWorldPrefab}' has no LoopWayGenerator.");
            var standardSo = new SerializedObject(standard);
            Prop(standardSo, "loopCount").intValue = 3;
            Apply(standardSo);
            standardGo.SetActive(false);

            var themed = new ThemedLoopWay[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                var go = new GameObject("Theme - " + definitions[i].concept);
                go.transform.SetParent(root.transform, false);
                themed[i] = AddRequired<ThemedLoopWay>(go);
                var so = new SerializedObject(themed[i]);
                Prop(so, "theme").objectReferenceValue = definitions[i];
                Apply(so);
                go.SetActive(false);
            }

            var viewSo = new SerializedObject(view);
            Prop(viewSo, "body").objectReferenceValue = body;
            Prop(viewSo, "standardWorld").objectReferenceValue = standard;
            SetObjectList(Prop(viewSo, "themedWorlds"), themed);
            Apply(viewSo);

            Transform track = SceneHomeTheme.FindByName(scene, TrackName);
            if (track != null) track.gameObject.SetActive(false);
            else Warn(Source, $"No '{TrackName}' object in the Race scene; the flat ground is not switched off.");

            Log(Source, $"Race: 'Race Worlds' with the standard city + {themed.Length} themed worlds, '{TrackName}' off.");
            return view;
        }

        private static RunnerBody RequireBody(Scene scene)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                RunnerBody found = sceneRoot.GetComponentInChildren<RunnerBody>(true);
                if (found != null) return found;
            }
            throw new InvalidOperationException($"No RunnerBody in '{scene.path}'; the theme view needs the runner.");
        }
    }
}
