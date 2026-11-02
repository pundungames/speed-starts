using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Physical;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Gives each race the hurdles of its event: at READY the event's layout goes to <see cref="RunnerHurdles"/>, and the jump
    /// only works where there are hurdles (a swipe up in a flat race does nothing). An event not found is a flat race.
    /// </summary>
    public sealed class RaceHurdles : MonoBehaviour
    {
        [SerializeField, Tooltip("Race session adapter: the READY signal and the current event.")]
        RaceSessionAdapter session;
        [SerializeField, Tooltip("The hurdles on the player's lane.")]
        RunnerHurdles hurdles;

        RaceEventCatalog _events;

        [Inject]
        void Construct([InjectOptional] RaceEventCatalog events) => _events = events;

        void OnEnable()
        {
            if (session) session.StartPhaseChanged += OnStartPhase;
        }

        void OnDisable()
        {
            if (session) session.StartPhaseChanged -= OnStartPhase;
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase != RaceStartPhase.Ready || !hurdles) return;
            RaceEventDefinition definition = null;
            bool found = _events != null && _events.TryGet(session.CurrentEventId, out definition);
            int count = found ? definition.HurdleCount : 0;
            hurdles.Configure(count, found ? definition.FirstHurdleMeters : 0f, found ? definition.HurdleSpacingMeters : 0f);
            if (hurdles.body) hurdles.body.acceptJump = count > 0;
        }
    }
}
