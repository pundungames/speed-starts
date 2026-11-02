using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// Plans the opponents of one race. Middle target = player's reference time x lerp(easy, hard, difficulty); the field is spread
    /// evenly around it, as wide as skill range says; each target then moves a little by the per-race randomness. Skill comes from
    /// the target time through the calibration table (inverse), and the driver numbers from the skill. Pure; the same seed gives the same plan.
    /// </summary>
    public static class BotFieldPlanner
    {
        /// <summary>
        /// Fastest target first. <paramref name="playerReferenceSeconds"/> null or not positive = use the config's fallback.
        /// The count is the config's bot count, capped by the name pool.
        /// </summary>
        public static IReadOnlyList<BotPlan> Plan(BotRaceConfig config, float? playerReferenceSeconds, Random random)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));

            IReadOnlyList<BotIdentity> pool = config.Identities;
            int count = Math.Min(Math.Max(config.BotCount, 0), pool.Count);
            var plans = new List<BotPlan>(count);
            if (count == 0) return plans;

            float reference = playerReferenceSeconds.HasValue && playerReferenceSeconds.Value > 0f
                ? playerReferenceSeconds.Value
                : config.FallbackReferenceSeconds;
            float center = reference * Lerp(config.EasyMultiplier, config.HardMultiplier, config.Difficulty);
            float spread = Lerp(config.NarrowSpread, config.WideSpread, config.SkillRange);

            var targets = new float[count];
            for (int i = 0; i < count; i++)
            {
                float u = count == 1 ? 0f : (float)i / (count - 1) * 2f - 1f; // -1 fastest .. +1 slowest
                float raceRandom = ((float)random.NextDouble() * 2f - 1f) * config.RaceRandomRatio;
                targets[i] = Math.Max(center * (1f + u * spread) * (1f + raceRandom), 0.1f);
            }
            Array.Sort(targets);

            // Names: draw without repeats (partial shuffle of the pool indices).
            var order = new int[pool.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 0; i < count; i++)
            {
                int j = i + random.Next(order.Length - i);
                (order[i], order[j]) = (order[j], order[i]);
            }

            for (int i = 0; i < count; i++)
            {
                BotIdentity who = pool[order[i]];
                float skill = Clamp01(BotSkillCurve.SkillForSeconds(config.Calibration, targets[i]));
                plans.Add(new BotPlan(
                    who.name, who.countryCode, targets[i], skill,
                    reactionSeconds: Lerp(config.ReactionWorstSeconds, config.ReactionBestSeconds, skill),
                    pressPeriodSeconds: Lerp(config.PeriodWorstSeconds, config.PeriodBestSeconds, skill),
                    holdSeconds: Lerp(config.HoldWorstSeconds, config.HoldBestSeconds, skill),
                    jitterSeconds: Lerp(config.JitterWorstMs, config.JitterBestMs, skill) * 0.001f,
                    mistakeChance: Lerp(config.MistakeWorstChance, config.MistakeBestChance, skill),
                    mistakeShiftRatio: config.MistakeShiftRatio));
            }
            return plans;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
