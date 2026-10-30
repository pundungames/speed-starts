namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// Every fixed sentence of the daily gift popup and the 7-day panel (the art has no baked text). Plain ASCII: the legacy
    /// UI font must have every glyph. No string table asset exists yet; a localization step replaces this class.
    /// </summary>
    public static class RewardTexts
    {
        public const string DailyGiftTitle = "DAILY GIFT";
        public const string EntryTicket = "ENTRY TICKET";
        public const string TicketsFull = "TICKETS FULL";
        public const string Claim = "CLAIM";
        public const string Ok = "OK";

        public const string SevenDayTitle = "7 DAY REWARDS";
        public const string Available = "AVAILABLE";
        public const string Claimed = "CLAIMED";

        /// <summary>"+1": what the daily gift gives.</summary>
        public static string Quantity(int amount) => $"+{amount}";

        /// <summary>"Tickets: 1 / 3": Entry Tickets held over the cap.</summary>
        public static string Capacity(int count, int cap) => $"Tickets: {count} / {cap}";

        /// <summary>"DAY 3".</summary>
        public static string DayLabel(int dayNumber) => $"DAY {dayNumber}";

        /// <summary>"x1": Timed Tickets a day gives.</summary>
        public static string TimedTicketAmount(int amount) => $"x{amount}";

        /// <summary>"100": Cash a day gives.</summary>
        public static string CashAmount(int amount) => amount.ToString();
    }
}
