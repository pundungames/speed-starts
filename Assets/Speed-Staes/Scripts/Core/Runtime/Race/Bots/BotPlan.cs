using System.Collections.Generic;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// One opponent as planned before the race: who it is, the time it should run, and the numbers its press driver uses.
    /// Immutable; <see cref="BotFieldPlanner"/> makes them, <see cref="BotPressSchedule"/> turns one into key events.
    /// </summary>
    public sealed class BotPlan
    {
        public readonly string Name;
        /// <summary>ISO 3166-1 alpha-2, empty = no flag.</summary>
        public readonly string CountryCode;
        /// <summary>The time this bot is meant to finish in (seconds), this race: over the event's own length with a measured profile, else a 100 m equivalent.</summary>
        public readonly float TargetSeconds;
        /// <summary>0..1, derived from <see cref="TargetSeconds"/> through the calibration table.</summary>
        public readonly float Skill;
        /// <summary>Delay of the first press after GO (seconds).</summary>
        public readonly float ReactionSeconds;
        /// <summary>Time between two presses (seconds).</summary>
        public readonly float PressPeriodSeconds;
        /// <summary>How long a key stays down (seconds).</summary>
        public readonly float HoldSeconds;
        /// <summary>Largest random shift of one press time, either way (seconds).</summary>
        public readonly float JitterSeconds;
        /// <summary>Chance that one press is early or late (0..1).</summary>
        public readonly float MistakeChance;
        /// <summary>How far a mistaken press is moved, as a fraction of the press period.</summary>
        public readonly float MistakeShiftRatio;
        /// <summary>What makes the bot look like a person pressing keys: wrong foot, rushes, tempo drift. Never null.</summary>
        public readonly BotQuirks Quirks;

        public BotPlan(string name, string countryCode, float targetSeconds, float skill, float reactionSeconds,
            float pressPeriodSeconds, float holdSeconds, float jitterSeconds, float mistakeChance, float mistakeShiftRatio,
            BotQuirks quirks = null)
        {
            Name = name ?? string.Empty;
            CountryCode = countryCode ?? string.Empty;
            TargetSeconds = targetSeconds;
            Skill = skill;
            ReactionSeconds = reactionSeconds;
            PressPeriodSeconds = pressPeriodSeconds;
            HoldSeconds = holdSeconds;
            JitterSeconds = jitterSeconds;
            MistakeChance = mistakeChance;
            MistakeShiftRatio = mistakeShiftRatio;
            Quirks = quirks ?? BotQuirks.None;
        }
    }

    /// <summary>
    /// A bot's habits beyond its timing jitter, so it does not run like a metronome. Each bot draws one of the styles per race
    /// (steady, quick short taps, long slow pushes, erratic...): its own tempo and hold, how much its tempo moves between faster and
    /// slower stretches, and how often it slips. Slips: the same foot twice; a rush (a burst of too-fast presses that tips the head
    /// forward, then slower ones while it finds its rhythm); a stumble (a few presses all over the place, then a pause, then the
    /// same slow way back). Chances are per press, before the style scales them. Immutable.
    /// </summary>
    public sealed class BotQuirks
    {
        public static readonly BotQuirks None = new BotQuirks(0f, 0f, 0f, 0, 0, 1f, 0, 1f, 0f, null);

        /// <summary>Chance (0..1) that a press uses the same foot as the one before.</summary>
        public readonly float WrongFootChance;
        /// <summary>Chance (0..1) that a press starts a rush.</summary>
        public readonly float RushChance;
        /// <summary>Chance (0..1) that a press starts a stumble.</summary>
        public readonly float StumbleChance;
        /// <summary>Presses in one rush, from min to max.</summary>
        public readonly int RushPressesMin, RushPressesMax;
        /// <summary>Press period during a rush, as a share of the normal one (below 1 = faster).</summary>
        public readonly float RushPeriodRatio;
        /// <summary>Slower presses after a rush or a stumble, while the bot finds its rhythm again.</summary>
        public readonly int RecoverPresses;
        /// <summary>Press period while recovering, as a share of the normal one (above 1 = slower).</summary>
        public readonly float RecoverPeriodRatio;
        /// <summary>The tempo moves up to this share of the period either way, before the style scales it.</summary>
        public readonly float TempoDrift;
        /// <summary>Styles a bot draws one from, by weight (the planner gives it just its own). Empty = no style: the numbers above as they are.</summary>
        public readonly IReadOnlyList<BotStyle> Styles;

        public BotQuirks(float wrongFootChance, float rushChance, float stumbleChance, int rushPressesMin, int rushPressesMax,
            float rushPeriodRatio, int recoverPresses, float recoverPeriodRatio, float tempoDrift, IReadOnlyList<BotStyle> styles)
        {
            WrongFootChance = wrongFootChance;
            RushChance = rushChance;
            StumbleChance = stumbleChance;
            RushPressesMin = rushPressesMin;
            RushPressesMax = rushPressesMax < rushPressesMin ? rushPressesMin : rushPressesMax;
            RushPeriodRatio = rushPeriodRatio;
            RecoverPresses = recoverPresses;
            RecoverPeriodRatio = recoverPeriodRatio;
            TempoDrift = tempoDrift;
            Styles = styles ?? System.Array.Empty<BotStyle>();
        }
    }
}
