namespace SpeedStars.Runner
{
    public enum LegState
    {
        /// <summary>Standing still (start / stopped).</summary>
        Grounded,
        /// <summary>Key up: foot on the Mid point (feet together, on the ground), waiting.</summary>
        Ready,
        /// <summary>Pressed: foot travels to the back point over stepDuration.</summary>
        Step,
        /// <summary>Still held after the step: foot stays on the back point.</summary>
        Hold,
        /// <summary>The other foot was pressed: this one travels to the front point over stepDuration.</summary>
        Reach,
        /// <summary>Reached the front point while the other foot is held back: waits there.</summary>
        FrontWait,
        /// <summary>Released (or the other foot released): returns to Mid over returnDuration.</summary>
        Return,
    }

    /// <summary>
    /// Logical step cycle of one leg, purely time based. No rendering, no IK, no speed:
    /// the step controller ticks it, the rig visual reads it.
    /// The legs move as a pair: Press → Step (to back) while the other foot Reaches (to front), both in
    /// stepDuration so they arrive together. Held → Hold at back, the other waits at front.
    /// Release → both Return to Mid in returnDuration. A quick press-release still completes the step.
    /// A press always starts a step from wherever the foot currently is.
    /// </summary>
    public sealed class LegCycle
    {
        public LegState State { get; private set; } = LegState.Grounded;
        /// <summary>Game time the current state was entered.</summary>
        public double StateTime { get; private set; }
        public long Steps { get; private set; }
        public bool Held { get; private set; }
        /// <summary>Foot is held back (or will stay back): the other foot waits at front meanwhile.</summary>
        public bool Holding => State == LegState.Hold || (State == LegState.Step && Held);
        /// <summary>Foot is on the ground carrying weight (not reaching forward, not swinging back to Mid).</summary>
        public bool Supporting => State != LegState.Reach && !(State == LegState.Return && _from < Mid);

        public const float Mid = 0.5f;

        /// <summary>Frontness the current move started from (a move can start anywhere).</summary>
        float _from = Mid;

        /// <summary>
        /// Where the foot is at <paramref name="time"/>, purely from the state timers:
        /// 0 = back point, 0.5 = Mid, 1 = front point.
        /// </summary>
        public float Frontness(double time, float stepDuration, float returnDuration)
        {
            float t = (float)(time - StateTime);
            float step = System.Math.Max(0.001f, stepDuration), ret = System.Math.Max(0.001f, returnDuration);
            switch (State)
            {
                case LegState.Step: return Lerp(_from, 0f, t / step);
                case LegState.Hold: return 0f;
                case LegState.Reach: return Lerp(_from, 1f, t / step);
                case LegState.FrontWait: return 1f;
                case LegState.Return: return Lerp(_from, Mid, t / ret);
                default: return Mid;
            }
        }

        public void Press(double time, float stepDuration, float returnDuration)
        {
            Held = true;
            Move(LegState.Step, time, stepDuration, returnDuration);
            Steps++;
        }

        /// <summary>The other foot was pressed: come forward.</summary>
        public void StartReach(double time, float stepDuration, float returnDuration) =>
            Move(LegState.Reach, time, stepDuration, returnDuration);

        /// <summary>Key released: a held foot returns to Mid now (a step in progress finishes first).</summary>
        public void Release(double time, float stepDuration, float returnDuration)
        {
            Held = false;
            if (State == LegState.Hold)
                Move(LegState.Return, System.Math.Max(time, StateTime), stepDuration, returnDuration);
        }

        /// <summary>The other foot let go: a foot waiting at front returns to Mid with it.</summary>
        public void PartnerReleased(double time, float stepDuration, float returnDuration)
        {
            if (State == LegState.FrontWait)
                Move(LegState.Return, System.Math.Max(time, StateTime), stepDuration, returnDuration);
        }

        public void Tick(double now, float stepDuration, float returnDuration, bool partnerHolding)
        {
            if (State == LegState.Step && now >= StateTime + stepDuration)
                Move(Held ? LegState.Hold : LegState.Return, StateTime + stepDuration, stepDuration, returnDuration);
            if (State == LegState.Reach && now >= StateTime + stepDuration)
                Move(partnerHolding ? LegState.FrontWait : LegState.Return, StateTime + stepDuration, stepDuration, returnDuration);
            if (State == LegState.Return && now >= StateTime + returnDuration)
                Move(LegState.Ready, StateTime + returnDuration, stepDuration, returnDuration);
        }

        public void SetGrounded(double now)
        {
            Held = false;
            _from = Mid;
            Enter(LegState.Grounded, now);
        }

        void Move(LegState state, double time, float stepDuration, float returnDuration)
        {
            _from = Frontness(time, stepDuration, returnDuration);
            Enter(state, time);
        }

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0f ? 0f : t > 1f ? 1f : t);
    }
}
