using System;

namespace SpeedStars.Meta.Relay
{
    /// <summary>Pure rules of the relay team, kept apart so they are tested without a scene.</summary>
    public static class RelayTeamRules
    {
        /// <summary>The preset a leg wears when the player never chose one: its own number, wrapping over the presets.</summary>
        public static int DefaultPreset(int leg, int presetCount)
        {
            if (presetCount <= 0 || leg < 0) return 0;
            return leg % presetCount;
        }

        /// <summary>A stored choice that is still a preset, else the leg's default (the preset count may have shrunk).</summary>
        public static int ResolvePreset(int stored, int leg, int presetCount)
        {
            if (presetCount <= 0) return 0;
            return stored >= 0 && stored < presetCount ? stored : DefaultPreset(leg, presetCount);
        }

        /// <summary>
        /// The legs the helper runners on the menu stage show: every leg except the selected one, in leg order (the main character
        /// wears the selected leg). Empty for fewer than two legs.
        /// </summary>
        public static int[] StageLegs(int legCount, int selectedLeg)
        {
            if (legCount < 2) return Array.Empty<int>();
            if (selectedLeg < 0 || selectedLeg >= legCount) selectedLeg = 0;
            var legs = new int[legCount - 1];
            int next = 0;
            for (int leg = 0; leg < legCount; leg++)
                if (leg != selectedLeg) legs[next++] = leg;
            return legs;
        }

        /// <summary>The largest relay leg count among the given counts (0 or 1 = not a relay); 0 when none is a relay.</summary>
        public static int TeamLegCount(System.Collections.Generic.IEnumerable<int> eventLegCounts)
        {
            int max = 0;
            if (eventLegCounts == null) return 0;
            foreach (int count in eventLegCounts)
                if (count > 1 && count > max) max = count;
            return max;
        }
    }
}
