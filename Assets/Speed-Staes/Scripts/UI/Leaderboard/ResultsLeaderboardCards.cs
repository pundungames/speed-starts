using System;
using System.Globalization;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Race;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>The three leaderboard cards of the Results screen as texts.</summary>
    public readonly struct ResultsCardTexts
    {
        public readonly string WorldPosition;
        public readonly string Percentile;
        public readonly string BestOfTheDay;

        public ResultsCardTexts(string worldPosition, string percentile, string bestOfTheDay)
        {
            WorldPosition = worldPosition;
            Percentile = percentile;
            BestOfTheDay = bestOfTheDay;
        }
    }

    /// <summary>
    /// WORLD POSITION / PERCENTILE / BEST OF THE DAY from the leaderboard (All Time, Global). No data = the placeholders the
    /// screen showed before the leaderboard existed ("--", "No races today"); nothing is made up.
    /// </summary>
    public static class ResultsLeaderboardCards
    {
        public static ResultsCardTexts Build(ILeaderboard leaderboard, RaceEventId eventId)
        {
            string world = ResultsTexts.NoValue;
            string percentile = ResultsTexts.NoValue;
            string best = ResultsTexts.NoDailyBest;
            if (leaderboard == null || eventId.IsEmpty) return new ResultsCardTexts(world, percentile, best);

            if (leaderboard.TryGetStanding(eventId, out int rank, out int total) && rank >= 1 && total >= 1)
            {
                world = Position(rank);
                percentile = Percentile(rank, total);
            }
            if (leaderboard.TryGetBestOfDay(eventId, out LeaderboardRow row) && row != null)
                best = BestOfDay(row.DisplayName, row.ElapsedMilliseconds);
            return new ResultsCardTexts(world, percentile, best);
        }

        /// <summary>"#12".</summary>
        public static string Position(int rank) => "#" + Math.Max(1, rank).ToString(CultureInfo.InvariantCulture);

        /// <summary>"TOP 8%": rank over total rounded up, never below 1 or above 100.</summary>
        public static string Percentile(int rank, int total)
        {
            if (total < 1) return ResultsTexts.NoValue;
            int clampedRank = Math.Min(Math.Max(1, rank), total);
            int percent = (int)Math.Ceiling(clampedRank * 100d / total - 1e-9);
            percent = Math.Min(100, Math.Max(1, percent));
            return "TOP " + percent.ToString(CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>"Deniz  10.42s" (the Results screen writes times with the unit glued to the number).</summary>
        public static string BestOfDay(string name, long milliseconds)
        {
            string time = ResultsTexts.Time(Math.Max(0L, milliseconds) / 1000d);
            return string.IsNullOrWhiteSpace(name) ? time : name.Trim() + "  " + time;
        }
    }
}
