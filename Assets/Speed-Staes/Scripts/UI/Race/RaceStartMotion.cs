using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.UI.Race
{
    /// <summary>What the start words and lines look like at one moment (all values the view copies onto its objects).</summary>
    public struct RaceStartPose
    {
        /// <summary>False = hide the whole group.</summary>
        public bool Visible;
        public float TextX;
        public float TextY;
        public Vector2 TextScale;
        public float TextAlpha;
        public float ImpactAlpha;
        public float TensionAlpha;
        public float ReleaseAlpha;
        /// <summary>Scale of the shared effects root (1 except during the GO! burst).</summary>
        public float EffectsScale;
    }

    /// <summary>
    /// The READY / SET / GO! motion of Yaris_Baslangic_UI_v1 as pure C#: pose = f(phase, seconds since the phase began). The view only
    /// asks for the pose each frame, so a phase that comes early simply restarts the clock (no old tween to kill) and the motion is the
    /// same at 30 / 60 / 120 fps. All numbers come from RaceStartConfig. Reduced motion keeps the words and the timing but drops
    /// slide, squash, shake and lines for a short fade.
    /// </summary>
    public sealed class RaceStartMotion
    {
        private readonly RaceStartConfig _config;

        public RaceStartMotion(RaceStartConfig config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        /// <summary>The pose <paramref name="seconds"/> after <paramref name="phase"/> began.</summary>
        public RaceStartPose Sample(RaceStartPhase phase, float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            switch (phase)
            {
                case RaceStartPhase.Ready: return Ready(seconds);
                case RaceStartPhase.Set: return Set(seconds);
                case RaceStartPhase.Go: return Go(seconds);
                default: return new RaceStartPose();
            }
        }

        /// <summary>True when nothing of the phase is left to show at this time (GO! after its burst, Idle).</summary>
        public bool IsFinished(RaceStartPhase phase, float seconds)
        {
            return phase == RaceStartPhase.Idle || (phase == RaceStartPhase.Go && seconds >= _config.GoSeconds);
        }

        private RaceStartPose Ready(float t)
        {
            RaceStartPose pose = Rest();
            if (_config.ReduceMotion)
            {
                pose.TextAlpha = Fade(t);
                return pose;
            }

            float enter = _config.ReadyEnterSeconds;
            if (t < enter)
            {
                pose.TextY = _config.ReadyEnterOffsetY * (1f - OutCubic(t / enter));
                return pose;
            }

            float u = Mathf.Clamp01((t - enter) / _config.ReboundSeconds);
            float eased = OutQuad(u);
            pose.TextScale = Vector2.Lerp(_config.ReboundScale, Vector2.one, eased);
            pose.TextY = _config.ReboundLiftY * Mathf.Sin(Mathf.PI * u);
            pose.ImpactAlpha = 1f - u;
            return pose;
        }

        private RaceStartPose Set(float t)
        {
            RaceStartPose pose = Rest();
            if (_config.ReduceMotion)
            {
                pose.TextAlpha = Fade(t);
                return pose;
            }

            float compress = _config.SetCompressSeconds;
            if (t < compress)
            {
                float u = t / compress;
                pose.TextScale = Vector2.Lerp(Vector2.one, _config.SetScale, OutQuad(u));
                pose.TensionAlpha = u;
                return pose;
            }

            // HOLD: waits for the start signal; a tiny two-swing shake and a tension pulse repeat.
            float hold = t - compress;
            float cycle = Mathf.Max(0.0001f, _config.HoldCycleSeconds);
            float inCycle = hold - Mathf.Floor(hold / cycle) * cycle;
            float shakeLength = Mathf.Max(0.0001f, _config.HoldShakeSeconds);
            pose.TextScale = _config.SetScale;
            pose.TextX = inCycle < shakeLength ? _config.HoldShakeAmplitude * Mathf.Sin(4f * Mathf.PI * inCycle / shakeLength) : 0f;
            float pulse = 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * hold / cycle); // starts at 1, where the compress ended
            pose.TensionAlpha = Mathf.Lerp(_config.HoldTensionMinAlpha, 1f, pulse);
            return pose;
        }

        private RaceStartPose Go(float t)
        {
            float duration = _config.GoSeconds;
            if (t >= duration) return new RaceStartPose();

            RaceStartPose pose = Rest();
            float opaque = Mathf.Min(_config.GoOpaqueSeconds, duration);
            pose.TextAlpha = t < opaque ? 1f : 1f - (t - opaque) / Mathf.Max(0.0001f, duration - opaque);
            if (_config.ReduceMotion) return pose;

            float u = t / duration;
            float scale = Mathf.Lerp(_config.GoScaleFrom, _config.GoScaleTo, OutQuad(u));
            pose.TextScale = new Vector2(scale, scale);
            pose.ReleaseAlpha = 1f - u;
            pose.EffectsScale = Mathf.Lerp(1f, _config.ReleaseScaleTo, OutQuad(u));
            return pose;
        }

        private static RaceStartPose Rest()
        {
            return new RaceStartPose { Visible = true, TextScale = Vector2.one, TextAlpha = 1f, EffectsScale = 1f };
        }

        private float Fade(float t) => Mathf.Clamp01(t / Mathf.Max(0.0001f, _config.ReducedFadeSeconds));

        private static float OutQuad(float x) => 1f - (1f - x) * (1f - x);

        private static float OutCubic(float x)
        {
            float k = 1f - x;
            return 1f - k * k * k;
        }
    }
}
