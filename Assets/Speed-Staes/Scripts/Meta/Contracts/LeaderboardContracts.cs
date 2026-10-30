using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>Time window of a leaderboard query. All windows are UTC: week starts Monday 00:00, month on the 1st 00:00, today at 00:00.</summary>
    public enum LeaderboardPeriod
    {
        AllTime,
        ThisMonth,
        ThisWeek,
        Today
    }

    /// <summary>One leaderboard request: which event, which window, which region and which slice of the ranking.</summary>
    public readonly struct LeaderboardQuery
    {
        public readonly RaceEventId EventId;
        public readonly LeaderboardPeriod Period;

        /// <summary>Two-letter country code; null or empty = Global.</summary>
        public readonly string CountryCode;

        /// <summary>Rows to skip (the previous page's <c>NextOffset</c>).</summary>
        public readonly int Offset;

        /// <summary>Most rows wanted; the source may return fewer (its page cap).</summary>
        public readonly int Limit;

        public LeaderboardQuery(RaceEventId eventId, LeaderboardPeriod period, string countryCode, int offset, int limit)
        {
            EventId = eventId;
            Period = period;
            CountryCode = countryCode;
            Offset = offset;
            Limit = limit;
        }
    }

    /// <summary>One ranked row. Rank is the source's (Global or inside the queried country), never computed from a page number.</summary>
    public sealed class LeaderboardRow
    {
        public string EntryId { get; }
        public int Rank { get; }
        public string DisplayName { get; }
        public string CountryCode { get; }

        /// <summary>Raw time in milliseconds; ranking uses this, not the rounded display.</summary>
        public long ElapsedMilliseconds { get; }

        public DateTime AchievedAtUtc { get; }
        public bool IsLocalPlayer { get; }

        public LeaderboardRow(string entryId, int rank, string displayName, string countryCode, long elapsedMilliseconds, DateTime achievedAtUtc, bool isLocalPlayer)
        {
            EntryId = entryId ?? string.Empty;
            Rank = rank;
            DisplayName = displayName ?? string.Empty;
            CountryCode = countryCode ?? string.Empty;
            ElapsedMilliseconds = elapsedMilliseconds;
            AchievedAtUtc = achievedAtUtc;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    /// <summary>One answer: a slice of rows, the filter's total, the next offset and the local player's own row.</summary>
    public sealed class LeaderboardPage
    {
        public IReadOnlyList<LeaderboardRow> Rows { get; }

        /// <summary>Rows in the whole filtered ranking (not just this page).</summary>
        public int TotalCount { get; }

        /// <summary>Offset of the next page; -1 = this was the last one.</summary>
        public int NextOffset { get; }

        /// <summary>The local player's row for the same filter (with their real rank); null = NOT RANKED.</summary>
        public LeaderboardRow OwnEntry { get; }

        /// <summary>Start of the queried window (UTC). AllTime = <c>DateTime.MinValue</c>.</summary>
        public DateTime PeriodStartUtc { get; }

        public LeaderboardPage(IReadOnlyList<LeaderboardRow> rows, int totalCount, int nextOffset, LeaderboardRow ownEntry, DateTime periodStartUtc)
        {
            Rows = rows ?? new List<LeaderboardRow>();
            TotalCount = totalCount;
            NextOffset = nextOffset;
            OwnEntry = ownEntry;
            PeriodStartUtc = periodStartUtc;
        }
    }

    public interface ILeaderboard
    {
        // Server-ready shape: the local source answers at once. requestId / stale answer handling belongs to the UI.
        void Query(LeaderboardQuery query, Action<LeaderboardPage> onDone, Action<string> onError);

        // Result screen cards (AllTime, Global): the player's rank and the total in this event; false when they have no record.
        bool TryGetStanding(RaceEventId eventId, out int rank, out int total);

        // Today's (UTC) best of the event, a bot or the player; false when there is no data.
        bool TryGetBestOfDay(RaceEventId eventId, out LeaderboardRow best);

        // Raised when the player set a new day best.
        event Action Changed;
    }
}
