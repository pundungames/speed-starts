namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// A popup of the popup layer (ScreenNavigator OpenPopup / ClosePopup; device back closes the top one first),
    /// animated with the popup numbers of UiMotionConfig. Put it on the popup prefab's root, on the popup canvas; a dim
    /// backdrop belongs inside the prefab so it fades with the popup. Popups start inactive in the scene.
    /// </summary>
    public class UiPopup : UiPanel
    {
        protected override bool IsPopupPanel => true;

        protected override UiTransition OpenMotion(UiMotionConfig motion) => motion.PopupOpen;
        protected override UiTransition CloseMotion(UiMotionConfig motion) => motion.PopupClose;
    }
}
