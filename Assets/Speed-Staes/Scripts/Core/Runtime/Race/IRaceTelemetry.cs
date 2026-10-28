namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Live numbers the race HUD shows (UI v2: speedometer, energy bar, cadence bar). Read-only and display-only: the HUD
    /// never feeds anything back into the run. Implemented by the Bridge adapter over the runner's sim.
    /// </summary>
    public interface IRaceTelemetry
    {
        /// <summary>Forward speed in metres per second.</summary>
        float SpeedMetersPerSecond { get; }

        /// <summary>Remaining energy, 0 (empty) .. 1 (full).</summary>
        float Energy01 { get; }

        /// <summary>Key presses per second over a short recent window (cadence).</summary>
        float PressesPerSecond { get; }
    }
}
