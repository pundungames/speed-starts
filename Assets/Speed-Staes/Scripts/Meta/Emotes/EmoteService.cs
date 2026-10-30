using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// Emote ownership, Cash purchases and the equipped emote. Save: <c>PlayerProgress</c>; prices: <c>EmoteCatalog</c>. A
    /// price of 0 makes the emote free. Buying spends Cash through the economy ledger and is idempotent: an owned emote is
    /// never charged again. The purchase and the ownership are saved together with the balance.
    /// </summary>
    public sealed class EmoteService : IEmoteService
    {
        private readonly EmoteCatalog _catalog;
        private readonly ProgressRepository _repository;
        private readonly IEconomyLedger _ledger;

        public EmoteService(EmoteCatalog catalog, ProgressRepository repository, IEconomyLedger ledger)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        }

        public event Action Changed;

        public IReadOnlyList<EmoteInfo> Emotes
        {
            get
            {
                var list = new List<EmoteInfo>();
                foreach (EmoteDefinition e in _catalog.GetEmotes()) list.Add(Info(e));
                return list;
            }
        }

        public string EquippedId
        {
            get
            {
                string saved = Progress.EquippedEmoteId;
                return !string.IsNullOrEmpty(saved) && IsOwned(saved) ? saved : _catalog.DefaultEmote?.Id ?? string.Empty;
            }
        }

        public bool TryGet(string id, out EmoteInfo info)
        {
            if (_catalog.TryGet(id, out EmoteDefinition emote))
            {
                info = Info(emote);
                return true;
            }
            info = default;
            return false;
        }

        public bool IsOwned(string id)
        {
            if (!_catalog.TryGet(id, out EmoteDefinition emote)) return false;
            return emote.CashPrice <= 0 || Progress.OwnedEmoteIds.Contains(id);
        }

        public EmotePurchaseResult TryPurchase(string id)
        {
            if (!_catalog.TryGet(id, out EmoteDefinition emote)) return EmotePurchaseResult.UnknownEmote;
            if (IsOwned(id)) return EmotePurchaseResult.AlreadyOwned;
            if (Progress.Cash < emote.CashPrice) return EmotePurchaseResult.NotEnoughCash;

            switch (_ledger.TrySpend(EconomyCurrency.Cash, emote.CashPrice, "emote:" + emote.Id))
            {
                case TransactionResult.Done: break;
                case TransactionResult.NotEnough: return EmotePurchaseResult.NotEnoughCash;
                default: return EmotePurchaseResult.AlreadyOwned; // the id was applied before: it was bought already
            }

            Progress.OwnedEmoteIds.Add(emote.Id);
            _repository.Save();
            Changed?.Invoke();
            return EmotePurchaseResult.Purchased;
        }

        public bool Equip(string id)
        {
            if (!IsOwned(id)) return false;
            if (string.Equals(id, EquippedId, StringComparison.Ordinal)) return true;

            Progress.EquippedEmoteId = id;
            _repository.Save();
            Changed?.Invoke();
            return true;
        }

        private static EmoteInfo Info(EmoteDefinition e) => new EmoteInfo(e.Id, e.DisplayName, e.CashPrice);

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the emote service is used.");
    }
}
