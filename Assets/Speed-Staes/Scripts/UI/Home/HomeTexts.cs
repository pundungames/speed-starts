namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Every fixed sentence the Home views write, in one place (no string table asset exists yet; a later localization
    /// step replaces this class). Plain ASCII: the legacy UI font must have every glyph.
    /// </summary>
    public static class HomeTexts
    {
        public const string UseEntryTicket = "USE TICKET";
        public const string TimedTicketActive = "Timed Ticket active";
        public const string TimedTicketLabel = "Timed Ticket";
        public const string NoTicketWarning = "No Entry Ticket left";
        public const string NotPlayableWarning = "Coming soon";
        public const string UnknownEventWarning = "Event not found";
        public const string BuyWithCash = "BUY WITH CASH";
        public const string WatchAd = "WATCH AD";
        public const string UseTimedTicket = "USE TIMED TICKET";
        public const string EntryTicketsFull = "TICKETS FULL";
        public const string GetTickets = "GET TICKETS";
        public const string EntryGateTitle = "RACE ENTRY";
        public const string DefaultRacerName = "Custom 1";
        public const string RacerHeight = "Height: 5'9\"";

        /// <summary>"2/3": amount held over the cap (resource bar and entry gate).</summary>
        public static string TicketCount(int count, int cap) => $"{count}/{cap}";

        /// <summary>"Tickets 2/3": the big count line of the entry gate.</summary>
        public static string TicketsHeld(string countText) => $"Tickets {countText}";

        /// <summary>"Next ticket in 12:34": regen countdown line of the entry gate.</summary>
        public static string NextTicketIn(string time) => $"Next ticket in {time}";

        public static string TicketsLeft(int count) => $"Left: {count}";

        /// <summary>"+3": Purple Gems won in a race (results screen).</summary>
        public static string GemReward(int gems) => $"+{gems}";
    }
}
