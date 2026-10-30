using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Leaderboard
{
    /// <summary>
    /// Leaderboard answered on the device: deterministic bot results plus the player's own day bests. Follows the same
    /// behaviour contract a server would (UTC windows, one row per player per window, ties by older date then id, ranks from
    /// the source), so only this source changes when a server arrives. Everything is computed per call from the clock,
    /// the saved day bests and the settings; nothing is cached except the bot table of each event.
    /// </summary>
    public sealed class LocalLeaderboard : ILeaderboard, IDisposable
    {
        private const string LocalPrefix = "local:";

        private readonly LeaderboardConfig _config;
        private readonly RaceEventCatalog _catalog;
        private readonly IDayBestSource _dayBests;
        private readonly IPlayerSettings _settings;
        private readonly IClock _clock;
        private readonly Dictionary<string, List<BotResult>> _botCache = new Dictionary<string, List<BotResult>>();

        public LocalLeaderboard(LeaderboardConfig config, RaceEventCatalog catalog, IDayBestSource dayBests, IPlayerSettings settings, IClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _dayBests = dayBests ?? throw new ArgumentNullException(nameof(dayBests));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _dayBests.DayBestChanged += OnDayBestChanged;
        }

        public event Action Changed;

        public void Dispose() => _dayBests.DayBestChanged -= OnDayBestChanged;

        public void Query(LeaderboardQuery query, Action<LeaderboardPage> onDone, Action<string> onError)
        {
            if (onDone == null) throw new ArgumentNullException(nameof(onDone));
            if (query.EventId.IsEmpty)
            {
                onError?.Invoke("No event given.");
                return;
            }

            LeaderboardPage page;
            try
            {
                page = BuildPage(query);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
                return;
            }
            onDone(page);
        }

        public bool TryGetStanding(RaceEventId eventId, out int rank, out int total)
        {
            rank = 0;
            total = 0;
            if (eventId.IsEmpty) return false;

            DateTime now = _clock.UtcNow;
            List<Entry> ranked = Rank(eventId, LeaderboardPeriod.AllTime, null, now, out _);
            total = ranked.Count;
            int index = IndexOfLocal(ranked);
            if (index < 0) return false;
            rank = index + 1;
            return true;
        }

        public bool TryGetBestOfDay(RaceEventId eventId, out LeaderboardRow best)
        {
            best = null;
            if (eventId.IsEmpty) return false;

            List<Entry> ranked = Rank(eventId, LeaderboardPeriod.Today, null, _clock.UtcNow, out _);
            if (ranked.Count == 0) return false;
            best = ToRow(ranked[0], 1);
            return true;
        }

        private LeaderboardPage BuildPage(LeaderboardQuery query)
        {
            string country = NormalizeCountry(query.CountryCode);
            List<Entry> ranked = Rank(query.EventId, query.Period, country, _clock.UtcNow, out DateTime periodStart);

            int offset = Math.Max(0, query.Offset);
            int limit = Math.Min(Math.Max(1, query.Limit), _config.MaxPageSize);
            var rows = new List<LeaderboardRow>();
            for (int i = offset; i < ranked.Count && rows.Count < limit; i++)
                rows.Add(ToRow(ranked[i], i + 1));

            int next = offset + rows.Count < ranked.Count && rows.Count > 0 ? offset + rows.Count : -1;
            int own = IndexOfLocal(ranked);
            LeaderboardRow ownRow = own >= 0 ? ToRow(ranked[own], own + 1) : null;
            return new LeaderboardPage(rows, ranked.Count, next, ownRow, periodStart);
        }

        /// <summary>The filtered ranking, best first. One entry per player: each bot has a single result, the player's is their best in the window.</summary>
        private List<Entry> Rank(RaceEventId eventId, LeaderboardPeriod period, string country, DateTime now, out DateTime periodStart)
        {
            periodStart = PeriodStart(period, now);
            long startTicks = period == LeaderboardPeriod.AllTime ? 0L : periodStart.Ticks;
            var entries = new List<Entry>();

            foreach (BotResult bot in BotsOf(eventId))
            {
                long achieved = now.Ticks - bot.AgeSeconds * TimeSpan.TicksPerSecond;
                if (achieved < startTicks) continue;
                if (country != null && !SameCountry(bot.CountryCode, country)) continue;
                entries.Add(new Entry(bot.EntryId, bot.Name, bot.CountryCode, bot.ElapsedMilliseconds, achieved, false));
            }

            if (TryGetOwnBest(eventId, startTicks, out DayBestRecord own))
            {
                string flag = _settings.FlagCode ?? string.Empty;
                if (country == null || SameCountry(flag, country))
                    entries.Add(new Entry(LocalPrefix + own.AchievedUtcTicks, _settings.PlayerName, flag, own.ElapsedMilliseconds, own.AchievedUtcTicks, true));
            }

            entries.Sort(Compare);
            return entries;
        }

        private bool TryGetOwnBest(RaceEventId eventId, long startTicks, out DayBestRecord best)
        {
            var list = new List<DayBestRecord>();
            _dayBests.CopyDayBests(eventId, list);
            best = default;
            bool found = false;
            foreach (DayBestRecord record in list)
            {
                if (record.AchievedUtcTicks < startTicks) continue;
                if (found && !(record.ElapsedMilliseconds < best.ElapsedMilliseconds
                               || (record.ElapsedMilliseconds == best.ElapsedMilliseconds && record.AchievedUtcTicks < best.AchievedUtcTicks)))
                    continue;
                best = record;
                found = true;
            }
            return found;
        }

        private List<BotResult> BotsOf(RaceEventId eventId)
        {
            if (_botCache.TryGetValue(eventId.Value, out List<BotResult> cached)) return cached;
            float distance = _catalog.TryGet(eventId, out RaceEventDefinition definition) ? definition.DistanceMeters : 0f;
            List<BotResult> bots = BotField.Generate(_config, eventId, distance);
            _botCache[eventId.Value] = bots;
            return bots;
        }

        /// <summary>UTC start of the window: month on the 1st, week on Monday, today at 00:00. AllTime = <c>DateTime.MinValue</c>.</summary>
        public static DateTime PeriodStart(LeaderboardPeriod period, DateTime utcNow)
        {
            DateTime day = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            switch (period)
            {
                case LeaderboardPeriod.Today:
                    return day;
                case LeaderboardPeriod.ThisWeek:
                    int sinceMonday = ((int)day.DayOfWeek + 6) % 7;
                    return day.AddDays(-sinceMonday);
                case LeaderboardPeriod.ThisMonth:
                    return new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                default:
                    return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
            }
        }

        private static int Compare(Entry a, Entry b)
        {
            int byTime = a.Milliseconds.CompareTo(b.Milliseconds);
            if (byTime != 0) return byTime;
            int byDate = a.AchievedTicks.CompareTo(b.AchievedTicks);
            if (byDate != 0) return byDate;
            return string.CompareOrdinal(a.Id, b.Id);
        }

        private static int IndexOfLocal(List<Entry> ranked)
        {
            for (int i = 0; i < ranked.Count; i++)
                if (ranked[i].IsLocal) return i;
            return -1;
        }

        private static LeaderboardRow ToRow(Entry e, int rank) =>
            new LeaderboardRow(e.Id, rank, e.Name, e.Country, e.Milliseconds, new DateTime(e.AchievedTicks, DateTimeKind.Utc), e.IsLocal);

        /// <summary>Null/empty = Global (null). UK is the same country as GB.</summary>
        private static string NormalizeCountry(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            string upper = code.Trim().ToUpperInvariant();
            return upper == "UK" ? "GB" : upper;
        }

        private static bool SameCountry(string a, string normalizedB) => string.Equals(NormalizeCountry(a), normalizedB, StringComparison.Ordinal);

        private void OnDayBestChanged() => Changed?.Invoke();

        private readonly struct Entry
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Country;
            public readonly long Milliseconds;
            public readonly long AchievedTicks;
            public readonly bool IsLocal;

            public Entry(string id, string name, string country, long milliseconds, long achievedTicks, bool isLocal)
            {
                Id = id;
                Name = name;
                Country = country;
                Milliseconds = milliseconds;
                AchievedTicks = achievedTicks;
                IsLocal = isLocal;
            }
        }
    }
}
