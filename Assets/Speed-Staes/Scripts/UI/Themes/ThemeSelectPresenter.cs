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

        public ThemeSelectSnapshot(string name, int price, ThemeActionLook action, bool canPrevious, bool canNext)
        {
            Name = name;
            Price = price;
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
            int price = known ? _themes.Themes[index].CashPrice : 0;
            ThemeActionState state = known ? _themes.GetActionState(_themes.ShownId) : ThemeActionState.NotForSale;
            return new ThemeSelectSnapshot(name, price, LookFor(state), _themes.CanStep(-1), _themes.CanStep(1));
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
