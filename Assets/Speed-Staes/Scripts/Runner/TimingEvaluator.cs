using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Timing graph: X = tap time minus the moment the leg became ready to step (seconds,
    /// negative = early, buffered until ready). Y = push efficiency 0..1.
    /// Taps earlier than the first key are not accepted at all.
    /// </summary>
    [System.Serializable]
    public sealed class TimingEvaluator
    {
        public AnimationCurve efficiency = new AnimationCurve(
            new Keyframe(-0.10f, 0f),
            new Keyframe(-0.03f, 0.8f),
            new Keyframe(0f, 1f),
            new Keyframe(0.04f, 1f),
            new Keyframe(0.12f, 0.55f),
            new Keyframe(0.30f, 0f));

        /// <summary>Earliest accepted tap relative to ready (negative seconds).</summary>
        public float EarliestError => efficiency.length > 0 ? efficiency[0].time : 0f;

        public float LatestError => efficiency.length > 0 ? efficiency[efficiency.length - 1].time : 0f;

        public float Evaluate(float error)
        {
            if (error < EarliestError) return 0f;
            if (error > LatestError) return Mathf.Clamp01(efficiency[efficiency.length - 1].value);
            return Mathf.Clamp01(efficiency.Evaluate(error));
        }
    }
}
