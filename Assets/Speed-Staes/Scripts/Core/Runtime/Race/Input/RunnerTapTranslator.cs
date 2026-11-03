namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Pure rules for a key that feeds the runner's press queue (T-081): the gate and the time conversion.
    /// Kept free of Input System and the runner so it is testable; the Bridge only connects the real sources.
    /// (Namespace stays SpeedStars.Core.Race: a namespace named Input would shadow UnityEngine.Input.)
    /// </summary>
    public static class RunnerTapTranslator
    {
        /// <summary>
        /// A press needs the runner to accept input (not before GO, not after the finish); a release always passes
        /// so a foot never stays held down.
        /// </summary>
        public static bool ShouldForward(bool pressed, bool runnerAcceptsInput) => !pressed || runnerAcceptsInput;

        /// <summary>
        /// Input System event times are on the realtimeSinceStartup timeline; move one onto game time. The event's age is
        /// never negative, so a clock hiccup cannot put a tap in the future.
        /// </summary>
        public static double ToGameTime(double eventRealtime, double nowRealtime, double nowGameTime)
        {
            double age = nowRealtime - eventRealtime;
            if (age < 0.0) age = 0.0;
            return nowGameTime - age;
        }
    }
}
