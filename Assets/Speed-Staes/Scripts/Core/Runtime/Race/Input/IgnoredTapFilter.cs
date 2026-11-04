namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Which presses and releases of the runner's queue must not count: those of a finger that came down on a button, because a
    /// tap on the UI is not a step. Counted per leg, not per finger, because the queue only knows legs: the Bridge marks them as
    /// they happen and asks as it drains. Free of Input System and the runner so it is testable.
    /// </summary>
    public sealed class IgnoredTapFilter
    {
        private const int LegCount = 2;

        private readonly int[] _presses = new int[LegCount];
        private readonly int[] _releases = new int[LegCount];

        public void IgnorePress(int leg) => _presses[leg]++;

        public void IgnoreRelease(int leg) => _releases[leg]++;

        /// <summary>True when this event is one of the marked ones; the mark is used up.</summary>
        public bool ShouldDrop(int leg, bool pressed)
        {
            int[] marks = pressed ? _presses : _releases;
            if (marks[leg] <= 0) return false;
            marks[leg]--;
            return true;
        }

        /// <summary>The queue is drained: a mark still here lost its event (a reset cleared the queue) and must not eat a later step.</summary>
        public void Clear()
        {
            for (int leg = 0; leg < LegCount; leg++)
            {
                _presses[leg] = 0;
                _releases[leg] = 0;
            }
        }
    }
}
