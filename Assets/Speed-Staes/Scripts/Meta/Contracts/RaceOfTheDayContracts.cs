using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>The rule's answer for one UTC day: how far everyone runs and what everyone wears.</summary>
    public readonly struct RaceOfTheDayInfo
    {
        /// <summary>UTC day number (days since 0001-01-01). The same key always gives the same distance and outfit.</summary>
        public readonly long DayKey;

        /// <summary>Race length of the day (m); 0 = the rule has no distance list (the event's catalog length applies).</summary>
        public readonly float DistanceMeters;

        /// <summary>Name of the day's racer look (debugging, tests). Empty when the pool is empty.</summary>
        public readonly string LookLabel;

        /// <summary>The ready-made outfit every runner wears in the day's race. Empty outfit when the pool is empty.</summary>
        public readonly CosmeticOutfit Outfit;

        public RaceOfTheDayInfo(long dayKey, float distanceMeters, string lookLabel, CosmeticOutfit outfit)
        {
            DayKey = dayKey;
            DistanceMeters = distanceMeters;
            LookLabel = lookLabel ?? string.Empty;
            Outfit = outfit ?? CosmeticOutfit.Empty;
        }
    }

    /// <summary>The fastest result of the day's field (a stand-in for the world until a server exists).</summary>
    public readonly struct DailyFieldRecord
    {
        public readonly string Name;
        public readonly string CountryCode;
        public readonly long ElapsedMilliseconds;

        public DailyFieldRecord(string name, string countryCode, long elapsedMilliseconds)
        {
            Name = name ?? string.Empty;
            CountryCode = countryCode ?? string.Empty;
            ElapsedMilliseconds = elapsedMilliseconds;
        }
    }

    /// <summary>
    /// Race of the Day (T-087): one event whose distance and runner outfit change every UTC day, the same for everybody.
    /// Free and unlimited. Times are recorded per distance (<c>RecordIdFor</c>), so a personal best only ever compares
    /// races of the same length.
    /// </summary>
    public interface IRaceOfTheDay
    {
        /// <summary>The catalog id of the event ("race_of_the_day").</summary>
        RaceEventId EventId { get; }

        /// <summary>Today's distance and outfit, computed from the clock at each call (the day may roll over while the app is open).</summary>
        RaceOfTheDayInfo Today { get; }

        bool IsRaceOfTheDay(RaceEventId id);

        /// <summary>The id the race's time is stored under: the event id plus the distance, e.g. "race_of_the_day:150m".</summary>
        RaceEventId RecordIdFor(float distanceMeters);

        /// <summary>Today's fastest result of the field; false without a field (no leaderboard numbers or no distance).</summary>
        bool TryGetFieldRecord(out DailyFieldRecord record);
    }
}
