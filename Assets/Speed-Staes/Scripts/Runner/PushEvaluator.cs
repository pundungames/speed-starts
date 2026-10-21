using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Push graph: X = readiness at the tap (0 = stride just started / key just pressed,
    /// 1 = feet together on the ground and the key rested), Y = push power 0..1. Waiting with the
    /// feet together is always X = 1, so it costs nothing but the push you are not making.
    /// The default line sags a little below straight so an early tap costs a bit more (0.5 → ~0.38).
    /// </summary>
    [System.Serializable]
    public sealed class PushEvaluator
    {
        public AnimationCurve power = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.5f),
            new Keyframe(1f, 1f, 1.5f, 0f));

        public float Evaluate(float frontness) => Mathf.Clamp01(power.Evaluate(Mathf.Clamp01(frontness)));
    }
}
