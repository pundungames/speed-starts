using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// Plans the opponents of one race. Middle target = player's reference time x lerp(easy, hard, difficulty); the field is spread
    /// evenly around it, as wide as skill range says; each target then moves a little by the per-race randomness, and the whole field
    /// by the race's draw (an easy, normal or hard race, decided before the start: no bot reacts to the player). Skill comes from
    /// the target time through the calibration table (inverse), and the driver numbers from the skill. Pure; the same seed gives the same plan.
    /// </summary>
    public static class BotFieldPlanner
    {
        /// <summary>
        /// Fastest target first. <paramref name="playerReferenceSeconds"/> null or not positive = use the config's fallback.
        /// The count is the config's bot count, capped by the name pool.
        /// </summary>
        /// <param name="profile">
        /// The event's measured table and fallback (T-086), or null = the config's 100 m table and fallback (the old way, bit for bit).
        /// The reference time then has to be in the table's own length (see <see cref="BotEventSetup.ReferenceSeconds"/>).
        /// </param>
        /// <param name="difficulty">The player's own difficulty (settings window), 0..1; null = the config's.</param>
        /// <param name="skillRange">The player's own skill range (settings window), 0..1; null = the config's.</param>
        /// <param name="count">How many to plan; null = the config's bot count (a relay plans one per team).</param>
        public static IReadOnlyList<BotPlan> Plan(BotRaceConfig config, float? playerReferenceSeconds, Random random, BotEventProfile profile = null,
            float? difficulty = null, float? skillRange = null, int? count = null, int? totalWins = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (random == null) throw new ArgumentNullException(nameof(random));

            IReadOnlyList<BotIdentity> pool = config.Identities;
            int planned = Math.Min(Math.Max(count ?? config.BotCount, 0), pool.Count);
            var plans = new List<BotPlan>(planned);
            if (planned == 0) return plans;

            float reference = playerReferenceSeconds.HasValue && playerReferenceSeconds.Value > 0f
                ? playerReferenceSeconds.Value
                : BotEventSetup.Fallback(config, profile);
            IReadOnlyList<BotCalibrationPoint> table = BotEventSetup.Table(config, profile);
            int tier = !totalWins.HasValue ? -1 : totalWins.Value <= 5 ? 0 : totalWins.Value <= 30 ? 1 : 2;
            float chosenDifficulty = difficulty ?? config.Difficulty;
            float effectiveDifficulty = tier < 0 ? chosenDifficulty :
                Lerp(tier == 0 ? 0.24f : tier == 1 ? 0.68f : 1f, Clamp01(chosenDifficulty), 0.35f);
            float rhythmErrors = tier < 0 ? 1f : tier == 0 ? 1.6f : tier == 1 ? 0.8f : 0.52f;
            float earlyStartChance = tier == 0 ? 0.28f : tier == 1 ? 0.055f : 0.02f;
            float fastChance = tier < 0 ? 0f : tier == 0 ? 0.008f : tier == 1 ? 0.045f : 0.075f;
            int fastPresses = tier == 0 ? 3 : tier == 1 ? 6 : 9;
            float center = reference * Lerp(config.EasyMultiplier, config.HardMultiplier, effectiveDifficulty);
            float spread = Lerp(config.NarrowSpread, config.WideSpread, Clamp01(skillRange ?? config.SkillRange));
            center *= RaceMultiplier(config, (float)random.NextDouble());

            var targets = new float[planned];
            for (int i = 0; i < planned; i++)
            {
                float u = planned == 1 ? 0f : (float)i / (planned - 1) * 2f - 1f; // -1 fastest .. +1 slowest
                float raceRandom = ((float)random.NextDouble() * 2f - 1f) * config.RaceRandomRatio;
                targets[i] = Math.Max(center * (1f + u * spread) * (1f + raceRandom), 0.1f);
            }
            Array.Sort(targets);

            // Names: draw without repeats (partial shuffle of the pool indices).
            var order = new int[pool.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 0; i < planned; i++)
            {
                int j = i + random.Next(order.Length - i);
                (order[i], order[j]) = (order[j], order[i]);
            }

            for (int i = 0; i < planned; i++)
            {
                BotIdentity who = pool[order[i]];
                int style = config.DrawStyle(random);
                // The table is measured without a style; a style runs slower or faster by its factor, so aim the table at the time
                // that the factor turns into the target.
                float plain = Clamp01(BotSkillCurve.SkillForSeconds(table, targets[i]));
                float skill = Clamp01(BotSkillCurve.SkillForSeconds(table, targets[i] / config.StyleTimeFactor(style, plain)));
                bool anticipates = tier >= 0 && random.NextDouble() < earlyStartChance;
                plans.Add(new BotPlan(
                    who.name, who.countryCode, targets[i], skill,
                    reactionSeconds: Lerp(config.ReactionWorstSeconds, config.ReactionBestSeconds, skill) + (anticipates ? 0.32f : 0f),
                    pressPeriodSeconds: Lerp(config.PeriodWorstSeconds, config.PeriodBestSeconds, skill),
                    holdSeconds: Lerp(config.HoldWorstSeconds, config.HoldBestSeconds, skill),
                    jitterSeconds: Lerp(config.JitterWorstMs, config.JitterBestMs, skill) * 0.001f * rhythmErrors,
                    mistakeChance: Clamp01(Lerp(config.MistakeWorstChance, config.MistakeBestChance, skill) * rhythmErrors),
                    mistakeShiftRatio: config.MistakeShiftRatio,
                    quirks: config.QuirksFor(skill, style),
                    fastSectionChance: fastChance, fastSectionPresses: fastPresses,
                    fastPeriodSeconds: config.PeriodBestSeconds, anticipatesStart: anticipates));
            }
            return plans;
        }

        private static float RaceMultiplier(BotRaceConfig config, float draw)
        {
            if (draw < config.EasyRaceChance) return config.EasyRaceMultiplier;
            if (draw < config.EasyRaceChance + config.HardRaceChance) return config.HardRaceMultiplier;
            return 1f;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
