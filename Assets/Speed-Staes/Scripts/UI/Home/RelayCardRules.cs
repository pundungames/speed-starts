namespace SpeedStars.UI.Home
{
    /// <summary>What the Home character card has to do about the relay team.</summary>
    public enum RelayCardAction
    {
        /// <summary>Nothing changes: still no relay, or still a relay (the leg's preset is re-applied, which is idempotent).</summary>
        None,

        /// <summary>A relay event was selected: remember the player's own preset and wear the selected leg's.</summary>
        EnterRelay,

        /// <summary>The relay event was left: back to the preset worn before it.</summary>
        LeaveRelay
    }

    /// <summary>
    /// Pure rule of the Home relay row (leg label and arrows): when the card turns into the team card and when it gives the player's
    /// own preset back. Race of the Day never counts as a relay, so the two card modes cannot overlap.
    /// </summary>
    public static class RelayCardRules
    {
        /// <param name="relaySelected">A relay event is selected (and it is not the Race of the Day).</param>
        /// <param name="relayShown">The card is already in team mode.</param>
        public static RelayCardAction Decide(bool relaySelected, bool relayShown)
        {
            if (relaySelected) return relayShown ? RelayCardAction.None : RelayCardAction.EnterRelay;
            return relayShown ? RelayCardAction.LeaveRelay : RelayCardAction.None;
        }

        /// <summary>The card shows the team only for a relay event that is not the day's race, with a team of at least two legs.</summary>
        public static bool ShowsTeam(bool isRelayEvent, bool isRaceOfTheDay, int teamLegCount) =>
            isRelayEvent && !isRaceOfTheDay && teamLegCount > 1;
    }
}
