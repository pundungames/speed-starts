using System;
using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// Pure maths of the baked emotes: the fade envelope and which two frames of a clip a time falls between. No scene, no
    /// bones; the menu character (Bridge) only applies what this returns.
    /// </summary>
    public static class EmoteSampler
    {
        /// <summary>Smooth-step of a 0..1 progress (clamped).</summary>
        public static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>
        /// How much of the emote shows at <paramref name="time"/>: fades in over <paramref name="fadeIn"/> (starting from
        /// <paramref name="carry"/>, the weight an emote it replaced had) and out over the last <paramref name="fadeOut"/>
        /// seconds. 0 at and after <paramref name="length"/>.
        /// </summary>
        public static float Weight(float time, float length, float fadeIn, float fadeOut, float carry)
        {
            if (time >= length) return 0f;
            float rise = fadeIn <= 0f ? 1f : Mathf.Lerp(Mathf.Clamp01(carry), 1f, Smooth(time / fadeIn));
            float fall = fadeOut <= 0f ? 1f : Smooth((length - time) / fadeOut);
            return Math.Min(rise, fall);
        }

        /// <summary>
        /// The two frames around <paramref name="clipTime"/> and the blend between them (0 = frame a, 1 = frame b). A time before
        /// the start holds the first frame, after the end the last; an empty clip returns 0, 0, 0.
        /// </summary>
        public static void Frame(float clipTime, float frameRate, int frameCount, out int a, out int b, out float blend)
        {
            if (frameCount <= 0 || frameRate <= 0f)
            {
                a = 0;
                b = 0;
                blend = 0f;
                return;
            }
            float position = Mathf.Clamp(clipTime * frameRate, 0f, frameCount - 1);
            a = Math.Min((int)position, frameCount - 1);
            b = Math.Min(a + 1, frameCount - 1);
            blend = position - a;
        }
    }
}
