using System;
using SpeedStars.UI.Framework;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>
    /// A window of the leaderboard screen (region list, period list, event modal) as a real popup of the popup layer:
    /// the navigator closes it first on Back / Escape, and its catcher (the base close button, set in the prefab) closes
    /// it on a tap outside. Raises <see cref="Hidden"/> so the screen can restore what the window was covering.
    /// </summary>
    public sealed class LeaderboardPopupPanel : UiPopup
    {
        public event Action Hidden;

        protected override void OnDisable()
        {
            base.OnDisable();
            Hidden?.Invoke();
        }
    }
}
