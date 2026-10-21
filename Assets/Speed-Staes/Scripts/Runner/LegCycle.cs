namespace SpeedStars.Runner
{
    public enum LegState
    {
        /// <summary>Standing still (start / stopped).</summary>
        Grounded,
        /// <summary>Running, no stride in progress: both feet together under the body (Mid), on the ground.</summary>
        Ready,
        /// <summary>Pressed: pushes Mid → Back on the ground.</summary>
        Drive,
        /// <summary>Still held after the drive: the foot stays at Back.</summary>
        Hold,
        /// <summary>Released / drive done: swings Back → Mid and lands under the body.</summary>
        Recover,
        /// <summary>The other leg of a press: swings Mid → Front.</summary>
        Reach,
        /// <summary>Reached Front while the other foot is held back: waits there on the ground.</summary>
        FrontWait,
        /// <summary>Landed at Front: comes back to Mid on the ground as the body passes over it.</summary>
        Land,
    }

    /// <summary>
    /// Logical cycle of one leg, purely time based. No rendering, no IK, no speed:
    /// the step controller drives it, the rig visual reads it.
    /// Press → Drive (Mid → Back). Still held → Hold at Back, released → Recover (Back → Mid).
    /// The other leg Reaches (Mid → Front), waits at Front while this one is held, then Lands (Front → Mid).
    /// A quick press-release is one stride: apart and together again in strideDuration.
    /// A press always starts a drive from wherever the foot currently is.
    /// </summary>
    public sealed class LegCycle
    {
        public LegState State { get; private set; } = LegState.Grounded;
        /// <summary>Game time the current state was entered.</summary>
        public double StateTime { get; private set; }
        public long Steps { get; private set; }
        public bool Held { get; private set; }

        /// <summary>Foot is (or will stay) held back: holding keeps the other foot waiting at Front.</summary>
        public bool Holding => State == LegState.Hold || (State == LegState.Drive && Held);

        /// <summary>
        /// How far the current stride has come at <paramref name="time"/>: 0 = just started,
        /// strideSplit = feet fully apart, 1 = together again (or waiting: Ready, Hold, FrontWait).
        /// </summary>
        public float Progress(double time, float strideDuration, float strideSplit)
        {
            float push = strideSplit * strideDuration, rest = (1f - strideSplit) * strideDuration;
            switch (State)
            {
                case LegState.Drive:
                case LegState.Reach:
                    return strideSplit * Fraction(time, push);
                case LegState.Recover:
                case LegState.Land:
                    return strideSplit + (1f - strideSplit) * Fraction(time, rest);
                default:
                    return 1f;
            }
        }

        public void Press(double time)
        {
            Held = true;
            Steps++;
            Enter(LegState.Drive, time);
        }

        public void Release(double time)
        {
            Held = false;
            if (State == LegState.Hold)
                Enter(LegState.Recover, System.Math.Max(time, StateTime));
        }

        public void StartReach(double time) => Enter(LegState.Reach, time);

        public void Tick(double now, float strideDuration, float strideSplit, bool partnerHolding)
        {
            double push = strideSplit * strideDuration, rest = (1f - strideSplit) * strideDuration;
            // While the other foot is held back this one belongs at Front, e.g. after its own release
            // from a both-held prone: head there from wherever it is.
            if (partnerHolding && (State == LegState.Recover || State == LegState.Land || State == LegState.Ready))
                Enter(LegState.Reach, System.Math.Max(now, StateTime));
            if (State == LegState.Drive && now >= StateTime + push)
                Enter(Held ? LegState.Hold : LegState.Recover, StateTime + push);
            if (State == LegState.Reach && now >= StateTime + push)
                Enter(partnerHolding ? LegState.FrontWait : LegState.Land, StateTime + push);
            if (State == LegState.FrontWait && !partnerHolding)
                Enter(LegState.Land, System.Math.Max(now, StateTime));
            if ((State == LegState.Recover || State == LegState.Land) && now >= StateTime + rest)
                Enter(LegState.Ready, StateTime + rest);
        }

        public void SetGrounded(double now)
        {
            Held = false;
            Enter(LegState.Grounded, now);
        }

        float Fraction(double time, double duration)
        {
            double f = (time - StateTime) / System.Math.Max(0.001, duration);
            return f < 0 ? 0f : f > 1 ? 1f : (float)f;
        }

        void Enter(LegState state, double time)
        {
            State = state;
            StateTime = time;
        }
    }
}
