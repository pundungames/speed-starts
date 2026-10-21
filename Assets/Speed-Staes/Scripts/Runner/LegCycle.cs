namespace SpeedStars.Runner
{
    public enum LegState
    {
        /// <summary>Standing still (start / stopped).</summary>
        Grounded,
        /// <summary>Key up: foot on the front point, waiting for its press.</summary>
        Ready,
        /// <summary>Pressed: foot travels front → back over a fixed duration.</summary>
        Step,
        /// <summary>Still held after the step: foot stays on the back point.</summary>
        Hold,
        /// <summary>Released: foot returns back → front over a fixed duration.</summary>
        Return,
    }

    /// <summary>
    /// Logical step cycle of one leg, purely time based. No rendering, no IK, no speed:
    /// the step controller ticks it, the rig visual reads it.
    /// Press → Step (front→back). Held → Hold at back. Release → Return (back→front).
    /// A quick press-release still completes the full step, then returns.
    /// A press always starts a step from wherever the foot currently is.
    /// </summary>
    public sealed class LegCycle
    {
        public LegState State { get; private set; } = LegState.Grounded;
        /// <summary>Game time the current state was entered.</summary>
        public double StateTime { get; private set; }
        public long Steps { get; private set; }
        public bool Held { get; private set; }
        /// <summary>Foot is on the ground carrying weight (everything except the airborne return).</summary>
        public bool Supporting => State != LegState.Return;

        /// <summary>Frontness the current step started from (a step can start mid-return).</summary>
        float _stepFrom = 1f;

        /// <summary>
        /// Where the foot is at <paramref name="time"/>, purely from the state timers:
        /// 0 = back point, 1 = front point. Ready / Grounded = 1, Hold = 0,
        /// Return rises 0 → 1 over returnDuration, Step falls from its start point to 0 over stepDuration.
        /// </summary>
        public float Frontness(double time, float stepDuration, float returnDuration)
        {
            float t = (float)(time - StateTime);
            switch (State)
            {
                case LegState.Step: return _stepFrom * (1f - Clamp01(t / System.Math.Max(0.001f, stepDuration)));
                case LegState.Hold: return 0f;
                case LegState.Return: return Clamp01(t / System.Math.Max(0.001f, returnDuration));
                default: return 1f;
            }
        }

        public void StartStep(double now, float fromFrontness)
        {
            Held = true;
            _stepFrom = Clamp01(fromFrontness);
            Enter(LegState.Step, now);
            Steps++;
        }

        public void Release(double time)
        {
            Held = false;
            if (State == LegState.Hold)
                Enter(LegState.Return, System.Math.Max(time, StateTime));
        }

        public void Tick(double now, float stepDuration, float returnDuration)
        {
            if (State == LegState.Step && now >= StateTime + stepDuration)
                Enter(Held ? LegState.Hold : LegState.Return, StateTime + stepDuration);
            if (State == LegState.Return && now >= StateTime + returnDuration)
                Enter(LegState.Ready, StateTime + returnDuration);
        }

        public void SetGrounded(double now)
        {
            Held = false;
            Enter(LegState.Grounded, now);
        }

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
