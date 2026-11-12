using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Core.Race.Relay;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// A relay race (4x100m) run by NPC teams, as in the reference game: every lane is a team of runners, one per leg, each a copy of
    /// Malik's runner prefab driven like the bots (<see cref="BotPressSchedule"/> through <c>RunnerInput.Inject</c>). The first leg
    /// starts from the blocks at GO; every other runner waits a zone length before its leg line, sets off when its incoming runner
    /// comes close, keeps below the incoming speed until the baton reaches it, then runs its leg (rules: <see cref="RelayRules"/>).
    /// The player's lane is a team too, and the player runs all of it: the player's keys (after <see cref="RunnerPressGate"/>) go to
    /// whichever runner holds the baton, and the player's swipe up (the jump input) is the handoff. The next runner still sets off on
    /// its own. The player's own runner stays hidden on the start line; the camera and the world follow the baton. Owned by <see cref="RaceBots"/>, which plans the teams, hands the order to the standings and tells the session the
    /// finish. Nothing in Malik's code is changed: runners are placed before they wake, and only his public API is used.
    /// </summary>
    [DefaultExecutionOrder(-140)] // after RunnerInput (-200) and the player's RunnerPressGate (-150), before RunnerBody (-100)
    public sealed class RaceRelay : MonoBehaviour
    {
        [Header("Field")]
        [SerializeField, Min(2), Tooltip("Teams in a relay, the player's included. The reference game runs 8.")]
        int teamCount = 8;

        [Header("Exchange (measured from the reference game)")]
        [SerializeField, Min(0f), Tooltip("The next runner waits this far before its leg line (start of the exchange zone, m).")]
        float zoneBeforeLineMeters = 20f;
        [SerializeField, Min(0.5f), Tooltip("The next runner sets off when its incoming runner is this close behind (m). Set so the baton arrives at about 7 m/s, as in the reference game (its runners start at 8-11 m but accelerate faster than this physics: 0 to 7 m/s takes about 3.5 s here).")]
        float startGapMeters = 20f;
        [SerializeField, Min(0f), Tooltip("Each team's start gap moves up to this much either way, so the teams do not hand over alike (m).")]
        float startGapJitterMeters = 2.5f;
        [SerializeField, Min(0f), Tooltip("Swipe up (the handoff): for this long the baton arm reaches forward and the next runner's arm back (s).")]
        float swipeSeconds = 0.4f;
        [SerializeField, Min(0f), Tooltip("During a swipe the baton reaches the next runner from this far (m): it changes hands, a perfect handoff.")]
        float armReachMeters = 1.4f;
        [SerializeField, Min(0f), Tooltip("Without a swipe the runners run into each other at this gap (m): the baton changes hands anyway, a late handoff (the reference game when nobody swipes).")]
        float contactMeters = 0.2f;
        [SerializeField, Min(0f), Tooltip("An NPC team swipes when its next runner is this far ahead (m) ...")]
        float npcSwipeGapMeters = 1.7f;
        [SerializeField, Range(0f, 1f), Tooltip("... and misses the moment (no swipe: late handoff) with this chance.")]
        float npcMissChance = 0.1f;
        [SerializeField, Min(0f), Tooltip("Until it has the baton, the next runner skips presses while it is at least the incoming speed minus this (m/s): the gap always closes.")]
        float holdBackMarginMetersPerSecond = 0.4f;
        [SerializeField, Min(0f), Tooltip("Reaction of a next runner from its start signal to its first press (s).")]
        float outgoingReactionSeconds = 0.12f;

        [Header("Baton")]
        [SerializeField, Tooltip("Baton size (m): diameter, length.")]
        Vector2 batonSize = new Vector2(0.045f, 0.30f);
        [SerializeField, Tooltip("Baton offset in the hand prop bone (m).")]
        Vector3 batonOffset = Vector3.zero;
        [SerializeField, Tooltip("Baton rotation in the left hand prop bone (deg): across the fist, as Malik set it in play mode (2026-11-03).")]
        Vector3 batonEulerLeft = new Vector3(357.97f, 16.44f, 15.23f);
        [SerializeField, Tooltip("Baton rotation in the right hand prop bone (deg): the mirror of the left one (the rig's hands are symmetric).")]
        Vector3 batonEulerRight = new Vector3(357.65f, 344.21f, 344.92f);
        [SerializeField, Tooltip("Which hand holds the baton on each leg (1st, 2nd...): true = right. The usual relay order: right, left, right, left.")]
        bool[] batonRightHand = { true, false, true, false };

        [Header("Look")]
        [SerializeField, Min(0f), Tooltip("A runner that handed over stays visible this long, slowing down behind, then is put away (s).")]
        float handedOverVisibleSeconds = 4f;
        [SerializeField, Min(1f), Tooltip("A runner that handed over keeps pressing at its press period times this (slower), so it slows down upright as in the reference game; with no keys at all it would drop into its crouch / dive.")]
        float handedOverPeriodFactor = 2.2f;
        [SerializeField, Min(0f), Tooltip("The distance marker over the player's next runner shows from this gap on (m). The reference game counts down from about 12 m.")]
        float markerFromMeters = 13f;
        [SerializeField, Tooltip("Height of the distance marker above the next runner's feet (m).")]
        float markerHeight = 2.2f;
        [SerializeField, Tooltip("Leg colours (1st, 2nd, 3rd, 4th...): the split text and the distance marker of that leg.")]
        Color[] legColors =
        {
            new Color(0.08f, 0.80f, 0.72f),
            new Color(0.20f, 0.80f, 0.18f),
            new Color(0.88f, 0.80f, 0.05f),
            new Color(1.00f, 0.40f, 0.12f)
        };

        [Header("Driver")]
        [SerializeField, Min(1f), Tooltip("A leg's key schedule is made for this multiple of its target time, so a slow leg still has presses left.")]
        float scheduleHorizonFactor = 2f;
        [SerializeField, Min(0f), Tooltip("Seconds added to a leg's schedule length on top of the factor.")]
        float scheduleHorizonPadding = 10f;

        sealed class Leg
        {
            public GameObject Root;
            public RunnerBody Body;
            public RunnerInput Input;
            public CosmeticApplier Look;
            public int Index;        // 0-based leg number within its team
            public bool PlayerTeam;  // this runner belongs to the player's team
            public RelayBatonArms Arms;
            public float StartMeters;
            public BotPressSchedule Schedule;
            public int Next;
            public bool Running;
            public double StartTime;
            public double HandedOverAt = -1.0;
            public readonly bool[] PlayerHeld = new bool[2]; // the player's keys this runner was given and not yet released
            public Behaviour[] Feedback = Array.Empty<Behaviour>(); // the player's step feedback (perfect / early / late / momentum); player's team only

            public float Position => StartMeters + (Body && Body.Sim != null ? Body.Sim.Distance : 0f);
            public float Speed => Body && Body.Sim != null ? Body.Sim.Speed : 0f;
        }

        sealed class Team
        {
            public string Name = string.Empty;
            public string Flag = string.Empty;
            public bool IsPlayer;
            public BotPlan Plan;
            public Leg[] Legs;
            public int Holder;
            public float StartGap;
            public int LinesDone;
            public readonly List<double> Splits = new List<double>();
            public bool Finished;
            public double FinishSeconds;
            public float PrevPosition;
            public double PrevTime;
            public Transform Baton;
            public double SwipeUntil = -1.0;
            public bool SwipeDecided; // NPC: this exchange's swipe (or miss) is already chosen
        }

        readonly List<Team> _teams = new List<Team>();
        readonly System.Random _random = new System.Random();
        RaceBots _owner;
        int _builtLegs;
        float _builtLegMeters;
        int _legCount;
        float _legMeters;
        float _raceMeters;
        double _goTime;
        bool _racing;
        double[] _bestSplits = Array.Empty<double>();
        RaceRelayHud _hud;
        Material _batonMaterial;
        SpeedStars.Meta.Contracts.IRelayTeam _playerTeam;
        SpeedStars.Meta.Contracts.ICosmeticService _teamCosmetics;
        SpeedStars.Meta.Config.CosmeticCatalog _teamCatalog;

        /// <summary>True while the current race is a relay (from READY until the next non-relay race).</summary>
        public bool Active { get; private set; }
        public int TeamCount => teamCount;
        public int LegCount => _legCount;
        public float LegMeters => _legMeters;
        public Color LegColor(int leg) => legColors.Length == 0 ? Color.white : legColors[Mathf.Clamp(leg, 0, legColors.Length - 1)];

        /// <summary>The player's team crossed a leg line: line number (1 = first leg line), race time, behind the fastest team at that line (null = first there), the leg's own time (null for the first leg).</summary>
        public event Action<int, double, double?, double?> LineCrossed;
        /// <summary>The player's team handed the baton to this leg (0-based) with this quality.</summary>
        public event Action<int, HandoffQuality> HandedOver;
        /// <summary>A relay race began (READY) or ended (another event): the HUD shows or hides itself.</summary>
        public event Action ActiveChanged;

        // The player's team, for the session's telemetry.
        public float PlayerDistance => PlayerTeam() is Team t ? Mathf.Min(t.Finished ? _raceMeters : Baton(t).Position, _raceMeters) : 0f;
        public float PlayerSpeed => PlayerTeam() is Team t ? Baton(t).Speed : 0f;
        public float PlayerEnergy01
        {
            get
            {
                if (!(PlayerTeam() is Team t)) return 0f;
                RunnerSim sim = Baton(t).Body ? Baton(t).Body.Sim : null;
                return sim != null ? sim.Energy01 : 0f;
            }
        }

        /// <summary>The runner that holds the player's baton now (null outside a relay).</summary>
        public Transform PlayerBatonRunner => PlayerTeam() is Team t && Baton(t).Root ? Baton(t).Root.transform : null;

        public void Bind(RaceBots owner)
        {
            _owner = owner;
            if (!_hud && Application.isPlaying)
            {
                _hud = GetComponent<RaceRelayHud>();
                if (!_hud) _hud = gameObject.AddComponent<RaceRelayHud>();
                _hud.Bind(this, owner.Session);
            }
        }

        /// <summary>
        /// The player's relay team (T-160): each runner of the player's lane wears the outfit of its leg's preset. Called by <see cref="RaceBots"/>
        /// after <see cref="Bind"/>; without it (or with a null argument) every runner is dressed at random like the NPC teams.
        /// </summary>
        public void UseTeam(SpeedStars.Meta.Contracts.IRelayTeam team, SpeedStars.Meta.Contracts.ICosmeticService cosmetics,
            SpeedStars.Meta.Config.CosmeticCatalog catalog)
        {
            _playerTeam = team;
            _teamCosmetics = cosmetics;
            _teamCatalog = catalog;
        }

        /// <summary>
        /// A new relay race at READY: teams on their lanes, every runner back at its waiting spot, first two legs visible.
        /// <paramref name="plans"/>: one per team, fastest first; the player's team takes <paramref name="playerPlan"/>.
        /// </summary>
        public void BeginRound(IReadOnlyList<BotPlan> plans, int playerPlan, string playerName, string playerFlag, int legCount, float raceMeters,
            Material batonMaterial = null)
        {
            _batonMaterial = batonMaterial;
            _legCount = Mathf.Max(2, legCount);
            _raceMeters = raceMeters;
            _legMeters = raceMeters / _legCount;
            if (_builtLegs != _legCount || !Mathf.Approximately(_builtLegMeters, _legMeters)) Build();

            _racing = false;
            _bestSplits = new double[_legCount + 1];
            for (int i = 0; i < _bestSplits.Length; i++) _bestSplits[i] = double.MaxValue;

            // The player's team keeps lane 0 (the player's spot); the other plans go to the other lanes in a shuffled order.
            var others = new List<int>();
            for (int i = 0; i < plans.Count; i++) if (i != playerPlan) others.Add(i);
            Shuffle(others);

            for (int t = 0; t < _teams.Count; t++)
            {
                Team team = _teams[t];
                bool hasPlan = t == 0 ? playerPlan >= 0 && playerPlan < plans.Count : t - 1 < others.Count;
                BotPlan plan = !hasPlan ? null : t == 0 ? plans[playerPlan] : plans[others[t - 1]];
                team.IsPlayer = t == 0;
                team.Plan = plan;
                team.Name = team.IsPlayer ? playerName : plan != null ? plan.Name : string.Empty;
                team.Flag = team.IsPlayer ? playerFlag : plan != null ? plan.CountryCode : string.Empty;
                team.Holder = 0;
                team.LinesDone = 0;
                team.Splits.Clear();
                team.Finished = plan == null; // an empty lane takes no part
                team.FinishSeconds = 0.0;
                team.PrevPosition = 0f;
                team.PrevTime = 0.0;
                team.StartGap = startGapMeters + ((float)_random.NextDouble() * 2f - 1f) * startGapJitterMeters;
                team.SwipeUntil = -1.0;
                team.SwipeDecided = false;

                for (int l = 0; l < team.Legs.Length; l++)
                {
                    Leg leg = team.Legs[l];
                    leg.Schedule = null;
                    leg.Next = 0;
                    leg.Running = false;
                    leg.HandedOverAt = -1.0;
                    if (plan != null && l <= 1) Show(leg);
                    else leg.Root.SetActive(false);
                }
                if (plan != null) team.Legs[0].Schedule = Schedule(plan, plan.ReactionSeconds);
                for (int l = 0; l < team.Legs.Length; l++) SetFeedback(team.Legs[l], l == 0);
                GiveBaton(team, 0);
                team.Baton.gameObject.SetActive(plan != null);
            }

            SetActive(true);
            _owner.FollowRunner(PlayerBatonRunner, snap: true);
        }

        /// <summary>GO: the first legs run from now on.</summary>
        public void StartRace()
        {
            if (!Active) return;
            _goTime = Time.timeAsDouble;
            foreach (Team team in _teams)
            {
                if (team.Plan == null) continue;
                Leg first = team.Legs[0];
                first.Running = true;
                first.StartTime = _goTime;
                team.PrevTime = _goTime;
            }
            _racing = true;
        }

        /// <summary>Another event: every relay runner away, the camera and the world back on the player.</summary>
        public void End()
        {
            if (!Active) return;
            _racing = false;
            foreach (Team team in _teams)
                foreach (Leg leg in team.Legs)
                    if (leg.Root) leg.Root.SetActive(false);
            SetActive(false);
        }

        /// <summary>The live order: one entry per team that runs, the player's team marked.</summary>
        public void FillEntries(List<RaceEntry> entries)
        {
            foreach (Team team in _teams)
            {
                if (team.Plan == null) continue;
                float d = team.Finished ? _raceMeters : Mathf.Min(Baton(team).Position, _raceMeters);
                entries.Add(new RaceEntry(team.Name, team.Flag, team.IsPlayer, d, team.Finished, team.FinishSeconds));
            }
        }

        /// <summary>The distance marker over the player's next runner: where, how many metres the incoming runner is behind, which leg.</summary>
        public bool TryGetMarker(out Vector3 world, out float meters, out int leg)
        {
            world = Vector3.zero;
            meters = 0f;
            leg = 0;
            if (!_racing || !(PlayerTeam() is Team t) || t.Finished || t.Holder >= t.Legs.Length - 1) return false;
            Leg next = t.Legs[t.Holder + 1];
            if (!next.Root || !next.Root.activeInHierarchy) return false;
            meters = next.Position - Baton(t).Position;
            if (meters > markerFromMeters) return false;
            meters = Mathf.Max(0f, meters);
            world = next.Root.transform.position + Vector3.up * markerHeight;
            leg = t.Holder + 1;
            return true;
        }

        void Update()
        {
            ForwardPlayerKeys();
            if (!_racing) return;
            double now = Time.timeAsDouble;
            foreach (Team team in _teams)
            {
                if (team.Plan == null) continue;
                for (int l = 0; l < team.Holder; l++)
                    if (team.Legs[l].HandedOverAt >= 0.0 && team.Legs[l].Root.activeSelf) Drive(team.Legs[l], now, null); // slowing down behind
                if (team.Finished) continue;
                Leg holder = Baton(team);
                if (!team.IsPlayer) Drive(holder, now, null); // the player's holder runs on the player's keys

                if (team.Holder >= team.Legs.Length - 1) continue;
                Leg next = team.Legs[team.Holder + 1];
                float gap = next.Position - holder.Position;
                if (!next.Running && RelayRules.ShouldStart(gap, team.StartGap))
                {
                    next.Running = true;
                    next.StartTime = now;
                    next.Next = 0;
                    next.Schedule = Schedule(team.Plan, outgoingReactionSeconds);
                }
                if (next.Running) Drive(next, now, holder);
            }
        }

        /// <summary>
        /// The player's keys, as the press gate left them in the player's queue (false start, UI touches and the finish already
        /// handled there), go to the runner that holds the player's baton; the hidden player's own runner gets none. A release goes
        /// only to the runner that got the press, so a key still held across a handoff does nothing to the new runner until the next
        /// press. Runs before RunnerBody (-100), which would otherwise take the queue. In the countdown only releases and the gate's
        /// false-start fall come through, so the first runner falls as the player's runner would.
        /// </summary>
        void ForwardPlayerKeys()
        {
            if (!Active || !_owner || !_owner.Player || !_owner.Player.input) return;
            RunnerInput source = _owner.Player.input;
            Team team = PlayerTeam();
            Leg holder = team != null && !team.Finished ? Baton(team) : null;
            while (source.TryDequeue(out TapEvent tap))
            {
                if (holder == null || tap.Leg < 0 || tap.Leg > 1) continue;
                if (!tap.Pressed && !holder.PlayerHeld[tap.Leg]) continue;
                holder.PlayerHeld[tap.Leg] = tap.Pressed;
                holder.Input.Inject(tap.Leg, tap.Pressed, tap.Time);
            }
        }

        /// <summary>The player's swipe up (the jump input): the handoff of the player's team.</summary>
        void OnPlayerSwipe()
        {
            if (_racing && PlayerTeam() is Team team) Swipe(team, Time.timeAsDouble);
        }

        /// <summary>
        /// The player's step feedback (the perfect step burst and the others) is on only on the runner that runs on the player's keys,
        /// as on the player's own runner in a sprint, and only with particle effects on in the settings. NPC teams have none.
        /// </summary>
        void SetFeedback(Leg leg, bool on)
        {
            bool show = on && _owner.ParticleEffectsOn;
            foreach (Behaviour effect in leg.Feedback)
                if (effect && effect.enabled != show) effect.enabled = show;
        }

        /// <summary>Lets go of the player's keys that this runner still holds (it gave the baton away).</summary>
        static void ReleasePlayerKeys(Leg leg, double now)
        {
            for (int side = 0; side < leg.PlayerHeld.Length; side++)
            {
                if (!leg.PlayerHeld[side]) continue;
                leg.PlayerHeld[side] = false;
                leg.Input.Inject(side, false, now);
            }
        }

        void OnDestroy()
        {
            if (Active && _owner && _owner.Player && _owner.Player.input) _owner.Player.input.JumpPressed -= OnPlayerSwipe;
        }

        void LateUpdate()
        {
            if (!_racing) return;
            double now = Time.timeAsDouble;
            foreach (Team team in _teams)
            {
                if (team.Plan == null) continue;
                PutAwayHandedOver(team, now);
                if (team.Finished) continue;

                // Exchange first: the line below is judged by the new holder's place.
                if (team.Holder < team.Legs.Length - 1) UpdateExchange(team, now);

                float position = Baton(team).Position;
                while (!team.Finished && team.LinesDone < _legCount && position >= (team.LinesDone + 1) * _legMeters)
                {
                    int line = team.LinesDone + 1;
                    double at = RelayRules.CrossingTime(line * _legMeters, team.PrevPosition, team.PrevTime, position, now) - _goTime;
                    CrossLine(team, line, at);
                }
                team.PrevPosition = position;
                team.PrevTime = now;
            }
        }

        void CrossLine(Team team, int line, double seconds)
        {
            team.LinesDone = line;
            team.Splits.Add(seconds);
            double? behind = _bestSplits[line] < double.MaxValue ? seconds - _bestSplits[line] : (double?)null;
            if (seconds < _bestSplits[line]) _bestSplits[line] = seconds;
            double? legTime = line >= 2 ? seconds - team.Splits[line - 2] : (double?)null;

            if (line >= _legCount)
            {
                team.Finished = true;
                team.FinishSeconds = seconds;
                LetGo(Baton(team));
            }
            if (!team.IsPlayer) return;
            LineCrossed?.Invoke(line, seconds, behind, legTime);
            if (team.Finished) _owner.RelayPlayerFinished(seconds);
        }

        /// <summary>
        /// The swipe and the baton: an NPC team swipes when its next runner is close (or misses the moment); while a swipe lasts
        /// the arms reach and the baton passes as soon as it reaches. Without a swipe the runners meet and the baton passes late.
        /// </summary>
        void UpdateExchange(Team team, double now)
        {
            Leg holder = Baton(team);
            Leg next = team.Legs[team.Holder + 1];
            if (!next.Running) return;
            float gap = next.Position - holder.Position;
            // From the moment it sets off until the baton is in its hand, the next runner runs with that hand held back.
            next.Arms.Set(RelayBatonArms.Reach.Take, RightHand(team.Holder + 1));

            if (!team.IsPlayer && !team.SwipeDecided && gap <= npcSwipeGapMeters) // the player's team swipes on the player's input
            {
                team.SwipeDecided = true;
                if (_random.NextDouble() >= npcMissChance) Swipe(team, now);
            }

            bool swiping = now <= team.SwipeUntil;
            if (swiping && RelayRules.ShouldExchange(gap, armReachMeters)) Exchange(team, now, HandoffQuality.Perfect);
            else if (RelayRules.ShouldExchange(gap, contactMeters)) Exchange(team, now, HandoffQuality.Late);
            else if (!swiping && team.SwipeUntil > 0.0) EndReach(team); // the swipe came too early: the arms come back
        }

        /// <summary>
        /// Swipe up: the baton arm reaches forward and the free arm back, for <see cref="swipeSeconds"/> (the next runner already
        /// holds its hand back). NPC teams call it themselves; the player's swipe up calls it for the player's team. Only once the next
        /// runner has set off: before that there is nobody to reach for.
        /// </summary>
        void Swipe(Team team, double now)
        {
            if (team.Finished || team.Holder >= team.Legs.Length - 1 || !team.Legs[team.Holder + 1].Running) return;
            team.SwipeUntil = now + swipeSeconds;
            Baton(team).Arms.Set(RelayBatonArms.Reach.Give, RightHand(team.Holder));
        }

        /// <summary>The giving arms come back (a swipe that came too early, or the exchange).</summary>
        void EndReach(Team team)
        {
            team.SwipeUntil = -1.0;
            Baton(team).Arms.Set(RelayBatonArms.Reach.None, false);
        }

        void Exchange(Team team, double now, HandoffQuality quality)
        {
            Leg incoming = Baton(team);
            Leg next = team.Legs[team.Holder + 1];
            EndReach(team);
            next.Arms.Set(RelayBatonArms.Reach.None, false); // the baton is in its hand: the arm swings again
            team.SwipeDecided = false;
            if (team.IsPlayer)
            {
                // The player's keys leave with the baton: the old runner lets go of them, the new one drops its NPC keys and waits for the player's next press.
                ReleasePlayerKeys(incoming, now);
                next.Input.Inject(0, false, now);
                next.Input.Inject(1, false, now);
            }
            // The step feedback follows the player's keys: the slowing-down presses behind are not the player's.
            SetFeedback(incoming, false);
            SetFeedback(next, true);
            GiveBaton(team, team.Holder + 1);
            // Slows down running: a slower schedule from now on, until it is put away.
            incoming.HandedOverAt = now;
            incoming.StartTime = now;
            incoming.Next = 0;
            incoming.Schedule = Schedule(team.Plan, 0f, handedOverPeriodFactor);
            team.Holder++;
            if (team.Holder + 1 < team.Legs.Length) Show(team.Legs[team.Holder + 1]);
            if (!team.IsPlayer) return;
            _owner.FollowRunner(next.Root.transform, snap: false);
            HandedOver?.Invoke(team.Holder, quality);
        }

        bool RightHand(int leg) => batonRightHand.Length == 0 || batonRightHand[leg % batonRightHand.Length];

        /// <summary>The team's baton into this leg's hand (its hand prop bone).</summary>
        void GiveBaton(Team team, int leg)
        {
            if (!team.Baton) team.Baton = CreateBaton();
            Transform hand = team.Legs[leg].Arms.HandProp(RightHand(leg));
            if (!hand) hand = team.Legs[leg].Root.transform;
            team.Baton.SetParent(hand, false);
            team.Baton.localPosition = batonOffset;
            team.Baton.localRotation = Quaternion.Euler(RightHand(leg) ? batonEulerRight : batonEulerLeft);
            // The same size in the world whatever the bone's scale (the primitive is 1 m wide and 2 m long).
            Vector3 parentScale = hand.lossyScale;
            Vector3 world = new Vector3(batonSize.x, batonSize.y * 0.5f, batonSize.x);
            team.Baton.localScale = new Vector3(world.x / Safe(parentScale.x), world.y / Safe(parentScale.y), world.z / Safe(parentScale.z));
        }

        static float Safe(float scale) => Mathf.Abs(scale) < 1e-6f ? 1f : scale;

        /// <summary>A thin cylinder with the event's baton material; no collider (nothing here uses physics contacts).</summary>
        Transform CreateBaton()
        {
            GameObject baton = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baton.name = "Relay Baton";
            Destroy(baton.GetComponent<Collider>());
            if (_batonMaterial) baton.GetComponent<MeshRenderer>().sharedMaterial = _batonMaterial;
            return baton.transform;
        }

        /// <summary>Feeds a running leg its due key events; an incoming runner given = this leg waits for the baton and may hold back.</summary>
        void Drive(Leg leg, double now, Leg incoming)
        {
            if (!leg.Running || leg.Schedule == null) return;
            IReadOnlyList<BotPressEvent> events = leg.Schedule.Events;
            while (leg.Next < events.Count && leg.StartTime + events[leg.Next].Time <= now)
            {
                BotPressEvent e = events[leg.Next++];
                if (e.IsPress && incoming != null && RelayRules.ShouldHoldBack(leg.Speed, incoming.Speed, holdBackMarginMetersPerSecond)) continue;
                leg.Input.Inject(e.Leg, e.IsPress, leg.StartTime + e.Time);
            }
        }

        void PutAwayHandedOver(Team team, double now)
        {
            foreach (Leg leg in team.Legs)
                if (leg.HandedOverAt >= 0.0 && now - leg.HandedOverAt > handedOverVisibleSeconds && leg.Root.activeSelf)
                    leg.Root.SetActive(false);
        }

        BotPressSchedule Schedule(BotPlan plan, float reactionSeconds, float periodFactor = 1f)
        {
            var legPlan = new BotPlan(plan.Name, plan.CountryCode, plan.TargetSeconds, plan.Skill, reactionSeconds, plan.PressPeriodSeconds * periodFactor,
                plan.HoldSeconds, plan.JitterSeconds, plan.MistakeChance, plan.MistakeShiftRatio, plan.Quirks);
            return new BotPressSchedule(legPlan, _random, plan.TargetSeconds * scheduleHorizonFactor + scheduleHorizonPadding);
        }

        /// <summary>No more presses, keys released: the runner slows down on its own physics.</summary>
        static void LetGo(Leg leg)
        {
            leg.Running = false;
            if (!leg.Body) return;
            leg.Body.acceptInput = false;
            double now = Time.timeAsDouble;
            leg.Input.Inject(0, false, now);
            leg.Input.Inject(1, false, now);
        }

        /// <summary>Wakes a runner at its waiting spot, standing still, freshly dressed.</summary>
        void Show(Leg leg)
        {
            leg.Root.SetActive(true);
            leg.Body.ResetRun(); // back to the spot it woke at, input queue cleared, acceptInput on
            leg.PlayerHeld[0] = leg.PlayerHeld[1] = false;
            leg.Arms.Clear();
            if (leg.PlayerTeam && WearTeamOutfit(leg)) return;
            _owner.DressRunner(leg.Look);
        }

        /// <summary>A leg of the player's team wears its saved preset. False when the team is not wired: the caller dresses it at random.</summary>
        bool WearTeamOutfit(Leg leg)
        {
            if (_playerTeam == null || _teamCosmetics == null || _teamCatalog == null || !leg.Look) return false;
            leg.Look.Initialize();
            leg.Look.Apply(_teamCosmetics.GetOutfit(_playerTeam.GetPreset(leg.Index)), _teamCatalog);
            return true;
        }

        /// <summary>One runner copy per team and leg, each placed at its waiting spot before it wakes (RunnerBody reads its start there).</summary>
        void Build()
        {
            foreach (Team team in _teams)
                foreach (Leg leg in team.Legs)
                    if (leg.Root) Destroy(leg.Root);
            _teams.Clear();

            GameObject prefab = _owner.RunnerPrefab;
            RunnerBody player = _owner.Player;
            if (!prefab || !player) return;

            var holder = new GameObject("Relay Spawn Holder");
            holder.SetActive(false);
            Vector3 forward = player.transform.forward;
            for (int t = 0; t < teamCount; t++)
            {
                var team = new Team { Legs = new Leg[_legCount] };
                Vector3 lane = _owner.RelayLanePosition(t == 0 ? (int?)null : t - 1, teamCount - 1);
                for (int l = 0; l < _legCount; l++)
                {
                    float start = RelayRules.LegStartMeters(l, _legMeters, zoneBeforeLineMeters);
                    team.Legs[l] = CreateRunner($"Relay {t + 1}.{l + 1}", lane + forward * start, start, holder.transform);
                    team.Legs[l].Index = l;
                    team.Legs[l].PlayerTeam = t == 0;
                    if (t == 0) AddPlayerFeedback(team.Legs[l]); // the player's team: the player's step feedback, as on the player's runner
                }
                _teams.Add(team);
            }
            Destroy(holder);
            _builtLegs = _legCount;
            _builtLegMeters = _legMeters;
        }

        static void AddPlayerFeedback(Leg leg)
        {
            leg.Body.EnablePlayerFeedback();
            GameObject root = leg.Body.gameObject;
            leg.Feedback = new Behaviour[]
            {
                root.GetComponent<PerfectStepFeedback>(), root.GetComponent<EarlyStepFeedback>(), root.GetComponent<LateStepFeedback>(),
                root.GetComponent<MomentumWindFeedback>(),
            };
        }

        Leg CreateRunner(string runnerName, Vector3 position, float startMeters, Transform holder)
        {
            GameObject root = Instantiate(_owner.RunnerPrefab, holder);
            root.SetActive(false); // stays asleep after leaving the holder; wakes (Awake reads its start) when its leg is shown
            root.name = runnerName;
            var leg = new Leg { Root = root, StartMeters = startMeters };
            leg.Input = root.GetComponentInChildren<RunnerInput>(true);
            leg.Body = root.GetComponentInChildren<RunnerBody>(true);
            if (leg.Body == null || leg.Input == null)
                throw new InvalidOperationException($"'{_owner.RunnerPrefab.name}' has no RunnerBody / RunnerInput; a relay runner cannot run on it.");
            leg.Input.enabled = false; // no keyboard / touch; Inject still fills its queue
            leg.Body.input = leg.Input;
            foreach (RunnerBodyHUD hud in root.GetComponentsInChildren<RunnerBodyHUD>(true)) DestroyImmediate(hud);
            foreach (RunnerReplay replay in root.GetComponentsInChildren<RunnerReplay>(true)) DestroyImmediate(replay);
            leg.Look = root.GetComponent<CosmeticApplier>();
            if (leg.Look == null) leg.Look = root.AddComponent<CosmeticApplier>();
            leg.Arms = root.AddComponent<RelayBatonArms>(); // the exchange pose, over the running arms

            root.transform.SetPositionAndRotation(position, _owner.Player.transform.rotation);
            root.transform.SetParent(transform, true);
            return leg;
        }

        void SetActive(bool active)
        {
            if (Active == active) return;
            Active = active;
            _owner.SetPlayerHidden(active);
            RunnerInput playerInput = _owner.Player ? _owner.Player.input : null;
            if (playerInput)
            {
                if (active) playerInput.JumpPressed += OnPlayerSwipe;
                else playerInput.JumpPressed -= OnPlayerSwipe;
            }
            if (!active) _owner.FollowRunner(null, snap: true);
            ActiveChanged?.Invoke();
        }

        Team PlayerTeam() => Active && _teams.Count > 0 && _teams[0].Plan != null ? _teams[0] : null;

        static Leg Baton(Team team) => team.Legs[Mathf.Clamp(team.Holder, 0, team.Legs.Length - 1)];

        void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
