using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race
{
    /// <summary>A point of a runner's run: distance reached this long after GO.</summary>
    public readonly struct RaceTraceSample
    {
        public readonly float DistanceMeters;
        public readonly double Seconds;

        public RaceTraceSample(float distanceMeters, double seconds)
        {
            DistanceMeters = distanceMeters;
            Seconds = seconds;
        }
    }

    /// <summary>One opponent's run so far.</summary>
    public readonly struct RaceTrace
    {
        public readonly string Name;
        public readonly IReadOnlyList<RaceTraceSample> Samples;
        public readonly bool Finished;
        /// <summary>Race time from GO; meaningful only when <see cref="Finished"/>.</summary>
        public readonly double FinishSeconds;

        public RaceTrace(string name, IReadOnlyList<RaceTraceSample> samples, bool finished, double finishSeconds)
        {
            Name = name ?? string.Empty;
            Samples = samples;
            Finished = finished;
            FinishSeconds = finishSeconds;
        }
    }

    /// <summary>The opponents' runs of the current race, for the Analysis screen.</summary>
    public interface IRaceTraces
    {
        IReadOnlyList<RaceTrace> Traces { get; }

        /// <summary>Raised when an opponent crosses the line.</summary>
        event Action TracesChanged;
    }
}
