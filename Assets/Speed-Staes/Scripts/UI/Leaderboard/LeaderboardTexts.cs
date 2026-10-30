using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>Fixed sentences of the leaderboard screen. Plain Latin-1 only: the legacy UI font must have every glyph.</summary>
    public static class LeaderboardTexts
    {
        public const string Title = "LEADERBOARD";
        public const string Global = "GLOBAL";
        public const string RegionHeader = "REGION";
        public const string PeriodHeader = "PERIOD";
        public const string EventHeader = "EVENT";
        public const string SearchPlaceholder = "Search country";
        public const string PeriodHint = "Personal best within selected period";
        public const string NoCountries = "NO COUNTRIES FOUND";
        public const string NoEvents = "NO EVENTS AVAILABLE";

        public const string ColumnRank = "RANK";
        public const string ColumnPlayer = "PLAYER";
        public const string ColumnTime = "TIME";
        public const string ColumnDate = "SET ON";

        public const string You = "YOU";
        public const string NotRanked = "NOT RANKED";
        public const string FindMe = "FIND ME";

        public const string Loading = "LOADING...";
        public const string EmptyTitle = "NO RESULTS YET";
        public const string EmptyHint = "COMPLETE A RACE TO SET A TIME";
        public const string ErrorTitle = "COULDN'T LOAD LEADERBOARD";
        public const string Retry = "RETRY";

        public const string Apply = "APPLY";
        public const string Cancel = "CANCEL";

        /// <summary>Em dash: "no value" in the YOU row (Latin-1 range neighbour, present in the built-in font).</summary>
        public const string NoValue = "\u2014";

        /// <summary>Between the category and the distance in the event filter ("SPRINTS · 100m").</summary>
        public const string EventSeparator = " \u00B7 ";

        public static string Period(LeaderboardPeriod period)
        {
            switch (period)
            {
                case LeaderboardPeriod.ThisMonth: return "THIS MONTH";
                case LeaderboardPeriod.ThisWeek: return "THIS WEEK";
                case LeaderboardPeriod.Today: return "TODAY";
                default: return "ALL TIME";
            }
        }
    }
}
