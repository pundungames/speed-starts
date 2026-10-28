using System;
using System.Collections.Generic;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Which screen is shown and which popups are open, for one scene (bound per scene by its installer).
    /// Screens: a stack where only the top one is visible; a screen is in the stack at most once, so going to one
    /// that is already below unwinds back to it instead of stacking a second copy. The root screen is never popped.
    /// Popups: a separate layer above the screens; screen moves leave it alone. Back closes the top popup first and
    /// only then pops a screen. Pure C#: views are reached through IUiView, so this runs in tests without a scene.
    /// State is updated before the views are called, so a view reacting to Show/Hide sees the new stack.
    /// </summary>
    public sealed class ScreenNavigator
    {
        private readonly List<IUiView> _screens = new List<IUiView>();
        private readonly List<IUiView> _popups = new List<IUiView>();

        /// <summary>Visible screen, null before the first Push.</summary>
        public IUiView CurrentScreen => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        /// <summary>Popup that Back would close, null when none is open.</summary>
        public IUiView TopPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        public int ScreenCount => _screens.Count;
        public int PopupCount => _popups.Count;

        /// <summary>Shows <paramref name="screen"/> on top and hides the one it covers. Already on top: nothing happens.</summary>
        public void Push(IUiView screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            int index = _screens.IndexOf(screen);
            if (index >= 0)
            {
                UnwindTo(index);
                return;
            }

            IUiView covered = CurrentScreen;
            _screens.Add(screen);
            covered?.Hide();
            screen.Show();
        }

        /// <summary>Hides the top screen and shows the one under it. False (and nothing changes) at the root.</summary>
        public bool Pop()
        {
            if (_screens.Count <= 1) return false;
            UnwindTo(_screens.Count - 2);
            return true;
        }

        /// <summary>
        /// Swaps the top screen for <paramref name="screen"/>, keeping the depth (e.g. a tab change). On an empty
        /// stack it is a Push; a screen already in the stack is unwound to, like Push.
        /// </summary>
        public void Replace(IUiView screen)
        {
            if (screen == null) throw new ArgumentNullException(nameof(screen));

            int index = _screens.IndexOf(screen);
            if (index >= 0)
            {
                UnwindTo(index);
                return;
            }
            if (_screens.Count == 0)
            {
                Push(screen);
                return;
            }

            int top = _screens.Count - 1;
            IUiView replaced = _screens[top];
            _screens[top] = screen;
            replaced.Hide();
            screen.Show();
        }

        /// <summary>Opens <paramref name="popup"/> above everything. False if it is already open (no second copy).</summary>
        public bool OpenPopup(IUiView popup)
        {
            if (popup == null) throw new ArgumentNullException(nameof(popup));
            if (_popups.Contains(popup)) return false;

            _popups.Add(popup);
            popup.Show();
            return true;
        }

        /// <summary>Closes <paramref name="popup"/> wherever it is in the layer. False if it was not open.</summary>
        public bool ClosePopup(IUiView popup)
        {
            if (popup == null) throw new ArgumentNullException(nameof(popup));
            if (!_popups.Remove(popup)) return false;

            popup.Hide();
            return true;
        }

        /// <summary>Closes the most recently opened popup. False when none is open.</summary>
        public bool CloseTopPopup()
        {
            IUiView top = TopPopup;
            return top != null && ClosePopup(top);
        }

        /// <summary>Closes every open popup, newest first.</summary>
        public void CloseAllPopups()
        {
            while (_popups.Count > 0) CloseTopPopup();
        }

        /// <summary>
        /// Device back: closes the top popup if there is one, otherwise pops a screen. False when there was nothing
        /// to go back from (root screen, no popup), so the caller decides what back means there.
        /// </summary>
        public bool Back()
        {
            return CloseTopPopup() || Pop();
        }

        private void UnwindTo(int index)
        {
            int top = _screens.Count - 1;
            if (index >= top) return;

            // Only the top screen is visible; the ones between it and the target are already hidden.
            IUiView leaving = _screens[top];
            IUiView target = _screens[index];
            _screens.RemoveRange(index + 1, top - index);
            leaving.Hide();
            target.Show();
        }
    }
}
