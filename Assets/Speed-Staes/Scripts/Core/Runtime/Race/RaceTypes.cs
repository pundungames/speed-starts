using System;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Identifies a race event (100m, 400m, 60mH ...). The id string comes from the event catalog asset,
    /// so code never hard-codes which events exist.
    /// </summary>
    public readonly struct RaceEventId : IEquatable<RaceEventId>
    {
        public readonly string Value;

        public RaceEventId(string value) => Value = value ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(RaceEventId other) => string.Equals(Value ?? string.Empty, other.Value ?? string.Empty, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RaceEventId other && Equals(other);
        public override int GetHashCode() => (Value ?? string.Empty).GetHashCode();
        public override string ToString() => Value ?? string.Empty;

        public static bool operator ==(RaceEventId a, RaceEventId b) => a.Equals(b);
        public static bool operator !=(RaceEventId a, RaceEventId b) => !a.Equals(b);
    }

    public enum RaceSessionState
    {
        Ready,
        Running,
        Finished
    }

    /// <summary>What the meta side asks the race to be: which event and how long. Tickets are settled before this.</summary>
    public readonly struct RaceStartRequest
    {
        public readonly RaceEventId EventId;
        public readonly float DistanceMeters;

        public RaceStartRequest(RaceEventId eventId, float distanceMeters)
        {
            EventId = eventId;
            DistanceMeters = distanceMeters;
        }
    }

    /// <summary>Frozen at the finish frame; the only input for results, best times, rank rewards and leaderboards.</summary>
    public readonly struct RaceOutcome
    {
        public readonly RaceEventId EventId;
        public readonly double TimeSeconds;
        public readonly float DistanceMeters;
        public readonly int Rank;
        public readonly int RunnerCount;

        public RaceOutcome(RaceEventId eventId, double timeSeconds, float distanceMeters, int rank, int runnerCount)
        {
            EventId = eventId;
            TimeSeconds = timeSeconds;
            DistanceMeters = distanceMeters;
            Rank = rank;
            RunnerCount = runnerCount;
        }
    }
}
