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

        double _readyAt;

        /// <summary>Exact game time the foot is (or will be) back on the front point; infinite while held.</summary>
        public double ReadyTime(float stepDuration, float returnDuration)
        {
            switch (State)
            {
                case LegState.Step:
                    return Held ? double.PositiveInfinity : StateTime + stepDuration + returnDuration;
                case LegState.Hold: return double.PositiveInfinity;
                case LegState.Return: return StateTime + returnDuration;
                default: return _readyAt;
            }
        }

        public void StartStep(double now)
        {
            Held = true;
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
            {
                _readyAt = StateTime + returnDuration;
                Enter(LegState.Ready, _readyAt);
            }
        }

        public void SetGrounded(double now)
        {
            Held = false;
            Enter(LegState.Grounded, now);
            _readyAt = now;
        }

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }
    }
}
