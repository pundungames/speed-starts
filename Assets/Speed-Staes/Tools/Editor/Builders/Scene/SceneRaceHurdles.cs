using System;
using SpeedStars.Bridge;
using SpeedStars.Physical;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The hurdles of the Race scene: one <see cref="RunnerHurdles"/> (the object copied from PhysicalTest, or a new one) that keeps
    /// its hands off the race length and starts empty, plus <see cref="RaceHurdles"/>, which gives it the current event's layout.
    /// Called by <c>SceneBuilder.BuildRace</c> after <c>SceneWiring.WireRace</c> (the adapter has to exist).
    /// </summary>
    internal static class SceneRaceHurdles
    {
        private const string Source = nameof(SceneRaceHurdles);

        public const string HurdlePrefabPath = "Assets/Speed-Staes/Prefabs/Hurdles/HurdleBarrier.prefab";
        public const string ObjectName = "Hurdles";

        public static void Add(Scene scene)
        {
            var prefab = LoadRequired<GameObject>(HurdlePrefabPath, "the hurdle prefab");
            RunnerBody player = RequireOne<RunnerBody>(scene, "RunnerBody (the player)");
            RaceController race = RequireOne<RaceController>(scene, "RaceController");
            RaceSessionAdapter adapter = RequireOne<RaceSessionAdapter>(scene, "RaceSessionAdapter (SceneWiring.WireRace runs first)");

            RunnerHurdles hurdles = FindOne<RunnerHurdles>(scene);
            if (hurdles == null)
            {
                var go = new GameObject(ObjectName);
                SceneManager.MoveGameObjectToScene(go, scene);
                hurdles = AddRequired<RunnerHurdles>(go);
            }

            var so = new SerializedObject(hurdles);
            Prop(so, "body").objectReferenceValue = player;
            Prop(so, "race").objectReferenceValue = race;
            Prop(so, "raceDistance").floatValue = 0f;
            Prop(so, "count").intValue = 0;
            Prop(so, "hurdlePrefab").objectReferenceValue = prefab;
            Apply(so);

            RaceHurdles link = hurdles.GetComponent<RaceHurdles>();
            if (link == null) link = AddRequired<RaceHurdles>(hurdles.gameObject);
            var linkSo = new SerializedObject(link);
            Prop(linkSo, "session").objectReferenceValue = adapter;
            Prop(linkSo, "hurdles").objectReferenceValue = hurdles;
            Apply(linkSo);

            Log(Source, $"Race: '{hurdles.name}' uses '{prefab.name}' and takes its layout from the current event.");
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static T RequireOne<T>(Scene scene, string what) where T : Component
        {
            T found = FindOne<T>(scene);
            return found != null ? found : throw new InvalidOperationException($"No {what} in '{scene.path}'.");
        }
    }
}
