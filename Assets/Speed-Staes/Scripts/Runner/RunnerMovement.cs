using UnityEngine;

namespace SpeedStars.Runner
{
    /// <summary>
    /// Forward progress only: turns push efficiency into acceleration and moves the runner
    /// root along +Z. Knows nothing about legs, IK or input. Ticked by the step controller.
    /// A push is not an instant speed jump: its speed gain is released over pushDuration.
    /// </summary>
    public sealed class RunnerMovement : MonoBehaviour
    {
        public float maxSpeed = 11.5f;
        [Tooltip("Speed gained by a push with efficiency 1 at 0 m/s; shrinks toward maxSpeed.")]
        public float pushGain = 1.6f;
        [Tooltip("Time over which a push's gain is applied (s).")]
        public float pushDuration = 0.12f;
        [Tooltip("Continuous loss (m/s per second).")]
        public float drag = 0.4f;

        public float Speed { get; private set; }
        public float Distance { get; private set; }

        float _pending;

        public void Push(float efficiency)
        {
            _pending += pushGain * Mathf.Clamp01(efficiency) * Mathf.Max(0f, 1f - Speed / maxSpeed);
        }

        public void Lose(float amount)
        {
            Speed = Mathf.Max(0f, Speed - amount);
        }

        public void Tick(float dt)
        {
            float applied = _pending * (1f - Mathf.Exp(-3f * dt / Mathf.Max(0.001f, pushDuration)));
            _pending -= applied;
            Speed = Mathf.Clamp(Speed + applied - drag * dt, 0f, maxSpeed);
            Distance += Speed * dt;
            transform.position += transform.forward * (Speed * dt);
        }

        public bool Idle => Speed <= 0f && _pending < 0.001f;

        public void ResetMotion()
        {
            Speed = 0f;
            Distance = 0f;
            _pending = 0f;
        }
    }
}
