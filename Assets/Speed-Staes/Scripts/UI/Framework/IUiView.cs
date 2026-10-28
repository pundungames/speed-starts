namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// What ScreenNavigator needs from a screen or a popup. UiScreen / UiPopup implement it with the motion recipe;
    /// tests use plain fakes, so the navigator runs without a scene. Both calls must be safe to repeat.
    /// </summary>
    public interface IUiView
    {
        void Show();
        void Hide();
    }
}
