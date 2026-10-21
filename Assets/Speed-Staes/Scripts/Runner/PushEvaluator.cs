using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Push graph: X = where the pressed foot is at the tap (0 = back point, 1 = front point),
    /// Y = push power 0..1. The foot waiting on its front point is always X = 1, so waiting
    /// there costs nothing but the push you are not making. The default line sags a little
    /// below straight so an early tap costs a bit more than its distance (0.5 → ~0.38).
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
