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
        public Rect SafeArea
        {
            get
            {
                Rect safe = Screen.safeArea;
#if UNITY_EDITOR
                // Game View resolution changes can leave Screen.safeArea in the previous
                // viewport's pixels. A landscape notch cannot remove most of its height.
                // Restore the full viewport instead of retaining an earlier squeezed preview.
                if (!SafeAreaMath.IsValidSample(ScreenSize, safe, 1f)
                    || (Screen.width >= Screen.height && safe.height < Screen.height * 0.8f))
                    return new Rect(0f, 0f, Screen.width, Screen.height);
#endif
                return safe;
            }
        }
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
