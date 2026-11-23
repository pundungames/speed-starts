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
    /// never before the start. Automatic emotes respect the player and competitor switches. The player uses the equipped clip;
    /// each AI picks one clip per round for overtakes and its finish.
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

        [SerializeField, Min(0f), Tooltip("Race seconds after GO before passing another runner can play an emote.")]
        float overtakeDelaySeconds = 3f;

        const float ConfirmedLeadMeters = 0.45f;

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
        readonly System.Random _clipRandom = new System.Random();
        readonly List<RunnerEmoteData> _botLibrary = new List<RunnerEmoteData>();
        readonly List<RaceBots.EmoteParticipant> _participants = new List<RaceBots.EmoteParticipant>();
        readonly Dictionary<Transform, RunnerEmote> _runnerEmotes = new Dictionary<Transform, RunnerEmote>();
        readonly Dictionary<int, RunnerEmoteData> _roundClips = new Dictionary<int, RunnerEmoteData>();
        readonly Dictionary<(int First, int Second), bool> _pairLeaders = new Dictionary<(int, int), bool>();
        readonly HashSet<int> _finishEmotes = new HashSet<int>();
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
            foreach (RunnerEmoteData clip in _allClips)
                if (clip && clip.FrameCount > 0) _botLibrary.Add(clip);
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
            if (phase != RaceStartPhase.Ready) return;
            _pairLeaders.Clear();
            _roundClips.Clear();
            _finishEmotes.Clear();
            _runnerEmotes.Clear();
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

            if (bots && _session != null) UpdateRaceEmotes();
        }

        void UpdateRaceEmotes()
        {
            if (PhaseOrNull() != null && PhaseOrNull() != RaceStartPhase.Go) return;
            bots.CollectEmoteParticipants(_participants);

            foreach (RaceBots.EmoteParticipant runner in _participants)
            {
                if (runner.IsPlayer) continue;
                if (!_roundClips.ContainsKey(runner.Slot) && _botLibrary.Count > 0)
                    _roundClips.Add(runner.Slot, _botLibrary[_clipRandom.Next(_botLibrary.Count)]);
                if (runner.Finished && _options.AutoEmoteCompetitors && _finishEmotes.Add(runner.Slot))
                    PlayAutomaticEmote(runner);
            }

            if (_session.State != RaceSessionState.Running || _session.RaceTime < overtakeDelaySeconds) return;
            UpdateOvertakes();
        }

        void UpdateOvertakes()
        {
            for (int i = 0; i < _participants.Count; i++)
            {
                RaceBots.EmoteParticipant first = _participants[i];
                if (first.Finished) continue;
                for (int j = i + 1; j < _participants.Count; j++)
                {
                    RaceBots.EmoteParticipant second = _participants[j];
                    if (second.Finished) continue;
                    float gap = first.Distance - second.Distance;
                    if (Mathf.Abs(gap) < ConfirmedLeadMeters) continue;

                    var pair = (first.Slot, second.Slot);
                    bool firstLeads = gap > 0f;
                    bool changed = _pairLeaders.TryGetValue(pair, out bool ledBefore) && ledBefore != firstLeads;
                    _pairLeaders[pair] = firstLeads;
                    if (changed) PlayAutomaticEmote(firstLeads ? first : second);
                }
            }
        }

        void PlayAutomaticEmote(RaceBots.EmoteParticipant runner)
        {
            RunnerEmoteData clip;
            if (runner.IsPlayer)
            {
                if (!RaceEmoteRules.AutoSelfWanted(_options.AutoEmoteSelf, _equippedClip != null)) return;
                clip = _equippedClip;
            }
            else
            {
                if (!_options.AutoEmoteCompetitors || !_roundClips.TryGetValue(runner.Slot, out clip)) return;
            }

            if (!_runnerEmotes.TryGetValue(runner.Root, out RunnerEmote emote))
            {
                emote = runner.Root.GetComponentInChildren<RunnerEmote>(true);
                if (emote) emote.showName = showClipName;
                _runnerEmotes.Add(runner.Root, emote);
            }
            if (!emote || !emote.TryPlay(clip) || !runner.IsPlayer) return;
            MarkBusy();
            Changed?.Invoke();
        }
    }
}
