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
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class RaceSessionAdapter : MonoBehaviour, IRaceSession, IRaceTelemetry
    {
        [SerializeField, Tooltip("The scene's RaceController (race length, clock, state).")]
        RaceController race;
        [SerializeField, Tooltip("The runner body: input gate, run reset, distance. Empty = race.body.")]
        RunnerBody body;
        [SerializeField, Min(0.1f), Tooltip("Cadence window (s): presses per second is the press count of this recent span divided by it.")]
        float cadenceWindowSeconds = 1f;

        RaceSessionState _state = RaceSessionState.Ready;
        RaceEventId _eventId;
        RaceOutcome _outcome;
        float _lastSeenFinishTime;
        bool _requesting;
        bool _subscribed;
        readonly Queue<double> _pressTimes = new Queue<double>();

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

        public event Action Started;
        public event Action<RaceOutcome> Finished;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        void OnDisable()
        {
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
            race.ResetRace();
            _requesting = false;

            _lastSeenFinishTime = race.LastRaceTime;
            if (body) body.acceptInput = true;
        }

        void Update()
        {
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
