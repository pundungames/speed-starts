using System;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Practice;
using SpeedStars.UI.Race;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Practice on the race (T-090): knows whether the current event is Practice (an event that runs alone), keeps the player's
    /// choices (<see cref="PracticeOptions"/>, saved) and hands them to the two places that build the race: the session adapter asks
    /// <see cref="AdjustDistance"/> for the start distance, <see cref="RaceHurdles"/> asks <see cref="TryGetHurdleLayout"/> for the
    /// hurdles. It binds itself into the panel view, so no installer line is needed. Lock energy, Reset energy and Standing start
    /// have no mechanic yet: they stay grey until <see cref="BindEnergy"/> / <see cref="BindStandingStart"/> get an implementation
    /// (needs: DECISION, the mechanics owner). Outside Practice nothing here changes the race.
    /// </summary>
    [DefaultExecutionOrder(5)]
    public sealed class PracticeSession : MonoBehaviour, IPracticeControl
    {
        [SerializeField, Tooltip("Race session adapter: the current event and the READY signal.")]
        RaceSessionAdapter session;
        [SerializeField, Tooltip("The Practice panel on the Race HUD; this component binds itself into it. Empty = no panel.")]
        PracticePanelView panel;
        [SerializeField, Tooltip("Start distances and hurdle layouts. Empty = the code defaults.")]
        PracticeConfig config;

        RaceEventCatalog _events;
        ISaveStore _store;
        PracticeOptions _options;
        IPracticeEnergy _energy;
        IPracticeStandingStart _standing;
        bool _subscribed;

        public event Action Changed;

        [Inject]
        void Construct([InjectOptional] RaceEventCatalog events, [InjectOptional] ISaveStore store)
        {
            _events = events;
            _store = store;
        }

        public PracticeOptions Options
        {
            get
            {
                if (_options == null)
                {
                    if (!config) config = PracticeConfig.CreateDefault();
                    _options = new PracticeOptions(config, _store);
                    _options.Changed += OnOptionsChanged;
                }
                return _options;
            }
        }

        public bool IsPractice => IsPracticeEvent(session ? session.CurrentEventId : default);
        public bool CanLockEnergy => _energy != null;
        public bool CanResetEnergy => _energy != null;
        public bool CanStandingStart => _standing != null;

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        void OnDisable()
        {
            if (_subscribed && session) session.StartPhaseChanged -= OnStartPhase;
            _subscribed = false;
        }

        void OnDestroy()
        {
            if (_options != null) _options.Changed -= OnOptionsChanged;
        }

        /// <summary>Idempotent setup; also callable by tests (AddComponent skips Awake in EditMode).</summary>
        public void Initialize()
        {
            if (!_subscribed && session)
            {
                session.StartPhaseChanged += OnStartPhase;
                _subscribed = true;
            }
            if (panel) panel.Bind(this);
        }

        /// <summary>The start distance of a race: the player's choice in Practice, else the requested one.</summary>
        public float AdjustDistance(RaceEventId eventId, float requestedMeters)
        {
            return IsPracticeEvent(eventId) ? Options.DistanceMeters : requestedMeters;
        }

        /// <summary>
        /// Practice: true, with the hurdles to lay (a count of 0 = a flat race). Not Practice: false, the event's own layout stands.
        /// </summary>
        public bool TryGetHurdleLayout(RaceEventId eventId, out int count, out float firstMeters, out float spacingMeters)
        {
            count = 0;
            firstMeters = 0f;
            spacingMeters = 0f;
            if (!IsPracticeEvent(eventId)) return false;
            if (Options.TryGetHurdles(out PracticeConfig.HurdleLayout layout))
            {
                count = layout.Count;
                firstMeters = layout.FirstMeters;
                spacingMeters = layout.SpacingMeters;
            }
            return true;
        }

        public void ResetEnergy()
        {
            if (_energy != null) _energy.Refill();
        }

        /// <summary>The runner's energy control, once the mechanics offer one. Applies the saved Lock energy choice.</summary>
        public void BindEnergy(IPracticeEnergy energy)
        {
            _energy = energy;
            ApplyEnergyLock();
            Changed?.Invoke();
        }

        /// <summary>The standing start, once the mechanics offer it.</summary>
        public void BindStandingStart(IPracticeStandingStart standing)
        {
            _standing = standing;
            ApplyStandingStart();
            Changed?.Invoke();
        }

        bool IsPracticeEvent(RaceEventId id)
        {
            return _events != null && !id.IsEmpty && _events.TryGet(id, out RaceEventDefinition definition) && !definition.HasOpponents;
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase != RaceStartPhase.Ready) return;
            // Each READY: the panel follows the event (shown in Practice only), the mechanics get the saved choices for this run.
            ApplyEnergyLock();
            ApplyStandingStart();
            Changed?.Invoke();
        }

        void OnOptionsChanged()
        {
            ApplyEnergyLock();
            ApplyStandingStart();
            Changed?.Invoke();
        }

        void ApplyEnergyLock()
        {
            if (_energy != null) _energy.SetLocked(IsPractice && Options.LockEnergy);
        }

        void ApplyStandingStart()
        {
            if (_standing != null) _standing.SetStanding(IsPractice && Options.StandingStart);
        }
    }
}
