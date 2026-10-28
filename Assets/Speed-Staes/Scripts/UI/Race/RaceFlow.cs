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
    /// BindInterfacesAndSelfTo; the entry gate prompt is optional.
    /// </summary>
    public sealed class RaceFlow : IInitializable, IDisposable, IRaceHudActions
    {
        private readonly RaceFlowLogic _logic;

        public RaceFlow(IRaceSession session, IRaceEntryGate gate, IRaceSelection selection, IRaceEventLookup events,
            ScreenNavigator navigator, SceneFlow sceneFlow, IResultsView results, IRaceRewardResolver rewards, [InjectOptional] IEntryGatePrompt prompt)
        {
            _logic = new RaceFlowLogic(session, gate, selection, events, navigator, sceneFlow, results, rewards, prompt);
        }

        public void Initialize() => _logic.Start();

        public void Dispose() => _logic.Dispose();

        public void RequestExit() => _logic.Exit();

        public void RequestRestart() => _logic.Restart();
    }
}
