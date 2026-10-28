using System;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// The single seam between our game flow and the runner system (Malik's). We only read state and react to
    /// Started/Finished; the runner code never learns about tickets, rewards or UI. Until the runner exposes real
    /// start/finish events, a Dev-only adapter fulfils this.
    /// </summary>
    public interface IRaceSession
    {
        RaceSessionState State { get; }
        double RaceTime { get; }
        float Distance { get; }
        float TargetDistance { get; }

        /// <summary>Raised once per race, when the runner leaves the start.</summary>
        event Action Started;

        /// <summary>Raised exactly once per race with the values of the finish frame.</summary>
        event Action<RaceOutcome> Finished;

        /// <summary>Puts the runner at the start, Ready, input open.</summary>
        void RequestStart(RaceStartRequest request);
    }
}
