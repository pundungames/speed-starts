namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Every fixed sentence the Home views write, in one place (no string table asset exists yet; a later localization
    /// step replaces this class). Plain ASCII: the legacy UI font must have every glyph.
    /// </summary>
    public static class HomeTexts
    {
        public const string UseEntryTicket = "Use Entry Ticket";
        public const string TimedTicketActive = "Timed Ticket active";
        public const string TimedTicketLabel = "Timed Ticket";
        public const string NoTicketWarning = "No Entry Ticket left";
        public const string NotPlayableWarning = "Coming soon";
        public const string UnknownEventWarning = "Event not found";
        public const string BuyWithCash = "Cash";
        public const string WatchAd = "Watch ad";
        public const string UseTimedTicket = "Use Timed Ticket";
        public const string EntryTicketsFull = "Entry Tickets Full";
        public const string GetTickets = "Get tickets";
        public const string DefaultRacerName = "Custom 1";
        public const string RacerHeight = "Height: 5'9\"";

        /// <summary>"2/3": amount held over the cap (resource bar and entry gate).</summary>
        public static string TicketCount(int count, int cap) => $"{count}/{cap}";

        public static string TicketsLeft(int count) => $"Left: {count}";

        /// <summary>"+3": Purple Gems won in a race (results screen).</summary>
        public static string GemReward(int gems) => $"+{gems}";
    }
}
