using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Wallet
{
    /// <summary>
    /// Read side of the wallet over the save (PDF §2.2). Earning and spending arrive in Faz 3; whoever changes the
    /// balances calls <c>NotifyChanged</c> so the resource bar redraws. Timed access is active while its saved
    /// deadline is later than the clock's now.
    /// </summary>
    public sealed class WalletService : IWallet
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;

        public WalletService(ProgressRepository repository, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action Changed;

        public int Cash => Progress.Cash;
        public int PurpleGems => Progress.PurpleGems;
        public int TimedTickets => Progress.TimedTickets;

        public bool TimedAccessActive => Progress.TimedAccessExpiresUtcTicks > _clock.UtcNow.Ticks;

        public DateTime? TimedAccessEndsUtc =>
            TimedAccessActive ? new DateTime(Progress.TimedAccessExpiresUtcTicks, DateTimeKind.Utc) : (DateTime?)null;

        /// <summary>Call after a balance or the timed-access window changed.</summary>
        public void NotifyChanged() => Changed?.Invoke();

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the wallet is used.");
    }
}
