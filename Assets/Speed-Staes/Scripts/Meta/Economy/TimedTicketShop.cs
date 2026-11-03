using System;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Buys one Timed Ticket with Cash: price check (0 = not for sale), the charge, then stock +1, written in one save.
    /// The stock has no cap. The grant id derives from the purchase id, so a repeated purchase id changes nothing.
    /// </summary>
    public sealed class TimedTicketShop : ITimedTicketShop
    {
        private readonly EconomyConfig _config;
        private readonly IEconomyLedger _ledger;
        private readonly ProgressRepository _repository;

        public TimedTicketShop(EconomyConfig config, IEconomyLedger ledger, ProgressRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public TimedTicketPurchaseResult TryBuyWithCash(string transactionId)
        {
            if (!_config.TryGetTimedTicketCashPrice(out int price)) return TimedTicketPurchaseResult.NotForSale;

            switch (_ledger.TrySpend(EconomyCurrency.Cash, price, transactionId))
            {
                case TransactionResult.Done: break;
                case TransactionResult.NotEnough: return TimedTicketPurchaseResult.NotEnough;
                case TransactionResult.AlreadyProcessed: return TimedTicketPurchaseResult.AlreadyProcessed;
                default: return TimedTicketPurchaseResult.NotForSale;
            }

            _ledger.Grant(EconomyCurrency.TimedTicket, 1, transactionId + ":stock");
            _repository.Save();
            return TimedTicketPurchaseResult.Bought;
        }
    }
}
