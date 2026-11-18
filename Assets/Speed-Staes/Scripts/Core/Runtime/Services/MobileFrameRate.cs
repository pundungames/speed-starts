using UnityEngine;

namespace SpeedStars.Core.Services
{
    /// <summary>
    /// Phones and tablets run at 30 fps unless asked (Unity's mobile default): the runner is tuned at 60 and stuttered there at
    /// speed while the PC ran smooth (user 18.11). Asked once, before the first scene loads, so every scene gets it without wiring.
    /// Desktop and the Editor are left as they are.
    /// </summary>
    public static class MobileFrameRate
    {
        /// <summary>Frames per second asked for on mobile.</summary>
        public const int Target = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (Application.isMobilePlatform) Application.targetFrameRate = Target;
        }
    }
}
