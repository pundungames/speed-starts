using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Records
{
    /// <summary>
    /// Personal best per event on top of <c>PlayerProgress.BestTimes</c>. A time is compared in whole centiseconds
    /// (truncated, like the stopwatch display), so "NEW RECORD" never shows a time that reads the same as the old best.
    /// The raw seconds are what is stored. A repeated race id returns the first result and writes nothing.
    /// </summary>
    public sealed class PersonalBestService : IPersonalBests
    {
        private readonly ProgressRepository _repository;

        private string _lastRaceId = string.Empty;
        private PersonalBestUpdate _lastUpdate;

        public PersonalBestService(ProgressRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public bool TryGetBest(RaceEventId eventId, out double seconds)
        {
            EventBestTime entry = Find(eventId);
            if (entry != null && IsValid(entry.BestSeconds))
            {
                seconds = entry.BestSeconds;
                return true;
            }
            seconds = 0d;
            return false;
        }

        public PersonalBestUpdate Record(RaceEventId eventId, double seconds, string raceId)
        {
            if (!string.IsNullOrEmpty(raceId) && string.Equals(raceId, _lastRaceId, StringComparison.Ordinal))
                return _lastUpdate;

            PersonalBestUpdate update = Apply(eventId, seconds);
            _lastRaceId = raceId ?? string.Empty;
            _lastUpdate = update;
            return update;
        }

        private PersonalBestUpdate Apply(RaceEventId eventId, double seconds)
        {
            EventBestTime entry = Find(eventId);
            bool hasOld = entry != null && IsValid(entry.BestSeconds);

            if (!IsValid(seconds))
                return hasOld ? new PersonalBestUpdate(false, true, entry.BestSeconds) : new PersonalBestUpdate(false, false, 0d);

            if (hasOld && Centis(seconds) >= Centis(entry.BestSeconds))
                return new PersonalBestUpdate(false, true, entry.BestSeconds);

            if (entry == null)
            {
                entry = new EventBestTime(eventId, seconds);
                _repository.Current.BestTimes.Add(entry);
            }
            else
            {
                entry.BestSeconds = seconds;
            }
            _repository.Save();
            return new PersonalBestUpdate(true, true, seconds);
        }

        private EventBestTime Find(RaceEventId eventId)
        {
            if (_repository.Current == null) throw new InvalidOperationException("Load the progress before using personal bests.");
            foreach (EventBestTime entry in _repository.Current.BestTimes)
                if (entry != null && entry.EventId == eventId) return entry;
            return null;
        }

        private static bool IsValid(double seconds) => !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds > 0d;

        private static long Centis(double seconds) => (long)Math.Floor(seconds * 100d + 1e-9);
    }
}
