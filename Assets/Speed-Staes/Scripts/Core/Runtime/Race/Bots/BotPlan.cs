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
        /// <summary>The 100 m time this bot is meant to finish in (seconds), this race.</summary>
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

        public BotPlan(string name, string countryCode, float targetSeconds, float skill, float reactionSeconds,
            float pressPeriodSeconds, float holdSeconds, float jitterSeconds, float mistakeChance, float mistakeShiftRatio)
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
        }
    }
}
