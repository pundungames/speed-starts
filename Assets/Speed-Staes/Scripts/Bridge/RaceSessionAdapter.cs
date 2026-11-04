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
        [SerializeField, Tooltip("The opponents (T-075). Their order at the player's finish gives the outcome's rank and runner count. Empty = the player runs alone: rank 1 of 1.")]
        RaceBots bots;
        [SerializeField, Tooltip("Practice (T-090): in a Practice race the start distance is the player's dropdown choice. Empty = every race takes the requested distance.")]
        PracticeSession practice;

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
        float _falseStartLeft;
        float _timeScaleBeforeFail = 1f;

        public RaceSessionState State => _state;
        public float TargetDistance => race ? race.raceDistance : 0f;

        public double RaceTime
        {
            get
            {
                if (_state == RaceSessionState.Finished) return _outcome.TimeSeconds;
                if (IsFailing) return 0.0; // the fall is made of presses, which start the runner's own clock
                return race ? race.RaceTime : 0.0;
            }
        }

        public float Distance
        {
            get
            {
                if (_state == RaceSessionState.Finished) return _outcome.DistanceMeters;
                if (Relay) return bots.Relay.PlayerDistance; // the baton of the player's team
                return body && body.Sim != null ? body.Sim.Distance : 0f;
            }
        }

        public float SpeedMetersPerSecond => IsFailing ? 0f : Relay ? bots.Relay.PlayerSpeed : body && body.Sim != null ? body.Sim.Speed : 0f;

        public float Energy01
        {
            get
            {
                if (Relay) return bots.Relay.PlayerEnergy01; // the runner with the baton: full again after every exchange
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

        /// <summary>The player's runner body (read-only): extra input sources such as the arrow keys feed its RunnerInput. Null before setup.</summary>
        public RunnerBody PlayerBody => body ? body : (race ? race.body : null);

        public RaceStartPhase StartPhase => _sequence != null ? _sequence.Phase : RaceStartPhase.Idle;

        /// <summary>The current race is a relay: distance, speed and energy come from the player's team, the finish from <see cref="FinishRelay"/>.</summary>
        bool Relay => bots && bots.RelayActive;

        /// <summary>READY or SET is running: a press now is a false start.</summary>
        public bool IsCountingDown => _sequence != null && _sequence.IsCounting;

        /// <summary>A false start is playing: the runner is falling and FAIL is up. No press counts until the countdown is back.</summary>
        public bool IsFailing => _falseStartLeft > 0f;

        /// <summary>
        /// A press before GO (RunnerPressGate): the game slows down, the runner falls (its own dive, both feet down: the gate presses them)
        /// and FAIL shows; then everybody is back on the line and the countdown starts over. No ticket and no reward moves, and the race
        /// never counts as started. False when no countdown is running.
        /// </summary>
        public bool ReportFalseStart()
        {
            if (_sequence == null || !_sequence.FalseStart()) return false;
            RaceStartConfig config = StartConfig();
            _falseStartLeft = config.FalseStartSeconds;
            _timeScaleBeforeFail = Time.timeScale;
            Time.timeScale = config.FalseStartTimeScale;
            if (body) body.acceptInput = true;
            return true;
        }

        /// <summary>Back to normal speed. Every way out of FAIL comes through here, so the slow motion cannot outlive it.</summary>
        void CancelFail()
        {
            if (!IsFailing) return;
            _falseStartLeft = 0f;
            Time.timeScale = _timeScaleBeforeFail;
        }

        /// <summary>The event of the current race (empty before the first RequestStart). The opponents scale to the player's best of it.</summary>
        public RaceEventId CurrentEventId => _eventId;

        public event Action Started;
        public event Action<RaceOutcome> Finished;
        public event Action<RaceStartPhase> StartPhaseChanged;

        void Awake()
        {
            Initialize();
            if (!GetComponent<RunnerPressGate>()) gameObject.AddComponent<RunnerPressGate>();
        }

        void OnEnable() => Initialize();

        /// <summary>Watch Replay: hands the Results screen the race's replay (until RaceSceneInstaller binds IRaceReplay itself).</summary>
        void Start()
        {
            var results = FindObjectOfType<SpeedStars.UI.Race.ResultsScreenView>(true);
            if (results) results.UseReplay(new RaceReplayAdapter());
        }

        void OnDisable()
        {
            _sequence?.Cancel(); // scene change / disable: no countdown survives, views drop their state
            CancelFail();
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
            if (body) body.EnablePlayerFeedback();
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
            float distance = practice ? practice.AdjustDistance(request.EventId, request.DistanceMeters) : request.DistanceMeters;
            if (distance > 0f) race.raceDistance = distance;

            StartOver();
        }

        /// <summary>Everybody back on the line, then the countdown.</summary>
        void StartOver()
        {
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
            CancelFail();
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
            if (phase == RaceStartPhase.Go)
            {
                if (body) body.acceptInput = true;
                if (race) race.StartClock(); // the race clock runs from GO, the reaction included, as the bots' does
            }
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

            if (IsFailing)
            {
                // Real time: the slow motion must not stretch its own length.
                _falseStartLeft -= Mathf.Min(Time.unscaledDeltaTime, StartConfig().MaxFrameSeconds);
                if (!IsFailing)
                {
                    Time.timeScale = _timeScaleBeforeFail;
                    if (race) StartOver();
                }
            }

            // Ready -> Running: the runner's own state is the source.
            // Not during FAIL: the fall is made of presses, and the clock counts a press as the start of the race.
            if (_state == RaceSessionState.Ready && !IsFailing && race && race.Current == RaceController.State.Running)
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
            int rank = 1;
            int runnerCount = 1;
            if (bots)
            {
                // The order at the finish frame: bots that crossed earlier in this frame are counted before the rank is read.
                bots.NotifyPlayerFinished(seconds);
                runnerCount = Mathf.Max(1, bots.RunnerCount);
                rank = Mathf.Clamp(bots.PlayerRank, 1, runnerCount);
            }
            _outcome = new RaceOutcome(_eventId, seconds, TargetDistance, rank, runnerCount);
            _sequence?.Cancel(); // a finish during the countdown (dev key): GO must not reopen input under Results
            CancelFail();
            // After RunnerBody set acceptInput = true (it runs before raising RunReset): keep the runner still under Results.
            if (body) body.acceptInput = false;
            Finished?.Invoke(_outcome);
        }

        /// <summary>
        /// A relay ends when the player's team brings the baton over the line (the player's own runner does not run): the outcome takes
        /// the team's time, interpolated inside the frame by the relay.
        /// </summary>
        public void FinishRelay(double seconds)
        {
            if (_state == RaceSessionState.Finished || !Relay) return;
            if (_state == RaceSessionState.Ready) BeginRunning();
            Complete(seconds);
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
