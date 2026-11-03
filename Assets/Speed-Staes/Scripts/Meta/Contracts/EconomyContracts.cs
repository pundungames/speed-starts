using System;
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

    /// <summary>
    /// What a finished ticketed race paid in Cash, split into the lines the result screen shows. <see cref="Total"/> is
    /// what reached the wallet; a line may be 0 (config off, no opponents passed).
    /// </summary>
    public readonly struct RaceCashReward
    {
        public RaceCashReward(int finish, int difficulty, int opponents, int passedOpponents, int totalOpponents)
        {
            Finish = finish;
            Difficulty = difficulty;
            Opponents = opponents;
            PassedOpponents = passedOpponents;
            TotalOpponents = totalOpponents;
        }

        public static RaceCashReward None => default;

        /// <summary>Finishing the race.</summary>
        public int Finish { get; }

        /// <summary>Flat difficulty bonus.</summary>
        public int Difficulty { get; }

        /// <summary>Share for the opponents the player beat.</summary>
        public int Opponents { get; }

        /// <summary>Opponents finished behind the player.</summary>
        public int PassedOpponents { get; }

        /// <summary>All opponents of the race (the player excluded).</summary>
        public int TotalOpponents { get; }

        public int Total => Finish + Difficulty + Opponents;

        public bool IsEmpty => Total <= 0;
    }

    /// <summary>Cash for a finished race against opponents (economy v2). A run alone pays nothing.</summary>
    public interface IRaceCashRewardResolver
    {
        /// <summary>
        /// Pays once per <paramref name="payoutId"/> and returns the breakdown for the result screen. Empty for a free
        /// event (or a run alone), an unfinished or invalid outcome, an unknown event or a repeated id.
        /// </summary>
        RaceCashReward Resolve(RaceOutcome outcome, string payoutId);
    }

    public enum TimedTicketPurchaseResult
    {
        Bought,
        /// <summary>No Cash price in config (0).</summary>
        NotForSale,
        NotEnough,
        AlreadyProcessed
    }

    /// <summary>Buying one Timed Ticket (stock +1) with Cash.</summary>
    public interface ITimedTicketShop
    {
        TimedTicketPurchaseResult TryBuyWithCash(string transactionId);
    }

    public enum RewardedCashOutcome
    {
        /// <summary>The ad was completed and the Cash was paid.</summary>
        Granted,
        Skipped,
        Failed,
        /// <summary>Today's Cash ads are used up (the ad was not even shown).</summary>
        LimitReached,
        /// <summary>No ad service, not ready, or config gives no Cash.</summary>
        Unavailable
    }

    /// <summary>Watch a rewarded ad for Cash, a fixed number of times per UTC day.</summary>
    public interface IRewardedCash
    {
        /// <summary>Cash one ad gives (config).</summary>
        int Amount { get; }

        /// <summary>Ads that count per day (config).</summary>
        int DailyLimit { get; }

        /// <summary>Ads left today; 0 once used up. A rolled-back device clock never gives rights back.</summary>
        int RemainingToday { get; }

        /// <summary>An ad is running; no second one starts.</summary>
        bool IsBusy { get; }

        /// <summary>The offer is open: amount and limit configured, an ad service ready, rights left, none running.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Starts one ad. False (nothing started, <paramref name="done"/> not called) when not available.
        /// <paramref name="done"/> gets the outcome once; it may run before this call returns.
        /// </summary>
        bool TryWatch(Action<RewardedCashOutcome> done);

        /// <summary>Raised after a payment (remaining count changed) and after the day rolled over.</summary>
        event Action Changed;
    }
}
