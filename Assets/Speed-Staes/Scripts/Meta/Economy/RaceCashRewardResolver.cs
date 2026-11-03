using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Cash for finishing a race against opponents (economy v2, as in the reference game): a finish payment, a flat
    /// difficulty bonus and an opponent bonus that grows with the share of opponents passed. Whether the event needs a
    /// ticket does not matter; a run alone (no opponents) pays nothing. The amounts live in
    /// <see cref="EconomyConfig"/>. One ledger transaction per race (the payout id), so the same race never pays twice;
    /// the gem reward uses its own id prefix, so the two never collide.
    /// </summary>
    public sealed class RaceCashRewardResolver : IRaceCashRewardResolver
    {
        private readonly RaceEventCatalog _events;
        private readonly EconomyConfig _config;
        private readonly IEconomyLedger _ledger;
        private readonly ProgressRepository _repository;

        public RaceCashRewardResolver(RaceEventCatalog events, EconomyConfig config, IEconomyLedger ledger,
            ProgressRepository repository)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public RaceCashReward Resolve(RaceOutcome outcome, string payoutId)
        {
            if (string.IsNullOrEmpty(payoutId)) return RaceCashReward.None;
            if (outcome.Rank < 1 || !(outcome.TimeSeconds > 0d)) return RaceCashReward.None;
            if (!_events.TryGet(outcome.EventId, out _)) return RaceCashReward.None;

            // RunnerCount includes the player; rank 1 passed everyone. A run alone (Practice) has no opponents and pays nothing.
            int totalOpponents = Math.Max(0, outcome.RunnerCount - 1);
            if (totalOpponents < 1) return RaceCashReward.None;
            int passed = Math.Max(0, Math.Min(totalOpponents, outcome.RunnerCount - outcome.Rank));
            int opponentBonus = (int)Math.Round((double)_config.RaceOpponentBonusCash * passed / totalOpponents, MidpointRounding.AwayFromZero);

            var reward = new RaceCashReward(_config.RaceFinishCash, _config.RaceDifficultyBonusCash, opponentBonus, passed, totalOpponents);
            if (reward.Total < 1) return RaceCashReward.None;

            if (_ledger.Grant(EconomyCurrency.Cash, reward.Total, "race-cash:" + payoutId) != TransactionResult.Done)
                return RaceCashReward.None;
            _repository.Save();
            return reward;
        }
    }
}
