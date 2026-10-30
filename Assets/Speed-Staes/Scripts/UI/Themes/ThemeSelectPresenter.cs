using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Themes
{
    /// <summary>Which of the action button's four backgrounds is shown (one at a time).</summary>
    public enum ThemeActionBackground
    {
        Purchase,
        Use,
        InUse,
        Disabled
    }

    /// <summary>What the action button's text shows.</summary>
    public enum ThemeActionLabel
    {
        /// <summary>The price next to the currency icon.</summary>
        Price,
        /// <summary>"USE".</summary>
        Use,
        /// <summary>"IN USE".</summary>
        InUse
    }

    /// <summary>Dark navy on the cream buttons, light cream on the dark "in use" button.</summary>
    public enum ThemeActionTone
    {
        Dark,
        Light
    }

    /// <summary>Which premium frame wraps a control. Gold and red are alternatives, never both.</summary>
    public enum PremiumFrame
    {
        None,
        Gold,
        Red
    }

    /// <summary>
    /// Premium layers of the theme name and the action button (Premium_Tema_UI_v1): normal = fill + gold frame, the theme
    /// in use = fill + red frame (gold off), no interaction = the same plus a disabled overlay on the action button.
    /// </summary>
    public readonly struct PremiumLook
    {
        public readonly bool IsPremium;
        public readonly PremiumFrame Frame;
        public readonly bool ActionOverlay;
        /// <summary>Which currency icon the price shows.</summary>
        public readonly ThemePriceCurrency Currency;

        public PremiumLook(bool isPremium, PremiumFrame frame, bool actionOverlay, ThemePriceCurrency currency)
        {
            IsPremium = isPremium;
            Frame = frame;
            ActionOverlay = actionOverlay;
            Currency = currency;
        }

        public bool ShowsGold => Frame == PremiumFrame.Gold;
        public bool ShowsRed => Frame == PremiumFrame.Red;

        /// <summary>
        /// Layers for one theme. The red frame belongs to the theme in use; the overlay dims an action button that cannot be
        /// pressed for another reason (too poor, not for sale), not the in-use one, which stays red and bright.
        /// </summary>
        public static PremiumLook For(bool isPremium, ThemePriceCurrency currency, ThemeActionState state, bool interactable)
        {
            if (!isPremium) return new PremiumLook(false, PremiumFrame.None, false, currency);
            bool active = state == ThemeActionState.Active;
            return new PremiumLook(true, active ? PremiumFrame.Red : PremiumFrame.Gold, !interactable && !active, currency);
        }
    }

    /// <summary>What a tap on the action button does.</summary>
    public enum ThemeActionClick
    {
        None,
        Purchase,
        Use
    }

    /// <summary>What happened after a tap on the action button.</summary>
    public enum ThemeActionOutcome
    {
        Nothing,
        Purchased,
        Used
    }

    /// <summary>How the action button looks and behaves for one <see cref="ThemeActionState"/>.</summary>
    public readonly struct ThemeActionLook
    {
        public readonly ThemeActionBackground Background;
        public readonly ThemeActionLabel Label;
        public readonly bool ShowsCurrencyIcon;
        public readonly bool Interactable;
        public readonly ThemeActionTone Tone;
        public readonly ThemeActionClick Click;

        public ThemeActionLook(ThemeActionBackground background, ThemeActionLabel label, bool showsCurrencyIcon, bool interactable,
            ThemeActionTone tone, ThemeActionClick click)
        {
            Background = background;
            Label = label;
            ShowsCurrencyIcon = showsCurrencyIcon;
            Interactable = interactable;
            Tone = tone;
            Click = click;
        }
    }

    /// <summary>Everything the screen draws for the shown theme.</summary>
    public readonly struct ThemeSelectSnapshot
    {
        public readonly string Name;
        public readonly int Price;
        public readonly ThemeActionLook Action;
        public readonly bool CanPrevious;
        public readonly bool CanNext;
        public readonly PremiumLook Premium;

        public ThemeSelectSnapshot(string name, int price, ThemeActionLook action, bool canPrevious, bool canNext, PremiumLook premium = default)
        {
            Name = name;
            Price = price;
            Premium = premium;
            Action = action;
            CanPrevious = canPrevious;
            CanNext = canNext;
        }
    }

    /// <summary>
    /// The theme select screen's logic as pure C# over <see cref="IThemeService"/> (no Unity types: EditMode tests run it).
    /// The service decides everything (price, ownership, active theme); this only maps the service's answers to what the
    /// buttons show and forwards taps. The view draws the snapshot and owns the events.
    /// </summary>
    public sealed class ThemeSelectPresenter
    {
        private readonly IThemeService _themes;

        public ThemeSelectPresenter(IThemeService themes)
        {
            _themes = themes;
        }

        /// <summary>Screen opened: browsing starts from the active theme.</summary>
        public void Open() => _themes.BeginBrowse();

        /// <summary>Screen left by any way: an unapplied preview is dropped and the saved theme comes back.</summary>
        public void Close()
        {
            if (_themes.IsBrowsing) _themes.CancelBrowse();
        }

        /// <summary>Arrow tap (-1 previous, +1 next). False at the ends (no wrap-around).</summary>
        public bool Step(int direction) => _themes.Step(direction);

        public ThemeSelectSnapshot Snapshot()
        {
            int index = _themes.ShownListIndex;
            bool known = index >= 0 && index < _themes.Themes.Count;
            string name = known ? _themes.Themes[index].DisplayName : string.Empty;
            int price = known ? _themes.Themes[index].Price : 0;
            ThemeActionState state = known ? _themes.GetActionState(_themes.ShownId) : ThemeActionState.NotForSale;
            ThemeActionLook look = LookFor(state);
            PremiumLook premium = known
                ? PremiumLook.For(_themes.Themes[index].IsPremium, _themes.Themes[index].Currency, state, look.Interactable)
                : default;
            return new ThemeSelectSnapshot(name, price, look, _themes.CanStep(-1), _themes.CanStep(1), premium);
        }

        /// <summary>Action button tap: buys or uses the shown theme, whatever the button state allows.</summary>
        public ThemeActionOutcome PressAction()
        {
            string id = _themes.ShownId;
            switch (Snapshot().Action.Click)
            {
                case ThemeActionClick.Purchase:
                    return _themes.TryPurchase(id) == ThemePurchaseResult.Purchased ? ThemeActionOutcome.Purchased : ThemeActionOutcome.Nothing;
                case ThemeActionClick.Use:
                    ThemeSelectResult used = _themes.Use(id);
                    return used == ThemeSelectResult.Selected ? ThemeActionOutcome.Used : ThemeActionOutcome.Nothing;
                default:
                    return ThemeActionOutcome.Nothing;
            }
        }

        /// <summary>The state table of the art's BEHAVIOR.md.</summary>
        public static ThemeActionLook LookFor(ThemeActionState state)
        {
            switch (state)
            {
                case ThemeActionState.UnownedAffordable:
                    return new ThemeActionLook(ThemeActionBackground.Purchase, ThemeActionLabel.Price, true, true, ThemeActionTone.Dark, ThemeActionClick.Purchase);
                case ThemeActionState.UnownedInsufficient:
                    return new ThemeActionLook(ThemeActionBackground.Disabled, ThemeActionLabel.Price, true, false, ThemeActionTone.Dark, ThemeActionClick.None);
                case ThemeActionState.OwnedInactive:
                    return new ThemeActionLook(ThemeActionBackground.Use, ThemeActionLabel.Use, false, true, ThemeActionTone.Dark, ThemeActionClick.Use);
                case ThemeActionState.Active:
                    return new ThemeActionLook(ThemeActionBackground.InUse, ThemeActionLabel.InUse, false, false, ThemeActionTone.Light, ThemeActionClick.None);
                default: // NotForSale: nothing to buy, nothing to use
                    return new ThemeActionLook(ThemeActionBackground.Disabled, ThemeActionLabel.Price, false, false, ThemeActionTone.Dark, ThemeActionClick.None);
            }
        }
    }
}
