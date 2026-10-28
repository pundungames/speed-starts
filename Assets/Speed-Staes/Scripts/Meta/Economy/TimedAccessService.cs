using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Wallet;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Timed Ticket activation (GDD §6): one stock ticket opens a real-time window of ticketed entry. While a window runs a
    /// second use is refused (nothing spent, time never stacks). The deadline is a UTC tick stamp from the trusted clock,
    /// so it runs while the game is closed. A race that is already running is not affected: only entry checks the window.
    /// </summary>
    public sealed class TimedAccessService : ITimedAccess
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly EconomyConfig _config;
        private readonly IEconomyLedger _ledger;
        private readonly WalletService _wallet;

        public TimedAccessService(ProgressRepository repository, IClock clock, EconomyConfig config,
            IEconomyLedger ledger, WalletService wallet)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public TimedAccessResult TryActivate()
        {
            PlayerProgress p = _repository.Current
                ?? throw new InvalidOperationException("ProgressRepository.Load() must run before timed access is used.");
            long now = _clock.UtcNow.Ticks;
            if (p.TimedAccessExpiresUtcTicks > now) return TimedAccessResult.AlreadyActive;
            if (_config.TimedAccessDuration.Ticks <= 0) return TimedAccessResult.NotAvailable;
            if (p.TimedTickets <= 0) return TimedAccessResult.NoTicket;

            // The activation instant makes the id unique per activation (a window cannot start twice at one instant).
            TransactionResult spent = _ledger.TrySpend(EconomyCurrency.TimedTicket, 1, "timed-activate:" + now);
            if (spent != TransactionResult.Done) return TimedAccessResult.NoTicket;

            p.TimedAccessExpiresUtcTicks = now + _config.TimedAccessDuration.Ticks;
            _repository.Save();
            _wallet.NotifyChanged();
            return TimedAccessResult.Activated;
        }
    }
}
