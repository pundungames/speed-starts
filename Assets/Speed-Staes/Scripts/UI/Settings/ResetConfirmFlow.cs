namespace SpeedStars.UI.Settings
{
    /// <summary>What a reset row of the settings window wipes.</summary>
    public enum ResetKind
    {
        None,
        PersonalBests,
        AllProgress
    }

    /// <summary>Which question of a reset the confirmation window is asking.</summary>
    public enum ResetStep
    {
        Closed,
        First,
        Second
    }

    /// <summary>
    /// The confirmation steps of a reset (no UI, no game state). Personal bests: one question. All progress: two questions
    /// ("Are you sure?", then "This cannot be undone"); only the answer to the last one runs the reset. Cancel at any
    /// step ends it with nothing done.
    /// </summary>
    public sealed class ResetConfirmFlow
    {
        public ResetKind Kind { get; private set; }

        public ResetStep Step { get; private set; }

        public bool IsOpen => Step != ResetStep.Closed;

        /// <summary>True when confirming now runs the reset (the question on screen is the last one).</summary>
        public bool IsFinalStep => IsOpen && (Kind == ResetKind.PersonalBests || Step == ResetStep.Second);

        /// <summary>Opens the first question of a reset. <see cref="ResetKind.None"/> opens nothing.</summary>
        public void Begin(ResetKind kind)
        {
            Kind = kind;
            Step = kind == ResetKind.None ? ResetStep.Closed : ResetStep.First;
        }

        /// <summary>
        /// The player said yes. Returns the reset to run when that was the last question (the flow closes), else
        /// <see cref="ResetKind.None"/> and the flow moves to the next question. Closed flow: None.
        /// </summary>
        public ResetKind Confirm()
        {
            if (!IsOpen) return ResetKind.None;
            if (!IsFinalStep)
            {
                Step = ResetStep.Second;
                return ResetKind.None;
            }

            ResetKind done = Kind;
            Close();
            return done;
        }

        public void Cancel() => Close();

        private void Close()
        {
            Kind = ResetKind.None;
            Step = ResetStep.Closed;
        }
    }
}
