using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The opponents of the Race scene (T-075). Each bot is a copy of Malik's runner prefab that runs his own physics (RunnerBody +
    /// RunnerSim) and is driven only through his public API: the bot's RunnerInput is disabled (no keyboard or touch) and
    /// <c>RunnerInput.Inject</c> feeds the key events of a <see cref="BotPressSchedule"/> at their own timestamps, the same way the
    /// player's presses arrive. Nothing in Malik's code is changed and the bots never touch the player's runner.
    /// Flow: READY (from <see cref="RaceSessionAdapter"/>'s start signal) plans a new field and puts everybody on the line, GO starts the
    /// schedules, every frame the live order is fed to a <see cref="RaceStandingsModel"/> that this component exposes as
    /// <see cref="IRaceStandings"/>. A bot's time is measured like the player's: from its first press (RaceController does the same).
    /// The runner copies are built on the first frame, after every scene Start, so the mechanic's single-runner fallback lookups
    /// (FindObjectOfType) never see them at start-up; the builder also fills those references explicitly.
    /// </summary>
    [DefaultExecutionOrder(-150)] // after RunnerInput (-200), before RunnerBody (-100): events with old timestamps land in the same frame
    public sealed class RaceBots : MonoBehaviour, IRaceStandings
    {
        [Header("Wiring (set by the Race scene builder)")]
        [SerializeField, Tooltip("Opponent numbers: count, difficulty, spread, driver limits, calibration, names.")]
        BotRaceConfig config;
        [SerializeField, Tooltip("Malik's runner prefab. Each bot is a copy of it (his prefab file is not touched).")]
        GameObject runnerPrefab;
        [SerializeField, Tooltip("The player's runner body: lane origin, distance for the live order.")]
        RunnerBody player;
        [SerializeField, Tooltip("The scene's RaceController: race length.")]
        RaceController race;
        [SerializeField, Tooltip("Race session adapter: the READY / SET / GO signal and the event id. Empty = bots only stand.")]
        RaceSessionAdapter session;

        [Header("Track")]
        [SerializeField, Min(0.1f), Tooltip("Distance (m) between neighbouring lanes. Lanes lie on the side away from the follow camera (the camera sits to the runner's right), so no bot can hide the player.")]
        float laneSpacing = 1.2f;

        [Header("Driver")]
        [SerializeField, Min(1f), Tooltip("The key schedule is made for this multiple of the bot's target time, so a slow run still has presses left.")]
        float scheduleHorizonFactor = 2f;
        [SerializeField, Min(0f), Tooltip("Seconds added to the schedule length on top of the factor.")]
        float scheduleHorizonPadding = 10f;

        [Header("Names")]
        [SerializeField, Tooltip("Shown for the player when the settings hold no name.")]
        string fallbackPlayerName = "YOU";

        sealed class Bot
        {
            public GameObject Root;
            public RunnerBody Body;
            public RunnerInput Input;
            public CosmeticApplier Look;
            public Vector3 Start;
            public bool Active;
            public BotPlan Plan;
            public BotPressSchedule Schedule;
            public int Next;
            public bool Started;
            public double FirstPressSimTime;
            public bool Finished;
            public double FinishSeconds;
            public double PrevSimTime;
            public float PrevDistance;
        }

        readonly List<Bot> _bots = new List<Bot>();
        readonly List<Transform> _roots = new List<Transform>();
        readonly List<RaceEntry> _entries = new List<RaceEntry>();
        readonly RaceStandingsModel _model = new RaceStandingsModel();
        readonly System.Random _random = new System.Random();

        ICosmeticService _cosmetics;
        CosmeticCatalog _catalog;
        IPlayerSettings _settings;
        IPersonalBests _bests;

        bool _initialized;
        bool _subscribed;
        bool _runnersReady;
        bool _roundPending = true;
        bool _racing;
        double _goTime;
        bool _playerFinished;
        double _playerTime;
        string _playerName = string.Empty;
        string _playerFlag = string.Empty;
        Vector3 _basePosition;
        Vector3 _laneDirection;

        // IRaceStandings: the model's own list and event; before the first frame it is empty.
        public IReadOnlyList<RaceEntry> Entries => _model.Entries;
        public int PlayerRank => _model.PlayerRank;
        public int RunnerCount => _model.RunnerCount;
        public event Action Changed
        {
            add => _model.Changed += value;
            remove => _model.Changed -= value;
        }

        /// <summary>The bots' runner roots in lane order (empty until the runners are built). For the shot tool.</summary>
        public IReadOnlyList<Transform> RunnerRoots => _roots;
        public BotRaceConfig Config => config;
        public RunnerBody Player => player;

        [Inject]
        void Construct([InjectOptional] ICosmeticService cosmetics, [InjectOptional] CosmeticCatalog catalog,
            [InjectOptional] IPlayerSettings settings, [InjectOptional] IPersonalBests bests)
        {
            _cosmetics = cosmetics;
            _catalog = catalog;
            _settings = settings;
            _bests = bests;
        }

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        void OnDestroy()
        {
            if (_subscribed && session) session.StartPhaseChanged -= OnStartPhase;
            _subscribed = false;
        }

        /// <summary>Idempotent: lane origin and the start signal. The runner copies come later (<see cref="BuildRunners"/>).</summary>
        public void Initialize()
        {
            if (!_initialized && player)
            {
                _basePosition = player.transform.position;
                _laneDirection = -player.transform.right; // away from the camera
                _initialized = true;
            }
            if (!_subscribed && session)
            {
                session.StartPhaseChanged += OnStartPhase;
                _subscribed = true;
            }
        }

        /// <summary>World position of a runner's lane at the start line.</summary>
        public Vector3 StartPositionOf(int runnerIndex) => _basePosition + _laneDirection * ((runnerIndex + 1) * laneSpacing);

        /// <summary>
        /// Builds the runner copies now (Play mode does it on the first frame by itself). Public for the shot tool, which builds them
        /// in Edit mode where nothing runs.
        /// </summary>
        public void BuildRunners()
        {
            Initialize();
            if (_runnersReady || !runnerPrefab || !player || !config) return;

            int count = Mathf.Min(Mathf.Max(config.BotCount, 0), config.Identities.Count);
            // Built under an inactive holder: nothing wakes up (RunnerInput would start listening to the keyboard) until it is prepared.
            var holder = new GameObject("Bot Spawn Holder");
            holder.SetActive(false);
            for (int i = 0; i < count; i++)
                _bots.Add(CreateRunner(i, holder.transform));
            DestroyNow(holder);
            _runnersReady = true;
        }

        Bot CreateRunner(int index, Transform holder)
        {
            GameObject root = Instantiate(runnerPrefab, holder);
            root.name = "Bot " + (index + 1);

            var bot = new Bot { Root = root, Start = StartPositionOf(index) };
            bot.Input = root.GetComponentInChildren<RunnerInput>(true);
            bot.Body = root.GetComponentInChildren<RunnerBody>(true);
            if (bot.Body == null || bot.Input == null)
                throw new InvalidOperationException($"'{runnerPrefab.name}' has no RunnerBody / RunnerInput; a bot cannot run on it.");

            bot.Input.enabled = false; // no keyboard / touch; Inject still fills its queue
            bot.Body.input = bot.Input;
            bot.Body.acceptInput = true;
            // Debug tools of the prefab: the replay keys (1-4, Esc) and the IMGUI panel must not exist on a bot.
            foreach (RunnerBodyHUD hud in root.GetComponentsInChildren<RunnerBodyHUD>(true)) DestroyNow(hud);
            foreach (RunnerReplay replay in root.GetComponentsInChildren<RunnerReplay>(true)) DestroyNow(replay);
            bot.Look = root.GetComponent<CosmeticApplier>();
            if (bot.Look == null) bot.Look = root.AddComponent<CosmeticApplier>();

            // Position first: RunnerBody takes its start line from the transform in Awake, which runs when the holder lets go.
            root.transform.SetPositionAndRotation(bot.Start, player.transform.rotation);
            root.transform.SetParent(transform, true);
            _roots.Add(root.transform);
            return bot;
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase == RaceStartPhase.Ready) BeginRound();
            else if (phase == RaceStartPhase.Go) StartRace();
            // Idle (the player finished / the countdown was cancelled): the bots keep running, the order keeps updating.
        }

        /// <summary>A new race: plan the field, put everybody on the line, clear the order.</summary>
        void BeginRound()
        {
            if (!_runnersReady) { _roundPending = true; return; }
            _roundPending = false;
            _racing = false;
            _playerFinished = false;
            _playerTime = 0.0;
            _model.Reset();
            CachePlayerIdentity();

            IReadOnlyList<BotPlan> plans = BotFieldPlanner.Plan(config, ReferenceSeconds(), _random);
            int[] lanes = ShuffledLanes(_bots.Count);
            for (int i = 0; i < _bots.Count; i++) _bots[i].Active = false;

            for (int i = 0; i < plans.Count && i < _bots.Count; i++)
            {
                Bot bot = _bots[lanes[i]];
                BotPlan plan = plans[i];
                bot.Plan = plan;
                bot.Schedule = new BotPressSchedule(plan, _random, plan.TargetSeconds * scheduleHorizonFactor + scheduleHorizonPadding);
                bot.Next = 0;
                bot.Started = false;
                bot.Finished = false;
                bot.FinishSeconds = 0.0;
                bot.FirstPressSimTime = 0.0;
                bot.PrevSimTime = 0.0;
                bot.PrevDistance = 0f;
                bot.Active = true;
                bot.Root.SetActive(true);
                bot.Body.ResetRun(); // sim to zero, back on its lane, input queue cleared
                Dress(bot);
            }
            for (int i = 0; i < _bots.Count; i++)
                if (!_bots[i].Active) _bots[i].Root.SetActive(false);

            PushStandings();
        }

        void StartRace()
        {
            if (!_runnersReady) return;
            _goTime = Time.timeAsDouble;
            _racing = true;
        }

        void Update()
        {
            if (!_runnersReady)
            {
                BuildRunners(); // first frame: every scene Start has run
                if (_runnersReady && _roundPending) BeginRound();
            }
            if (!_racing) return;

            double now = Time.timeAsDouble;
            for (int i = 0; i < _bots.Count; i++)
            {
                Bot bot = _bots[i];
                if (!bot.Active || bot.Finished) continue;
                IReadOnlyList<BotPressEvent> events = bot.Schedule.Events;
                while (bot.Next < events.Count && _goTime + events[bot.Next].Time <= now)
                {
                    BotPressEvent e = events[bot.Next++];
                    bot.Input.Inject(e.Leg, e.IsPress, _goTime + e.Time); // its own timestamp, not the frame's
                }
            }
        }

        void LateUpdate()
        {
            if (!_runnersReady) return;
            DetectBots();
            PushStandings();
        }

        /// <summary>
        /// The player crossed the line with this race time. Called by <see cref="RaceSessionAdapter"/> before it builds the outcome, so the
        /// rank already knows every bot that finished earlier in this same frame.
        /// </summary>
        public void NotifyPlayerFinished(double seconds)
        {
            if (!_runnersReady) return;
            _playerFinished = true;
            _playerTime = seconds;
            DetectBots();
            PushStandings();
        }

        /// <summary>Marks bots that crossed the line (time interpolated inside the frame) and lets go of their keys.</summary>
        void DetectBots()
        {
            if (!_racing) return;
            float distance = RaceDistance;
            for (int i = 0; i < _bots.Count; i++)
            {
                Bot bot = _bots[i];
                if (!bot.Active || bot.Finished || bot.Body.Sim == null) continue;
                RunnerSim sim = bot.Body.Sim;
                if (!bot.Started && sim.Presses > 0)
                {
                    bot.Started = true;
                    bot.FirstPressSimTime = sim.LastPressTime; // the clock starts at the first press, like RaceController's
                }

                float d = sim.Distance;
                double t = sim.Time;
                if (d >= distance && bot.Started)
                {
                    float span = d - bot.PrevDistance;
                    double fraction = span > 0f ? (distance - bot.PrevDistance) / span : 1.0;
                    double crossed = bot.PrevSimTime + fraction * (t - bot.PrevSimTime);
                    bot.FinishSeconds = crossed - bot.FirstPressSimTime;
                    bot.Finished = true;
                    LetGo(bot);
                }
                else
                {
                    bot.PrevDistance = d;
                    bot.PrevSimTime = t;
                }
            }
        }

        /// <summary>After the line: no more presses, keys released; the runner coasts to a stop on its own physics.</summary>
        static void LetGo(Bot bot)
        {
            bot.Body.acceptInput = false;
            double now = Time.timeAsDouble;
            bot.Input.Inject(0, false, now);
            bot.Input.Inject(1, false, now);
        }

        void PushStandings()
        {
            float distance = RaceDistance;
            _entries.Clear();
            float playerDistance = _playerFinished ? distance : (player && player.Sim != null ? player.Sim.Distance : 0f);
            _entries.Add(new RaceEntry(_playerName, _playerFlag, true, Mathf.Min(playerDistance, distance), _playerFinished, _playerTime));
            for (int i = 0; i < _bots.Count; i++)
            {
                Bot bot = _bots[i];
                if (!bot.Active) continue;
                float d = bot.Finished ? distance : (bot.Body.Sim != null ? Mathf.Min(bot.Body.Sim.Distance, distance) : 0f);
                _entries.Add(new RaceEntry(bot.Plan.Name, bot.Plan.CountryCode, false, d, bot.Finished, bot.FinishSeconds));
            }
            _model.Update(_entries);
        }

        float RaceDistance => race ? race.raceDistance : 100f;

        /// <summary>The player's best time of the current event (the difficulty is relative to it); null = none yet, the config's fallback.</summary>
        float? ReferenceSeconds()
        {
            if (_bests == null || !session) return null;
            RaceEventId id = session.CurrentEventId;
            if (id.IsEmpty) return null;
            return _bests.TryGetBest(id, out double seconds) && seconds > 0.0 ? (float)seconds : (float?)null;
        }

        void CachePlayerIdentity()
        {
            string name = _settings != null ? _settings.PlayerName : string.Empty;
            _playerName = string.IsNullOrWhiteSpace(name) ? fallbackPlayerName : name.Trim();
            _playerFlag = _settings != null ? _settings.FlagCode ?? string.Empty : string.Empty;
        }

        /// <summary>A random outfit and skin tone from what the cosmetic services know; without them the bot keeps the prefab's look.</summary>
        void Dress(Bot bot)
        {
            if (_cosmetics == null || _catalog == null || bot.Look == null) return;
            CosmeticOutfit outfit = _cosmetics.CreateRandomOutfit(_random);
            IReadOnlyList<string> tones = _catalog.SkinTones;
            if (tones.Count > 0) outfit = outfit.WithSkinColor(tones[_random.Next(tones.Count)]);
            bot.Look.Initialize();
            bot.Look.Apply(outfit, _catalog);
        }

        /// <summary>Which runner copy (lane) each planned bot takes this race, so the fastest bot is not always in the same lane.</summary>
        int[] ShuffledLanes(int count)
        {
            var lanes = new int[count];
            for (int i = 0; i < count; i++) lanes[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (lanes[i], lanes[j]) = (lanes[j], lanes[i]);
            }
            return lanes;
        }

        static void DestroyNow(UnityEngine.Object target)
        {
            if (target) UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
