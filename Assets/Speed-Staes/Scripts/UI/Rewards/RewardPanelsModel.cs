using System.Collections.Generic;
using System.Text;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Rewards
{
    /// <summary>Which picture a reward card shows (the art has the cyan Cash diamond and the gold Timed Ticket).</summary>
    public enum RewardIcon
    {
        /// <summary>No picture of its own: the card shows its amount only.</summary>
        None,
        Diamond,
        TimedTicket,
        /// <summary>A wearable: its own icon (the wardrobe picture of the same cosmetic id).</summary>
        Cosmetic
    }

    /// <summary>What the daily gift popup does with the answer of a Claim.</summary>
    public enum DailyClaimOutcome
    {
        /// <summary>A ticket was added: close the popup, then fly one ticket to the resource bar.</summary>
        Fly,
        /// <summary>The inventory filled up meanwhile: switch to the TICKETS FULL view (OK), no reward.</summary>
        ShowFull,
        /// <summary>Nothing to claim any more (already taken, not due): close without reward.</summary>
        Close
    }

    /// <summary>The texts and button of the daily gift popup for one state. One geometry for every state (the art is one canvas).</summary>
    public readonly struct DailyGiftLayout
    {
        public DailyGiftLayout(bool showQuantity, string quantity, string state, string capacity, string button, bool buttonIsOk, bool buttonInteractable)
        {
            ShowQuantity = showQuantity;
            Quantity = quantity;
            State = state;
            Capacity = capacity;
            Button = button;
            ButtonIsOk = buttonIsOk;
            ButtonInteractable = buttonInteractable;
        }

        /// <summary>"+1" next to the ticket: only while there is something to claim.</summary>
        public bool ShowQuantity { get; }
        public string Quantity { get; }

        /// <summary>"ENTRY TICKET" or "TICKETS FULL".</summary>
        public string State { get; }

        /// <summary>"Tickets: 1 / 3".</summary>
        public string Capacity { get; }

        /// <summary>"CLAIM" or "OK".</summary>
        public string Button { get; }

        /// <summary>OK art and colours instead of the red CLAIM.</summary>
        public bool ButtonIsOk { get; }
        public bool ButtonInteractable { get; }
    }

    /// <summary>What one 7-day card shows: layers on or off, texts. Nothing here knows a Unity object.</summary>
    public readonly struct DayCardLayout
    {
        public DayCardLayout(string dayText, SevenDayCardState state, RewardIcon icon, string amountText)
        {
            DayText = dayText;
            State = state;
            Icon = icon;
            AmountText = amountText;
        }

        public string DayText { get; }
        public SevenDayCardState State { get; }
        public RewardIcon Icon { get; }

        /// <summary>"100", "x1"; empty for a cosmetic (its caption is a separate text).</summary>
        public string AmountText { get; }

        public bool ShowAvailableLook => State == SevenDayCardState.Available;
        public bool ShowClaimedLook => State == SevenDayCardState.Claimed;
        public bool ShowLockedLook => State == SevenDayCardState.Locked;

        /// <summary>Lock icon at the card's top-right corner.</summary>
        public bool ShowLock => State == SevenDayCardState.Locked;

        /// <summary>Tick and the dark CLAIMED footer.</summary>
        public bool ShowClaimedFooter => State == SevenDayCardState.Claimed;

        /// <summary>Red AVAILABLE footer.</summary>
        public bool ShowAvailableFooter => State == SevenDayCardState.Available;
    }

    /// <summary>
    /// The decisions of the daily gift popup, the 7-day panel and Home's red dot as pure functions: what each state shows,
    /// which buttons work, when the gift opens by itself. The views only draw what this returns. No Unity types; the
    /// amounts and counts come from the services (nothing here is a balance number).
    /// </summary>
    public static class RewardPanelsModel
    {
        // ---------------------------------------------------------------- daily gift

        /// <summary>
        /// Layout of the popup. Available: icon, "+amount", ENTRY TICKET, the held count, CLAIM. Full: no "+amount",
        /// TICKETS FULL and OK. Not due: the claim view with a dead button (nothing to take).
        /// </summary>
        public static DailyGiftLayout Daily(DailyGiftState state, int amount, int count, int cap)
        {
            string capacity = RewardTexts.Capacity(count, cap);
            if (state == DailyGiftState.Full)
                return new DailyGiftLayout(false, string.Empty, RewardTexts.TicketsFull, capacity, RewardTexts.Ok, buttonIsOk: true, buttonInteractable: true);

            return new DailyGiftLayout(true, RewardTexts.Quantity(amount), RewardTexts.EntryTicket, capacity, RewardTexts.Claim,
                buttonIsOk: false, buttonInteractable: state == DailyGiftState.Available);
        }

        /// <summary>Only a granted ticket flies; a Full answer turns the popup into the OK view; the rest just closes it.</summary>
        public static DailyClaimOutcome OutcomeOf(DailyGiftResult result)
        {
            switch (result)
            {
                case DailyGiftResult.Granted: return DailyClaimOutcome.Fly;
                case DailyGiftResult.Full: return DailyClaimOutcome.ShowFull;
                default: return DailyClaimOutcome.Close;
            }
        }

        /// <summary>
        /// The gift opens by itself when Home opens and there is something to show (a claimable ticket or the full notice).
        /// Never over another popup and never while another screen is on top: two modals do not stack.
        /// </summary>
        public static bool ShouldAutoOpenDailyGift(DailyGiftState state, bool homeIsTopScreen, bool anotherPopupOpen)
        {
            if (!homeIsTopScreen || anotherPopupOpen) return false;
            return state == DailyGiftState.Available || state == DailyGiftState.Full;
        }

        // ---------------------------------------------------------------- 7 day panel

        public static DayCardLayout DayCard(int dayNumber, SevenDayCardState state, RewardIcon icon, int amount)
        {
            string amountText;
            switch (icon)
            {
                case RewardIcon.Diamond: amountText = RewardTexts.CashAmount(amount); break;
                case RewardIcon.TimedTicket: amountText = RewardTexts.TimedTicketAmount(amount); break;
                default: amountText = string.Empty; break;
            }
            return new DayCardLayout(RewardTexts.DayLabel(dayNumber), state, icon, amountText);
        }

        /// <summary>
        /// Picture of a reward from the NAME of the contract's reward kind (the kind enum is the service's; matching by name
        /// keeps this model free of it). Cash is the cyan diamond; a kind without art shows its amount only.
        /// </summary>
        public static RewardIcon IconOf(string kindName)
        {
            switch (kindName)
            {
                case "Cash": return RewardIcon.Diamond;
                case "TimedTicket": return RewardIcon.TimedTicket;
                case "Cosmetic": return RewardIcon.Cosmetic;
                default: return RewardIcon.None;
            }
        }

        /// <summary>Index of the card that can be claimed now; -1 when none (CLAIM stays dead).</summary>
        public static int AvailableIndex(IReadOnlyList<SevenDayCardState> states)
        {
            if (states == null) return -1;
            for (int i = 0; i < states.Count; i++)
                if (states[i] == SevenDayCardState.Available) return i;
            return -1;
        }

        /// <summary>CLAIM works only while a card is available and no claim is running.</summary>
        public static bool SevenDayClaimEnabled(IReadOnlyList<SevenDayCardState> states, bool claimRunning)
        {
            return !claimRunning && AvailableIndex(states) >= 0;
        }

        /// <summary>
        /// Caption of the cosmetic card from the item's own record: a premium item "PH02" in Headwear reads "PREMIUM 02 HEADWEAR".
        /// Letters of the part name are dropped, its number is kept; an item without a number is just its category.
        /// </summary>
        public static string CosmeticCaption(string itemName, string categoryName, bool isPremium)
        {
            var digits = new StringBuilder();
            foreach (char c in itemName ?? string.Empty)
                if (char.IsDigit(c)) digits.Append(c);

            var caption = new StringBuilder();
            if (isPremium) caption.Append("PREMIUM ");
            if (digits.Length > 0) caption.Append(digits).Append(' ');
            caption.Append(categoryName ?? string.Empty);
            return caption.ToString().Trim().ToUpperInvariant();
        }
    }
}
