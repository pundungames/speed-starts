using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Rank reward in Purple Gems (GDD §1.3). Only events flagged AwardsPurpleGems pay, only for a finished outcome
    /// (rank and time present); the amount is the shared GemRewardConfig row. The transaction id is the race's payout id,
    /// so the same race never pays twice.
    /// </summary>
    public sealed class RaceRewardResolver : IRaceRewardResolver
    {
        private readonly RaceEventCatalog _events;
        private readonly GemRewardConfig _rewards;
        private readonly IEconomyLedger _ledger;
        private readonly ProgressRepository _repository;

        public RaceRewardResolver(RaceEventCatalog events, GemRewardConfig rewards, IEconomyLedger ledger,
            ProgressRepository repository)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public int Resolve(RaceOutcome outcome, string payoutId)
        {
            if (string.IsNullOrEmpty(payoutId)) return 0;
            if (outcome.Rank < 1 || !(outcome.TimeSeconds > 0d)) return 0;
            if (!_events.TryGet(outcome.EventId, out RaceEventDefinition definition) || !definition.AwardsPurpleGems) return 0;

            int gems = _rewards.GetPurpleGems(outcome.Rank);
            if (gems < 1) return 0;

            if (_ledger.Grant(EconomyCurrency.PurpleGem, gems, "race-payout:" + payoutId) != TransactionResult.Done) return 0;
            _repository.Save();
            return gems;
        }
    }
}
