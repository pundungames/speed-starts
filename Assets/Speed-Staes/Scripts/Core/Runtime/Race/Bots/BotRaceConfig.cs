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
        [SerializeField] private float easyMultiplier = 1.25f;

        [Tooltip("Middle target = reference x this at difficulty 1.")]
        [Min(0.1f)]
        [SerializeField] private float hardMultiplier = 0.85f;

        [Tooltip("Half-width of the field as a fraction of the middle target at skill range 0.")]
        [Range(0f, 1f)]
        [SerializeField] private float narrowSpread = 0.03f;

        [Tooltip("Half-width of the field as a fraction of the middle target at skill range 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float wideSpread = 0.18f;

        [Tooltip("Per-race randomness: each bot's target time moves by up to this fraction, so the same bot runs a different time each race.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float raceRandomRatio = 0.05f;

        [Header("Press driver (best skill .. worst skill)")]
        [Tooltip("Reaction after GO of the best bot (seconds).")]
        [Min(0f)]
        [SerializeField] private float reactionBestSeconds = 0.18f;

        [Tooltip("Reaction after GO of the worst bot (seconds).")]
        [Min(0f)]
        [SerializeField] private float reactionWorstSeconds = 0.45f;

        [Tooltip("Time between presses of the best bot (seconds). The runner likes about 0.32.")]
        [Min(0.05f)]
        [SerializeField] private float periodBestSeconds = 0.32f;

        [Tooltip("Time between presses of the worst bot (seconds).")]
        [Min(0.05f)]
        [SerializeField] private float periodWorstSeconds = 0.55f;

        [Tooltip("How long the best bot keeps a key down (seconds).")]
        [Min(0.02f)]
        [SerializeField] private float holdBestSeconds = 0.10f;

        [Tooltip("How long the worst bot keeps a key down (seconds).")]
        [Min(0.02f)]
        [SerializeField] private float holdWorstSeconds = 0.14f;

        [Tooltip("Timing jitter of the best bot, either way (milliseconds).")]
        [Min(0f)]
        [SerializeField] private float jitterBestMs = 10f;

        [Tooltip("Timing jitter of the worst bot, either way (milliseconds).")]
        [Min(0f)]
        [SerializeField] private float jitterWorstMs = 50f;

        [Tooltip("Chance of an early or late press for the best bot (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float mistakeBestChance = 0.02f;

        [Tooltip("Chance of an early or late press for the worst bot (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float mistakeWorstChance = 0.15f;

        [Tooltip("A mistaken press moves by up to this fraction of the press period.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float mistakeShiftRatio = 0.5f;

        [Header("Calibration (skill -> expected 100 m time)")]
        [Tooltip("Skill to expected time rows, skill ascending, seconds falling. Placeholder until the calibration tool fills it.")]
        [SerializeField] private List<BotCalibrationPoint> calibration = new List<BotCalibrationPoint>
        {
            new BotCalibrationPoint(0f, 32f),
            new BotCalibrationPoint(0.5f, 19f),
            new BotCalibrationPoint(1f, 13f),
        };

        [Header("Names")]
        [Tooltip("Pool of opponent names and flags; a race draws without repeats.")]
        [SerializeField] private List<BotIdentity> identities = new List<BotIdentity>
        {
            new BotIdentity("Kaito Arai", "JP"), new BotIdentity("Mateo Duarte", "PT"), new BotIdentity("Liam Brennan", "IE"),
            new BotIdentity("Amara Okafor", "NG"), new BotIdentity("Soren Lindqvist", "SE"), new BotIdentity("Diego Salazar", "CO"),
            new BotIdentity("Yusuf Demir", "TR"), new BotIdentity("Anika Rao", "IN"), new BotIdentity("Luca Ferrante", "IT"),
            new BotIdentity("Zane Mitchell", "AU"), new BotIdentity("Elias Haddad", "LB"), new BotIdentity("Noor Rahman", "BD"),
            new BotIdentity("Tomas Novak", "CZ"), new BotIdentity("Kofi Mensah", "GH"), new BotIdentity("Jae-won Seo", "KR"),
            new BotIdentity("Pablo Quintero", "MX"), new BotIdentity("Ingrid Solheim", "NO"), new BotIdentity("Hamza Idrissi", "MA"),
            new BotIdentity("Wei Lan", "CN"), new BotIdentity("Oskar Wolny", "PL"), new BotIdentity("Rafael Moreira", "BR"),
            new BotIdentity("Thabo Nkosi", "ZA"), new BotIdentity("Ciaran Doyle", "GB"), new BotIdentity("Mika Virtanen", "FI"),
            new BotIdentity("Andrei Popescu", "RO"), new BotIdentity("Camille Laurent", "FR"), new BotIdentity("Nikos Papas", "GR"),
            new BotIdentity("Santiago Vera", "AR"), new BotIdentity("Dmitri Volkov", "KZ"), new BotIdentity("Jonas Keller", "DE"),
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
        public IReadOnlyList<BotIdentity> Identities => identities;

        /// <summary>Replaces the calibration table (used by the calibration tool). Rows are sorted by skill.</summary>
        public void SetCalibration(IEnumerable<BotCalibrationPoint> points)
        {
            calibration = new List<BotCalibrationPoint>(points ?? Array.Empty<BotCalibrationPoint>());
            calibration.Sort((a, b) => a.skill.CompareTo(b.skill));
        }

        /// <summary>Difficulty and skill range are the two settings the player may change.</summary>
        public void SetDifficulty(float newDifficulty, float newSkillRange)
        {
            difficulty = Mathf.Clamp01(newDifficulty);
            skillRange = Mathf.Clamp01(newSkillRange);
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
