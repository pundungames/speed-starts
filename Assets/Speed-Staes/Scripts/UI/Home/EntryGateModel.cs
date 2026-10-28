using System;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Everything the entry gate needs to know to decide what it shows: the ticket inventory, the Timed Ticket stock and
    /// window, and the ways to get more tickets. Plain values; the view fills it from the services.
    /// </summary>
    public readonly struct EntryGateState
    {
        public readonly int TicketCount;
        public readonly int TicketCap;
        public readonly bool TimedAccessActive;
        public readonly int TimedStock;
        public readonly TimeSpan TimedRemaining;
        public readonly int CashPrice;
        public readonly int Cash;
        public readonly bool AdAvailable;

        public EntryGateState(int ticketCount, int ticketCap, bool timedAccessActive, int timedStock, TimeSpan timedRemaining,
            int cashPrice, int cash, bool adAvailable)
        {
            TicketCount = ticketCount;
            TicketCap = ticketCap;
            TimedAccessActive = timedAccessActive;
            TimedStock = timedStock;
            TimedRemaining = timedRemaining;
            CashPrice = cashPrice;
            Cash = cash;
            AdAvailable = adAvailable;
        }
    }

    /// <summary>What the entry gate modal shows for one moment of ticket state (PDF 1.1/1.2). Pure values; the view only writes them.</summary>
    public readonly struct EntryGateModel
    {
        public readonly string TicketCountText;
        public readonly string RemainingText;
        public readonly bool ShowUseTicketButton;
        public readonly bool UseTicketInteractable;
        public readonly bool TimedAccessActive;
        public readonly string TimedStatusText;

        /// <summary>Timed Ticket value: the countdown while the window runs, the stock otherwise.</summary>
        public readonly string TimedValueText;

        /// <summary>"Use Timed Ticket" is offered: a ticket in stock and no window running (time does not stack).</summary>
        public readonly bool ShowUseTimedButton;

        /// <summary>The Entry Ticket inventory is at its cap: getting more is closed.</summary>
        public readonly bool TicketsFull;

        /// <summary>Caption of the "get tickets" group: says so when the inventory is full.</summary>
        public readonly string OffersCaption;

        public readonly bool BuyWithCashInteractable;
        public readonly string CashPriceText;
        public readonly bool WatchAdInteractable;

        private EntryGateModel(string countText, string remainingText, bool showUse, bool useInteractable, bool timedActive, string timedText,
            string timedValue, bool showTimed, bool full, string offersCaption, bool cashInteractable, string cashPriceText, bool adInteractable)
        {
            TicketCountText = countText;
            RemainingText = remainingText;
            ShowUseTicketButton = showUse;
            UseTicketInteractable = useInteractable;
            TimedAccessActive = timedActive;
            TimedStatusText = timedText;
            TimedValueText = timedValue;
            ShowUseTimedButton = showTimed;
            TicketsFull = full;
            OffersCaption = offersCaption;
            BuyWithCashInteractable = cashInteractable;
            CashPriceText = cashPriceText;
            WatchAdInteractable = adInteractable;
        }

        /// <summary>The ticket part only (no Timed stock, no ways to get tickets): everything else stays closed.</summary>
        public static EntryGateModel Build(int ticketCount, int ticketCap, bool timedAccessActive)
        {
            return Build(new EntryGateState(ticketCount, ticketCap, timedAccessActive, 0, TimeSpan.Zero, 0, 0, false));
        }

        public static EntryGateModel Build(EntryGateState state)
        {
            bool full = state.TicketCap > 0 && state.TicketCount >= state.TicketCap;
            bool priced = state.CashPrice > 0;
            bool canPay = priced && state.Cash >= state.CashPrice;

            return new EntryGateModel(
                HomeTexts.TicketCount(state.TicketCount, state.TicketCap),
                HomeTexts.TicketsLeft(state.TicketCount),
                showUse: !state.TimedAccessActive,
                useInteractable: state.TicketCount > 0,
                timedActive: state.TimedAccessActive,
                timedText: state.TimedAccessActive ? HomeTexts.TimedTicketActive : HomeTexts.TimedTicketLabel,
                timedValue: state.TimedAccessActive ? CountdownFormatter.Format(state.TimedRemaining) : state.TimedStock.ToString(),
                showTimed: !state.TimedAccessActive && state.TimedStock > 0,
                full: full,
                offersCaption: full ? HomeTexts.EntryTicketsFull : HomeTexts.GetTickets,
                cashInteractable: !full && canPay,
                cashPriceText: priced ? state.CashPrice.ToString() : string.Empty,
                adInteractable: !full && state.AdAvailable);
        }
    }
}
