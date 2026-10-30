using System;
using System.Collections.Generic;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Rewards
{
    /// <summary>
    /// 7-day check-in over the save. <c>ClaimedCount</c> only grows; the position in the calendar is count modulo 7
    /// (restart policy) so a missed day changes nothing. One claim per UTC day. The reward goes out through the ledger
    /// (transaction id per claim number) and the progress moves only after it succeeded; both are written in one save.
    /// A Cosmetic day owns the item, or pays its price back when it is already owned. Timed Tickets go to stock.
    /// </summary>
    public sealed class SevenDayRewardService : ISevenDayRewards
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly SevenDayRewardConfig _config;
        private readonly IEconomyLedger _ledger;
        private readonly ICosmeticService _cosmetics;

        public SevenDayRewardService(ProgressRepository repository, IClock clock, SevenDayRewardConfig config,
            IEconomyLedger ledger, ICosmeticService cosmetics)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
        }

        public event Action Changed;

        public IReadOnlyList<SevenDayCard> Cards
        {
            get
            {
                int done = ClaimedInCycle(out bool claimable);
                var cards = new SevenDayCard[SevenDayRewardConfig.DayCount];
                for (int i = 0; i < cards.Length; i++)
                {
                    SevenDayReward reward = _config.GetDay(i + 1);
                    bool cosmetic = reward.Kind == SevenDayRewardKind.Cosmetic;
                    SevenDayCardState state = i < done ? SevenDayCardState.Claimed
                        : i == done && claimable ? SevenDayCardState.Available
                        : SevenDayCardState.Locked;
                    cards[i] = new SevenDayCard(i + 1, state, reward.Kind, cosmetic ? 0 : reward.Amount,
                        cosmetic && reward.Cosmetic != null ? reward.Cosmetic.Id : string.Empty);
                }
                return cards;
            }
        }

        public bool HasClaimable
        {
            get
            {
                ClaimedInCycle(out bool claimable);
                return claimable;
            }
        }

        public SevenDayClaimResult Claim()
        {
            int done = ClaimedInCycle(out bool claimable);
            if (!claimable) return SevenDayClaimResult.NothingAvailable;

            SevenDayCheckInState state = Progress.SevenDay;
            SevenDayReward reward = _config.GetDay(done + 1);
            if (!TryGive(reward, "sevenday:" + state.ClaimedCount)) return SevenDayClaimResult.Failed;

            state.ClaimedCount++;
            state.LastClaimDayKey = RewardDay.Key(_clock.UtcNow);
            _repository.Save();
            Changed?.Invoke();
            return SevenDayClaimResult.Claimed;
        }

        /// <summary>Days of the current round already claimed (0..7) and whether the next one can be claimed today.</summary>
        private int ClaimedInCycle(out bool claimable)
        {
            SevenDayCheckInState state = Progress.SevenDay;
            int days = SevenDayRewardConfig.DayCount;
            bool claimedToday = state.LastClaimDayKey == RewardDay.Key(_clock.UtcNow);
            int count = Math.Max(0, state.ClaimedCount);

            if (_config.CyclePolicy == SevenDayCyclePolicy.EndAfterLastDay)
            {
                claimable = count < days && !claimedToday;
                return Math.Min(count, days);
            }

            int done = count % days;
            // The round just finished today: show it complete until tomorrow starts the next one.
            if (count > 0 && done == 0 && claimedToday) done = days;
            claimable = done < days && !claimedToday;
            return done;
        }

        private bool TryGive(SevenDayReward reward, string transactionId)
        {
            if (reward.IsEmpty) return true; // an empty day advances the calendar and gives nothing

            switch (reward.Kind)
            {
                case SevenDayRewardKind.Cash:
                    return Applied(_ledger.Grant(EconomyCurrency.Cash, reward.Amount, transactionId));
                case SevenDayRewardKind.TimedTicket:
                    return Applied(_ledger.Grant(EconomyCurrency.TimedTicket, reward.Amount, transactionId));
                case SevenDayRewardKind.Cosmetic:
                    return GiveCosmetic(reward.Cosmetic, transactionId);
                default:
                    return false; // EntryTicket days have no grant path here (the gift uses the ticket source)
            }
        }

        private bool GiveCosmetic(CosmeticDefinition item, string transactionId)
        {
            string id = item.Id;
            if (!_cosmetics.IsOwned(id))
            {
                List<string> owned = Progress.OwnedCosmeticIds;
                if (!owned.Contains(id)) owned.Add(id);
                return true;
            }

            // Already owned: its price comes back in the item's currency; a free item gives nothing.
            if (item.Price < 1) return true;
            EconomyCurrency currency = item.Currency == CosmeticCurrency.Cash ? EconomyCurrency.Cash : EconomyCurrency.PurpleGem;
            return Applied(_ledger.Grant(currency, item.Price, transactionId));
        }

        private static bool Applied(TransactionResult result) =>
            result == TransactionResult.Done || result == TransactionResult.AlreadyProcessed;

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the 7-day rewards are used.");
    }
}
