using System;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// What happens on screen between the line and the Results: the race flow hands over here and is called back when the Results may
    /// open. A new race or leaving the scene stops the sequence, and the callback does not come then.
    /// </summary>
    public interface IRaceFinishSequence
    {
        /// <param name="outcome">The finished race.</param>
        /// <param name="onResultsDue">Runs once when the Results may open; null when none will (Practice).</param>
        void Play(RaceOutcome outcome, Action onResultsDue);
    }
}
