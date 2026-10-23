using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// One damped spring (mass 1): value is pulled toward a target with a natural frequency (Hz) and
    /// a damping ratio (1 = no overshoot, below 1 = overshoots and swings). Semi-implicit Euler, which
    /// blows up when a step is long for the spring (a stiff spring at frame rate), so a long step is cut
    /// into sub-steps short enough for this frequency and damping.
    /// </summary>
    [System.Serializable]
    public struct Spring
    {
        /// <summary>Largest ω·h (and 2ζω·h) per sub-step; well inside the stable range.</summary>
        const float MaxStepPhase = 0.5f;

        public float Value;
        public float Velocity;

        public void Step(float target, float frequency, float damping, float dt)
        {
            if (dt <= 0f) return;
            float w = 2f * Mathf.PI * frequency;
            float stiffness = Mathf.Max(w, 2f * damping * w) * dt;
            int steps = Mathf.Clamp(Mathf.CeilToInt(stiffness / MaxStepPhase), 1, 64);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                Velocity += (w * w * (target - Value) - 2f * damping * w * Velocity) * h;
                Value += Velocity * h;
            }
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
