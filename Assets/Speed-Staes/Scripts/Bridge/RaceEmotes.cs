using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;
using SpeedStars.Physical;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The emotes of the Race scene (T-089). The player's runner carries Malik's <c>RunnerEmote</c> (baked animations laid over the
    /// run; its key and his play-next rule stay as they are). This component only chooses WHICH clip it plays: it hands the
    /// runner's <c>clips</c> list the one equipped clip (none = empty list, so the key does nothing), and only from GO on, so
    /// the emote key, the HUD button (<see cref="IRaceEmoteControl"/>) and the automatic emote all play the equipped emote and
    /// never before the start. With <see cref="IEmoteRaceOptions.AutoEmoteSelf"/> the player's runner plays it in the finish sequence
    /// (<see cref="PlayFinishEmote"/>); with <see cref="IEmoteRaceOptions.AutoEmoteCompetitors"/> each opponent
    /// plays one of the clips when it crosses the line (read from his runner body, no change in RaceBots). Decisions are in
    /// <c>RaceEmoteRules</c> (pure, tested). Added to the player's runner by the Race scene builder; every dependency is optional,
    /// without them the runner keeps its prefab list.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class RaceEmotes : MonoBehaviour, IRaceEmoteControl
    {
        [SerializeField, Tooltip("The player's RunnerEmote. Empty = the one on this object or below it.")]
        RunnerEmote playerEmote;

        [SerializeField, Tooltip("The opponents (their runner roots are read for the automatic emote). Empty = no opponent emotes.")]
        RaceBots bots;

        [SerializeField, Tooltip("Show the playing emote's name on screen (RunnerEmote.showName). Off: the clip asset name is a file name, the HUD shows the display name.")]
        bool showClipName;

        sealed class Opponent
        {
            public RunnerBody Body;
            public RunnerEmote Emote;
            public RunnerEmoteData[] Clips;
            public bool Done;
        }

        IEmoteService _emotes;
        EmoteCatalog _catalog;
        IRaceSession _session;
        IRaceStartSignal _signal;
        IEmoteRaceOptions _options = new DefaultEmoteRaceOptions();

        RunnerEmoteData[] _allClips = new RunnerEmoteData[0];
        RunnerEmoteData _equippedClip;
        string _equippedName = string.Empty;
        bool _initialized;
        bool _subscribed;
        bool _clipsOpen;
        bool _lastCan;
        float _busyUntil;
        int _round;
        readonly List<Opponent> _opponents = new List<Opponent>();
        int _opponentRoots = -1;
        static readonly RunnerEmoteData[] NoClips = new RunnerEmoteData[0];

        public event Action Changed;

        // Optional so a scene without the emote bindings keeps the runner as the prefab has it.
        [Inject]
        void Construct([InjectOptional] IEmoteService emotes, [InjectOptional] EmoteCatalog catalog, [InjectOptional] IRaceSession session,
            [InjectOptional] IEmoteRaceOptions options)
        {
            _emotes = emotes;
            _catalog = catalog;
            _session = session;
            _signal = session as IRaceStartSignal;
            if (options != null) _options = options;
        }

        public bool HasEmote
        {
            get
            {
                Initialize();
                return _equippedClip != null;
            }
        }

        public bool CanEmote => RaceEmoteRules.CanPlay(HasEmote, PhaseOrNull(), Busy);

        public string EmoteName => HasEmote ? _equippedName : string.Empty;

        bool Busy => Time.time < _busyUntil;

        RaceStartPhase? PhaseOrNull() => _signal != null ? _signal.StartPhase : (RaceStartPhase?)null;

        void Awake() => Initialize();

        void OnEnable()
        {
            Initialize();
            Subscribe();
        }

        void Start()
        {
            // Injection may land after Awake / OnEnable: read the equipped emote again and bind to the injected services.
            Unsubscribe();
            if (_initialized)
            {
                ReadEquipped();
                ApplyPlayerClips(force: true);
            }
            Subscribe();
        }

        void OnDisable() => Unsubscribe();

        /// <summary>Idempotent: finds the runner's emote player and remembers its clip list (also callable where AddComponent skips Awake).</summary>
        public void Initialize()
        {
            if (_initialized) return;
            if (!playerEmote) playerEmote = GetComponentInChildren<RunnerEmote>(true);
            if (!playerEmote) return;
            _initialized = true;
            _allClips = playerEmote.clips ?? NoClips;
            playerEmote.showName = showClipName;
            ReadEquipped();
            ApplyPlayerClips(force: true);
        }

        void Subscribe()
        {
            if (_subscribed || !_initialized) return;
            _subscribed = true;
            if (_emotes != null) _emotes.Changed += OnEquippedChanged;
            if (_signal != null) _signal.StartPhaseChanged += OnPhaseChanged;
            _options.Changed += OnOptionsChanged;
            if (playerEmote.input) playerEmote.input.EmotePressed += OnKeyPressed;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_emotes != null) _emotes.Changed -= OnEquippedChanged;
            if (_signal != null) _signal.StartPhaseChanged -= OnPhaseChanged;
            _options.Changed -= OnOptionsChanged;
            if (playerEmote && playerEmote.input) playerEmote.input.EmotePressed -= OnKeyPressed;
        }

        /// <summary>Plays the equipped emote on the player's runner; false when it is not accepted (see <see cref="CanEmote"/>).</summary>
        public bool RequestEmote()
        {
            Initialize();
            if (!CanEmote) return false;
            ApplyPlayerClips(force: false);
            playerEmote.Play();
            MarkBusy();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// The finish sequence's moment: the equipped emote, whatever the phase (the countdown word is gone by then). Seconds it lasts;
        /// 0 when nothing plays (none equipped, or the player's "Emote myself" is off). A clip still playing just goes on.
        /// </summary>
        public float PlayFinishEmote()
        {
            Initialize();
            if (!RaceEmoteRules.AutoSelfWanted(_options.AutoEmoteSelf, _equippedClip != null)) return 0f;
            _clipsOpen = true; // the next Update sees the phase and empties the list again; a key press finds nothing
            playerEmote.clips = new[] { _equippedClip };
            playerEmote.Play();
            MarkBusy();
            Changed?.Invoke();
            return _equippedClip.Length;
        }

        void OnEquippedChanged()
        {
            ReadEquipped();
            ApplyPlayerClips(force: true);
            Changed?.Invoke();
        }

        void OnOptionsChanged() => Changed?.Invoke();

        void OnPhaseChanged(RaceStartPhase phase)
        {
            if (phase == RaceStartPhase.Go) return;
            // A new race is being prepared: opponents may emote again, and each round starts their clips from another place.
            _round++;
            foreach (Opponent opponent in _opponents) opponent.Done = false;
            _busyUntil = 0f;
        }

        // The runner's own emote key goes straight to RunnerEmote; we only learn that it was taken.
        void OnKeyPressed()
        {
            if (_equippedClip != null && !Busy && _clipsOpen) MarkBusy();
        }

        void MarkBusy() => _busyUntil = Time.time + (_equippedClip != null ? _equippedClip.Length : 0f);

        void ReadEquipped()
        {
            _equippedClip = null;
            _equippedName = string.Empty;
            if (_emotes == null || _catalog == null) return;
            if (!_catalog.TryGet(_emotes.EquippedId, out EmoteDefinition emote) || !emote.HasClip) return;
            foreach (RunnerEmoteData clip in _allClips)
            {
                if (clip == null || !string.Equals(clip.name, emote.ClipName, StringComparison.Ordinal)) continue;
                _equippedClip = clip;
                _equippedName = emote.DisplayName;
                return;
            }
        }

        /// <summary>The runner's clip list is the equipped clip while a press is allowed, empty otherwise. Only rewritten when that changes.</summary>
        void ApplyPlayerClips(bool force)
        {
            if (!playerEmote) return;
            bool open = _equippedClip != null && RaceEmoteRules.CanPlay(true, PhaseOrNull(), false);
            if (!force && open == _clipsOpen) return;
            _clipsOpen = open;
            playerEmote.clips = open ? new[] { _equippedClip } : NoClips;
        }

        void Update()
        {
            if (!_initialized) return;
            ApplyPlayerClips(force: false);

            bool can = CanEmote;
            if (can != _lastCan)
            {
                _lastCan = can;
                Changed?.Invoke();
            }

            if (_options.AutoEmoteCompetitors && bots != null && _session != null) UpdateOpponents();
        }

        void UpdateOpponents()
        {
            if (PhaseOrNull() != null && PhaseOrNull() != RaceStartPhase.Go) return;
            IReadOnlyList<Transform> roots = bots.RunnerRoots;
            if (roots.Count != _opponentRoots) CollectOpponents(roots);

            float target = _session.TargetDistance;
            for (int i = 0; i < _opponents.Count; i++)
            {
                Opponent opponent = _opponents[i];
                if (opponent.Done || opponent.Body == null || opponent.Body.Sim == null) continue;
                if (!RaceEmoteRules.CrossedLine(opponent.Body.Sim.Distance, target)) continue;
                opponent.Done = true;
                int pick = RaceEmoteRules.BotClipIndex(i, _round, opponent.Clips.Length);
                if (pick < 0 || opponent.Clips[pick] == null) continue;
                opponent.Emote.clips = new[] { opponent.Clips[pick] };
                opponent.Emote.Play();
            }
        }

        // Runs when the bots are built (their count changes), never per frame.
        void CollectOpponents(IReadOnlyList<Transform> roots)
        {
            _opponentRoots = roots.Count;
            _opponents.Clear();
            foreach (Transform root in roots)
            {
                var body = root.GetComponentInChildren<RunnerBody>(true);
                var emote = root.GetComponentInChildren<RunnerEmote>(true);
                if (body == null || emote == null || emote.clips == null || emote.clips.Length == 0) continue;
                emote.showName = showClipName;
                _opponents.Add(new Opponent { Body = body, Emote = emote, Clips = emote.clips });
            }
        }
    }
}
