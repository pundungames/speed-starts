namespace SpeedStars.Core.Race.Relay
{
    /// <summary>How a baton exchange went, as the banner shows it.</summary>
    public enum HandoffQuality
    {
        Perfect,
        Late
    }

    /// <summary>
    /// The baton exchange of a relay, measured from the reference game (2026-11-03, 4x100m): the next runner waits a zone length
    /// before the leg line, starts on its own when the incoming runner is a few metres behind and never runs away from it (it keeps
    /// below the incoming speed until the baton arrives). A swipe up reaches the baton over to it (perfect handoff); without one
    /// the runners meet and the baton passes anyway (late handoff). Pure; distances are along the track from the race start.
    /// </summary>
    public static class RelayRules
    {
        /// <summary>Where a leg's runner stands at the start: the race start for the first leg, the zone start for the others.</summary>
        public static float LegStartMeters(int leg, float legMeters, float zoneBeforeLineMeters) =>
            leg <= 0 ? 0f : leg * legMeters - zoneBeforeLineMeters;

        /// <summary>True = the waiting runner sets off now: the incoming runner is this close (or closer).</summary>
        public static bool ShouldStart(float gapMeters, float startGapMeters) => gapMeters <= startGapMeters;

        /// <summary>True = the baton changes hands now: the incoming runner reached the next one.</summary>
        public static bool ShouldExchange(float gapMeters, float reachMeters) => gapMeters <= reachMeters;

        /// <summary>
        /// True = the running next runner skips this press: it is already as fast as the incoming runner minus the margin, so the
        /// gap would stop closing. Without it a slow incoming runner could never hand over.
        /// </summary>
        public static bool ShouldHoldBack(float outgoingSpeed, float incomingSpeed, float marginMetersPerSecond) =>
            outgoingSpeed >= incomingSpeed - marginMetersPerSecond;

        /// <summary>
        /// Time of a line crossing inside the frame: the baton was at <paramref name="prevMeters"/> at <paramref name="prevTime"/> and
        /// is at <paramref name="meters"/> at <paramref name="time"/>.
        /// </summary>
        public static double CrossingTime(float lineMeters, float prevMeters, double prevTime, float meters, double time)
        {
            float span = meters - prevMeters;
            double fraction = span > 0f ? (lineMeters - prevMeters) / span : 1.0;
            if (fraction < 0.0) fraction = 0.0;
            if (fraction > 1.0) fraction = 1.0;
            return prevTime + fraction * (time - prevTime);
        }
    }
}
