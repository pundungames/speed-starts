namespace SpeedStars.Core.Audio
{
    /// <summary>
    /// Which sound a race event plays (T-120). Pure, so the choices are tested without the engine; the views and the step
    /// listener only ask here and hand the answer to <see cref="ISfx"/>.
    /// </summary>
    public static class RaceSfxRules
    {
        public const int LeftLeg = 0;
        public const int RightLeg = 1;

        /// <summary>Left foot (leg 0) = StepLeft, anything else = StepRight.</summary>
        public static SfxId StepFor(int leg) => leg == LeftLeg ? SfxId.StepLeft : SfxId.StepRight;

        /// <summary>First place with opponents = FinishFirst; every other finish (and a solo run) = FinishRace.</summary>
        public static SfxId FinishFor(int rank, int runnerCount) => rank == 1 && runnerCount > 1 ? SfxId.FinishFirst : SfxId.FinishRace;

        /// <summary>
        /// A step is heard only when it is the player's own running step: a press (not a release), the race is open (GO has
        /// come and the line is not crossed) and the runner is not falling after a false start. Bots, the replay and the
        /// cool-down steps after the line never reach it as "true".
        /// </summary>
        public static bool StepAudible(bool pressed, bool raceOpen, bool failing) => pressed && raceOpen && !failing;
    }
}
