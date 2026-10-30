using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Themes
{
    /// <summary>Balance is the player's stock of the theme's price currency. The action button's state for one theme, from plain facts (no service, no Unity).</summary>
    public static class ThemeActionRules
    {
        public static ThemeActionState Decide(bool owned, bool active, int price, int balance)
        {
            if (owned) return active ? ThemeActionState.Active : ThemeActionState.OwnedInactive;
            if (price <= 0) return ThemeActionState.NotForSale;
            return balance >= price ? ThemeActionState.UnownedAffordable : ThemeActionState.UnownedInsufficient;
        }
    }
}
