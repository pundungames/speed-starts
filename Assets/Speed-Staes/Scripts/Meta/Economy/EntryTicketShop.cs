using System;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Tickets;

namespace SpeedStars.Meta.Economy
{
    /// <summary>
    /// Buys one Entry Ticket with Cash. Order matters: price (0 = not for sale), then fullness, then the charge, so a full
    /// inventory is never charged. Cash and ticket are written in one save.
    /// </summary>
    public sealed class EntryTicketShop : IEntryTicketShop
    {
        private readonly EconomyConfig _config;
        private readonly IEconomyLedger _ledger;
        private readonly EntryTicketService _tickets;
        private readonly ProgressRepository _repository;

        public EntryTicketShop(EconomyConfig config, IEconomyLedger ledger, EntryTicketService tickets,
            ProgressRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public TicketPurchaseResult TryBuyWithCash(string transactionId)
        {
            if (!_config.TryGetEntryTicketCashPrice(out int price)) return TicketPurchaseResult.NotForSale;
            if (_tickets.IsFull) return TicketPurchaseResult.Full;

            switch (_ledger.TrySpend(EconomyCurrency.Cash, price, transactionId))
            {
                case TransactionResult.Done: break;
                case TransactionResult.NotEnough: return TicketPurchaseResult.NotEnough;
                case TransactionResult.AlreadyProcessed: return TicketPurchaseResult.AlreadyProcessed;
                default: return TicketPurchaseResult.NotForSale;
            }

            _tickets.TryGrant(TicketGrantSource.CashPurchase);
            _repository.Save();
            return TicketPurchaseResult.Bought;
        }
    }
}
