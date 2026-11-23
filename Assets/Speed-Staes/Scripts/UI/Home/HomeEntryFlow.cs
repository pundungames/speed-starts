using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Pays for an entry through the gate and, when it went through, moves to the race scene. Shared by the Play button
    /// (free events) and the entry gate modal's confirm. No Zenject attribute: the container picks the single constructor.
    /// </summary>
    public sealed class HomeEntryFlow
    {
        private readonly IRaceEntryGate _gate;
        private readonly SceneFlow _sceneFlow;

        public HomeEntryFlow(IRaceEntryGate gate, SceneFlow sceneFlow)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public bool IsTransitioning => _sceneFlow.IsTransitioning;

        internal event Action<bool> TransitionChanged
        {
            add => _sceneFlow.TransitionChanged += value;
            remove => _sceneFlow.TransitionChanged -= value;
        }

        public TimeSpan DailyRaceCooldown => (_gate as IDailyRaceEntryStatus)?.DailyRaceCooldown ?? TimeSpan.Zero;

        /// <summary>Enters <paramref name="id"/>; the race scene is requested only for a successful entry.</summary>
        public EntryOutcome TryEnter(RaceEventId id)
        {
            EntryOutcome outcome = HomeEntryRules.Resolve(_gate.Enter(id));
            if (outcome == EntryOutcome.StartRace) _sceneFlow.GoTo(GameScene.Race);
            return outcome;
        }
    }
}
