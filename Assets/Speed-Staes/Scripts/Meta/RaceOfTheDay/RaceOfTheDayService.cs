using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Leaderboard;

namespace SpeedStars.Meta.DailyRace
{
    /// <summary>
    /// Today's Race of the Day from the clock and the config lists. Nothing is stored: the rule is a pure function of the UTC day.
    /// The field record (the day's "world record" until a server exists) comes from the leaderboard's deterministic bot generator,
    /// seeded per day and distance, so it changes with the day's race and stays the same on every launch.
    /// </summary>
    public sealed class RaceOfTheDayService : IRaceOfTheDay
    {
        private readonly IClock _clock;
        private readonly string _eventId;
        private readonly IReadOnlyList<float> _distances;
        private readonly IReadOnlyList<RaceOfTheDayLook> _looks;
        private readonly LeaderboardConfig _field;

        private long _cachedDay = long.MinValue;
        private RaceOfTheDayInfo _cachedInfo;
        private long _fieldDay = long.MinValue;
        private bool _hasField;
        private DailyFieldRecord _fieldRecord;

        /// <param name="field">Numbers of the bot field the day's record is drawn from; null = no field record.</param>
        public RaceOfTheDayService(IClock clock, string eventId, IReadOnlyList<float> distances, IReadOnlyList<RaceOfTheDayLook> looks, LeaderboardConfig field = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventId = string.IsNullOrEmpty(eventId) ? "race_of_the_day" : eventId;
            _distances = distances ?? Array.Empty<float>();
            _looks = looks ?? Array.Empty<RaceOfTheDayLook>();
            _field = field;
        }

        /// <summary>The composition root's entry: the lists come from the config asset.</summary>
        public static RaceOfTheDayService Create(IClock clock, RaceOfTheDayConfig config, LeaderboardConfig field)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return new RaceOfTheDayService(clock, config.EventId, config.Distances, config.Looks, field);
        }

        public RaceEventId EventId => new RaceEventId(_eventId);

        public RaceOfTheDayInfo Today
        {
            get
            {
                long day = RaceOfTheDayRule.DayKey(_clock.UtcNow);
                if (day != _cachedDay)
                {
                    _cachedInfo = Build(day);
                    _cachedDay = day;
                }
                return _cachedInfo;
            }
        }

        public bool IsRaceOfTheDay(RaceEventId id) => id == EventId;

        public RaceEventId RecordIdFor(float distanceMeters) => new RaceEventId(RaceOfTheDayRule.RecordId(_eventId, distanceMeters));

        public bool TryGetFieldRecord(out DailyFieldRecord record)
        {
            RaceOfTheDayInfo today = Today;
            if (today.DayKey != _fieldDay)
            {
                _hasField = TryBuildField(today, out _fieldRecord);
                _fieldDay = today.DayKey;
            }
            record = _fieldRecord;
            return _hasField;
        }

        private RaceOfTheDayInfo Build(long day)
        {
            int distanceIndex = RaceOfTheDayRule.IndexFor(day, _distances.Count, RaceOfTheDayRule.DistanceSalt);
            int lookIndex = RaceOfTheDayRule.IndexFor(day, _looks.Count, RaceOfTheDayRule.LookSalt);
            float distance = distanceIndex >= 0 ? _distances[distanceIndex] : 0f;
            if (lookIndex < 0) return new RaceOfTheDayInfo(day, distance, string.Empty, CosmeticOutfit.Empty);
            RaceOfTheDayLook look = _looks[lookIndex];
            return new RaceOfTheDayInfo(day, distance, look.Label, look.ToOutfit());
        }

        private bool TryBuildField(RaceOfTheDayInfo today, out DailyFieldRecord record)
        {
            record = default;
            if (_field == null || today.DistanceMeters <= 0f) return false;

            // One field per (day, distance): the day key in the id keeps it from repeating tomorrow.
            var id = new RaceEventId(_eventId + ":" + today.DayKey);
            List<BotResult> field = BotField.Generate(_field, id, today.DistanceMeters);
            if (field.Count == 0) return false;

            BotResult best = field[0];
            for (int i = 1; i < field.Count; i++)
                if (field[i].ElapsedMilliseconds < best.ElapsedMilliseconds) best = field[i];
            record = new DailyFieldRecord(best.Name, best.CountryCode, best.ElapsedMilliseconds);
            return true;
        }
    }
}
