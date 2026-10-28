using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Themes
{
    /// <summary>The action button's state for one theme, from plain facts (no service, no Unity).</summary>
    public static class ThemeActionRules
    {
        public static ThemeActionState Decide(bool owned, bool active, int cashPrice, int cash)
        {
            if (owned) return active ? ThemeActionState.Active : ThemeActionState.OwnedInactive;
            if (cashPrice <= 0) return ThemeActionState.NotForSale;
            return cash >= cashPrice ? ThemeActionState.UnownedAffordable : ThemeActionState.UnownedInsufficient;
        }
    }
}
