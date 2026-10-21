namespace SpeedStars.Runner
{
    public enum LegState
    {
        /// <summary>Standing still (start / stopped).</summary>
        Grounded,
        /// <summary>Running, no stride in progress: both feet together under the body, on the ground.</summary>
        Ready,
        /// <summary>Pressed leg of a stride: pushes Mid → Back on the ground, then swings back to Mid.</summary>
        Drive,
        /// <summary>Other leg of a stride: swings Mid → Front in the air, then lands and comes back to Mid.</summary>
        Reach,
    }

    /// <summary>
    /// Logical stride role of one leg, purely time based. No rendering, no IK, no speed:
    /// the step controller sets the role on every stride, the rig visual reads it.
    /// A stride is one press: the pressed leg drives, the other reaches, and after strideDuration both
    /// are together under the body again (Ready). A press during a stride starts a new one from there.
    /// </summary>
    public sealed class LegCycle
    {
        public LegState State { get; private set; } = LegState.Grounded;
        /// <summary>Game time the current state was entered.</summary>
        public double StateTime { get; private set; }
        public long Steps { get; private set; }

        /// <summary>Stride phase 0..1 at <paramref name="time"/>; 1 when no stride is in progress.</summary>
        public float Phase(double time, float strideDuration)
        {
            if (State != LegState.Drive && State != LegState.Reach) return 1f;
            float s = (float)(time - StateTime) / System.Math.Max(0.001f, strideDuration);
            return s < 0f ? 0f : s > 1f ? 1f : s;
        }

        public void Begin(LegState role, double time)
        {
            if (role == LegState.Drive) Steps++;
            Enter(role, time);
        }

        public void Tick(double now, float strideDuration)
        {
            if ((State == LegState.Drive || State == LegState.Reach) && now >= StateTime + strideDuration)
                Enter(LegState.Ready, StateTime + strideDuration);
        }

        public void SetGrounded(double now) => Enter(LegState.Grounded, now);

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }
    }
}
