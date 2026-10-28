namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// A full screen of the screen stack (ScreenNavigator Push / Pop / Replace), animated with the screen numbers of
    /// UiMotionConfig. Put it on the screen prefab's root; concrete screen views may derive from it (override the
    /// Unity messages and call base). Screens other than the first one start inactive in the scene.
    /// </summary>
    public class UiScreen : UiPanel
    {
        protected override UiTransition OpenMotion(UiMotionConfig motion) => motion.ScreenOpen;
        protected override UiTransition CloseMotion(UiMotionConfig motion) => motion.ScreenClose;
    }
}
