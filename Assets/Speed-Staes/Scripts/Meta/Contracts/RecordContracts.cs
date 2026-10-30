using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>The player's best finished race of one event on one UTC day (what the leaderboard ranks).</summary>
    public readonly struct DayBestRecord
    {
        public readonly long DayUtcTicks;
        public readonly long ElapsedMilliseconds;
        public readonly long AchievedUtcTicks;

        public DayBestRecord(long dayUtcTicks, long elapsedMilliseconds, long achievedUtcTicks)
        {
            DayUtcTicks = dayUtcTicks;
            ElapsedMilliseconds = elapsedMilliseconds;
            AchievedUtcTicks = achievedUtcTicks;
        }
    }

    /// <summary>Read side of the player's per-day bests, written by <c>IPersonalBests.Record</c>. The local leaderboard builds on it.</summary>
    public interface IDayBestSource
    {
        /// <summary>Raised after a race changed (added or improved) a day best.</summary>
        event Action DayBestChanged;

        /// <summary>Appends the event's day bests to <paramref name="into"/>.</summary>
        void CopyDayBests(RaceEventId eventId, List<DayBestRecord> into);
    }

    /// <summary>What recording one finished race did to the event's personal best.</summary>
    public readonly struct PersonalBestUpdate
    {
        /// <summary>True when this race beat the stored best (the first valid race of an event counts).</summary>
        public readonly bool IsNewRecord;

        /// <summary>False when the event has no valid best at all (e.g. the only time given was invalid).</summary>
        public readonly bool HasBest;

        /// <summary>The event's best time after this race, in seconds (0 when <c>HasBest</c> is false).</summary>
        public readonly double BestSeconds;

        public PersonalBestUpdate(bool isNewRecord, bool hasBest, double bestSeconds)
        {
            IsNewRecord = isNewRecord;
            HasBest = hasBest;
            BestSeconds = bestSeconds;
        }
    }

    /// <summary>
    /// The player's best time per race event, saved. Written once per finished race; there is no world or friends data
    /// here (that is the leaderboard, a later phase).
    /// </summary>
    public interface IPersonalBests
    {
        /// <summary>The stored best of an event; false when the event has never been finished.</summary>
        bool TryGetBest(RaceEventId eventId, out double seconds);

        /// <summary>
        /// Records a finished race. <paramref name="raceId"/> identifies the race (the payout id): the same id again does
        /// not write twice and returns the first result. A time that is not a positive finite number is ignored.
        /// "New record" only when the time is better by at least one centisecond (what the stopwatch can show).
        /// </summary>
        PersonalBestUpdate Record(RaceEventId eventId, double seconds, string raceId);
    }
}
