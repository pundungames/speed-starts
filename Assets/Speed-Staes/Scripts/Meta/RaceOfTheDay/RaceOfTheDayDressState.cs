namespace SpeedStars.Meta.DailyRace
{
    /// <summary>
    /// When the day's outfit goes on. A round starts with READY; the opponents are dressed with a random look at that same moment
    /// (RaceBots), so the day's outfit is applied once, later in the frame, after theirs, and only when the opponents' runners exist.
    /// Pure, so the order of these steps is testable without a scene.
    /// </summary>
    public sealed class RaceOfTheDayDressState
    {
        private bool _pending;

        /// <summary>True from READY until the outfit was applied (or the round turned out not to be a Race of the Day).</summary>
        public bool Pending => _pending;

        /// <summary>READY: a new round begins, everybody has to be dressed again.</summary>
        public void RoundStarted() => _pending = true;

        /// <summary>
        /// Asked once per frame (after the opponents were dressed). True = apply the day's outfit now. A round of another event
        /// clears the pending state without applying; runners not built yet keep it pending.
        /// </summary>
        public bool TakeApply(bool isRaceOfTheDay, bool runnersBuilt)
        {
            if (!_pending) return false;
            if (!isRaceOfTheDay)
            {
                _pending = false;
                return false;
            }
            if (!runnersBuilt) return false;
            _pending = false;
            return true;
        }
    }
}
