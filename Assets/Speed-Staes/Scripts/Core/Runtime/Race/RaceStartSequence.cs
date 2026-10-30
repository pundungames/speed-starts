using System;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// The timing of the race start as pure C#: Ready for a fixed time, Set for a random wait (the runners must not be able to
    /// time GO), then Go. The caller feeds frame time through <see cref="Tick"/>; the randomness comes from outside so a test can
    /// fix it. Phase events fire in order even when one big frame crosses several limits (nothing is skipped, nothing is delayed:
    /// Go is never held back for a missed Ready or Set).
    /// </summary>
    public sealed class RaceStartSequence
    {
        private readonly float _readySeconds;
        private readonly float _setMin;
        private readonly float _setMax;
        private readonly Random _random;

        private float _elapsed;
        private float _setWait;
        private int _version; // bumps on every state change, so a handler that restarts or cancels stops the running Tick

        public RaceStartSequence(float readySeconds, float setWaitMinSeconds, float setWaitMaxSeconds, Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _readySeconds = Math.Max(0f, readySeconds);
            float min = Math.Max(0f, setWaitMinSeconds);
            float max = Math.Max(0f, setWaitMaxSeconds);
            _setMin = Math.Min(min, max);
            _setMax = Math.Max(min, max);
        }

        public RaceStartPhase Phase { get; private set; }

        /// <summary>True while Ready or Set runs (time still matters).</summary>
        public bool IsCounting => Phase == RaceStartPhase.Ready || Phase == RaceStartPhase.Set;

        /// <summary>How long the current (or last) Set holds, drawn when Set begins; 0 before.</summary>
        public float SetWaitSeconds => _setWait;

        public event Action<RaceStartPhase> PhaseChanged;

        /// <summary>Starts over at Ready, whatever the state was (first start, Restart, restart during the countdown or the race).</summary>
        public void Begin()
        {
            _elapsed = 0f;
            _setWait = 0f;
            Enter(RaceStartPhase.Ready);
        }

        /// <summary>Back to Idle (restart elsewhere, finish, scene change). No event when already Idle.</summary>
        public void Cancel()
        {
            _elapsed = 0f;
            if (Phase == RaceStartPhase.Idle) return;
            Enter(RaceStartPhase.Idle);
        }

        /// <summary>Advances the countdown by <paramref name="deltaSeconds"/>; ignored when not counting or not positive.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsCounting || deltaSeconds <= 0f) return;
            _elapsed += deltaSeconds;

            while (IsCounting)
            {
                int version = _version;
                if (Phase == RaceStartPhase.Ready)
                {
                    if (_elapsed < _readySeconds) return;
                    _elapsed -= _readySeconds;
                    _setWait = _setMin + (float)_random.NextDouble() * (_setMax - _setMin);
                    Enter(RaceStartPhase.Set);
                }
                else
                {
                    if (_elapsed < _setWait) return;
                    _elapsed = 0f;
                    Enter(RaceStartPhase.Go);
                }
                if (version + 1 != _version) return; // a handler changed the state under us
            }
        }

        private void Enter(RaceStartPhase phase)
        {
            Phase = phase;
            _version++;
            PhaseChanged?.Invoke(phase);
        }
    }
}
