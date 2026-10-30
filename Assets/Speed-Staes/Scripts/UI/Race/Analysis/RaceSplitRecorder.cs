using System;
using System.Collections.Generic;

namespace SpeedStars.UI.Race
{
    /// <summary>One recorded point of a run: cumulative time at a distance.</summary>
    public readonly struct SplitPoint
    {
        public readonly float Distance;
        public readonly double Seconds;

        public SplitPoint(float distance, double seconds)
        {
            Distance = distance;
            Seconds = seconds;
        }
    }

    /// <summary>The recorded points of one runner, ordered by distance. Finished = the runner reached the finish line.</summary>
    public sealed class SplitSeries
    {
        private static readonly SplitPoint[] NoPoints = new SplitPoint[0];

        public SplitSeries(int runnerId, IReadOnlyList<SplitPoint> points, bool finished, double finishSeconds)
        {
            RunnerId = runnerId;
            Points = points ?? NoPoints;
            Finished = finished;
            FinishSeconds = finished ? finishSeconds : 0d; // a runner that did not finish has no finish time (never invented)
        }

        public int RunnerId { get; }
        public IReadOnlyList<SplitPoint> Points { get; }
        public bool Finished { get; }
        public double FinishSeconds { get; }

        /// <summary>A line needs two points; fewer is an empty (or one-point) run that draws nothing.</summary>
        public bool HasLine => Points.Count >= 2;

        public SplitPoint Last => Points.Count > 0 ? Points[Points.Count - 1] : default(SplitPoint);
    }

    /// <summary>
    /// Collects the player's (distance, cumulative time) points during a race. Pure C# (the flow feeds it frame samples).
    /// A point is kept at every multiple of the step; frames that skip over several steps are filled by straight
    /// interpolation between the two real samples around them. The run starts at (0, 0); a finished run ends at exactly
    /// (event distance, finish time), so the last chart point equals the time on the results table. A run that did not
    /// reach the line ends at its last real sample and has no finish time.
    /// </summary>
    public sealed class RaceSplitRecorder
    {
        private const float Epsilon = 1e-4f;
        private const float MinStep = 0.01f;

        private readonly float _step;
        private readonly float _finishTolerance;
        private readonly List<SplitPoint> _points = new List<SplitPoint>();

        private float _target;
        private float _rawDistance;
        private double _rawSeconds;
        private float _nextBoundary;
        private bool _active;

        public RaceSplitRecorder(float stepMeters, float finishToleranceMeters)
        {
            _step = stepMeters < MinStep ? MinStep : stepMeters;
            _finishTolerance = finishToleranceMeters < 0f ? 0f : finishToleranceMeters;
        }

        public bool IsActive => _active;

        /// <summary>Number of points kept so far.</summary>
        public int Count => _points.Count;

        /// <summary>Starts a new run over <paramref name="targetDistance"/> metres; whatever was recorded before is dropped.</summary>
        public void Begin(float targetDistance)
        {
            _points.Clear();
            _target = targetDistance > 0f ? targetDistance : 0f;
            _rawDistance = 0f;
            _rawSeconds = 0d;
            _nextBoundary = _step;
            _points.Add(new SplitPoint(0f, 0d));
            _active = true;
        }

        /// <summary>
        /// A frame sample. Samples that do not move forward (distance not above the last one, time going back, bad numbers)
        /// are ignored, so a runner reset never writes a bogus point.
        /// </summary>
        public void Sample(float distance, double seconds)
        {
            if (!_active) return;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            if (distance <= _rawDistance + Epsilon || seconds < _rawSeconds) return;

            float limit = _target > 0f ? Math.Min(distance, _target) : distance;
            while (_nextBoundary <= limit + Epsilon)
            {
                double t = _rawSeconds + (seconds - _rawSeconds) * ((_nextBoundary - _rawDistance) / (double)(distance - _rawDistance));
                _points.Add(new SplitPoint(_nextBoundary, t));
                _nextBoundary += _step;
            }

            _rawDistance = distance;
            _rawSeconds = seconds;
        }

        /// <summary>
        /// Ends the run. Reaching the line (last real sample within the tolerance of the event distance) with a positive
        /// time finishes it at (event distance, <paramref name="finishSeconds"/>); anything else stays a not-finished run.
        /// </summary>
        public SplitSeries Finish(int runnerId, double finishSeconds)
        {
            bool reached = _active && _target > 0f && _rawDistance >= _target - _finishTolerance
                && finishSeconds > 0d && !double.IsNaN(finishSeconds) && !double.IsInfinity(finishSeconds);
            _active = false;

            var points = new List<SplitPoint>(_points);
            if (!reached) return new SplitSeries(runnerId, points, false, 0d);

            // The results time is the truth: points that ran past it come off, the line ends exactly on it.
            while (points.Count > 1 && (points[points.Count - 1].Seconds > finishSeconds || points[points.Count - 1].Distance >= _target - Epsilon))
                points.RemoveAt(points.Count - 1);
            points.Add(new SplitPoint(_target, finishSeconds));
            return new SplitSeries(runnerId, points, true, finishSeconds);
        }
    }
}
