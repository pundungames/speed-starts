using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.DailyRace;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Race of the Day (T-087): everybody in the race wears the day's outfit. It starts a round with READY like everything else;
    /// <c>RaceBots</c> dresses its opponents with a random look right then, so this component waits for the end of that frame
    /// (<c>LateUpdate</c>, decided by <see cref="RaceOfTheDayDressState"/>) and puts the day's outfit over the player's runner and
    /// every running opponent. The player's saved outfit and presets are never written: the next race of another event starts
    /// again from <c>RaceRunnerOutfit</c> (a fresh scene) and the bots' own random dressing. Added to the player's runner by the
    /// Race scene builder (<c>SceneCosmetics</c>); every dependency is optional, without them nothing changes.
    /// </summary>
    [DefaultExecutionOrder(30)]
    public sealed class RaceOfTheDayOutfit : MonoBehaviour
    {
        [SerializeField, Tooltip("The scene's race session adapter: its start phase and the event id of the current race. Empty = this component does nothing.")]
        RaceSessionAdapter session;

        [SerializeField, Tooltip("The opponents (their runner roots are dressed too). Empty = only the player's runner is dressed.")]
        RaceBots bots;

        [SerializeField, Tooltip("Applier on the player's runner root. Empty = the one on this object (added when missing).")]
        CosmeticApplier playerApplier;

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
            if (_daily == null || _catalog == null || !session) { _state.TakeApply(false, false); return; }

            bool runnersBuilt = !bots || bots.RunnerRoots.Count > 0;
            if (!_state.TakeApply(_daily.IsRaceOfTheDay(session.CurrentEventId), runnersBuilt)) return;
            Dress(_daily.Today.Outfit);
        }

        void Dress(CosmeticOutfit outfit)
        {
            if (!playerApplier) playerApplier = GetComponent<CosmeticApplier>() ?? gameObject.AddComponent<CosmeticApplier>();
            playerApplier.Initialize();
            playerApplier.Apply(outfit, _catalog);

            if (!bots) return;
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
