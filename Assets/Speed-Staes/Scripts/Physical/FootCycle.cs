namespace SpeedStars.Physical
{
    public enum FootState
    {
        /// <summary>Just reset: settling onto home.</summary>
        Grounded,
        /// <summary>Home (Rest), on the ground, waiting.</summary>
        Ready,
        /// <summary>Pressed: going back to Back along the lower arc.</summary>
        Step,
        /// <summary>Still held after the step: stays at Back.</summary>
        Hold,
        /// <summary>The other foot was pressed: coming forward to Front through the air.</summary>
        Reach,
        /// <summary>At Front, in the air, while the other foot is still pushing or held back.</summary>
        FrontWait,
        /// <summary>No key down: going home (from ahead along the ground, from behind through the air).</summary>
        Return,
    }

    /// <summary>
    /// Visual step state of one foot, purely time based. Unlike the old LegCycle every move has its own
    /// duration, which RunnerPoseDriver sets from the distance to travel and a speed: feet moving at the
    /// same speed arrive in order of distance, nobody waits a fixed time. The pair rules (which foot does
    /// what when) live in the driver.
    /// </summary>
    public sealed class FootCycle
    {
        public FootState State { get; private set; } = FootState.Grounded;
        /// <summary>Sim time the current state was entered.</summary>
        public double StateTime { get; private set; }
        /// <summary>Length of the current move (s); 0 for the waiting states.</summary>
        public float Duration { get; private set; }
        /// <summary>The key of this foot is down.</summary>
        public bool Held;
        /// <summary>The current Return started ahead of home: it comes back along the ground.</summary>
        public bool FromFront { get; private set; }

        public double EndTime => StateTime + Duration;
        public bool Moving => State == FootState.Step || State == FootState.Reach || State == FootState.Return;
        /// <summary>Held back, or will stay back once its step ends.</summary>
        public bool Holding => State == FootState.Hold || (State == FootState.Step && Held);
        public bool AtHome => State == FootState.Ready || State == FootState.Grounded;

        public void Enter(FootState state, double time, float duration = 0f, bool fromFront = false)
        {
            State = state;
            StateTime = time;
            Duration = duration;
            FromFront = fromFront;
        }
    }
}
