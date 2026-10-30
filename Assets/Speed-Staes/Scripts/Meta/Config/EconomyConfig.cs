using System;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Ticket and wallet numbers (PDF §1.1, §1.2, §2.1). Every value lives in the asset and belongs to the user; code
    /// defaults are 0 on purpose so a value nobody entered reads as "off", never as a made-up number. A ticket option
    /// without a price is closed: the PDF prices nothing (MD §18.4), so the UI shows those options greyed out.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Economy", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Header("Entry Ticket (Adet Bileti)")]
        [Tooltip("Most Entry Tickets the player can hold. At the cap regen stops, the daily gift gives nothing and purchases are greyed out.")]
        [SerializeField, Min(0)] private int entryTicketCap;

        [Tooltip("Real-time minutes for one Entry Ticket to come back while below the cap.")]
        [SerializeField, Min(0f)] private float entryTicketRegenMinutes;

        [Tooltip("Entry Tickets given by the first login of the day (Giris Hediyesi), only while below the cap.")]
        [SerializeField, Min(0)] private int dailyGiftEntryTickets;

        [Header("Timed Ticket (Sureli Bilet)")]
        [Tooltip("Real-time minutes of unlimited ticketed entry after one Timed Ticket is used (runs while the game is closed too).")]
        [SerializeField, Min(0f)] private float timedAccessMinutes;

        [Header("Starting balances (new save only)")]
        [Tooltip("Cash in a brand-new save.")]
        [SerializeField, Min(0)] private int startingCash;

        [Tooltip("Purple Gems in a brand-new save.")]
        [SerializeField, Min(0)] private int startingPurpleGems;

        [Tooltip("Entry Tickets in a brand-new save.")]
        [SerializeField, Min(0)] private int startingEntryTickets;

        [Tooltip("Timed Tickets (unused stock) in a brand-new save.")]
        [SerializeField, Min(0)] private int startingTimedTickets;

        [Header("Ticket purchase with Cash (0 = no price = option closed)")]
        [Tooltip("Cash price of one Entry Ticket. 0 = no price: the option is shown greyed out.")]
        [SerializeField, Min(0)] private int entryTicketCashPrice;

        [Tooltip("Cash price of one Timed Ticket. 0 = no price: the option is shown greyed out.")]
        [SerializeField, Min(0)] private int timedTicketCashPrice;

        [Header("Entry Ticket from a rewarded ad")]
        [Tooltip("Offer Entry Tickets for watching a rewarded ad.")]
        [SerializeField] private bool rewardedAdEnabled;

        [Tooltip("Entry Tickets one rewarded ad gives. 0 = option closed even when enabled.")]
        [SerializeField, Min(0)] private int rewardedAdEntryTickets;

        [Header("Transaction log")]
        [Tooltip("How many processed transaction ids the save remembers (duplicate protection). The oldest are dropped first. Technical limit, not balance; keep it well above the number of purchases a player can repeat within one session.")]
        [SerializeField, Min(1)] private int processedTransactionCap = 200;

        public int EntryTicketCap => entryTicketCap;
        public TimeSpan EntryTicketRegenInterval => TimeSpan.FromMinutes(entryTicketRegenMinutes);
        public int DailyGiftEntryTickets => dailyGiftEntryTickets;
        public TimeSpan TimedAccessDuration => TimeSpan.FromMinutes(timedAccessMinutes);

        public int StartingCash => startingCash;
        public int StartingPurpleGems => startingPurpleGems;
        public int StartingEntryTickets => startingEntryTickets;
        public int StartingTimedTickets => startingTimedTickets;

        public int ProcessedTransactionCap => processedTransactionCap < 1 ? 1 : processedTransactionCap;

        /// <summary>False when the Entry Ticket has no Cash price: buying it with Cash is closed.</summary>
        public bool TryGetEntryTicketCashPrice(out int cashPrice) => TryGetPrice(entryTicketCashPrice, out cashPrice);

        /// <summary>False when the Timed Ticket has no Cash price: buying it with Cash is closed.</summary>
        public bool TryGetTimedTicketCashPrice(out int cashPrice) => TryGetPrice(timedTicketCashPrice, out cashPrice);

        /// <summary>False unless the rewarded ad is enabled and gives at least one Entry Ticket.</summary>
        public bool TryGetRewardedAdEntryTickets(out int entryTickets)
        {
            entryTickets = rewardedAdEnabled && rewardedAdEntryTickets > 0 ? rewardedAdEntryTickets : 0;
            return entryTickets > 0;
        }

        private static bool TryGetPrice(int price, out int result)
        {
            result = price > 0 ? price : 0;
            return result > 0;
        }
    }
}
