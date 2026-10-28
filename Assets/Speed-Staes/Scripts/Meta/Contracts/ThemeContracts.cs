using System;
using System.Collections.Generic;

namespace SpeedStars.Meta.Contracts
{
    public enum ThemeSelectResult
    {
        /// <summary>The theme is now the active one.</summary>
        Selected,
        /// <summary>It already was the active theme; nothing changed.</summary>
        AlreadySelected,
        /// <summary>The theme is not owned and themes are not free in this build; nothing changed.</summary>
        NotUsable,
        /// <summary>No theme with that id in the catalog.</summary>
        UnknownTheme
    }

    public enum ThemePurchaseResult
    {
        /// <summary>Cash was spent once and the theme is owned now.</summary>
        Purchased,
        /// <summary>Already owned: nothing was charged (a repeated call cannot pay twice).</summary>
        AlreadyOwned,
        /// <summary>The theme has no price (0 in the catalog), so it cannot be bought yet.</summary>
        NotForSale,
        /// <summary>Not enough Cash; nothing changed.</summary>
        NotEnoughCash,
        UnknownTheme
    }

    /// <summary>What the shown theme's action button does and looks like (theme select screen).</summary>
    public enum ThemeActionState
    {
        /// <summary>Not owned, enough Cash: price label + currency icon, click buys.</summary>
        UnownedAffordable,
        /// <summary>Not owned, not enough Cash: price label, not interactable.</summary>
        UnownedInsufficient,
        /// <summary>Not owned and no price (0 in the catalog): not interactable.</summary>
        NotForSale,
        /// <summary>Owned, not the active theme: "USE", click makes it active.</summary>
        OwnedInactive,
        /// <summary>The active theme: "IN USE", not interactable.</summary>
        Active
    }

    /// <summary>One theme as the UI lists it.</summary>
    public readonly struct ThemeInfo
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly int ShowcaseIndex;
        public readonly int CashPrice;

        public ThemeInfo(string id, string displayName, int showcaseIndex, int cashPrice)
        {
            Id = id;
            DisplayName = displayName;
            ShowcaseIndex = showcaseIndex;
            CashPrice = cashPrice;
        }
    }

    /// <summary>
    /// World themes (main menu now, race later). Two ideas: the ACTIVE theme (saved, what the menu shows when the
    /// player is not choosing) and the SHOWN theme (what the world displays right now). Browsing (theme select screen)
    /// lets the shown theme move through the list without saving or spending; leaving without Use restores the active
    /// one. Themes are bought with Cash; in development builds every theme counts as owned.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>All themes in showcase order (the order of the prev / next arrows).</summary>
        IReadOnlyList<ThemeInfo> Themes { get; }

        /// <summary>Saved theme. A saved id that is gone from the catalog or not owned falls back to the default.</summary>
        string ActiveId { get; }
        int ActiveShowcaseIndex { get; }

        /// <summary>Theme the world displays now: the active one, or the previewed one while browsing.</summary>
        string ShownId { get; }
        int ShownShowcaseIndex { get; }

        /// <summary>Position of the shown theme in <see cref="Themes"/>.</summary>
        int ShownListIndex { get; }

        /// <summary>Raised when the shown theme changed (preview, use, cancel).</summary>
        event Action ShownChanged;

        /// <summary>Raised when the active theme changed.</summary>
        event Action ActiveChanged;

        /// <summary>Raised when a purchase changed what is owned.</summary>
        event Action OwnershipChanged;

        bool IsBrowsing { get; }
        event Action BrowsingChanged;

        bool IsOwned(string id);

        /// <summary>Starts browsing from the active theme. Nothing is saved or spent while browsing.</summary>
        void BeginBrowse();

        /// <summary>Shows a theme without saving or paying (also unowned ones). False for an unknown id.</summary>
        bool Preview(string id);

        /// <summary>True when the arrow in that direction (-1 previous, +1 next) has a theme to go to. No wrap-around.</summary>
        bool CanStep(int direction);

        /// <summary>Previews the neighbour in that direction; false at the ends.</summary>
        bool Step(int direction);

        /// <summary>Ends browsing and restores the active theme if a preview replaced it.</summary>
        void CancelBrowse();

        /// <summary>Makes an owned theme the active one (saved) and shows it. Browsing stays as it is.</summary>
        ThemeSelectResult Use(string id);

        /// <summary>Buys a theme with Cash: once, and nothing changes on failure. Does not make it active.</summary>
        ThemePurchaseResult TryPurchase(string id);

        /// <summary>Button state for a theme (the shown one for the screen's action button).</summary>
        ThemeActionState GetActionState(string id);
    }
}
