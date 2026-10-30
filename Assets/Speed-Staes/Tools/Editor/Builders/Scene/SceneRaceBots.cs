using System;
using SpeedStars.Bridge;
using SpeedStars.Core.Race.Bots;
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
    /// The opponents of the Race scene (T-075): a "Race Bots" object with <see cref="RaceBots"/>, wired to Malik's runner prefab, the bot
    /// config asset, the player's runner and race controller, and the session adapter (which also gets the bots for the rank). Called by
    /// <c>SceneBuilder.BuildRace</c> after <c>SceneWiring.WireRace</c> (the adapter has to exist). The runner copies are made at run time.
    /// It also fills, as scene overrides, the references Malik's / Serhat's code would otherwise look up with FindObjectOfType (one
    /// runner assumed): RaceController.body and the worlds' runner.
    /// </summary>
    internal static class SceneRaceBots
    {
        private const string Source = nameof(SceneRaceBots);

        public const string RunnerPrefabPath = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        public const string ObjectName = "Race Bots";

        public static RaceBots Add(Scene scene)
        {
            var prefab = LoadRequired<GameObject>(RunnerPrefabPath, "Malik's runner prefab");
            BotRaceConfig config = BotRaceConfigSeed.Seed(overwrite: false)
                ?? throw new InvalidOperationException($"No BotRaceConfig at '{BotRaceConfigSeed.Path}'.");

            RunnerBody player = RequireOne<RunnerBody>(scene, "RunnerBody (the player)");
            RaceController race = RequireOne<RaceController>(scene, "RaceController");
            RaceSessionAdapter adapter = RequireOne<RaceSessionAdapter>(scene, "RaceSessionAdapter (SceneWiring.WireRace runs first)");

            RaceBots bots = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bots = root.GetComponentInChildren<RaceBots>(true);
                if (bots != null) break;
            }
            if (bots == null)
            {
                var go = new GameObject(ObjectName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                bots = AddRequired<RaceBots>(go);
            }

            var so = new SerializedObject(bots);
            Prop(so, "config").objectReferenceValue = config;
            Prop(so, "runnerPrefab").objectReferenceValue = prefab;
            Prop(so, "player").objectReferenceValue = player;
            Prop(so, "race").objectReferenceValue = race;
            Prop(so, "session").objectReferenceValue = adapter;
            Apply(so);

            var adapterSo = new SerializedObject(adapter);
            Prop(adapterSo, "bots").objectReferenceValue = bots;
            Apply(adapterSo);

            HardenSingleRunnerLookups(scene, player, race);
            WireInstaller(scene, bots);
            Log(Source, $"Race: '{ObjectName}' ({config.BotCount} bots from '{prefab.name}') wired to the adapter; single-runner lookups set explicitly.");
            return bots;
        }

        /// <summary>
        /// Malik / Serhat code falls back to FindObjectOfType when a reference is empty. With more runners in the scene that would pick
        /// one at random, so the references are written explicitly (scene overrides only; their files are not touched).
        /// </summary>
        private static void HardenSingleRunnerLookups(Scene scene, RunnerBody player, RaceController race)
        {
            var raceSo = new SerializedObject(race);
            SerializedProperty body = Prop(raceSo, "body");
            if (body.objectReferenceValue == null)
            {
                body.objectReferenceValue = player;
                Apply(raceSo);
                Log(Source, "RaceController.body was empty (FindObjectOfType<RunnerBody>); set to the player's runner.");
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (ThemedLoopWay world in root.GetComponentsInChildren<ThemedLoopWay>(true)) SetRunner(world, player);
                foreach (LoopWayGenerator world in root.GetComponentsInChildren<LoopWayGenerator>(true)) SetRunner(world, player);
            }
        }

        private static void SetRunner(Component world, RunnerBody player)
        {
            var so = new SerializedObject(world);
            SerializedProperty runner = so.FindProperty("runner");
            if (runner == null || runner.objectReferenceValue != null) return;
            runner.objectReferenceValue = player.transform;
            Apply(so);
        }

        /// <summary>The installer binds IRaceStandings from this object (field "raceBots"); a scene whose installer has no such field yet only warns.</summary>
        private static void WireInstaller(Scene scene, RaceBots bots)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.GetType().Name != "RaceSceneInstaller") continue;
                    var so = new SerializedObject(behaviour);
                    SerializedProperty field = so.FindProperty("raceBots");
                    if (field == null)
                    {
                        Warn(Source, "RaceSceneInstaller has no 'raceBots' field yet: IRaceStandings is not bound (open WIRING need, T-075).");
                        return;
                    }
                    field.objectReferenceValue = bots;
                    Apply(so);
                    return;
                }
            }
            Warn(Source, "No RaceSceneInstaller in the scene; IRaceStandings is not bound.");
        }

        private static T RequireOne<T>(Scene scene, string what) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            throw new InvalidOperationException($"No {what} in '{scene.path}'.");
        }
    }
}
