using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Wallet;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// The single place Cash, Purple Gems and Timed Ticket stock change. The transaction id is looked up in the save's
    /// processed list first, so a repeated id (double tap, retried reward) changes nothing. Failed spends record nothing.
    /// The list is capped by config (oldest dropped). Never saves: callers write balance and purchase together.
    /// </summary>
    public sealed class EconomyLedger : IEconomyLedger
    {
        private readonly ProgressRepository _repository;
        private readonly WalletService _wallet;
        private readonly EconomyConfig _config;

        public EconomyLedger(ProgressRepository repository, WalletService wallet, EconomyConfig config)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public TransactionResult TrySpend(EconomyCurrency currency, int amount, string transactionId)
        {
            TransactionResult pre = Check(amount, transactionId);
            if (pre != TransactionResult.Done) return pre;

            if (Get(currency) < amount) return TransactionResult.NotEnough;
            Set(currency, Get(currency) - amount);
            Record(transactionId);
            return TransactionResult.Done;
        }

        public TransactionResult Grant(EconomyCurrency currency, int amount, string transactionId)
        {
            TransactionResult pre = Check(amount, transactionId);
            if (pre != TransactionResult.Done) return pre;

            long sum = (long)Get(currency) + amount;
            Set(currency, sum > int.MaxValue ? int.MaxValue : (int)sum);
            Record(transactionId);
            return TransactionResult.Done;
        }

        private TransactionResult Check(int amount, string transactionId)
        {
            if (amount < 1 || string.IsNullOrEmpty(transactionId)) return TransactionResult.InvalidAmount;
            return Progress.ProcessedTransactionIds.Contains(transactionId)
                ? TransactionResult.AlreadyProcessed
                : TransactionResult.Done;
        }

        private void Record(string transactionId)
        {
            List<string> ids = Progress.ProcessedTransactionIds;
            ids.Add(transactionId);
            int excess = ids.Count - _config.ProcessedTransactionCap;
            if (excess > 0) ids.RemoveRange(0, excess);
            _wallet.NotifyChanged();
        }

        private int Get(EconomyCurrency currency)
        {
            PlayerProgress p = Progress;
            switch (currency)
            {
                case EconomyCurrency.Cash: return p.Cash;
                case EconomyCurrency.PurpleGem: return p.PurpleGems;
                case EconomyCurrency.TimedTicket: return p.TimedTickets;
                default: throw new ArgumentOutOfRangeException(nameof(currency));
            }
        }

        private void Set(EconomyCurrency currency, int value)
        {
            PlayerProgress p = Progress;
            switch (currency)
            {
                case EconomyCurrency.Cash: p.Cash = value; break;
                case EconomyCurrency.PurpleGem: p.PurpleGems = value; break;
                case EconomyCurrency.TimedTicket: p.TimedTickets = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(currency));
            }
        }

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the ledger is used.");
    }
}
