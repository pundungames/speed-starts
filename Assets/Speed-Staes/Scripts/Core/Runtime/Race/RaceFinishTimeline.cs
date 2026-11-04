using UnityEngine;

namespace SpeedStars.Core.Race
{
    /// <summary>Where the finish sequence is. Holding = the Results may be up; the runner stays where it stopped until the next race.</summary>
    public enum RaceFinishPhase
    {
        Idle,
        SlowMotion,
        Slowdown,
        Celebrate,
        Holding
    }

    /// <summary>
    /// When things happen in the finish sequence, without any Unity object: slow motion at the line, the runner easing off, the emote once
    /// it is slow, the Results when the emote has been seen. The Bridge component only asks and acts. Real time throughout, so the slow
    /// motion cannot stretch its own length. A tap skips to the Results (not while the finger may still be mashing at the line).
    /// </summary>
    public sealed class RaceFinishTimeline
    {
        private readonly RaceFinishConfig _config;
        private float _elapsed;
        private float _celebrateLeft;
        private bool _emoteAsked;
        private bool _emoteDue;
        private bool _resultsDue;

        public RaceFinishTimeline(RaceFinishConfig config) => _config = config;

        public RaceFinishPhase Phase { get; private set; }

        /// <summary>The game speed wanted now: slow right after the line, then back to normal; normal at once when skipped.</summary>
        public float TimeScale
        {
            get
            {
                if (Phase == RaceFinishPhase.Idle || Phase == RaceFinishPhase.Holding) return 1f;
                float back = (_elapsed - _config.SlowMotionSeconds) / Mathf.Max(0.0001f, _config.SlowMotionReturnSeconds);
                return Mathf.Lerp(_config.SlowMotionScale, 1f, Mathf.SmoothStep(0f, 1f, back));
            }
        }

        /// <summary>How far the camera has swung (0 = where it was at the line, 1 = its place), eased; it waits for the slow motion.</summary>
        public float CameraProgress =>
            Mathf.SmoothStep(0f, 1f, (_elapsed - _config.SlowMotionSeconds) / Mathf.Max(0.0001f, _config.CameraMoveSeconds));

        public void Begin()
        {
            Phase = RaceFinishPhase.SlowMotion;
            _elapsed = 0f;
            _celebrateLeft = 0f;
            _emoteAsked = false;
            _emoteDue = false;
            _resultsDue = false;
        }

        public void Tick(float realSeconds, float runnerSpeed)
        {
            if (Phase == RaceFinishPhase.Idle) return;
            _elapsed += realSeconds;

            if (Phase == RaceFinishPhase.SlowMotion && _elapsed >= _config.SlowMotionSeconds) Phase = RaceFinishPhase.Slowdown;

            // Also after a skip: the Results are up, the runner still gets its emote when it has stopped.
            bool slowEnough = runnerSpeed <= _config.EmoteStartSpeed || _elapsed >= _config.SlowMotionSeconds + _config.SlowdownMaxSeconds;
            if (!_emoteAsked && Phase != RaceFinishPhase.SlowMotion && slowEnough)
            {
                _emoteAsked = true;
                _emoteDue = true;
                if (Phase == RaceFinishPhase.Slowdown)
                {
                    Phase = RaceFinishPhase.Celebrate;
                    _celebrateLeft = _config.NoEmoteSeconds;
                }
                return; // the wait starts when the sequence has answered EmoteStarted
            }

            if (Phase != RaceFinishPhase.Celebrate) return;
            _celebrateLeft -= realSeconds;
            if (_celebrateLeft <= 0f) OpenResults();
        }

        /// <summary>True once when the emote should start now.</summary>
        public bool TakeEmoteDue()
        {
            bool due = _emoteDue;
            _emoteDue = false;
            return due;
        }

        /// <summary>The emote started and lasts this long (0 = none played): the Results wait for it, up to the config's limit.</summary>
        public void EmoteStarted(float seconds)
        {
            if (Phase == RaceFinishPhase.Celebrate && seconds > 0f) _celebrateLeft = Mathf.Min(seconds, _config.MaxEmoteWaitSeconds);
        }

        /// <summary>True once when the Results should open now.</summary>
        public bool TakeResultsDue()
        {
            bool due = _resultsDue;
            _resultsDue = false;
            return due;
        }

        /// <summary>A tap: to the Results at once. Ignored while it may still be the finger that was mashing at the line.</summary>
        public void Skip()
        {
            if (Phase == RaceFinishPhase.Idle || Phase == RaceFinishPhase.Holding || _elapsed < _config.SkipLockSeconds) return;
            OpenResults();
        }

        public void Stop() => Phase = RaceFinishPhase.Idle;

        private void OpenResults()
        {
            Phase = RaceFinishPhase.Holding;
            _resultsDue = true;
        }
    }
}
