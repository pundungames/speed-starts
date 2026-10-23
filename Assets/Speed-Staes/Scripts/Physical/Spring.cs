using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// One damped spring (mass 1): value is pulled toward a target with a natural frequency (Hz) and
    /// a damping ratio (1 = no overshoot, below 1 = overshoots and swings). Semi-implicit Euler; keep
    /// steps small (RunnerSim sub-steps), which is stable for every frequency used here.
    /// </summary>
    [System.Serializable]
    public struct Spring
    {
        public float Value;
        public float Velocity;

        public void Step(float target, float frequency, float damping, float dt)
        {
            float w = 2f * Mathf.PI * frequency;
            Velocity += (w * w * (target - Value) - 2f * damping * w * Velocity) * dt;
            Value += Velocity * dt;
        }

        public void Reset(float value = 0f)
        {
            Value = value;
            Velocity = 0f;
        }
    }

    /// <summary>Spring settings as one inspector line.</summary>
    [System.Serializable]
    public struct SpringSettings
    {
        [Tooltip("Natural frequency (Hz): how fast it gets there.")]
        public float frequency;
        [Tooltip("Damping ratio: 1 = no overshoot, < 1 = overshoots and swings.")]
        public float damping;

        public SpringSettings(float frequency, float damping)
        {
            this.frequency = frequency;
            this.damping = damping;
        }
    }
}
