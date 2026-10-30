using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Translates the runner system (RaceController / RunnerBody, read-only) into IRaceSession and IRaceTelemetry. No decisions here.
    /// Today the runner has no finish event and resets in the finish frame, so a RunReset that follows a changed
    /// LastRaceTime is the finish. When the runner gets a real finish event, only this class changes.
    /// It also owns the race start (T-039): every RequestStart closes the runner's input and runs the READY / SET / GO countdown
    /// (timing in RaceStartSequence); input opens exactly at GO and the phase event follows. Views only react to the events.
    /// The runner's own clock still starts at the first press after GO (Malik's RaceController; see the T-039 needs).
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class RaceSessionAdapter : MonoBehaviour, IRaceSession, IRaceTelemetry, IRaceStartSignal
    {
        [SerializeField, Tooltip("The scene's RaceController (race length, clock, state).")]
        RaceController race;
        [SerializeField, Tooltip("The runner body: input gate, run reset, distance. Empty = race.body.")]
        RunnerBody body;
        [SerializeField, Min(0.1f), Tooltip("Cadence window (s): presses per second is the press count of this recent span divided by it.")]
        float cadenceWindowSeconds = 1f;
        [SerializeField, Tooltip("Countdown numbers (READY time, SET wait range). Empty = the config's own defaults.")]
        RaceStartConfig startConfig;

        RaceSessionState _state = RaceSessionState.Ready;
        RaceEventId _eventId;
        RaceOutcome _outcome;
        float _lastSeenFinishTime;
        bool _requesting;
        bool _subscribed;
        readonly Queue<double> _pressTimes = new Queue<double>();
        readonly System.Random _random = new System.Random();
        RaceStartSequence _sequence;
        RaceStartConfig _fallbackConfig;

        public RaceSessionState State => _state;
        public float TargetDistance => race ? race.raceDistance : 0f;

        public double RaceTime
        {
            get
            {
                if (_state == RaceSessionState.Finished) return _outcome.TimeSeconds;
                return race ? race.RaceTime : 0.0;
            }
        }

        public float Distance
        {
            get
            {
                if (_state == RaceSessionState.Finished) return _outcome.DistanceMeters;
                return body && body.Sim != null ? body.Sim.Distance : 0f;
            }
        }

        public float SpeedMetersPerSecond => body && body.Sim != null ? body.Sim.Speed : 0f;

        public float Energy01
        {
            get
            {
                if (!body || body.Sim == null) return 0f;
                float max = body.Sim.T.energyMax;
                return max > 0f ? Mathf.Clamp01(body.Sim.Energy / max) : 0f;
            }
        }

        public float PressesPerSecond
        {
            get
            {
                float window = Mathf.Max(0.1f, cadenceWindowSeconds);
                double now = Time.timeAsDouble;
                while (_pressTimes.Count > 0 && now - _pressTimes.Peek() > window) _pressTimes.Dequeue();
                return _pressTimes.Count / window;
            }
        }

        public RaceStartPhase StartPhase => _sequence != null ? _sequence.Phase : RaceStartPhase.Idle;

        public event Action Started;
        public event Action<RaceOutcome> Finished;
        public event Action<RaceStartPhase> StartPhaseChanged;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        void OnDisable()
        {
            _sequence?.Cancel(); // scene change / disable: no countdown survives, views drop their state
            if (_subscribed && body)
            {
                body.RunReset -= OnRunReset;
                body.Tapped -= OnTapped;
            }
            _subscribed = false;
        }

        /// <summary>Idempotent setup; also callable by the composition root or tests (AddComponent skips Awake in EditMode).</summary>
        public void Initialize(RaceController raceController = null, RunnerBody runnerBody = null)
        {
            if (raceController) race = raceController;
            if (runnerBody) body = runnerBody;
            if (!body && race) body = race.body;
            if (race) _lastSeenFinishTime = race.LastRaceTime;
            if (_subscribed || !body) return;
            body.RunReset += OnRunReset;
            body.Tapped += OnTapped;
            _subscribed = true;
        }

        public void RequestStart(RaceStartRequest request)
        {
            Initialize();
            if (!race) return;
            _eventId = request.EventId;
            if (request.DistanceMeters > 0f) race.raceDistance = request.DistanceMeters;

            _requesting = true;
            _state = RaceSessionState.Ready;
            race.ResetRace(); // the runner reopens its input on a reset; the countdown closes it again right below
            _requesting = false;

            _lastSeenFinishTime = race.LastRaceTime;
            BeginCountdown();
        }

        /// <summary>Input off, then Ready: a restart (also mid-countdown or mid-race) always starts the countdown over, never a second GO.</summary>
        void BeginCountdown()
        {
            if (body) body.acceptInput = false;
            // A fresh sequence per start: the config's numbers apply, and a pending Go of the old one cannot fire.
            if (_sequence != null) _sequence.PhaseChanged -= OnSequencePhase;
            RaceStartConfig config = StartConfig();
            _sequence = new RaceStartSequence(config.ReadySeconds, config.SetWaitMinSeconds, config.SetWaitMaxSeconds, _random);
            _sequence.PhaseChanged += OnSequencePhase;
            _sequence.Begin();
        }

        void OnSequencePhase(RaceStartPhase phase)
        {
            // The gate first, then the event: a view or sound reacting to Go sees input already open (the authoritative start).
            if (phase == RaceStartPhase.Go && body) body.acceptInput = true;
            StartPhaseChanged?.Invoke(phase);
        }

        RaceStartConfig StartConfig()
        {
            if (startConfig) return startConfig;
            if (!_fallbackConfig) _fallbackConfig = ScriptableObject.CreateInstance<RaceStartConfig>();
            return _fallbackConfig;
        }

        void Update()
        {
            if (_sequence != null && _sequence.IsCounting)
                _sequence.Tick(Mathf.Min(Time.deltaTime, StartConfig().MaxFrameSeconds));

            // Ready -> Running: the runner's own state is the source.
            if (_state == RaceSessionState.Ready && race && race.Current == RaceController.State.Running)
                BeginRunning();
        }

        void BeginRunning()
        {
            _state = RaceSessionState.Running;
            Started?.Invoke();
        }

        // Releases are not presses; the sim already filtered repeats.
        void OnTapped(int leg, bool pressed, double simTime)
        {
            if (pressed) _pressTimes.Enqueue(Time.timeAsDouble);
        }

        void OnRunReset()
        {
            _pressTimes.Clear();
            // Our own RequestStart reset, or a reset after we already finished: not a finish.
            if (_requesting || _state == RaceSessionState.Finished || !race) return;
            if (Mathf.Approximately(race.LastRaceTime, _lastSeenFinishTime)) return;

            // Finished before our poll saw Running (same frame): still report Started first.
            if (_state == RaceSessionState.Ready) BeginRunning();
            _lastSeenFinishTime = race.LastRaceTime;
            Complete(race.LastRaceTime);
        }

        void Complete(double seconds)
        {
            _state = RaceSessionState.Finished;
            _outcome = new RaceOutcome(_eventId, seconds, TargetDistance, 1, 1);
            _sequence?.Cancel(); // a finish during the countdown (dev key): GO must not reopen input under Results
            // After RunnerBody set acceptInput = true (it runs before raising RunReset): keep the runner still under Results.
            if (body) body.acceptInput = false;
            Finished?.Invoke(_outcome);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Dev only: finish now with the current race time (see Dev/DevRaceFinish).</summary>
        public void DevForceFinish()
        {
            if (_state == RaceSessionState.Finished || !race) return;
            if (_state == RaceSessionState.Ready) BeginRunning();
            Complete(race.RaceTime);
        }
#endif
    }
}
