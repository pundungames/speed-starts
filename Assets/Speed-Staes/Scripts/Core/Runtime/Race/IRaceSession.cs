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

        /// <summary>
        /// Puts the runner at the start and begins the READY / SET / GO countdown (see <see cref="IRaceStartSignal"/>);
        /// runner input is closed until GO. Also the Restart path: a countdown in progress starts over.
        /// </summary>
        void RequestStart(RaceStartRequest request);
    }

    /// <summary>
    /// The authoritative race start signal, a side interface of <see cref="IRaceSession"/> (a session that has no countdown simply does not
    /// implement it; the project's .NET level has no default interface members). The session owns the countdown; views only react.
    /// Order of events per race: Ready, Set, Go. A restart or a cancel raises Ready again / Idle, so a view always drops the old state.
    /// </summary>
    public interface IRaceStartSignal
    {
        /// <summary>Current phase; a view that binds late reads it instead of waiting for the next event.</summary>
        RaceStartPhase StartPhase { get; }

        /// <summary>Raised on every phase change, after the runner's input gate already matches the phase (open from Go).</summary>
        event Action<RaceStartPhase> StartPhaseChanged;
    }
}
