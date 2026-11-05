using SpeedStars.Core.Audio;
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
    /// T-120: a hurdle the player knocks over plays ObstacleHit. The runner has no event for it, so the count of
    /// <see cref="RunnerHurdles.Knocked"/> is watched; a count that drops (new race, reset) only follows it down, silently.
    /// </summary>
    public sealed class RaceHurdles : MonoBehaviour
    {
        [SerializeField, Tooltip("Race session adapter: the READY signal and the current event.")]
        RaceSessionAdapter session;
        [SerializeField, Tooltip("The hurdles on the player's lane.")]
        RunnerHurdles hurdles;
        [SerializeField, Tooltip("Practice (T-090): in a Practice race the Hurdles box decides the layout. Empty = the event's own layout.")]
        PracticeSession practice;

        RaceEventCatalog _events;
        ISfx _sfx;
        int _knockedSeen;

        [Inject]
        void Construct([InjectOptional] RaceEventCatalog events) => _events = events;

        /// <summary>Optional: without a bound sound system a knocked hurdle is silent.</summary>
        [Inject]
        void InjectAudio([InjectOptional] ISfx sfx) => _sfx = sfx;

        void OnEnable()
        {
            if (session) session.StartPhaseChanged += OnStartPhase;
        }

        void OnDisable()
        {
            if (session) session.StartPhaseChanged -= OnStartPhase;
        }

        void Update()
        {
            if (!hurdles) return;
            int knocked = hurdles.Knocked;
            if (knocked > _knockedSeen) _sfx?.Play(SfxId.ObstacleHit);
            _knockedSeen = knocked;
        }

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase != RaceStartPhase.Ready || !hurdles) return;
            RaceEventDefinition definition = null;
            bool found = _events != null && _events.TryGet(session.CurrentEventId, out definition);
            int count = found ? definition.HurdleCount : 0;
            float first = found ? definition.FirstHurdleMeters : 0f;
            float spacing = found ? definition.HurdleSpacingMeters : 0f;
            if (practice && practice.TryGetHurdleLayout(session.CurrentEventId, out int pCount, out float pFirst, out float pSpacing))
            {
                count = pCount;
                first = pFirst;
                spacing = pSpacing;
            }
            hurdles.Configure(count, first, spacing);
            _knockedSeen = hurdles.Knocked;
            if (hurdles.body) hurdles.body.acceptJump = count > 0;
        }
    }
}
