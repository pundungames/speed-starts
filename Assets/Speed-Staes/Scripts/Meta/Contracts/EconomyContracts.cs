using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>Balances the ledger moves. Serialized nowhere; safe to reorder, but append by habit.</summary>
    public enum EconomyCurrency
    {
        Cash,
        PurpleGem,
        /// <summary>Timed Ticket stock (Sureli Bilet), not the running access window.</summary>
        TimedTicket
    }

    public enum TransactionResult
    {
        /// <summary>Applied; the balance changed and the id was recorded.</summary>
        Done,
        /// <summary>The id was applied before: nothing changed.</summary>
        AlreadyProcessed,
        /// <summary>Spend only: balance below the amount; nothing changed, the id is NOT recorded.</summary>
        NotEnough,
        /// <summary>Amount below 1 or empty transaction id; nothing changed.</summary>
        InvalidAmount
    }

    /// <summary>
    /// The only way balances change (GDD §14: idempotent transactions). The same transaction id applies once, a failed
    /// transaction changes nothing. Applies in memory and tells the wallet; it does NOT save, so the caller can write
    /// the balance and what it bought in one save (a crash cannot take one without the other).
    /// </summary>
    public interface IEconomyLedger
    {
        TransactionResult TrySpend(EconomyCurrency currency, int amount, string transactionId);
        TransactionResult Grant(EconomyCurrency currency, int amount, string transactionId);
    }

    public enum TimedAccessResult
    {
        Activated,
        /// <summary>A window is running: no ticket spent, time does not stack.</summary>
        AlreadyActive,
        NoTicket,
        /// <summary>Config has no duration: nothing spent.</summary>
        NotAvailable
    }

    /// <summary>Timed Ticket (Sureli Bilet) activation. The window is a saved UTC deadline and runs while the game is closed.</summary>
    public interface ITimedAccess
    {
        TimedAccessResult TryActivate();
    }

    /// <summary>Purple Gems for a finished race (GDD §1.3, §7).</summary>
    public interface IRaceRewardResolver
    {
        /// <summary>
        /// Pays the rank reward once per <paramref name="payoutId"/> (one id per race session) and returns the gems won,
        /// for the result screen. 0 for events without gem rewards, unfinished or invalid outcomes, a repeated id.
        /// </summary>
        int Resolve(RaceOutcome outcome, string payoutId);
    }

    public enum TicketGrantSource
    {
        DailyGift,
        RewardedAd,
        CashPurchase
    }

    public enum TicketGrantResult
    {
        Granted,
        /// <summary>Entry Ticket cap reached: acquisition refused, nothing changed.</summary>
        Full,
        /// <summary>The source gives nothing (config 0 / closed).</summary>
        NotAvailable
    }

    public enum TicketPurchaseResult
    {
        Bought,
        Full,
        /// <summary>No Cash price in config (0).</summary>
        NotForSale,
        NotEnough,
        AlreadyProcessed
    }

    /// <summary>Acquisition of Entry Tickets that is closed while the inventory is full.</summary>
    public interface IEntryTicketSource
    {
        /// <summary>Grants the amount the source is configured to give (clamped to the cap). Does not save.</summary>
        TicketGrantResult TryGrant(TicketGrantSource source);
    }

    /// <summary>Buying one Entry Ticket with Cash. Nothing is charged when the inventory is full.</summary>
    public interface IEntryTicketShop
    {
        TicketPurchaseResult TryBuyWithCash(string transactionId);
    }
}
