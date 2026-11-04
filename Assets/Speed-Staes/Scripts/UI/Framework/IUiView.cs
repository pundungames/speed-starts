namespace SpeedStars.UI.Framework
{
    /// <summary>A view may close an internal list before Back closes the view itself.</summary>
    public interface IUiBackHandler
    {
        bool TryHandleBack();
    }

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
