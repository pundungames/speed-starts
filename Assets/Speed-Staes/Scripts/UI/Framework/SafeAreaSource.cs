using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>Where the safe rectangle and the screen size come from; a fake in tests, the device in the game.</summary>
    public interface ISafeAreaSource
    {
        /// <summary>Safe rectangle in screen pixels (same space as Screen.safeArea).</summary>
        Rect SafeArea { get; }

        /// <summary>Screen size in pixels.</summary>
        Vector2 ScreenSize { get; }
    }

    /// <summary>The real device: Screen.safeArea (the Device Simulator reports it in the Editor too).</summary>
    public sealed class ScreenSafeAreaSource : ISafeAreaSource
    {
        public Rect SafeArea => Screen.safeArea;
        public Vector2 ScreenSize => new Vector2(Screen.width, Screen.height);
    }

    /// <summary>A fixed rectangle; used by tests to pretend a notch.</summary>
    public sealed class FixedSafeAreaSource : ISafeAreaSource
    {
        public FixedSafeAreaSource(Vector2 screenSize, Rect safeArea)
        {
            ScreenSize = screenSize;
            SafeArea = safeArea;
        }

        public Rect SafeArea { get; set; }
        public Vector2 ScreenSize { get; set; }
    }
}
