namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// What a panel's Back / Close button does, as pure C# (UiPanel calls it): a popup closes itself, a screen goes
    /// back one step (which first closes a popup that is open above it, then pops).
    /// </summary>
    public static class PanelCloseRule
    {
        /// <returns>True if something was closed.</returns>
        public static bool Close(ScreenNavigator navigator, IUiView panel, bool isPopup)
        {
            if (navigator == null) return false;
            return isPopup ? navigator.ClosePopup(panel) : navigator.Back();
        }
    }
}
