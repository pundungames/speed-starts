using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpeedStars.UI.Race
{
    /// <summary>Cup shown on a result row: first three places only.</summary>
    public enum ResultTrophy
    {
        None,
        Gold,
        Silver,
        Bronze
    }

    /// <summary>One line of the results table, ready to draw.</summary>
    public readonly struct ResultRowData
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly string TimeText;
        public readonly bool IsPlayer;
        public readonly ResultTrophy Trophy;

        /// <summary>PB: this race is a new personal record (player row only).</summary>
        public readonly bool ShowPersonalBest;

        /// <summary>DB: this race is the player's best of today, UTC (player row only).</summary>
        public readonly bool ShowDayBest;

        /// <summary>ISO 3166-1 alpha-2 of the runner's flag; empty = no flag (the rows of the single-runner table have none).</summary>
        public readonly string CountryCode;

        public ResultRowData(int rank, string name, string timeText, bool isPlayer, ResultTrophy trophy,
            bool showPersonalBest = false, bool showDayBest = false, string countryCode = "")
        {
            CountryCode = countryCode ?? string.Empty;
            ShowPersonalBest = showPersonalBest;
            ShowDayBest = showDayBest;
            Rank = rank;
            Name = name ?? string.Empty;
            TimeText = timeText ?? string.Empty;
            IsPlayer = isPlayer;
            Trophy = trophy;
        }
    }

    /// <summary>Another runner of the same race (the mechanics will hand these over later; none exist today).</summary>
    public readonly struct ResultEntry
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly double TimeSeconds;

        public ResultEntry(int rank, string name, double timeSeconds)
        {
            Rank = rank;
            Name = name ?? string.Empty;
            TimeSeconds = timeSeconds;
        }
    }

    /// <summary>Everything the Results screen shows for one finished race, as plain texts and rows.</summary>
    public readonly struct ResultsData
    {
        public readonly string EventTitle;
        public readonly string CashText;
        public readonly string PersonalBestTitle;
        public readonly string PersonalBestValue;
        public readonly string PersonalBestEvent;
        public readonly bool IsNewRecord;
        public readonly string WorldPositionText;
        public readonly string PercentileText;
        public readonly string DailyBestText;
        public readonly IReadOnlyList<ResultRowData> Rows;

        public ResultsData(string eventTitle, string cashText, string personalBestTitle, string personalBestValue,
            string personalBestEvent, bool isNewRecord, string worldPositionText, string percentileText, string dailyBestText,
            IReadOnlyList<ResultRowData> rows)
        {
            EventTitle = eventTitle ?? string.Empty;
            CashText = cashText ?? string.Empty;
            PersonalBestTitle = personalBestTitle ?? string.Empty;
            PersonalBestValue = personalBestValue ?? string.Empty;
            PersonalBestEvent = personalBestEvent ?? string.Empty;
            IsNewRecord = isNewRecord;
            WorldPositionText = worldPositionText ?? string.Empty;
            PercentileText = percentileText ?? string.Empty;
            DailyBestText = dailyBestText ?? string.Empty;
            Rows = rows ?? Array.Empty<ResultRowData>();
        }
    }

    /// <summary>Fixed sentences of the Results screen (plain ASCII: the legacy UI font must have every glyph).</summary>
    public static class ResultsTexts
    {
        public const string PersonalBest = "PERSONAL BEST";
        public const string NewRecord = "NEW RECORD";
        public const string DefaultPlayerName = "YOU";

        /// <summary>No data behind a card yet (world position and percentile wait for the leaderboard; no sample numbers).</summary>
        public const string NoValue = "--";

        /// <summary>The Best of the day panel has no data source yet.</summary>
        public const string NoDailyBest = "No races today";

        /// <summary>Time cell of a runner still on the track.</summary>
        public const string NotFinished = "-";

        /// <summary>"9.58s": stopwatch time with the unit.</summary>
        public static string Time(double seconds) => RaceDisplayFormat.Time(seconds) + "s";

        /// <summary>"+3 Purple Gems".</summary>
        public static string GemReward(int gems) => "+" + gems.ToString(CultureInfo.InvariantCulture) + " Purple Gems";
    }

    /// <summary>What the flow knows about a finished race; <see cref="ResultsModel.Build"/> turns it into screen data.</summary>
    public readonly struct ResultsInput
    {
        public readonly string EventTitle;
        public readonly int PlayerRank;
        public readonly double PlayerSeconds;
        public readonly string PlayerName;
        public readonly int Cash;
        public readonly bool HasPersonalBest;
        public readonly double PersonalBestSeconds;
        public readonly bool IsNewRecord;
        public readonly IReadOnlyList<ResultEntry> Others;

        /// <summary>This race is the player's best of today (see <see cref="ResultsModel.IsDayBest"/>).</summary>
        public readonly bool IsDayBest;

        public ResultsInput(string eventTitle, int playerRank, double playerSeconds, string playerName, int cash,
            bool hasPersonalBest, double personalBestSeconds, bool isNewRecord, IReadOnlyList<ResultEntry> others,
            bool isDayBest = false)
        {
            IsDayBest = isDayBest;
            EventTitle = eventTitle ?? string.Empty;
            PlayerRank = playerRank;
            PlayerSeconds = playerSeconds;
            PlayerName = playerName ?? string.Empty;
            Cash = cash;
            HasPersonalBest = hasPersonalBest;
            PersonalBestSeconds = personalBestSeconds;
            IsNewRecord = isNewRecord;
            Others = others;
        }
    }

    /// <summary>
    /// Pure rules of the Results screen: card texts, the table rows and the cups. No Unity types, so EditMode tests cover it.
    /// Only real data is shown: the player's own race and, when the mechanics hand them over, the other runners.
    /// </summary>
    public static class ResultsModel
    {
        /// <summary>Rows the table panel has room for (the art shows six); the player's own row is always kept.</summary>
        public const int MaxRows = 6;

        public static ResultsData Build(ResultsInput input)
        {
            bool record = input.IsNewRecord && input.HasPersonalBest;
            string bestValue = input.HasPersonalBest ? ResultsTexts.Time(input.PersonalBestSeconds) : ResultsTexts.NoValue;

            return new ResultsData(
                input.EventTitle,
                Math.Max(0, input.Cash).ToString(CultureInfo.InvariantCulture),
                record ? ResultsTexts.NewRecord : ResultsTexts.PersonalBest,
                bestValue,
                input.EventTitle,
                record,
                ResultsTexts.NoValue,
                ResultsTexts.NoValue,
                ResultsTexts.NoDailyBest,
                Rows(input));
        }

        /// <summary>
        /// True when the stored best of today equals this race (same rounding the day-best store uses: whole milliseconds).
        /// A slower run after a faster one today leaves the stored value lower, so it is not a day best.
        /// </summary>
        public static bool IsDayBest(bool hasDayBest, long dayBestMilliseconds, double seconds)
        {
            if (!hasDayBest || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d) return false;
            return dayBestMilliseconds == (long)Math.Round(seconds * 1000d);
        }

        /// <summary>Cup of a place: gold, silver, bronze for 1 to 3, none after.</summary>
        public static ResultTrophy TrophyFor(int rank)
        {
            switch (rank)
            {
                case 1: return ResultTrophy.Gold;
                case 2: return ResultTrophy.Silver;
                case 3: return ResultTrophy.Bronze;
                default: return ResultTrophy.None;
            }
        }

        private static IReadOnlyList<ResultRowData> Rows(ResultsInput input)
        {
            int playerRank = input.PlayerRank < 1 ? 1 : input.PlayerRank;
            string playerName = string.IsNullOrWhiteSpace(input.PlayerName) ? ResultsTexts.DefaultPlayerName : input.PlayerName.Trim();

            var ordered = new List<ResultRowData>();
            Insert(ordered, new ResultRowData(playerRank, playerName, ResultsTexts.Time(input.PlayerSeconds), true, TrophyFor(playerRank),
                input.IsNewRecord && input.HasPersonalBest, input.IsDayBest));

            if (input.Others != null)
            {
                foreach (ResultEntry other in input.Others)
                {
                    int rank = other.Rank < 1 ? 1 : other.Rank;
                    Insert(ordered, new ResultRowData(rank, other.Name, ResultsTexts.Time(other.TimeSeconds), false, TrophyFor(rank)));
                }
            }

            // Over the room: drop the worst-ranked other runners, never the player.
            while (ordered.Count > MaxRows)
            {
                int drop = ordered.Count - 1;
                if (ordered[drop].IsPlayer) drop--;
                ordered.RemoveAt(drop);
            }
            return ordered;
        }

        /// <summary>Stable by rank: an equal rank goes after the rows already there (the player, added first, stays first).</summary>
        private static void Insert(List<ResultRowData> list, ResultRowData row)
        {
            int at = list.Count;
            while (at > 0 && list[at - 1].Rank > row.Rank) at--;
            list.Insert(at, row);
        }
    }
}
