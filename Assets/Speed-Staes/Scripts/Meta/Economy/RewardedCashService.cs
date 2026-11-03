using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Rewards;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Rewarded ad for Cash, N per UTC day (economy v2). The day is the same UTC day key as the daily gift; the counter
    /// (day key + ads watched) is saved with the Cash payment in one save. The clock is the trusted one: a rolled-back
    /// device clock stays at the latest time seen, and a counter stamped with a later day than "today" is still counted
    /// for today, so winding the clock back never gives rights back. Only a completed ad pays; each payment has its own
    /// ledger id (day + ad number), so a repeated callback cannot pay twice.
    /// </summary>
    public sealed class RewardedCashService : IRewardedCash
    {
        private readonly IRewardedAds _ads;
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly EconomyConfig _config;
        private readonly IEconomyLedger _ledger;

        /// <param name="ads">Null in a build without an ad service: the offer stays closed.</param>
        public RewardedCashService(IRewardedAds ads, ProgressRepository repository, IClock clock, EconomyConfig config,
            IEconomyLedger ledger)
        {
            _ads = ads;
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public event Action Changed;

        public int Amount => _config.RewardedAdCash;

        public int DailyLimit => _config.RewardedAdCashDailyLimit;

        public bool IsBusy { get; private set; }

        public int RemainingToday => Math.Max(0, DailyLimit - WatchedToday());

        public bool IsAvailable => !IsBusy && _ads != null && _ads.IsReady && Amount > 0 && RemainingToday > 0;

        public bool TryWatch(Action<RewardedCashOutcome> done)
        {
            if (!IsAvailable) return false;

            IsBusy = true;
            bool answered = false;
            try
            {
                _ads.Show(result =>
                {
                    if (answered) return; // an ad answers once; a repeat is ignored
                    answered = true;
                    IsBusy = false;
                    RewardedCashOutcome outcome = Resolve(result);
                    done?.Invoke(outcome);
                });
            }
            catch
            {
                IsBusy = false;
                throw;
            }
            return true;
        }

        private RewardedCashOutcome Resolve(AdResult result)
        {
            if (result == AdResult.Skipped) return RewardedCashOutcome.Skipped;
            if (result != AdResult.Completed) return RewardedCashOutcome.Failed;

            if (Amount < 1) return RewardedCashOutcome.Unavailable;
            if (RemainingToday < 1) return RewardedCashOutcome.LimitReached;

            PlayerProgress p = Progress;
            string today = RewardDay.Key(_clock.UtcNow);
            int watched = WatchedToday();
            string day = IsCountedForToday(p, today) ? p.AdCashDayKey : today;
            string id = "ad-cash:" + day + ":" + (watched + 1);
            if (_ledger.Grant(EconomyCurrency.Cash, Amount, id) != TransactionResult.Done) return RewardedCashOutcome.Failed;

            // Cash and counter are written in one save.
            p.AdCashDayKey = day;
            p.AdCashWatched = watched + 1;
            _repository.Save();
            Changed?.Invoke();
            return RewardedCashOutcome.Granted;
        }

        /// <summary>Ads counted for today: the saved counter if it is today's (or from a later day: clock rolled back), else 0.</summary>
        private int WatchedToday()
        {
            PlayerProgress p = Progress;
            return IsCountedForToday(p, RewardDay.Key(_clock.UtcNow)) ? Math.Max(0, p.AdCashWatched) : 0;
        }

        private static bool IsCountedForToday(PlayerProgress p, string today) =>
            p.AdCashDayKey.Length > 0 && string.CompareOrdinal(p.AdCashDayKey, today) >= 0;

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the Cash ad is used.");
    }
}
