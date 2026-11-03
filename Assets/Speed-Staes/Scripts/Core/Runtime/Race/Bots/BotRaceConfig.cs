using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>One calibration row: a bot of this skill runs about this 100 m time (filled by the calibration tool).</summary>
    [Serializable]
    public struct BotCalibrationPoint
    {
        [Tooltip("Skill 0..1 (0 = slowest bot, 1 = fastest).")]
        [Range(0f, 1f)] public float skill;
        [Tooltip("Expected 100 m time of a bot with this skill (seconds). Falls as the skill rises.")]
        [Min(0f)] public float seconds;

        public BotCalibrationPoint(float skill, float seconds) { this.skill = skill; this.seconds = seconds; }
    }

    /// <summary>
    /// The bots' calibration of one race event (T-086): the skill to time table measured over that event's own length (and its hurdles,
    /// with the bots' jumps), plus the reference time used when the player has no result yet. The 100 m table stays in
    /// <see cref="BotRaceConfig"/> itself; an event without a profile here keeps the old way (the 100 m table, the player's time scaled).
    /// </summary>
    [Serializable]
    public sealed class BotEventProfile
    {
        [Tooltip("Race event id this table was measured for (\"400mh\"). Written by the calibration tool.")]
        [SerializeField] private string eventId = string.Empty;

        [Tooltip("Reference time (seconds) of this event when the player has no result yet. 0 = the config's own fallback.")]
        [Min(0f)]
        [SerializeField] private float fallbackReferenceSeconds;

        [Tooltip("Skill to expected time rows over this event's length, skill ascending, seconds falling.")]
        [SerializeField] private List<BotCalibrationPoint> calibration = new List<BotCalibrationPoint>();

        public BotEventProfile() { }

        public BotEventProfile(string eventId, float fallbackReferenceSeconds, IEnumerable<BotCalibrationPoint> calibration)
        {
            this.eventId = eventId ?? string.Empty;
            this.fallbackReferenceSeconds = fallbackReferenceSeconds;
            this.calibration = new List<BotCalibrationPoint>(calibration ?? Array.Empty<BotCalibrationPoint>());
            this.calibration.Sort((a, b) => a.skill.CompareTo(b.skill));
        }

        public string EventId => eventId ?? string.Empty;
        /// <summary>0 = none: the config's own fallback applies.</summary>
        public float FallbackReferenceSeconds => fallbackReferenceSeconds;
        public IReadOnlyList<BotCalibrationPoint> Calibration => calibration;
    }

    /// <summary>A made-up opponent name with an ISO country code. Not taken from any other game.</summary>
    [Serializable]
    public struct BotIdentity
    {
        [Tooltip("Display name.")]
        public string name;
        [Tooltip("ISO 3166-1 alpha-2 country code (\"TR\"); empty = no flag.")]
        public string countryCode;

        public BotIdentity(string name, string countryCode) { this.name = name; this.countryCode = countryCode; }
    }

    /// <summary>
    /// Numbers of the opponent bots in one asset (Data/Config/BotRaceConfig): how many, how hard, how spread out, and the limits of the
    /// press driver. Difficulty picks the middle target time (a multiple of the player's reference time), skill range widens the field
    /// around it. The field defaults are what BotRaceConfigSeed writes once; at runtime the asset counts. The calibration table is a
    /// monotonic placeholder until the calibration tool measures the real runner.
    /// </summary>
    [CreateAssetMenu(fileName = "BotRaceConfig", menuName = "Speed Stars/Config/Bot Race")]
    public sealed class BotRaceConfig : ScriptableObject
    {
        [Header("Field")]
        [Tooltip("How many opponents run (the player is extra). Capped by the name list.")]
        [Min(1)]
        [SerializeField] private int botCount = 5;

        [Tooltip("0 = easy, 1 = hard. Moves the middle target time between the easy and hard multiples of the reference time.")]
        [Range(0f, 1f)]
        [SerializeField] private float difficulty = 0.5f;

        [Tooltip("0 = bots finish close together, 1 = widely spread.")]
        [Range(0f, 1f)]
        [SerializeField] private float skillRange = 0.5f;

        [Header("Target time")]
        [Tooltip("Reference time (seconds) when the player has no result yet.")]
        [Min(1f)]
        [SerializeField] private float fallbackReferenceSeconds = 20f;

        [Tooltip("Middle target = reference x this at difficulty 0 (above 1 = slower than the player).")]
        [Min(0.1f)]
        [SerializeField] private float easyMultiplier = 1.27f;

        [Tooltip("Middle target = reference x this at difficulty 1.")]
        [Min(0.1f)]
        [SerializeField] private float hardMultiplier = 0.87f;

        [Tooltip("Half-width of the field as a fraction of the middle target at skill range 0.")]
        [Range(0f, 1f)]
        [SerializeField] private float narrowSpread = 0.03f;

        [Tooltip("Half-width of the field as a fraction of the middle target at skill range 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float wideSpread = 0.18f;

        [Tooltip("Per-race randomness: each bot's target time moves by up to this fraction, so the same bot runs a different time each race.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float raceRandomRatio = 0.03f;

        [Tooltip("Share of races (0..1) where the whole field is slower: the player's chance at a win without a perfect run.")]
        [Range(0f, 1f)]
        [SerializeField] private float easyRaceChance = 0.3f;

        [Tooltip("In such a race every target time is multiplied by this (above 1 = slower).")]
        [Min(0.1f)]
        [SerializeField] private float easyRaceMultiplier = 1.08f;

        [Tooltip("Share of races (0..1) where the whole field is faster. The rest of the races are unchanged.")]
        [Range(0f, 1f)]
        [SerializeField] private float hardRaceChance = 0.2f;

        [Tooltip("In such a race every target time is multiplied by this (below 1 = faster).")]
        [Min(0.1f)]
        [SerializeField] private float hardRaceMultiplier = 0.96f;

        [Header("Press driver (best skill .. worst skill)")]
        [Tooltip("Reaction after GO of the best bot (seconds).")]
        [Min(0f)]
        [SerializeField] private float reactionBestSeconds = 0.18f;

        [Tooltip("Reaction after GO of the worst bot (seconds).")]
        [Min(0f)]
        [SerializeField] private float reactionWorstSeconds = 0.45f;

        [Tooltip("Time between presses of the best bot (seconds). The runner likes about 0.32.")]
        [Min(0.05f)]
        [SerializeField] private float periodBestSeconds = 0.24f;

        [Tooltip("Time between presses of the worst bot (seconds).")]
        [Min(0.05f)]
        [SerializeField] private float periodWorstSeconds = 0.55f;

        [Tooltip("How long the best bot keeps a key down (seconds).")]
        [Min(0.02f)]
        [SerializeField] private float holdBestSeconds = 0.08f;

        [Tooltip("How long the worst bot keeps a key down (seconds).")]
        [Min(0.02f)]
        [SerializeField] private float holdWorstSeconds = 0.14f;

        [Tooltip("Timing jitter of the best bot, either way (milliseconds).")]
        [Min(0f)]
        [SerializeField] private float jitterBestMs = 5f;

        [Tooltip("Timing jitter of the worst bot, either way (milliseconds).")]
        [Min(0f)]
        [SerializeField] private float jitterWorstMs = 50f;

        [Tooltip("Chance of an early or late press for the best bot (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float mistakeBestChance = 0f;

        [Tooltip("Chance of an early or late press for the worst bot (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float mistakeWorstChance = 0.15f;

        [Tooltip("A mistaken press moves by up to this fraction of the press period.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float mistakeShiftRatio = 0.5f;

        [Header("Calibration (skill -> expected 100 m time)")]
        [Tooltip("Skill to expected time rows, skill ascending, seconds falling. Measured against the runner sim by the calibration tool.")]
        [SerializeField] private List<BotCalibrationPoint> calibration = new List<BotCalibrationPoint>
        {
            new BotCalibrationPoint(0f, 22.72887f),
            new BotCalibrationPoint(0.1f, 21.64705f),
            new BotCalibrationPoint(0.2f, 20.65838f),
            new BotCalibrationPoint(0.3f, 19.69595f),
            new BotCalibrationPoint(0.4f, 18.57485f),
            new BotCalibrationPoint(0.5f, 17.01661f),
            new BotCalibrationPoint(0.6f, 15.20134f),
            new BotCalibrationPoint(0.7f, 13.37755f),
            new BotCalibrationPoint(0.8f, 12.07655f),
            new BotCalibrationPoint(0.9f, 11.00178f),
            new BotCalibrationPoint(1f, 10.50756f),
        };

        [Header("Event profiles (one table per event; the table above is the 100 m base)")]
        [Tooltip("Tables measured over an event's own length and hurdles by the calibration tool. An event not listed uses the 100 m table above.")]
        [SerializeField] private List<BotEventProfile> eventProfiles = new List<BotEventProfile>
        {
            new BotEventProfile("110mh", 21.44f, new[]
            {
                new BotCalibrationPoint(0f, 24.378f),
                new BotCalibrationPoint(0.1f, 23.22f),
                new BotCalibrationPoint(0.2f, 22.163f),
                new BotCalibrationPoint(0.3f, 21.105f),
                new BotCalibrationPoint(0.4f, 19.526f),
                new BotCalibrationPoint(0.5f, 17.97f),
                new BotCalibrationPoint(0.6f, 16.14f),
                new BotCalibrationPoint(0.7f, 14.566f),
                new BotCalibrationPoint(0.8f, 13.199f),
                new BotCalibrationPoint(0.9f, 12.163f),
                new BotCalibrationPoint(1f, 11.501f),
            }),
            new BotEventProfile("200m", 36.98f, new[]
            {
                new BotCalibrationPoint(0f, 42.33f),
                new BotCalibrationPoint(0.1f, 40.185f),
                new BotCalibrationPoint(0.2f, 38.24f),
                new BotCalibrationPoint(0.3f, 36.403f),
                new BotCalibrationPoint(0.4f, 34.333f),
                new BotCalibrationPoint(0.5f, 31.428f),
                new BotCalibrationPoint(0.6f, 28.04f),
                new BotCalibrationPoint(0.7f, 24.586f),
                new BotCalibrationPoint(0.8f, 21.996f),
                new BotCalibrationPoint(0.9f, 19.961f),
                new BotCalibrationPoint(1f, 18.792f),
            }),
            new BotEventProfile("400m", 71.12f, new[]
            {
                new BotCalibrationPoint(0f, 81.625f),
                new BotCalibrationPoint(0.1f, 77.382f),
                new BotCalibrationPoint(0.2f, 73.58f),
                new BotCalibrationPoint(0.3f, 69.981f),
                new BotCalibrationPoint(0.4f, 66.001f),
                new BotCalibrationPoint(0.5f, 60.195f),
                new BotCalibrationPoint(0.6f, 53.683f),
                new BotCalibrationPoint(0.7f, 47.002f),
                new BotCalibrationPoint(0.8f, 41.845f),
                new BotCalibrationPoint(0.9f, 37.894f),
                new BotCalibrationPoint(1f, 35.358f),
            }),
            new BotEventProfile("60mh", 13.19f, new[]
            {
                new BotCalibrationPoint(0f, 14.677f),
                new BotCalibrationPoint(0.1f, 14.18f),
                new BotCalibrationPoint(0.2f, 13.7f),
                new BotCalibrationPoint(0.3f, 12.958f),
                new BotCalibrationPoint(0.4f, 11.935f),
                new BotCalibrationPoint(0.5f, 10.986f),
                new BotCalibrationPoint(0.6f, 9.956f),
                new BotCalibrationPoint(0.7f, 8.885f),
                new BotCalibrationPoint(0.8f, 8.155f),
                new BotCalibrationPoint(0.9f, 7.696f),
                new BotCalibrationPoint(1f, 7.338f),
            }),
            new BotEventProfile("400mh", 70.84f, new[]
            {
                new BotCalibrationPoint(0f, 81.482f),
                new BotCalibrationPoint(0.1f, 76.63f),
                new BotCalibrationPoint(0.2f, 72.946f),
                new BotCalibrationPoint(0.3f, 69.863f),
                new BotCalibrationPoint(0.4f, 65.853f),
                new BotCalibrationPoint(0.5f, 59.952f),
                new BotCalibrationPoint(0.6f, 53.126f),
                new BotCalibrationPoint(0.7f, 47.433f),
                new BotCalibrationPoint(0.8f, 41.691f),
                new BotCalibrationPoint(0.9f, 37.988f),
                new BotCalibrationPoint(1f, 35.17f),
            }),
        };

        [Header("Names")]
        [Tooltip("Pool of opponent names and flags; a race draws without repeats. Every country code must have flag art (flag_XX.png: BE CH DE DK FI FR GB IT JP NL NO PL SE TR UA US), otherwise the grey unknown flag shows.")]
        [SerializeField] private List<BotIdentity> identities = new List<BotIdentity>
        {
            new BotIdentity("Kaito Arai", "JP"), new BotIdentity("Haruto Sakamoto", "JP"),
            new BotIdentity("Soren Lindqvist", "SE"), new BotIdentity("Elsa Bergman", "SE"),
            new BotIdentity("Ingrid Solheim", "NO"), new BotIdentity("Magnus Haugen", "NO"),
            new BotIdentity("Mika Virtanen", "FI"), new BotIdentity("Aino Korhonen", "FI"),
            new BotIdentity("Mads Kjeldsen", "DK"), new BotIdentity("Freja Holm", "DK"),
            new BotIdentity("Jonas Keller", "DE"), new BotIdentity("Lena Brandt", "DE"),
            new BotIdentity("Camille Laurent", "FR"), new BotIdentity("Theo Marchand", "FR"),
            new BotIdentity("Luca Ferrante", "IT"), new BotIdentity("Giulia Moretti", "IT"),
            new BotIdentity("Ciaran Doyle", "GB"), new BotIdentity("Harriet Cole", "GB"),
            new BotIdentity("Zane Mitchell", "US"), new BotIdentity("Dana Whitfield", "US"),
            new BotIdentity("Oskar Wolny", "PL"), new BotIdentity("Kasia Zielinska", "PL"),
            new BotIdentity("Bram de Vries", "NL"), new BotIdentity("Sanne Visser", "NL"),
            new BotIdentity("Lars Peeters", "BE"), new BotIdentity("Margaux Dubois", "BE"),
            new BotIdentity("Reto Baumann", "CH"), new BotIdentity("Nadine Keller-Frei", "CH"),
            new BotIdentity("Andriy Kovalenko", "UA"), new BotIdentity("Oksana Melnyk", "UA"),
            new BotIdentity("Yusuf Demir", "TR"),
        };

        public int BotCount => botCount;
        public float Difficulty => difficulty;
        public float SkillRange => skillRange;
        public float FallbackReferenceSeconds => fallbackReferenceSeconds;
        public float EasyMultiplier => easyMultiplier;
        public float HardMultiplier => hardMultiplier;
        public float NarrowSpread => narrowSpread;
        public float WideSpread => wideSpread;
        public float RaceRandomRatio => raceRandomRatio;
        public float EasyRaceChance => easyRaceChance;
        public float EasyRaceMultiplier => easyRaceMultiplier;
        public float HardRaceChance => hardRaceChance;
        public float HardRaceMultiplier => hardRaceMultiplier;
        public float ReactionBestSeconds => reactionBestSeconds;
        public float ReactionWorstSeconds => reactionWorstSeconds;
        public float PeriodBestSeconds => periodBestSeconds;
        public float PeriodWorstSeconds => periodWorstSeconds;
        public float HoldBestSeconds => holdBestSeconds;
        public float HoldWorstSeconds => holdWorstSeconds;
        public float JitterBestMs => jitterBestMs;
        public float JitterWorstMs => jitterWorstMs;
        public float MistakeBestChance => mistakeBestChance;
        public float MistakeWorstChance => mistakeWorstChance;
        public float MistakeShiftRatio => mistakeShiftRatio;
        public IReadOnlyList<BotCalibrationPoint> Calibration => calibration;
        public IReadOnlyList<BotEventProfile> EventProfiles => eventProfiles;
        public IReadOnlyList<BotIdentity> Identities => identities;

        /// <summary>Replaces the calibration table (used by the calibration tool). Rows are sorted by skill.</summary>
        public void SetCalibration(IEnumerable<BotCalibrationPoint> points)
        {
            calibration = new List<BotCalibrationPoint>(points ?? Array.Empty<BotCalibrationPoint>());
            calibration.Sort((a, b) => a.skill.CompareTo(b.skill));
        }

        /// <summary>The measured profile of an event; false when it has none (or an empty table): the 100 m table applies.</summary>
        public bool TryGetProfile(string eventId, out BotEventProfile profile)
        {
            profile = null;
            if (string.IsNullOrEmpty(eventId) || eventProfiles == null) return false;
            foreach (BotEventProfile candidate in eventProfiles)
            {
                if (candidate == null || candidate.Calibration.Count == 0) continue;
                if (!string.Equals(candidate.EventId, eventId, StringComparison.Ordinal)) continue;
                profile = candidate;
                return true;
            }
            return false;
        }

        /// <summary>Adds the profile, or replaces the one of the same event (used by the calibration tool).</summary>
        public void SetProfile(BotEventProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (eventProfiles == null) eventProfiles = new List<BotEventProfile>();
            for (int i = 0; i < eventProfiles.Count; i++)
            {
                if (eventProfiles[i] == null || !string.Equals(eventProfiles[i].EventId, profile.EventId, StringComparison.Ordinal)) continue;
                eventProfiles[i] = profile;
                return;
            }
            eventProfiles.Add(profile);
        }

        /// <summary>Difficulty and skill range are the two settings the player may change.</summary>
        public void SetDifficulty(float newDifficulty, float newSkillRange)
        {
            difficulty = Mathf.Clamp01(newDifficulty);
            skillRange = Mathf.Clamp01(newSkillRange);
        }
    }

    /// <summary>Which table and reference time a race of an event uses. Pure, so the choice can be tested.</summary>
    public static class BotEventSetup
    {
        /// <summary>The length the config's own (base) table was measured over (m).</summary>
        public const float BaseTableMeters = 100f;

        /// <summary>The event's profile, or null (no config, no id, or no measured table): the base table applies.</summary>
        public static BotEventProfile Find(BotRaceConfig config, string eventId)
            => config != null && config.TryGetProfile(eventId, out BotEventProfile profile) ? profile : null;

        /// <summary>The table the planner inverts for this event.</summary>
        public static IReadOnlyList<BotCalibrationPoint> Table(BotRaceConfig config, BotEventProfile profile)
            => profile != null && profile.Calibration.Count > 0 ? profile.Calibration : config.Calibration;

        /// <summary>The reference time when the player has no result.</summary>
        public static float Fallback(BotRaceConfig config, BotEventProfile profile)
            => profile != null && profile.FallbackReferenceSeconds > 0f ? profile.FallbackReferenceSeconds : config.FallbackReferenceSeconds;

        /// <summary>
        /// The player's best time as the planner's reference. With a profile the table has the event's own length, so the time is used as
        /// it is; without one it is scaled to the 100 m table (good near 100 m). Null = no usable result.
        /// </summary>
        public static float? ReferenceSeconds(BotEventProfile profile, double? bestSeconds, float raceMeters)
        {
            if (!bestSeconds.HasValue || bestSeconds.Value <= 0.0) return null;
            float best = (float)bestSeconds.Value;
            if (profile != null) return best;
            return raceMeters > 0f ? best * BaseTableMeters / raceMeters : best;
        }
    }

    /// <summary>Skill and time lookups over a calibration table (linear between rows, clamped outside).</summary>
    public static class BotSkillCurve
    {
        /// <summary>Expected 100 m time for a skill. Empty table returns 0.</summary>
        public static float SecondsForSkill(IReadOnlyList<BotCalibrationPoint> table, float skill)
        {
            if (table == null || table.Count == 0) return 0f;
            if (skill <= table[0].skill) return table[0].seconds;
            for (int i = 1; i < table.Count; i++)
            {
                if (skill > table[i].skill) continue;
                float span = table[i].skill - table[i - 1].skill;
                float t = span <= 0f ? 1f : (skill - table[i - 1].skill) / span;
                return table[i - 1].seconds + (table[i].seconds - table[i - 1].seconds) * t;
            }
            return table[table.Count - 1].seconds;
        }

        /// <summary>The skill whose expected time is <paramref name="seconds"/> (inverse of the table), clamped to the table ends. Empty table returns 0.5.</summary>
        public static float SkillForSeconds(IReadOnlyList<BotCalibrationPoint> table, float seconds)
        {
            if (table == null || table.Count == 0) return 0.5f;
            if (seconds >= table[0].seconds) return table[0].skill;
            for (int i = 1; i < table.Count; i++)
            {
                if (seconds < table[i].seconds) continue;
                float span = table[i - 1].seconds - table[i].seconds;
                float t = span <= 0f ? 1f : (table[i - 1].seconds - seconds) / span;
                return table[i - 1].skill + (table[i].skill - table[i - 1].skill) * t;
            }
            return table[table.Count - 1].skill;
        }
    }
}
