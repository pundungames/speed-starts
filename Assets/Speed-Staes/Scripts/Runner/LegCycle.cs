namespace SpeedStars.Runner
{
    public enum LegState
    {
        /// <summary>Standing still (start / stopped).</summary>
        Grounded,
        /// <summary>Key up: foot on the Mid point (feet together, on the ground), waiting.</summary>
        Ready,
        /// <summary>Pressed: foot travels to the back point over Step.</summary>
        Step,
        /// <summary>Still held after the step: foot stays on the back point.</summary>
        Hold,
        /// <summary>The other foot was pressed: this one travels to the front point over Reach (arrives first).</summary>
        Reach,
        /// <summary>At the front point, on the ground, until the other foot is back at Mid.</summary>
        FrontWait,
        /// <summary>Back to Mid: from back over Return, from front over FrontReturn.</summary>
        Return,
    }

    /// <summary>Fixed move durations (seconds), tuned by hand on the step controller.</summary>
    public struct LegTimings
    {
        /// <summary>Pressed foot → back.</summary>
        public float Step;
        /// <summary>Other foot → front (shorter: arrives before the pressed foot is back).</summary>
        public float Reach;
        /// <summary>Back → Mid after release.</summary>
        public float Return;
        /// <summary>Front → Mid, once the other foot is at Mid.</summary>
        public float FrontReturn;
    }

    /// <summary>
    /// Logical step cycle of one leg, purely time based. No rendering, no IK, no speed:
    /// the step controller ticks it, the rig visual reads it.
    /// The legs move as a pair: Press → Step (to back) while the other foot Reaches (to front, arriving
    /// first). Held → Hold at back. Release → the back foot Returns to Mid while the front one waits
    /// (FrontWait); once the back foot is at Mid, the front one Returns to Mid too.
    /// A quick press-release still completes the step. A press starts from wherever the foot is.
    /// </summary>
    public sealed class LegCycle
    {
        public LegState State { get; private set; } = LegState.Grounded;
        /// <summary>Game time the current state was entered.</summary>
        public double StateTime { get; private set; }
        public long Steps { get; private set; }
        public bool Held { get; private set; }
        /// <summary>Foot is held back (or will stay back): pressing the other keeps both back.</summary>
        public bool Holding => State == LegState.Hold || (State == LegState.Step && Held);
        /// <summary>Foot is resting at Mid: the other foot may come back from the front.</summary>
        public bool AtMid => State == LegState.Ready || State == LegState.Grounded;
        /// <summary>The current Return started ahead of Mid (front → Mid on the ground).</summary>
        public bool FromFront => _from > Mid;
        /// <summary>Foot is on the ground carrying weight (not reaching forward, not swinging back to Mid).</summary>
        public bool Supporting => State != LegState.Reach && !(State == LegState.Return && !FromFront);

        public const float Mid = 0.5f;

        /// <summary>Frontness the current move started from (a move can start anywhere).</summary>
        float _from = Mid;

        /// <summary>
        /// Where the foot is at <paramref name="time"/>, purely from the state timers:
        /// 0 = back point, 0.5 = Mid, 1 = front point.
        /// </summary>
        public float Frontness(double time, LegTimings d)
        {
            float t = (float)(time - StateTime);
            switch (State)
            {
                case LegState.Step: return Lerp(_from, 0f, t / Safe(d.Step));
                case LegState.Hold: return 0f;
                case LegState.Reach: return Lerp(_from, 1f, t / Safe(d.Reach));
                case LegState.FrontWait: return 1f;
                case LegState.Return: return Lerp(_from, Mid, t / Safe(ReturnDuration(d)));
                default: return Mid;
            }
        }

        /// <summary>Duration of the current Return: from back or from front.</summary>
        public float ReturnDuration(LegTimings d) => FromFront ? d.FrontReturn : d.Return;

        public void Press(double time, LegTimings d)
        {
            Held = true;
            Move(LegState.Step, time, d);
            Steps++;
        }

        /// <summary>The other foot was pressed: come forward.</summary>
        public void StartReach(double time, LegTimings d) => Move(LegState.Reach, time, d);

        /// <summary>Key released: a held foot returns to Mid now (a step in progress finishes first).</summary>
        public void Release(double time, LegTimings d)
        {
            Held = false;
            if (State == LegState.Hold)
                Move(LegState.Return, System.Math.Max(time, StateTime), d);
        }

        public void Tick(double now, LegTimings d, LegCycle partner)
        {
            if (State == LegState.Step && now >= StateTime + d.Step)
                Move(Held ? LegState.Hold : LegState.Return, StateTime + d.Step, d);
            if (State == LegState.Reach && now >= StateTime + d.Reach)
                Move(LegState.FrontWait, StateTime + d.Reach, d);
            // The front foot comes back only once the other foot is at Mid.
            if (State == LegState.FrontWait && partner.AtMid)
                Move(LegState.Return, System.Math.Max(StateTime, partner.StateTime), d);
            if (State == LegState.Return && now >= StateTime + ReturnDuration(d))
                Move(LegState.Ready, StateTime + ReturnDuration(d), d);
        }

        public void SetGrounded(double now)
        {
            Held = false;
            _from = Mid;
            Enter(LegState.Grounded, now);
        }

        void Move(LegState state, double time, LegTimings d)
        {
            _from = Frontness(time, d);
            Enter(state, time);
        }

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }

        static float Safe(float duration) => duration > 0.001f ? duration : 0.001f;
        static float Lerp(float a, float b, float t) => a + (b - a) * (t < 0f ? 0f : t > 1f ? 1f : t);
    }
}
