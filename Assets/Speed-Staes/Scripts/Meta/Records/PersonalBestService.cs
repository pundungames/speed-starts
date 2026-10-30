using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Records
{
    /// <summary>
    /// Personal best per event on top of <c>PlayerProgress.BestTimes</c>. A time is compared in whole centiseconds
    /// (truncated, like the stopwatch display), so "NEW RECORD" never shows a time that reads the same as the old best.
    /// The raw seconds are what is stored. A repeated race id returns the first result and writes nothing.
    /// Every valid finish also updates the player's best of that (event, UTC day) in <c>PlayerProgress.DayBests</c>
    /// (the leaderboard's source); the list is capped by the leaderboard config and keeps each event's all-time best.
    /// </summary>
    public sealed class PersonalBestService : IPersonalBests, IDayBestSource
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly LeaderboardConfig _config;

        private string _lastRaceId = string.Empty;
        private PersonalBestUpdate _lastUpdate;

        public PersonalBestService(ProgressRepository repository, IClock clock, LeaderboardConfig config)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public event Action DayBestChanged;

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

            PersonalBestUpdate update = Apply(eventId, seconds, out bool dayChanged);
            _lastRaceId = raceId ?? string.Empty;
            _lastUpdate = update;
            if (dayChanged) DayBestChanged?.Invoke();
            return update;
        }

        public void CopyDayBests(RaceEventId eventId, List<DayBestRecord> into)
        {
            if (into == null) throw new ArgumentNullException(nameof(into));
            Require();
            foreach (DayBestEntry entry in _repository.Current.DayBests)
                if (entry != null && entry.EventId == eventId)
                    into.Add(new DayBestRecord(entry.DayUtcTicks, entry.ElapsedMilliseconds, entry.AchievedUtcTicks));
        }

        private PersonalBestUpdate Apply(RaceEventId eventId, double seconds, out bool dayChanged)
        {
            EventBestTime entry = Find(eventId);
            bool hasOld = entry != null && IsValid(entry.BestSeconds);
            dayChanged = false;

            if (!IsValid(seconds))
                return hasOld ? new PersonalBestUpdate(false, true, entry.BestSeconds) : new PersonalBestUpdate(false, false, 0d);

            dayChanged = RecordDayBest(eventId, seconds);

            if (hasOld && Centis(seconds) >= Centis(entry.BestSeconds))
            {
                if (dayChanged) _repository.Save();
                return new PersonalBestUpdate(false, true, entry.BestSeconds);
            }

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

        /// <summary>Adds or improves today's entry of the event; true when the list changed. Does not save.</summary>
        private bool RecordDayBest(RaceEventId eventId, double seconds)
        {
            DateTime now = _clock.UtcNow;
            long dayTicks = now.Date.Ticks;
            long ms = (long)Math.Round(seconds * 1000d);
            if (ms <= 0L) return false;

            List<DayBestEntry> list = _repository.Current.DayBests;
            foreach (DayBestEntry existing in list)
            {
                if (existing.EventId != eventId || existing.DayUtcTicks != dayTicks) continue;
                if (ms >= existing.ElapsedMilliseconds) return false;
                existing.ElapsedMilliseconds = ms;
                existing.AchievedUtcTicks = now.Ticks;
                return true;
            }

            list.Add(new DayBestEntry(eventId, dayTicks, ms, now.Ticks));
            Trim(list);
            return true;
        }

        /// <summary>
        /// Drops the oldest entries over the cap. An event's fastest entry (its all-time best) stays, so the cap never
        /// erases a record; if only such entries are left the list is allowed to stay over the cap.
        /// </summary>
        private void Trim(List<DayBestEntry> list)
        {
            int cap = _config.MaxDayBestEntries;
            while (list.Count > cap)
            {
                int victim = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (IsEventBest(list, list[i])) continue;
                    if (victim < 0 || IsOlder(list[i], list[victim])) victim = i;
                }
                if (victim < 0) return;
                list.RemoveAt(victim);
            }
        }

        private static bool IsOlder(DayBestEntry a, DayBestEntry b) =>
            a.DayUtcTicks != b.DayUtcTicks ? a.DayUtcTicks < b.DayUtcTicks : a.AchievedUtcTicks < b.AchievedUtcTicks;

        private static bool IsEventBest(List<DayBestEntry> list, DayBestEntry candidate)
        {
            foreach (DayBestEntry other in list)
            {
                if (ReferenceEquals(other, candidate) || other.EventId != candidate.EventId) continue;
                if (other.ElapsedMilliseconds < candidate.ElapsedMilliseconds) return false;
                if (other.ElapsedMilliseconds == candidate.ElapsedMilliseconds && other.AchievedUtcTicks < candidate.AchievedUtcTicks) return false;
            }
            return true;
        }

        private EventBestTime Find(RaceEventId eventId)
        {
            Require();
            foreach (EventBestTime entry in _repository.Current.BestTimes)
                if (entry != null && entry.EventId == eventId) return entry;
            return null;
        }

        private void Require()
        {
            if (_repository.Current == null) throw new InvalidOperationException("Load the progress before using personal bests.");
        }

        private static bool IsValid(double seconds) => !double.IsNaN(seconds) && !double.IsInfinity(seconds) && seconds > 0d;

        private static long Centis(double seconds) => (long)Math.Floor(seconds * 100d + 1e-9);
    }
}
