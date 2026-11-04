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
    /// <see cref="IRaceStandings"/>. A bot's time is measured like the player's: from GO, its reaction included.
    /// The runner copies are built on the first frame, after every scene Start, so the mechanic's single-runner fallback lookups
    /// (FindObjectOfType) never see them at start-up; the builder also fills those references explicitly.
    /// </summary>
    [DefaultExecutionOrder(-150)] // after RunnerInput (-200), before RunnerBody (-100): events with old timestamps land in the same frame
    public sealed class RaceBots : MonoBehaviour, IRaceStandings, IRaceTraces
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
        [SerializeField, Tooltip("The hurdle object on the bots' lanes in a hurdle event.")]
        GameObject hurdlePrefab;

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
            public double GoSimTime;
            public readonly List<RaceTraceSample> Samples = new List<RaceTraceSample>();
            public bool Finished;
            public double FinishSeconds;
            public double PrevSimTime;
            public float PrevDistance;
            public RunnerHurdles Hurdles;
            public BotJumpDriver Jumper;
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
        RaceEventCatalog _events;

        bool _initialized;
        bool _subscribed;
        bool _runnersReady;
        bool _roundPending = true;
        bool _racing;
        double _goTime;
        bool _playerFinished;
        double _playerTime;
        const float TraceStepMeters = 0.25f; // a chart point per this many metres at most

        string _playerName = string.Empty;
        string _playerFlag = string.Empty;
        Vector3 _basePosition;
        Vector3 _laneDirection;

        /// <summary>IRaceTraces: every racing bot's run so far, for the Analysis screen.</summary>
        public IReadOnlyList<RaceTrace> Traces
        {
            get
            {
                var traces = new List<RaceTrace>(_bots.Count);
                foreach (Bot bot in _bots)
                    if (bot.Active) traces.Add(new RaceTrace(bot.Plan.Name, bot.Samples, bot.Finished, bot.FinishSeconds));
                return traces;
            }
        }

        public event Action TracesChanged;

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

        /// <summary>The name shown for this runner (the player's or a bot's); null if it is not in this race.</summary>
        public string NameOf(RunnerBody body)
        {
            if (!body) return null;
            if (body == player) return _playerName;
            for (int i = 0; i < _bots.Count; i++)
                if (_bots[i].Body == body) return _bots[i].Plan != null ? _bots[i].Plan.Name : _bots[i].Root.name;
            return null;
        }
        public BotRaceConfig Config => config;
        public RunnerBody Player => player;

        /// <summary>The relay of this scene (made on the first frame); <see cref="RaceRelay.Active"/> while the current race is a relay.</summary>
        public RaceRelay Relay => _relay;
        public bool RelayActive => _relay && _relay.Active;
        internal GameObject RunnerPrefab => runnerPrefab;
        internal RaceSessionAdapter Session => session;

        [Inject]
        void ConstructEvents([InjectOptional] RaceEventCatalog events) => _events = events;

        [Inject]
        void Construct([InjectOptional] ICosmeticService cosmetics, [InjectOptional] CosmeticCatalog catalog,
            [InjectOptional] IPlayerSettings settings, [InjectOptional] IPersonalBests bests)
        {
            _cosmetics = cosmetics;
            _catalog = catalog;
            _settings = settings;
            _bests = bests;
        }

        SettingsRunnerAdapter _settingsAdapter;
        RaceRelay _relay;
        Renderer[] _hiddenPlayerRenderers = Array.Empty<Renderer>();

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
            if (!_settingsAdapter && Application.isPlaying) // the shot tool builds in Edit mode: the scene stays untouched there
            {
                _settingsAdapter = GetComponent<SettingsRunnerAdapter>();
                if (!_settingsAdapter) _settingsAdapter = gameObject.AddComponent<SettingsRunnerAdapter>();
            }
            if (!_relay && Application.isPlaying)
            {
                _relay = GetComponent<RaceRelay>();
                if (!_relay) _relay = gameObject.AddComponent<RaceRelay>();
                _relay.Bind(this);
            }
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

        /// <summary>
        /// World position of a runner's lane at the start line. The player's lane choice shifts the bots: the player stays where the scene put it and the bots take the other lanes, so a
        /// better lane number puts some of them on the camera side (negative offset). Automatic = the player nearest the camera.
        /// </summary>
        public Vector3 StartPositionOf(int runnerIndex) =>
            _basePosition + _laneDirection * (BotLanes.BotOffset(runnerIndex, PlayerLaneSlot()) * laneSpacing);

        int PlayerLaneSlot()
        {
            int bots = config ? Mathf.Min(Mathf.Max(config.BotCount, 0), config.Identities.Count) : 0;
            return BotLanes.PlayerSlot(_settings != null ? _settings.LanePreference : 0, bots);
        }

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
            if (_settingsAdapter) _settingsAdapter.Bind(_settings, player);
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
            bot.Hurdles = CreateLane(bot, index);
            bot.Jumper = new BotJumpDriver(_random);
            return bot;
        }

        /// <summary>The bot's own hurdles: same judging as the player's, without the screen flash. Set up while inactive, so its Awake sees them.</summary>
        RunnerHurdles CreateLane(Bot bot, int index)
        {
            var go = new GameObject("Bot " + (index + 1) + " Hurdles");
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            RunnerHurdles hurdles = go.AddComponent<RunnerHurdles>();
            hurdles.body = bot.Body;
            hurdles.race = race;
            hurdles.raceDistance = 0f;
            hurdles.count = 0;
            hurdles.hurdlePrefab = hurdlePrefab;
            hurdles.screenFlash = false;
            go.SetActive(true);
            return hurdles;
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

            if (TryGetRelay(out RaceEventDefinition relayEvent))
            {
                BeginRelayRound(relayEvent);
                return;
            }
            if (_relay) _relay.End();

            BotEventProfile profile = CurrentProfile();
            bool tuned = _settings != null && _settings.HasCompetitorTuning;
            IReadOnlyList<BotPlan> plans = HasOpponents()
                ? BotFieldPlanner.Plan(config, ReferenceSeconds(profile), _random, profile,
                    tuned ? _settings.CompetitorDifficulty : (float?)null, tuned ? _settings.CompetitorSkillRange : (float?)null)
                : Array.Empty<BotPlan>();
            int[] lanes = ShuffledLanes(_bots.Count);
            // Without a profile the target is a 100 m equivalent while the run is the real length: the key schedule has to last for it.
            float lengthFactor = profile != null ? 1f : Mathf.Max(1f, RaceDistance / BotEventSetup.BaseTableMeters);
            for (int i = 0; i < _bots.Count; i++) _bots[i].Active = false;

            for (int i = 0; i < plans.Count && i < _bots.Count; i++)
            {
                Bot bot = _bots[lanes[i]];
                BotPlan plan = plans[i];
                bot.Plan = plan;
                bot.Schedule = new BotPressSchedule(plan, _random, plan.TargetSeconds * scheduleHorizonFactor * lengthFactor + scheduleHorizonPadding);
                bot.Next = 0;
                bot.Started = false;
                bot.Finished = false;
                bot.FinishSeconds = 0.0;
                bot.GoSimTime = 0.0;
                bot.Samples.Clear();
                bot.PrevSimTime = 0.0;
                bot.PrevDistance = 0f;
                bot.Active = true;
                bot.Root.SetActive(true);
                bot.Body.ResetRun(); // sim to zero, back on its lane, input queue cleared
                Dress(bot);
            }
            for (int i = 0; i < _bots.Count; i++)
                if (!_bots[i].Active) _bots[i].Root.SetActive(false);

            if (_settingsAdapter) _settingsAdapter.Bind(_settings, player); // new outfits and feedback components exist now: hide the effects again if asked
            SetUpLanes();
            PushStandings();
        }

        /// <summary>The current event's hurdles on every running bot's lane; none on an empty lane or in a flat event.</summary>
        void SetUpLanes()
        {
            RaceEventDefinition definition = null;
            bool found = _events != null && session && _events.TryGet(session.CurrentEventId, out definition);
            int count = found ? definition.HurdleCount : 0;
            float first = found ? definition.FirstHurdleMeters : 0f;
            float spacing = found ? definition.HurdleSpacingMeters : 0f;
            for (int i = 0; i < _bots.Count; i++)
            {
                Bot bot = _bots[i];
                bot.Hurdles.Configure(bot.Active ? count : 0, first, spacing);
                bot.Jumper.Reset();
            }
        }

        /// <summary>Asks for the jump that puts the middle of the bot's flight over the next hurdle (the rule is <see cref="BotJumpDriver"/>).</summary>
        void DriveJump(Bot bot)
        {
            RunnerSim sim = bot.Body.Sim;
            if (sim == null || !bot.Hurdles.TryGetNext(out float hurdle, out int index)) return;
            if (WantsJump(sim, bot.Jumper, bot.Plan, hurdle, index)) bot.Input.InjectJump();
        }

        /// <summary>
        /// The bot's jump decision from a runner sim's state: the flight and wind-up times come from the sim's own tuning at the current
        /// speed. Public and static so the calibration tool measures the bots with exactly this rule.
        /// </summary>
        public static bool WantsJump(RunnerSim sim, BotJumpDriver jumper, BotPlan plan, float hurdleMeters, int hurdleIndex)
        {
            float speed01 = sim.Speed / sim.T.maxSpeed;
            float air = Mathf.Lerp(sim.T.jumpTime.x, sim.T.jumpTime.y, speed01);
            float windup = Mathf.Lerp(sim.T.jumpWindup.x, sim.T.jumpWindup.y, speed01);
            return jumper.ShouldJump(hurdleIndex, hurdleMeters, sim.Distance, sim.Speed, sim.InJump, windup, air, plan.JitterSeconds, plan.MistakeChance);
        }

        void StartRace()
        {
            if (!_runnersReady) return;
            _goTime = Time.timeAsDouble;
            for (int i = 0; i < _bots.Count; i++)
                if (_bots[i].Active && _bots[i].Body.Sim != null) _bots[i].GoSimTime = _bots[i].Body.Sim.Time;
            _racing = true;
            if (RelayActive)
            {
                player.acceptInput = false; // the session opened it at GO; in a relay the player's own runner stays still (hidden)
                _relay.StartRace();
            }
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
                DriveJump(bot);
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
                if (!bot.Started && sim.Presses > 0) bot.Started = true;

                float d = sim.Distance;
                double t = sim.Time;
                if (d >= distance && bot.Started)
                {
                    float span = d - bot.PrevDistance;
                    double fraction = span > 0f ? (distance - bot.PrevDistance) / span : 1.0;
                    double crossed = bot.PrevSimTime + fraction * (t - bot.PrevSimTime);
                    bot.FinishSeconds = crossed - bot.GoSimTime;
                    bot.Finished = true;
                    bot.Samples.Add(new RaceTraceSample(distance, bot.FinishSeconds));
                    if (session) RunnerCooldown.Begin(bot.Body, session.FinishConfig);
                    else LetGo(bot);
                    TracesChanged?.Invoke();
                }
                else
                {
                    if (bot.Samples.Count == 0 || d >= bot.Samples[bot.Samples.Count - 1].DistanceMeters + TraceStepMeters)
                        bot.Samples.Add(new RaceTraceSample(d, t - bot.GoSimTime));
                    bot.PrevDistance = d;
                    bot.PrevSimTime = t;
                }
            }
        }

        /// <summary>After the line, in a scene without a session: no more presses, keys released; the runner coasts to a stop on its own physics.</summary>
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
            if (RelayActive)
            {
                _relay.FillEntries(_entries); // one entry per team
                _model.Update(_entries);
                return;
            }
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

        /// <summary>The current event is a relay (its definition says how many legs).</summary>
        bool TryGetRelay(out RaceEventDefinition definition)
        {
            definition = null;
            if (_events == null || !session || !_relay) return false;
            RaceEventId id = session.CurrentEventId;
            return !id.IsEmpty && _events.TryGet(id, out definition) && definition.IsRelay;
        }

        /// <summary>
        /// A relay at READY: the sprint bots step aside and the relay plans one team per lane. A team's runners share one plan, made
        /// from the table and the player's best of the flat event as long as one leg (100 m for 4x100m), like a sprint field.
        /// </summary>
        void BeginRelayRound(RaceEventDefinition definition)
        {
            foreach (Bot bot in _bots)
            {
                bot.Active = false;
                bot.Root.SetActive(false);
            }

            float legMeters = RaceDistance / Mathf.Max(1, definition.RelayLegCount);
            IReadOnlyList<BotEventSetup.FlatEvent> flat = FlatEvents();
            BotEventProfile profile = BotEventSetup.ForDistance(config, definition.Id.Value, legMeters, flat);
            double? best = null;
            foreach (BotEventSetup.FlatEvent e in flat)
                if (Mathf.Approximately(e.Meters, legMeters) && _bests != null && _bests.TryGetBest(new RaceEventId(e.EventId), out double seconds))
                    best = seconds;
            bool tuned = _settings != null && _settings.HasCompetitorTuning;
            int teams = HasOpponents() ? _relay.TeamCount : 1;
            IReadOnlyList<BotPlan> plans = BotFieldPlanner.Plan(config, BotEventSetup.ReferenceSeconds(profile, best, legMeters), _random, profile,
                tuned ? _settings.CompetitorDifficulty : (float?)null, tuned ? _settings.CompetitorSkillRange : (float?)null, teams);

            _relay.BeginRound(plans, plans.Count > 0 ? _random.Next(plans.Count) : -1, _playerName, _playerFlag, definition.RelayLegCount, RaceDistance,
                definition.BatonMaterial);
            PushStandings();
        }

        /// <summary>World position of a relay lane at the start line: null = the player's lane, else the n-th other team among <paramref name="others"/>.</summary>
        internal Vector3 RelayLanePosition(int? other, int others)
        {
            Initialize();
            int slot = BotLanes.PlayerSlot(_settings != null ? _settings.LanePreference : 0, others);
            int offset = other.HasValue ? BotLanes.BotOffset(other.Value, slot) : 0;
            return _basePosition + _laneDirection * (offset * laneSpacing);
        }

        SideFollowCamera _camera;
        RaceThemeView _theme;

        /// <summary>The camera and the streaming world follow this runner (null = the player's own runner again).</summary>
        internal void FollowRunner(Transform runner, bool snap)
        {
            Transform target = runner ? runner : (player ? player.transform : null);
            if (!_camera) _camera = FindObjectOfType<SideFollowCamera>();
            if (_camera && target && _camera.target != target)
            {
                _camera.target = target;
                if (_camera.enabled)
                {
                    // The camera reads its runner body (press zoom, reset snap) when it is enabled.
                    _camera.enabled = false;
                    _camera.enabled = true;
                }
                if (snap) _camera.SnapToTarget();
            }
            if (!_theme) _theme = FindObjectOfType<RaceThemeView>();
            if (_theme) _theme.Follow(runner);
        }

        /// <summary>In a relay the player's own runner stays on the start line, hidden; NPC runners run the player's lane.</summary>
        internal void SetPlayerHidden(bool hidden)
        {
            if (!player) return;
            if (hidden)
            {
                var shown = new List<Renderer>();
                foreach (Renderer r in player.GetComponentsInChildren<Renderer>(true))
                    if (r.enabled) { r.enabled = false; shown.Add(r); }
                _hiddenPlayerRenderers = shown.ToArray();
                return;
            }
            foreach (Renderer r in _hiddenPlayerRenderers)
                if (r) r.enabled = true;
            _hiddenPlayerRenderers = Array.Empty<Renderer>();
        }

        /// <summary>The player's relay team crossed the finish: the session ends the race with this time.</summary>
        internal void RelayPlayerFinished(double seconds)
        {
            if (session) session.FinishRelay(seconds);
        }

        /// <summary>The current event may be run alone (Practice), or the player chose time trial; an event not found keeps the opponents.</summary>
        bool HasOpponents()
        {
            if (_settings != null && _settings.CompetitorType == CompetitorType.TimeTrial) return false;
            if (_events == null || !session) return true;
            RaceEventId id = session.CurrentEventId;
            return id.IsEmpty || !_events.TryGet(id, out RaceEventDefinition definition) || definition.HasOpponents;
        }

        /// <summary>The measured bot table of the current event (T-086); null = the 100 m table with scaling, as before.</summary>
        BotEventProfile CurrentProfile()
        {
            if (!config || !session) return null;
            RaceEventId id = session.CurrentEventId;
            if (id.IsEmpty) return null;
            // An event without a table of its own (Race of the Day, whose length changes by day) takes the table of the run's length.
            return BotEventSetup.ForDistance(config, id.Value, RaceDistance, FlatEvents());
        }

        /// <summary>The flat events with opponents and their lengths: the anchors for choosing a table by distance.</summary>
        IReadOnlyList<BotEventSetup.FlatEvent> FlatEvents()
        {
            var flat = new List<BotEventSetup.FlatEvent>();
            if (_events == null) return flat;
            foreach (RaceEventDefinition definition in _events.GetEvents())
                if (definition.HurdleCount == 0 && definition.HasOpponents && definition.DistanceMeters > 0f)
                    flat.Add(new BotEventSetup.FlatEvent(definition.Id.Value, definition.DistanceMeters));
            return flat;
        }

        /// <summary>The player's best time of the current event (the difficulty is relative to it); null = none yet, the fallback.</summary>
        float? ReferenceSeconds(BotEventProfile profile)
        {
            if (_bests == null || !session) return null;
            RaceEventId id = session.CurrentEventId;
            if (id.IsEmpty) return null;
            double? best = _bests.TryGetBest(id, out double seconds) ? seconds : (double?)null;
            return BotEventSetup.ReferenceSeconds(profile, best, RaceDistance);
        }

        void CachePlayerIdentity()
        {
            string name = _settings != null ? _settings.PlayerName : string.Empty;
            _playerName = string.IsNullOrWhiteSpace(name) ? fallbackPlayerName : name.Trim();
            _playerFlag = _settings != null ? _settings.FlagCode ?? string.Empty : string.Empty;
        }

        /// <summary>A random outfit and skin tone from what the cosmetic services know; without them the bot keeps the prefab's look.</summary>
        void Dress(Bot bot) => DressRunner(bot.Look);

        /// <summary>A random outfit and skin tone on one runner copy (a bot or a relay runner).</summary>
        internal void DressRunner(CosmeticApplier look)
        {
            if (_cosmetics == null || _catalog == null || look == null) return;
            CosmeticOutfit outfit = _cosmetics.CreateRandomOutfit(_random);
            IReadOnlyList<string> tones = _catalog.SkinTones;
            if (tones.Count > 0) outfit = outfit.WithSkinColor(tones[_random.Next(tones.Count)]);
            look.Initialize();
            look.Apply(outfit, _catalog);
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
