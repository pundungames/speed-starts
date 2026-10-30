using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Cosmetics
{
    /// <summary>
    /// Racer presets, ownership and purchase over the save. Prices, preset count, default outfit and palette are read from
    /// the cosmetic catalog on every call. Ownership: an item with no price that is not premium is owned from the start;
    /// anything else needs its id in <c>OwnedCosmeticIds</c> (written by <c>TryPurchase</c>). Presets are filled up to the
    /// configured count lazily, so a raised count or an old save needs no migration step beyond the repository's.
    /// The price is charged through the economy ledger (transaction id "cosmetic:<id>"), which also tells the wallet to redraw.
    /// </summary>
    public sealed class CosmeticService : ICosmeticService
    {
        private readonly ProgressRepository _repository;
        private readonly IEconomyLedger _ledger;
        private readonly CosmeticCatalog _catalog;

        public CosmeticService(ProgressRepository repository, IEconomyLedger ledger, CosmeticCatalog catalog)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public event Action Changed;

        public int PresetCount => _catalog.PresetCount;

        public string GetPresetName(int index) => string.Format(_catalog.PresetNameFormat, index + 1);

        public int SelectedPreset
        {
            get
            {
                EnsurePresets();
                return Progress.SelectedPresetIndex;
            }
        }

        public bool SelectPreset(int index)
        {
            if (index < 0 || index >= PresetCount) return false;
            EnsurePresets();
            if (Progress.SelectedPresetIndex != index)
            {
                Progress.SelectedPresetIndex = index;
                _repository.Save();
            }
            Changed?.Invoke();
            return true;
        }

        public CosmeticOutfit SelectedOutfit => GetOutfit(SelectedPreset);

        public CosmeticOutfit DefaultOutfit
        {
            get
            {
                CosmeticOutfit outfit = CosmeticOutfit.Empty;
                foreach (CosmeticOutfitEntry entry in _catalog.DefaultOutfit)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.CosmeticId)) continue;
                    outfit = outfit.With(entry.Category, entry.CosmeticId, CosmeticColor.Normalize(entry.ColorHex));
                }
                return outfit;
            }
        }

        public CosmeticOutfit GetOutfit(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= PresetCount) return CosmeticOutfit.Empty;
            EnsurePresets();

            CosmeticOutfit outfit = CosmeticOutfit.Empty;
            foreach (SelectedCosmetic saved in Progress.CosmeticPresets[presetIndex].Slots)
            {
                if (!Enum.TryParse(saved.Category, out CosmeticCategory category)) continue;
                // Unknown, mis-slotted or locked items (removed from the catalog, tampered file) are simply not worn.
                if (!_catalog.TryGet(saved.CosmeticId, out CosmeticDefinition item) || item.Category != category) continue;
                if (!IsOwned(item)) continue;
                outfit = outfit.WithSlot(category, ToSlot(item.Id, saved));
            }
            return outfit;
        }

        public CosmeticSaveResult SaveOutfit(int presetIndex, CosmeticOutfit outfit)
        {
            if (presetIndex < 0 || presetIndex >= PresetCount) return CosmeticSaveResult.InvalidPreset;
            outfit = outfit ?? CosmeticOutfit.Empty;

            // Validate everything before touching the save: a rejected outfit changes nothing.
            foreach (CosmeticCategory category in CosmeticOutfit.Categories)
            {
                CosmeticSlot slot = outfit.Get(category);
                if (slot.IsEmpty) continue;
                if (!_catalog.TryGet(slot.CosmeticId, out CosmeticDefinition item) || item.Category != category)
                    return CosmeticSaveResult.UnknownItem;
                if (!IsOwned(item)) return CosmeticSaveResult.LockedItem;
            }

            EnsurePresets();
            List<SelectedCosmetic> slots = Progress.CosmeticPresets[presetIndex].Slots;
            slots.Clear();
            foreach (CosmeticCategory category in CosmeticOutfit.Categories)
            {
                CosmeticSlot slot = outfit.Get(category);
                if (slot.IsEmpty) continue;
                slots.Add(ToSaved(category, slot));
            }

            bool persisted = _repository.Save();
            Changed?.Invoke();
            return persisted ? CosmeticSaveResult.Saved : CosmeticSaveResult.NotPersisted;
        }

        public IReadOnlyList<CosmeticItemInfo> GetItems(CosmeticCategory category)
        {
            IReadOnlyList<CosmeticDefinition> definitions = _catalog.GetByCategory(category);
            var items = new List<CosmeticItemInfo>(definitions.Count);
            foreach (CosmeticDefinition definition in definitions) items.Add(Describe(definition));
            return items;
        }

        public bool TryGetItem(string cosmeticId, out CosmeticItemInfo item)
        {
            if (_catalog.TryGet(cosmeticId, out CosmeticDefinition definition))
            {
                item = Describe(definition);
                return true;
            }
            item = default;
            return false;
        }

        public bool IsOwned(string cosmeticId) => _catalog.TryGet(cosmeticId, out CosmeticDefinition item) && IsOwned(item);

        public bool IsLocked(string cosmeticId) => _catalog.TryGet(cosmeticId, out CosmeticDefinition item) && !IsOwned(item);

        public CosmeticPurchaseResult TryPurchase(string cosmeticId)
        {
            if (!_catalog.TryGet(cosmeticId, out CosmeticDefinition item)) return CosmeticPurchaseResult.UnknownItem;
            if (IsOwned(item)) return CosmeticPurchaseResult.AlreadyOwned;
            if (!item.IsForSale) return CosmeticPurchaseResult.NotForSale;

            EconomyCurrency currency;
            switch (item.Currency)
            {
                case CosmeticCurrency.PurpleGem: currency = EconomyCurrency.PurpleGem; break;
                case CosmeticCurrency.Cash: currency = EconomyCurrency.Cash; break;
                default: return CosmeticPurchaseResult.NotForSale;
            }

            switch (_ledger.TrySpend(currency, item.Price, "cosmetic:" + item.Id))
            {
                case TransactionResult.Done: break;
                case TransactionResult.NotEnough: return CosmeticPurchaseResult.NotEnough;
                case TransactionResult.AlreadyProcessed: return CosmeticPurchaseResult.AlreadyOwned;
                default: return CosmeticPurchaseResult.NotForSale;
            }

            // Money and ownership change together and are written in one save, so a crash cannot take one without the other.
            Progress.OwnedCosmeticIds.Add(item.Id);
            _repository.Save();
            Changed?.Invoke();
            return CosmeticPurchaseResult.Purchased;
        }

        public IReadOnlyList<string> ColorPalette
        {
            get
            {
                var colors = new List<string>();
                foreach (string hex in _catalog.ColorPalette)
                {
                    string normalized = CosmeticColor.Normalize(hex);
                    if (normalized.Length > 0) colors.Add(normalized);
                }
                return colors;
            }
        }

        public CosmeticOutfit CreateRandomOutfit(System.Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            IReadOnlyList<string> palette = ColorPalette;
            CosmeticOutfit outfit = CosmeticOutfit.Empty;
            foreach (CosmeticCategory category in CosmeticOutfit.Categories)
            {
                var owned = new List<CosmeticDefinition>();
                foreach (CosmeticDefinition item in _catalog.GetByCategory(category))
                    if (IsOwned(item)) owned.Add(item);
                if (owned.Count == 0) continue;

                CosmeticDefinition pick = owned[random.Next(owned.Count)];
                // Every colour channel of the item gets its own palette colour.
                var slot = new CosmeticSlot(pick.Id, string.Empty);
                for (int channel = 0; channel < pick.Channels.Count; channel++)
                {
                    string color = palette.Count > 0 ? palette[random.Next(palette.Count)] : string.Empty;
                    slot = slot.WithColor(channel, color);
                }
                outfit = outfit.WithSlot(category, slot);
            }
            return outfit;
        }

        private bool IsOwned(CosmeticDefinition item)
        {
            if (!item.IsPremium && !item.IsForSale) return true;
            return Progress.OwnedCosmeticIds.Contains(item.Id);
        }

        private CosmeticItemInfo Describe(CosmeticDefinition item)
        {
            string name = item.PartNames.Count > 0 ? item.PartNames[0] : item.Id;
            var defaults = new string[item.Channels.Count];
            for (int i = 0; i < defaults.Length; i++) defaults[i] = CosmeticColor.Normalize(item.DefaultColorAt(i));
            return new CosmeticItemInfo(item.Id, item.Category, name, item.IsPremium, item.Price, item.Currency, IsOwned(item), defaults);
        }

        /// <summary>Saved entry to slot: colours normalized; a pre-channel save has only the primary one, which is index 0.</summary>
        private static CosmeticSlot ToSlot(string cosmeticId, SelectedCosmetic saved) =>
            new CosmeticSlot(cosmeticId, CosmeticColor.Normalize(saved.ColorHex),
                CosmeticColor.Normalize(saved.MoreColorAt(1)), CosmeticColor.Normalize(saved.MoreColorAt(2)));

        private static SelectedCosmetic ToSaved(CosmeticCategory category, CosmeticSlot slot) =>
            new SelectedCosmetic(category.ToString(), slot.CosmeticId, CosmeticColor.Normalize(slot.ColorAt(0)),
                CosmeticColor.Normalize(slot.ColorAt(1)), CosmeticColor.Normalize(slot.ColorAt(2)));

        /// <summary>
        /// Fills missing presets with the default outfit and keeps the selected index valid. Idempotent; changes the live
        /// progress without saving (it is written with the next save, and re-derived identically if it never is).
        /// </summary>
        private void EnsurePresets()
        {
            PlayerProgress progress = Progress;
            int count = PresetCount;
            while (progress.CosmeticPresets.Count < count)
            {
                var preset = new CosmeticPreset();
                CosmeticOutfit defaults = DefaultOutfit;
                foreach (CosmeticCategory category in CosmeticOutfit.Categories)
                {
                    CosmeticSlot slot = defaults.Get(category);
                    if (!slot.IsEmpty) preset.Slots.Add(ToSaved(category, slot));
                }
                progress.CosmeticPresets.Add(preset);
            }
            if (progress.SelectedPresetIndex < 0 || progress.SelectedPresetIndex >= count) progress.SelectedPresetIndex = 0;
        }

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the cosmetic service is used.");
    }
}
