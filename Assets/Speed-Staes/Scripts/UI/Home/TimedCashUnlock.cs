using System;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Home
{
    /// <summary>How one "Spend Cash - unlock for the Timed Ticket window" tap ended.</summary>
    public enum TimedCashUnlockResult
    {
        /// <summary>The Cash was spent and the Timed Ticket window is running.</summary>
        Unlocked,
        /// <summary>The Cash was spent, the window did not start (the Timed Ticket stays in stock; the gate offers "Use Timed Ticket").</summary>
        BoughtNotStarted,
        NotEnough,
        NotForSale,
        /// <summary>A window already runs: nothing was spent (time does not stack).</summary>
        AlreadyActive,
        /// <summary>No shop or access service is bound (build before its WIRING): nothing happens.</summary>
        Unavailable
    }

    /// <summary>
    /// The one-tap Cash unlock of a premium event (entry gate): buy one Timed Ticket with Cash (<see cref="ITimedTicketShop"/>), then start
    /// its window (<see cref="ITimedAccess"/>) in the same tap. The prices and the window length live in the economy config and the
    /// services; nothing here knows a number. A window that already runs is never paid for.
    /// </summary>
    public sealed class TimedCashUnlock
    {
        private readonly ITimedTicketShop _shop;
        private readonly ITimedAccess _access;
        private readonly Func<bool> _windowRunning;
        private readonly Func<string> _newTransactionId;

        /// <param name="windowRunning">True while a Timed Ticket window runs (checked before any Cash is spent).</param>
        /// <param name="newTransactionId">One fresh id per tap; defaults to a new GUID.</param>
        public TimedCashUnlock(ITimedTicketShop shop, ITimedAccess access, Func<bool> windowRunning, Func<string> newTransactionId = null)
        {
            _shop = shop;
            _access = access;
            _windowRunning = windowRunning ?? throw new ArgumentNullException(nameof(windowRunning));
            _newTransactionId = newTransactionId ?? (() => Guid.NewGuid().ToString("N"));
        }

        public TimedCashUnlockResult TryUnlock()
        {
            if (_shop == null || _access == null) return TimedCashUnlockResult.Unavailable;
            if (_windowRunning()) return TimedCashUnlockResult.AlreadyActive;

            switch (_shop.TryBuyWithCash(_newTransactionId()))
            {
                case TimedTicketPurchaseResult.Bought:
                    break;
                case TimedTicketPurchaseResult.NotEnough:
                    return TimedCashUnlockResult.NotEnough;
                case TimedTicketPurchaseResult.NotForSale:
                    return TimedCashUnlockResult.NotForSale;
                default:
                    return TimedCashUnlockResult.NotForSale; // AlreadyProcessed cannot happen with a fresh id; nothing was charged here
            }

            TimedAccessResult started = _access.TryActivate();
            return started == TimedAccessResult.Activated || started == TimedAccessResult.AlreadyActive
                ? TimedCashUnlockResult.Unlocked
                : TimedCashUnlockResult.BoughtNotStarted;
        }
    }
}
