using System;
using SpeedStars.Bridge;
using UnityEditor;
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
    internal static class SceneCosmetics
    {
        private const string Source = nameof(SceneCosmetics);
        private const string WardrobeName = "Accessories";

        /// <summary>Applier + RaceRunnerOutfit on the runner root of the Race scene (the object that owns the Accessories wardrobe).</summary>
        public static void AddToRaceRunner(Scene scene)
        {
            GameObject runner = FindRunnerRoot(scene)
                ?? throw new InvalidOperationException($"Scene '{scene.path}': no object with an '{WardrobeName}' child; the runner prefab is not in the scene.");
            AddApplier(runner);
            if (runner.GetComponent<RaceRunnerOutfit>() == null) runner.AddComponent<RaceRunnerOutfit>();
            Log(Source, $"Race: CosmeticApplier + RaceRunnerOutfit added to '{runner.name}'.");
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
