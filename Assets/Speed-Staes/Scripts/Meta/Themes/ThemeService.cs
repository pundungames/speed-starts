using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Wallet;

namespace SpeedStars.Meta.Themes
{
    /// <summary>
    /// Active (saved) and shown (previewed) world theme, ownership and Cash purchases. Save: <c>PlayerProgress</c>;
    /// prices: <c>ThemeCatalog</c>. The default theme is always owned. Buying spends Cash through the wallet and is
    /// idempotent: an owned theme is never charged again. <c>allThemesOwned</c> is the development switch (Editor /
    /// Development Build): every theme counts as owned; the caller decides, so tests control it.
    /// </summary>
    public sealed class ThemeService : IThemeService
    {
        private readonly ThemeCatalog _catalog;
        private readonly ProgressRepository _repository;
        private readonly WalletService _wallet;
        private readonly bool _allThemesOwned;

        private string _previewId; // null = showing the active theme

        public ThemeService(ThemeCatalog catalog, ProgressRepository repository, WalletService wallet, bool allThemesOwned)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
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
                    list.Add(new ThemeInfo(e.Id, e.DisplayName, e.ShowcaseIndex, e.CashPrice));
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
            if (!_catalog.TryGet(id, out _)) return false;
            return _allThemesOwned
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
            if (entry.CashPrice <= 0) return ThemePurchaseResult.NotForSale;
            if (Progress.Cash < entry.CashPrice) return ThemePurchaseResult.NotEnoughCash;

            Progress.Cash -= entry.CashPrice;
            Progress.OwnedThemeIds.Add(entry.Id);
            _repository.Save();
            _wallet.NotifyChanged();
            OwnershipChanged?.Invoke();
            return ThemePurchaseResult.Purchased;
        }

        public ThemeActionState GetActionState(string id)
        {
            bool known = _catalog.TryGet(id, out ThemeEntry entry);
            return ThemeActionRules.Decide(
                IsOwned(id),
                string.Equals(id, ActiveId, StringComparison.Ordinal),
                known ? entry.CashPrice : 0,
                Progress.Cash);
        }

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
