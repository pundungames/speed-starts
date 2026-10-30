using System;

namespace SpeedStars.Core.Menu
{
    /// <summary>
    /// Pure maths of the menu character spin (no Unity types, so EditMode tests can reach it). A horizontal drag adds yaw in
    /// proportion to the screen width; on release the angular velocity decays exponentially, so the coast does not depend on
    /// the frame rate. The angle just stays where the coast ends.
    /// </summary>
    public sealed class SpinMotion
    {
        private readonly float _degreesPerScreen;
        private readonly float _decayPerSecond;
        private readonly float _stopSpeed;
        private readonly float _velocitySmoothing;

        /// <summary>Accumulated yaw (degrees, unwrapped).</summary>
        public float Yaw { get; private set; }

        /// <summary>Angular velocity (degrees per second).</summary>
        public float Velocity { get; private set; }

        public bool Dragging { get; private set; }

        /// <param name="degreesPerScreen">Yaw for a drag across the full screen width.</param>
        /// <param name="decayPerSecond">k in exp(-k t); bigger = stops sooner.</param>
        /// <param name="stopSpeed">Below this speed (deg/s) the coast ends at once.</param>
        /// <param name="velocitySmoothing">Time constant (s) of the release-speed average; 0 = last frame only.</param>
        public SpinMotion(float degreesPerScreen, float decayPerSecond, float stopSpeed, float velocitySmoothing)
        {
            _degreesPerScreen = degreesPerScreen;
            _decayPerSecond = Math.Max(0.0001f, decayPerSecond);
            _stopSpeed = Math.Max(0f, stopSpeed);
            _velocitySmoothing = Math.Max(0f, velocitySmoothing);
        }

        /// <summary>Screen pixels to degrees; the same swipe spans the same fraction of any screen.</summary>
        public static float PixelsToDegrees(float pixels, float screenWidthPixels, float degreesPerScreen)
        {
            if (screenWidthPixels <= 0f) return 0f;
            return pixels / screenWidthPixels * degreesPerScreen;
        }

        public void BeginDrag()
        {
            Dragging = true;
            Velocity = 0f; // a touch stops a running coast
        }

        /// <summary>One frame of dragging; call every frame while held, with 0 pixels when the finger rests.</summary>
        public void Drag(float deltaPixels, float screenWidthPixels, float dt)
        {
            if (!Dragging || dt <= 0f) return;
            float deg = PixelsToDegrees(deltaPixels, screenWidthPixels, _degreesPerScreen);
            Yaw += deg;
            float instant = deg / dt;
            float blend = _velocitySmoothing <= 0f ? 1f : 1f - (float)Math.Exp(-dt / _velocitySmoothing);
            Velocity += (instant - Velocity) * blend;
        }

        public void EndDrag() => Dragging = false;

        /// <summary>Coast step. Exact integral of v0*exp(-k t), so the total travel is the same at any frame rate.</summary>
        public void Step(float dt)
        {
            if (Dragging || dt <= 0f || Velocity == 0f) return;
            if (Math.Abs(Velocity) < _stopSpeed) { Velocity = 0f; return; }
            float decay = (float)Math.Exp(-_decayPerSecond * dt);
            Yaw += Velocity * (1f - decay) / _decayPerSecond;
            Velocity *= decay;
            if (Math.Abs(Velocity) < _stopSpeed) Velocity = 0f;
        }
    }
}
