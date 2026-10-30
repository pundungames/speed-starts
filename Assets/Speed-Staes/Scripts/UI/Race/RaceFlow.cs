using System;
using SpeedStars.Core.Race;
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
            [InjectOptional] IPersonalBests records, [InjectOptional] IWallet wallet, [InjectOptional] IPlayerSettings settings)
        {
            _logic = new RaceFlowLogic(session, gate, selection, events, navigator, sceneFlow, results, rewards, prompt, records, wallet, settings);
        }

        public void Initialize() => _logic.Start();

        /// <summary>Samples the run for the Analysis chart (ITickable comes with BindInterfacesAndSelfTo).</summary>
        public void Tick() => _logic.Tick();

        public void Dispose() => _logic.Dispose();

        public void RequestExit() => _logic.Exit();

        public void RequestRestart() => _logic.Restart();
    }
}
