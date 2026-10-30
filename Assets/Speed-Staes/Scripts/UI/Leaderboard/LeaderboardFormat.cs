using System;
using System.Globalization;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Race;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>One table line, ready to draw.</summary>
    public readonly struct LeaderboardRowData
    {
        public readonly string RankText;
        public readonly string Name;
        public readonly string CountryCode;
        public readonly string TimeText;
        public readonly string DateText;
        public readonly bool IsLocalPlayer;

        public LeaderboardRowData(string rankText, string name, string countryCode, string timeText, string dateText, bool isLocalPlayer)
        {
            RankText = rankText ?? string.Empty;
            Name = name ?? string.Empty;
            CountryCode = countryCode ?? string.Empty;
            TimeText = timeText ?? string.Empty;
            DateText = dateText ?? string.Empty;
            IsLocalPlayer = isLocalPlayer;
        }
    }

    /// <summary>Pure text rules of the leaderboard: time, UTC date, rows. No Unity types.</summary>
    public static class LeaderboardFormat
    {
        private static readonly string[] Months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        /// <summary>"10.42 s" from raw milliseconds: centiseconds are truncated like the race clock, the ranking itself uses the raw value.</summary>
        public static string Time(long milliseconds)
        {
            return RaceDisplayFormat.Time(Math.Max(0L, milliseconds) / 10L) + " s";
        }

        /// <summary>"28 OCT 2026": the UTC day of the stamp, English month, fixed (never the device culture).</summary>
        public static string Date(DateTime achievedAtUtc)
        {
            DateTime utc = achievedAtUtc.Kind == DateTimeKind.Local ? achievedAtUtc.ToUniversalTime() : achievedAtUtc;
            return utc.Day.ToString("00", CultureInfo.InvariantCulture) + " " + Months[utc.Month - 1] + " "
                + utc.Year.ToString("0000", CultureInfo.InvariantCulture);
        }

        public static LeaderboardRowData Row(LeaderboardRow row)
        {
            return new LeaderboardRowData(
                row.Rank.ToString(CultureInfo.InvariantCulture),
                row.DisplayName,
                row.CountryCode,
                Time(row.ElapsedMilliseconds),
                Date(row.AchievedAtUtc),
                row.IsLocalPlayer);
        }

        /// <summary>The pinned YOU line: the real rank, or "NOT RANKED" with dashes (no made-up rank).</summary>
        public static LeaderboardRowData You(LeaderboardRow own, string countryCode)
        {
            if (own == null)
                return new LeaderboardRowData(LeaderboardTexts.You, LeaderboardTexts.NotRanked, string.Empty, LeaderboardTexts.NoValue, LeaderboardTexts.NoValue, true);
            return new LeaderboardRowData(
                own.Rank.ToString(CultureInfo.InvariantCulture),
                LeaderboardTexts.You,
                own.CountryCode ?? countryCode,
                Time(own.ElapsedMilliseconds),
                Date(own.AchievedAtUtc),
                true);
        }
    }
}
