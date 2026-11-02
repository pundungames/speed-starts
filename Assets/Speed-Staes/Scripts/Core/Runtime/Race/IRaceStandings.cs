using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race
{
    /// <summary>One runner of the current race (the player or an opponent) as the HUD and the results list show it.</summary>
    public readonly struct RaceEntry
    {
        public readonly string Name;
        /// <summary>ISO 3166-1 alpha-2 ("TR"); empty = no flag.</summary>
        public readonly string CountryCode;
        public readonly bool IsPlayer;
        public readonly float DistanceMeters;
        public readonly bool Finished;
        /// <summary>Race time from GO; meaningful only when <see cref="Finished"/>.</summary>
        public readonly double TimeSeconds;

        public RaceEntry(string name, string countryCode, bool isPlayer, float distanceMeters, bool finished, double timeSeconds)
        {
            Name = name ?? string.Empty;
            CountryCode = countryCode ?? string.Empty;
            IsPlayer = isPlayer;
            DistanceMeters = distanceMeters;
            Finished = finished;
            TimeSeconds = timeSeconds;
        }
    }

    /// <summary>
    /// Live order of every runner in the current race, player included. Opponents are run by the race scene (T-075);
    /// the HUD shows the player's place and the results screen lists everyone (T-076). Order: finished runners by time,
    /// then runners still on the track by distance. A restart clears it back to the start line.
    /// </summary>
    public interface IRaceStandings
    {
        /// <summary>Sorted, index 0 = leader. Includes the player.</summary>
        IReadOnlyList<RaceEntry> Entries { get; }

        /// <summary>The player's place, 1-based.</summary>
        int PlayerRank { get; }

        int RunnerCount { get; }

        /// <summary>Raised when the order changes or someone finishes (not every frame).</summary>
        event Action Changed;
    }
}
