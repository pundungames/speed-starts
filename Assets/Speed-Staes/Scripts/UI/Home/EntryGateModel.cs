namespace SpeedStars.UI.Home
{
    /// <summary>What the entry gate modal shows for one moment of ticket state (PDF 1.1/1.2). Pure values; the view only writes them.</summary>
    public readonly struct EntryGateModel
    {
        public readonly string TicketCountText;
        public readonly string RemainingText;
        public readonly bool ShowUseTicketButton;
        public readonly bool UseTicketInteractable;
        public readonly bool TimedAccessActive;
        public readonly string TimedStatusText;

        private EntryGateModel(string countText, string remainingText, bool showUse, bool useInteractable, bool timedActive, string timedText)
        {
            TicketCountText = countText;
            RemainingText = remainingText;
            ShowUseTicketButton = showUse;
            UseTicketInteractable = useInteractable;
            TimedAccessActive = timedActive;
            TimedStatusText = timedText;
        }

        public static EntryGateModel Build(int ticketCount, int ticketCap, bool timedAccessActive)
        {
            return new EntryGateModel(
                HomeTexts.TicketCount(ticketCount, ticketCap),
                HomeTexts.TicketsLeft(ticketCount),
                showUse: !timedAccessActive,
                useInteractable: ticketCount > 0,
                timedActive: timedAccessActive,
                timedText: timedAccessActive ? HomeTexts.TimedTicketActive : HomeTexts.TimedTicketLabel);
        }
    }
}
