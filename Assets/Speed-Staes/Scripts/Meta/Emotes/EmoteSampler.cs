using System;
using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// Pure maths of the procedural emotes: time to bone rotation, easing and the fade weight. No scene, no bones; the menu
    /// character (Bridge) only applies what this returns.
    /// </summary>
    public static class EmoteSampler
    {
        /// <summary>Easing of a 0..1 progress (clamped).</summary>
        public static float Ease(EmoteEase ease, float x)
        {
            x = Mathf.Clamp01(x);
            switch (ease)
            {
                case EmoteEase.Smooth: return x * x * (3f - 2f * x);
                case EmoteEase.EaseOut: return 1f - (1f - x) * (1f - x);
                case EmoteEase.EaseIn: return x * x;
                default: return x;
            }
        }

        /// <summary>
        /// Rotation (euler degrees, runner space) of one bone at <paramref name="time"/>. Between two keys of the bone it
        /// goes from the earlier to the later with the later key's ease; before the first key / after the last it holds that
        /// key; a bone without keys is zero.
        /// </summary>
        public static Vector3 Evaluate(EmoteKey[] keys, EmoteBone bone, float time)
        {
            if (keys == null) return Vector3.zero;

            EmoteKey before = null;
            EmoteKey after = null;
            EmoteKey first = null;
            foreach (EmoteKey key in keys)
            {
                if (key == null || key.Bone != bone) continue;
                if (first == null || key.Time < first.Time) first = key;
                if (key.Time <= time && (before == null || key.Time >= before.Time)) before = key;
                if (key.Time > time && (after == null || key.Time < after.Time)) after = key;
            }

            if (first == null) return Vector3.zero;
            if (before == null) return first.Euler;
            if (after == null) return before.Euler;

            float span = after.Time - before.Time;
            float t = span <= 0f ? 1f : (time - before.Time) / span;
            return Vector3.LerpUnclamped(before.Euler, after.Euler, Ease(after.Ease, t));
        }

        /// <summary>
        /// How much of the emote shows at <paramref name="time"/>: fades in over <paramref name="blendIn"/> (starting from
        /// <paramref name="carry"/>, the weight an emote it replaced had) and out over the last <paramref name="blendOut"/>
        /// seconds. 0 at and after <paramref name="duration"/>.
        /// </summary>
        public static float Weight(float time, float duration, float blendIn, float blendOut, float carry)
        {
            if (time >= duration) return 0f;
            float rise = blendIn <= 0f ? 1f : Mathf.Lerp(Mathf.Clamp01(carry), 1f, Ease(EmoteEase.Smooth, time / blendIn));
            float fall = blendOut <= 0f ? 1f : Ease(EmoteEase.Smooth, (duration - time) / blendOut);
            return Math.Min(rise, fall);
        }
    }
}
