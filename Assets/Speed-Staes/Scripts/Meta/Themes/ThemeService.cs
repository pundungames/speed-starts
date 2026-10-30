using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Themes
{
    /// <summary>
    /// Active (saved) and shown (previewed) world theme, ownership and purchases (Cash, or Purple Gems for premium themes). Save: <c>PlayerProgress</c>;
    /// prices and currency: <c>ThemeCatalog</c>. The default theme and any theme with price 0 are always owned (free). Buying spends the price currency through the economy ledger and is
    /// idempotent: an owned theme is never charged again. <c>allThemesOwned</c> is the development switch (Editor /
    /// Development Build): every theme counts as owned; the caller decides, so tests control it.
    /// </summary>
    public sealed class ThemeService : IThemeService
    {
        private readonly ThemeCatalog _catalog;
        private readonly ProgressRepository _repository;
        private readonly IEconomyLedger _ledger;
        private readonly bool _allThemesOwned;

        private string _previewId; // null = showing the active theme

        public ThemeService(ThemeCatalog catalog, ProgressRepository repository, IEconomyLedger ledger, bool allThemesOwned)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _allThemesOwned = allThemesOwned;
        }

        public event Action ShownChanged;
        public event Action ActiveChanged;
        public event Action OwnershipChanged;
        public event Action BrowsingChanged;

        public bool IsBrowsing { get; private set; }

        public IReadOnlyList<ThemeInfo> Themes
        {
            get
            {
                var list = new List<ThemeInfo>();
                foreach (ThemeEntry e in _catalog.GetThemes())
                    list.Add(new ThemeInfo(e.Id, e.DisplayName, e.ShowcaseIndex, e.Price, e.PriceCurrency, e.IsPremium));
                return list;
            }
        }

        public string ActiveId
        {
            get
            {
                string saved = Progress.SelectedThemeId;
                return !string.IsNullOrEmpty(saved) && IsOwned(saved) ? saved : DefaultId;
            }
        }

        public int ActiveShowcaseIndex => ShowcaseIndexOf(ActiveId);

        public string ShownId => _previewId ?? ActiveId;
        public int ShownShowcaseIndex => ShowcaseIndexOf(ShownId);

        public int ShownListIndex
        {
            get
            {
                IReadOnlyList<ThemeEntry> all = _catalog.GetThemes();
                string shown = ShownId;
                for (int i = 0; i < all.Count; i++)
                    if (string.Equals(all[i].Id, shown, StringComparison.Ordinal)) return i;
                return 0;
            }
        }

        public bool IsOwned(string id)
        {
            if (!_catalog.TryGet(id, out ThemeEntry entry)) return false;
            // Price 0 = free: owned in every build, no purchase and no charge (T-070).
            return _allThemesOwned
                   || entry.Price <= 0
                   || string.Equals(id, DefaultId, StringComparison.Ordinal)
                   || Progress.OwnedThemeIds.Contains(id);
        }

        public void BeginBrowse()
        {
            if (IsBrowsing) return;
            IsBrowsing = true;
            BrowsingChanged?.Invoke();
        }

        public bool Preview(string id)
        {
            if (!_catalog.TryGet(id, out _)) return false;
            BeginBrowse();
            SetShown(string.Equals(id, ActiveId, StringComparison.Ordinal) ? null : id);
            return true;
        }

        public bool CanStep(int direction)
        {
            int target = ShownListIndex + (direction < 0 ? -1 : 1);
            return direction != 0 && target >= 0 && target < _catalog.GetThemes().Count;
        }

        public bool Step(int direction)
        {
            if (!CanStep(direction)) return false;
            IReadOnlyList<ThemeEntry> all = _catalog.GetThemes();
            return Preview(all[ShownListIndex + (direction < 0 ? -1 : 1)].Id);
        }

        public void CancelBrowse()
        {
            if (!IsBrowsing) return;
            SetShown(null);
            IsBrowsing = false;
            BrowsingChanged?.Invoke();
        }

        public ThemeSelectResult Use(string id)
        {
            if (!_catalog.TryGet(id, out _)) return ThemeSelectResult.UnknownTheme;
            if (!IsOwned(id)) return ThemeSelectResult.NotUsable;
            if (string.Equals(id, ActiveId, StringComparison.Ordinal))
            {
                SetShown(null);
                return ThemeSelectResult.AlreadySelected;
            }

            Progress.SelectedThemeId = id;
            _repository.Save();
            _previewId = null;
            ActiveChanged?.Invoke();
            ShownChanged?.Invoke();
            return ThemeSelectResult.Selected;
        }

        public ThemePurchaseResult TryPurchase(string id)
        {
            if (!_catalog.TryGet(id, out ThemeEntry entry)) return ThemePurchaseResult.UnknownTheme;
            if (IsOwned(id)) return ThemePurchaseResult.AlreadyOwned;
            if (entry.Price <= 0) return ThemePurchaseResult.NotForSale;
            if (BalanceOf(entry.PriceCurrency) < entry.Price) return ThemePurchaseResult.NotEnoughCash;

            // Same transaction id for every currency: one theme can only ever be paid for once.
            switch (_ledger.TrySpend(ToEconomy(entry.PriceCurrency), entry.Price, "theme:" + entry.Id))
            {
                case TransactionResult.Done: break;
                case TransactionResult.NotEnough: return ThemePurchaseResult.NotEnoughCash;
                case TransactionResult.AlreadyProcessed: return ThemePurchaseResult.AlreadyOwned;
                default: return ThemePurchaseResult.NotForSale;
            }

            Progress.OwnedThemeIds.Add(entry.Id);
            _repository.Save();
            OwnershipChanged?.Invoke();
            return ThemePurchaseResult.Purchased;
        }

        public ThemeActionState GetActionState(string id)
        {
            bool known = _catalog.TryGet(id, out ThemeEntry entry);
            return ThemeActionRules.Decide(
                IsOwned(id),
                string.Equals(id, ActiveId, StringComparison.Ordinal),
                known ? entry.Price : 0,
                known ? BalanceOf(entry.PriceCurrency) : 0);
        }

        private int BalanceOf(ThemePriceCurrency currency) =>
            currency == ThemePriceCurrency.PurpleGem ? Progress.PurpleGems : Progress.Cash;

        private static EconomyCurrency ToEconomy(ThemePriceCurrency currency) =>
            currency == ThemePriceCurrency.PurpleGem ? EconomyCurrency.PurpleGem : EconomyCurrency.Cash;

        /// <summary>null = back to the active theme.</summary>
        private void SetShown(string previewId)
        {
            string before = ShownId;
            _previewId = previewId;
            if (!string.Equals(before, ShownId, StringComparison.Ordinal)) ShownChanged?.Invoke();
        }

        private string DefaultId => _catalog.DefaultTheme?.Id ?? string.Empty;

        private int ShowcaseIndexOf(string id) => _catalog.TryGet(id, out ThemeEntry entry) ? entry.ShowcaseIndex : 0;

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the theme service is used.");
    }
}
