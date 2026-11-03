using UnityEngine;

namespace SpeedStars.Meta.Settings
{
    /// <summary>Pure rules of the settings values that need a little arithmetic (no state, no config reads).</summary>
    public static class SettingsRules
    {
        /// <summary>
        /// Swipe length (share of the screen height) for a sensitivity: 0 gives <paramref name="distanceAtZero"/> (long swipe),
        /// 1 gives <paramref name="distanceAtFull"/> (short swipe). The sensitivity is kept inside 0..1.
        /// </summary>
        public static float SwipeDistance(float sensitivity, float distanceAtFull, float distanceAtZero) =>
            Mathf.Lerp(distanceAtZero, distanceAtFull, Mathf.Clamp01(sensitivity));

        /// <summary>A lane choice kept inside 0..<paramref name="choiceCount"/> (0 = automatic).</summary>
        public static int ClampLane(int lane, int choiceCount) => Mathf.Clamp(lane, 0, Mathf.Max(0, choiceCount));

        /// <summary>A slider value kept inside 0..1; a never-touched (negative) value reads as the fallback.</summary>
        public static float UnitOrFallback(float saved, float fallback) => saved < 0f ? Mathf.Clamp01(fallback) : Mathf.Clamp01(saved);
    }
}
