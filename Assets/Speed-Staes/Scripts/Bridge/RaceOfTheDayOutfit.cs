using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.DailyRace;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>Applies the day's cosmetic theme to the opponents after their normal outfits are set.</summary>
    [DefaultExecutionOrder(30)]
    public sealed class RaceOfTheDayOutfit : MonoBehaviour
    {
        [SerializeField, Tooltip("The scene's race session adapter: its start phase and the event id of the current race. Empty = this component does nothing.")]
        RaceSessionAdapter session;

        [SerializeField, Tooltip("The opponents to dress in the daily theme.")]
        RaceBots bots;

        IRaceOfTheDay _daily;
        CosmeticCatalog _catalog;
        readonly RaceOfTheDayDressState _state = new RaceOfTheDayDressState();
        bool _subscribed;

        [Inject]
        void Construct([InjectOptional] IRaceOfTheDay daily, [InjectOptional] CosmeticCatalog catalog)
        {
            _daily = daily;
            _catalog = catalog;
        }

        void Awake() => Initialize();
        void OnEnable() => Initialize();

        void OnDisable()
        {
            if (_subscribed && session) session.StartPhaseChanged -= OnPhase;
            _subscribed = false;
        }

        /// <summary>Idempotent (also callable where AddComponent skips Awake): subscribes to the start phase.</summary>
        public void Initialize()
        {
            if (_subscribed || !session) return;
            session.StartPhaseChanged += OnPhase;
            _subscribed = true;
            // Bound after READY already went out (the first race of the scene): dress this round too.
            if (session.StartPhase == RaceStartPhase.Ready || session.StartPhase == RaceStartPhase.Set) _state.RoundStarted();
        }

        void OnPhase(RaceStartPhase phase)
        {
            if (phase == RaceStartPhase.Ready) _state.RoundStarted();
        }

        void LateUpdate()
        {
            if (!_state.Pending) return;
            if (_daily == null || _catalog == null || !session || !bots) { _state.TakeApply(false, false); return; }

            bool runnersBuilt = bots.RunnerRoots.Count > 0;
            if (!_state.TakeApply(_daily.IsRaceOfTheDay(session.CurrentEventId), runnersBuilt)) return;
            Dress(_daily.Today.Outfit);
        }

        void Dress(CosmeticOutfit outfit)
        {
            foreach (Transform root in bots.RunnerRoots)
            {
                if (!root || !root.gameObject.activeInHierarchy) continue; // a lane without a bot this race
                var applier = root.GetComponent<CosmeticApplier>();
                if (!applier) continue;
                applier.Initialize();
                applier.Apply(outfit, _catalog);
            }
        }
    }
}
