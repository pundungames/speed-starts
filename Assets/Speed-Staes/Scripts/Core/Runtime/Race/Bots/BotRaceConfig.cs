using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// One way a bot can run; each bot draws one per race, by weight. Scales on the bot's own numbers: an early presser taps faster
    /// than the step wants, a late one waits too long, an erratic one slips a lot. Its time factors keep it on its target time.
    /// </summary>
    [Serializable]
    public struct BotStyle
    {
        [Tooltip("Name, only for reading the list.")]
        public string name;
        [Tooltip("How often this style is drawn, against the others.")]
        [Min(0f)] public float weight;
        [Tooltip("Press period x this (below 1 = taps faster, above = slower).")]
        [Range(0.5f, 1.6f)] public float periodScale;
        [Tooltip("Key hold x this (short taps or long pushes).")]
        [Range(0.3f, 2f)] public float holdScale;
        [Tooltip("Tempo wander x this (0 = metronome).")]
        [Range(0f, 4f)] public float driftScale;
        [Tooltip("Wrong foot chance x this.")]
        [Range(0f, 4f)] public float wrongFootScale;
        [Tooltip("Rush chance x this.")]
        [Range(0f, 4f)] public float rushScale;
        [Tooltip("Stumble chance x this.")]
        [Range(0f, 4f)] public float stumbleScale;
        [Tooltip("Measured by the calibration tool: this style's 100 m time / the plain table's, for the worst bot. The planner " +
                 "aims a styled bot at the time that this turns into its target.")]
        [Min(0.1f)] public float timeFactorWorst;
        [Tooltip("Measured by the calibration tool: the same for the best bot.")]
        [Min(0.1f)] public float timeFactorBest;

        public BotStyle(string name, float weight, float periodScale, float holdScale, float driftScale, float wrongFootScale,
            float rushScale, float stumbleScale, float timeFactorWorst = 1f, float timeFactorBest = 1f)
        {
            this.timeFactorWorst = timeFactorWorst;
            this.timeFactorBest = timeFactorBest;
            this.name = name;
            this.weight = weight;
            this.periodScale = periodScale;
            this.holdScale = holdScale;
            this.driftScale = driftScale;
            this.wrongFootScale = wrongFootScale;
            this.rushScale = rushScale;
            this.stumbleScale = stumbleScale;
        }
    }

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

        [Header("Liveliness (best skill .. worst skill): habits that keep a bot from running like a metronome")]
        [Tooltip("Chance per press that the best bot presses the same foot twice (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float wrongFootBestChance = 0.01f;

        [Tooltip("Chance per press that the worst bot presses the same foot twice (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float wrongFootWorstChance = 0.05f;

        [Tooltip("Chance per press that the best bot starts a rush: too many presses, the head tips forward, then it recovers (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float rushBestChance = 0.015f;

        [Tooltip("Chance per press that the worst bot starts a rush (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float rushWorstChance = 0.05f;

        [Tooltip("Fewest presses in one rush.")]
        [Min(1)]
        [SerializeField] private int rushPressesMin = 3;

        [Tooltip("Most presses in one rush.")]
        [Min(1)]
        [SerializeField] private int rushPressesMax = 6;

        [Tooltip("Press period during a rush, as a share of the bot's own (0.45 = about twice as fast).")]
        [Range(0.2f, 1f)]
        [SerializeField] private float rushPeriodRatio = 0.45f;

        [Tooltip("Chance per press that the best bot stumbles: a few presses all over the place, a pause, then it recovers (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float stumbleBestChance = 0.004f;

        [Tooltip("Chance per press that the worst bot stumbles (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float stumbleWorstChance = 0.02f;

        [Tooltip("Slower presses after a rush or a stumble while the bot finds its rhythm again.")]
        [Min(0)]
        [SerializeField] private int recoverPresses = 3;

        [Tooltip("Press period while recovering, as a share of the bot's own (above 1 = slower).")]
        [Range(1f, 2f)]
        [SerializeField] private float recoverPeriodRatio = 1.2f;

        [Tooltip("The tempo moves between faster and slower stretches, up to this share of the press period either way, before " +
                 "the style scales it (0 = metronome).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float tempoDrift = 0.08f;

        [Tooltip("Each bot draws one of these per race, by weight: its own tempo, hold, wander and slips. After changing them, run " +
                 "the bot calibration again (it measures each style's time factors).")]
        [SerializeField] private List<BotStyle> styles = new List<BotStyle>
        {
            new BotStyle("On time", 0.3f, 1f, 1f, 0.3f, 0.3f, 0.3f, 0f, 0.995f, 0.975f),
            new BotStyle("Early", 0.25f, 0.75f, 0.6f, 1f, 1f, 1.5f, 0.5f, 0.822f, 1.037f),
            new BotStyle("Late", 0.25f, 1.3f, 1.5f, 0.8f, 0.6f, 0.5f, 0.6f, 1.142f, 1.228f),
            new BotStyle("Erratic", 0.2f, 1f, 1f, 2.5f, 2.5f, 2f, 3f, 1.034f, 1.048f),
        };

        [Header("Calibration (skill -> expected 100 m time)")]
        [Tooltip("Skill to expected time rows, skill ascending, seconds falling. Measured against the runner sim by the calibration tool.")]
        [SerializeField] private List<BotCalibrationPoint> calibration = new List<BotCalibrationPoint>
        {
            new BotCalibrationPoint(0f, 22.468f),
            new BotCalibrationPoint(0.1f, 21.316f),
            new BotCalibrationPoint(0.2f, 21.016f),
            new BotCalibrationPoint(0.3f, 19.719f),
            new BotCalibrationPoint(0.4f, 18.931f),
            new BotCalibrationPoint(0.5f, 17.192f),
            new BotCalibrationPoint(0.6f, 15.561f),
            new BotCalibrationPoint(0.7f, 13.75f),
            new BotCalibrationPoint(0.8f, 12.381f),
            new BotCalibrationPoint(0.9f, 11.319f),
            new BotCalibrationPoint(1f, 11.137f),
        };

        [Header("Event profiles (one table per event; the table above is the 100 m base)")]
        [Tooltip("Tables measured over an event's own length and hurdles by the calibration tool. An event not listed uses the 100 m table above.")]
        [SerializeField] private List<BotEventProfile> eventProfiles = new List<BotEventProfile>
        {
            new BotEventProfile("110mh", 21.43f, new[]
            {
                new BotCalibrationPoint(0f, 24.652f),
                new BotCalibrationPoint(0.1f, 23.058f),
                new BotCalibrationPoint(0.2f, 22.767f),
                new BotCalibrationPoint(0.3f, 21.059f),
                new BotCalibrationPoint(0.4f, 20.101f),
                new BotCalibrationPoint(0.5f, 18.109f),
                new BotCalibrationPoint(0.6f, 16.657f),
                new BotCalibrationPoint(0.7f, 14.895f),
                new BotCalibrationPoint(0.8f, 13.432f),
                new BotCalibrationPoint(0.9f, 12.307f),
                new BotCalibrationPoint(1f, 12.162f),
            }),
            new BotEventProfile("200m", 37.32f, new[]
            {
                new BotCalibrationPoint(0f, 42.387f),
                new BotCalibrationPoint(0.1f, 39.373f),
                new BotCalibrationPoint(0.2f, 39.124f),
                new BotCalibrationPoint(0.3f, 36.82f),
                new BotCalibrationPoint(0.4f, 35.015f),
                new BotCalibrationPoint(0.5f, 31.923f),
                new BotCalibrationPoint(0.6f, 28.967f),
                new BotCalibrationPoint(0.7f, 25.502f),
                new BotCalibrationPoint(0.8f, 22.7f),
                new BotCalibrationPoint(0.9f, 20.628f),
                new BotCalibrationPoint(1f, 19.67f),
            }),
            new BotEventProfile("400m", 71.46f, new[]
            {
                new BotCalibrationPoint(0f, 81.177f),
                new BotCalibrationPoint(0.1f, 76.52f),
                new BotCalibrationPoint(0.2f, 74.678f),
                new BotCalibrationPoint(0.3f, 70.577f),
                new BotCalibrationPoint(0.4f, 67.268f),
                new BotCalibrationPoint(0.5f, 61.379f),
                new BotCalibrationPoint(0.6f, 55.446f),
                new BotCalibrationPoint(0.7f, 48.81f),
                new BotCalibrationPoint(0.8f, 43.41f),
                new BotCalibrationPoint(0.9f, 39.032f),
                new BotCalibrationPoint(1f, 36.676f),
            }),
            new BotEventProfile("60mh", 13.13f, new[]
            {
                new BotCalibrationPoint(0f, 14.706f),
                new BotCalibrationPoint(0.1f, 13.973f),
                new BotCalibrationPoint(0.2f, 13.963f),
                new BotCalibrationPoint(0.3f, 12.904f),
                new BotCalibrationPoint(0.4f, 12.058f),
                new BotCalibrationPoint(0.5f, 11.064f),
                new BotCalibrationPoint(0.6f, 10.192f),
                new BotCalibrationPoint(0.7f, 9.026f),
                new BotCalibrationPoint(0.8f, 8.356f),
                new BotCalibrationPoint(0.9f, 7.666f),
                new BotCalibrationPoint(1f, 7.656f),
            }),
            new BotEventProfile("400mh", 71.18f, new[]
            {
                new BotCalibrationPoint(0f, 81.296f),
                new BotCalibrationPoint(0.1f, 76.427f),
                new BotCalibrationPoint(0.2f, 74.125f),
                new BotCalibrationPoint(0.3f, 70.369f),
                new BotCalibrationPoint(0.4f, 66.735f),
                new BotCalibrationPoint(0.5f, 60.871f),
                new BotCalibrationPoint(0.6f, 55.06f),
                new BotCalibrationPoint(0.7f, 48.787f),
                new BotCalibrationPoint(0.8f, 43.437f),
                new BotCalibrationPoint(0.9f, 38.883f),
                new BotCalibrationPoint(1f, 36.605f),
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

        public IReadOnlyList<BotStyle> Styles => styles;

        /// <summary>A style index drawn by weight; -1 when there are none.</summary>
        public int DrawStyle(System.Random random)
        {
            float total = 0f;
            for (int i = 0; i < styles.Count; i++) total += Mathf.Max(0f, styles[i].weight);
            if (total <= 0f) return -1;
            double draw = random.NextDouble() * total;
            for (int i = 0; i < styles.Count; i++)
            {
                draw -= Mathf.Max(0f, styles[i].weight);
                if (draw < 0.0) return i;
            }
            return styles.Count - 1;
        }

        /// <summary>A style's time / the plain table's time for a bot of this skill (1 for no style).</summary>
        public float StyleTimeFactor(int style, float skill)
        {
            if (style < 0 || style >= styles.Count) return 1f;
            float f = Mathf.Lerp(styles[style].timeFactorWorst, styles[style].timeFactorBest, Mathf.Clamp01(skill));
            return f > 0.1f ? f : 1f;
        }

        /// <summary>Writes a style's measured time factors (calibration tool).</summary>
        public void SetStyleTimeFactors(int style, float worst, float best)
        {
            BotStyle s = styles[style];
            s.timeFactorWorst = worst;
            s.timeFactorBest = best;
            styles[style] = s;
        }

        /// <summary>
        /// The habits of a bot of this skill (0 = worst, 1 = best) in one style (-1 = plain, no style: what the calibration table is
        /// measured with). Used by the planner and the calibration tool alike.
        /// </summary>
        public BotQuirks QuirksFor(float skill, int style = -1)
        {
            float s = skill < 0f ? 0f : (skill > 1f ? 1f : skill);
            return new BotQuirks(
                wrongFootChance: wrongFootWorstChance + (wrongFootBestChance - wrongFootWorstChance) * s,
                rushChance: rushWorstChance + (rushBestChance - rushWorstChance) * s,
                stumbleChance: stumbleWorstChance + (stumbleBestChance - stumbleWorstChance) * s,
                rushPressesMin: rushPressesMin, rushPressesMax: rushPressesMax, rushPeriodRatio: rushPeriodRatio,
                recoverPresses: recoverPresses, recoverPeriodRatio: recoverPeriodRatio, tempoDrift: tempoDrift,
                styles: style >= 0 && style < styles.Count ? new[] { styles[style] } : null);
        }
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

        /// <summary>A flat (hurdle-free) event with opponents and its length, the anchors of <see cref="ForDistance"/>.</summary>
        public readonly struct FlatEvent
        {
            public readonly string EventId;
            public readonly float Meters;

            public FlatEvent(string eventId, float meters)
            {
                EventId = eventId ?? string.Empty;
                Meters = meters;
            }
        }

        /// <summary>
        /// The profile for a race: the event's own when it has one (today's selection, unchanged). An event without one (Race of the Day, whose
        /// length changes by day) is chosen by the run's LENGTH: a flat event of the same length gives its table (the 100 m one is the base
        /// table, so null); otherwise the table is interpolated by distance between the two nearest flat tables, and scaled by the ratio
        /// to the nearest one outside their range. Null = the base 100 m table applies.
        /// </summary>
        public static BotEventProfile ForDistance(BotRaceConfig config, string eventId, float raceMeters, IReadOnlyList<FlatEvent> flatEvents)
        {
            BotEventProfile own = Find(config, eventId);
            if (own != null || config == null || raceMeters <= 0f || config.Calibration.Count == 0) return own;

            // Anchors by length: the base table (100 m, profile null) and every flat event that has a measured profile.
            var anchors = new List<Anchor> { new Anchor(BaseTableMeters, null, config.Calibration, config.FallbackReferenceSeconds) };
            if (flatEvents != null)
            {
                foreach (FlatEvent flat in flatEvents)
                {
                    if (flat.Meters <= 0f || Math.Abs(flat.Meters - BaseTableMeters) < SameLengthTolerance) continue;
                    BotEventProfile profile = Find(config, flat.EventId);
                    if (profile == null) continue;
                    anchors.Add(new Anchor(flat.Meters, profile, profile.Calibration, Fallback(config, profile)));
                }
            }
            anchors.Sort((a, b) => a.Meters.CompareTo(b.Meters));

            foreach (Anchor anchor in anchors)
                if (Math.Abs(anchor.Meters - raceMeters) < SameLengthTolerance) return anchor.Profile;

            if (raceMeters < anchors[0].Meters) return Scaled(eventId, anchors[0], raceMeters);
            if (raceMeters > anchors[anchors.Count - 1].Meters) return Scaled(eventId, anchors[anchors.Count - 1], raceMeters);

            for (int i = 1; i < anchors.Count; i++)
            {
                if (raceMeters > anchors[i].Meters) continue;
                Anchor lo = anchors[i - 1], hi = anchors[i];
                float t = (raceMeters - lo.Meters) / (hi.Meters - lo.Meters);
                var rows = new List<BotCalibrationPoint>(lo.Table.Count);
                foreach (BotCalibrationPoint row in lo.Table)
                {
                    float loSeconds = row.seconds;
                    float hiSeconds = BotSkillCurve.SecondsForSkill(hi.Table, row.skill);
                    rows.Add(new BotCalibrationPoint(row.skill, loSeconds + (hiSeconds - loSeconds) * t));
                }
                return new BotEventProfile(eventId, lo.Fallback + (hi.Fallback - lo.Fallback) * t, rows);
            }
            return own;
        }

        /// <summary>Lengths within this many metres count as the same race length.</summary>
        public const float SameLengthTolerance = 0.5f;

        private readonly struct Anchor
        {
            public readonly float Meters;
            public readonly BotEventProfile Profile;
            public readonly IReadOnlyList<BotCalibrationPoint> Table;
            public readonly float Fallback;

            public Anchor(float meters, BotEventProfile profile, IReadOnlyList<BotCalibrationPoint> table, float fallback)
            {
                Meters = meters;
                Profile = profile;
                Table = table;
                Fallback = fallback;
            }
        }

        /// <summary>Outside the measured lengths: the nearest table's times scaled by the length ratio.</summary>
        private static BotEventProfile Scaled(string eventId, Anchor near, float raceMeters)
        {
            float ratio = raceMeters / near.Meters;
            var rows = new List<BotCalibrationPoint>(near.Table.Count);
            foreach (BotCalibrationPoint row in near.Table) rows.Add(new BotCalibrationPoint(row.skill, row.seconds * ratio));
            return new BotEventProfile(eventId, near.Fallback * ratio, rows);
        }

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
