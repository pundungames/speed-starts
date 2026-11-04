using System;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Zenject shell of the Race scene flow (the decisions are in RaceFlowLogic). Bound in RaceSceneInstaller with
    /// BindInterfacesAndSelfTo; the entry gate prompt, personal bests, wallet and settings are optional.
    /// </summary>
    public sealed class RaceFlow : IInitializable, ITickable, IDisposable, IRaceHudActions
    {
        private readonly RaceFlowLogic _logic;

        public RaceFlow(IRaceSession session, IRaceEntryGate gate, IRaceSelection selection, IRaceEventLookup events,
            ScreenNavigator navigator, SceneFlow sceneFlow, IResultsView results, IRaceRewardResolver rewards, [InjectOptional] IEntryGatePrompt prompt,
            [InjectOptional] IPersonalBests records, [InjectOptional] IWallet wallet, [InjectOptional] IPlayerSettings settings,
            [InjectOptional] IDayBestSource dayBests, [InjectOptional] IClock clock, [InjectOptional] IRaceTraces traces,
            [InjectOptional] IRaceCashRewardResolver cashRewards, [InjectOptional] IRaceOfTheDay raceOfTheDay,
            [InjectOptional] RaceEventCatalog catalog)
        {
            _logic = new RaceFlowLogic(session, gate, selection, events, navigator, sceneFlow, results, rewards, prompt, records, wallet, settings, dayBests, clock, traces, cashRewards, raceOfTheDay, IsPracticeIn(catalog), FindPracticeIn(catalog));
        }

        /// <summary>The first playable, free event without opponents (the rule of the Practice panel); empty id when the catalog has none.</summary>
        private static Func<RaceEventId> FindPracticeIn(RaceEventCatalog catalog)
        {
            if (catalog == null) return null;
            return () =>
            {
                foreach (RaceEventDefinition definition in catalog.GetEvents())
                    if (definition.IsPlayable && !definition.RequiresTicket && !definition.HasOpponents) return definition.Id;
                return default;
            };
        }

        /// <summary>Practice = an event with no opponents (the same rule as the Practice panel); no catalog = no event is Practice.</summary>
        private static Func<RaceEventId, bool> IsPracticeIn(RaceEventCatalog catalog)
        {
            if (catalog == null) return null;
            return id => catalog.TryGet(id, out RaceEventDefinition definition) && !definition.HasOpponents;
        }

        public void Initialize() => _logic.Start();

        /// <summary>Samples the run for the Analysis chart (ITickable comes with BindInterfacesAndSelfTo).</summary>
        public void Tick() => _logic.Tick();

        public void Dispose() => _logic.Dispose();

        public void RequestExit() => _logic.Exit();

        public void RequestRestart() => _logic.Restart();
    }
}
