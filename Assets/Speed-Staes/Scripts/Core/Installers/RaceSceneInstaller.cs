using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;
using UnityEngine;

namespace SpeedStars.Installers
{
    /// <summary>
    /// Race scene: IRaceSession is the Bridge adapter over the runner's RaceController (scene instance, Inspector reference),
    /// RaceFlow drives start / finish / results, the Results screen is bound as its view, the opponent bots as IRaceStandings.
    /// </summary>
    public sealed class RaceSceneInstaller : SceneUiInstaller
    {
        [Tooltip("Adapter over the runner's RaceController / RunnerBody in this scene (SpeedStars.Bridge).")]
        [SerializeField] private RaceSessionAdapter raceSession;

        [Tooltip("Results screen in this scene; RaceFlow opens it when the race finishes.")]
        [SerializeField] private ResultsScreenView results;

        [Tooltip("Opponent bots of this race (Bridge); they also keep the live order. Empty = no opponents, the HUD place and the results list stay single-runner.")]
        [SerializeField] private RaceBots raceBots;

        protected override void InstallSceneBindings()
        {
            if (raceSession == null || results == null)
            {
                Debug.LogError("RaceSceneInstaller: raceSession and results must be assigned.", this);
                return;
            }
            // One adapter serves the race flow (IRaceSession) and the HUD numbers (IRaceTelemetry, UI v2).
            Container.Bind(typeof(IRaceSession), typeof(IRaceTelemetry)).FromInstance(raceSession).AsSingle();
            Container.Bind<IResultsView>().FromInstance(results).AsSingle();
            // T-075/T-076: the bots keep the live order the HUD and the results list read (both inject it as optional).
            if (raceBots != null) Container.Bind<IRaceStandings>().FromInstance(raceBots).AsSingle();
            Container.Bind<IRaceEventLookup>().To<CatalogRaceEventLookup>().AsSingle();
            Container.BindInterfacesAndSelfTo<RaceFlow>().AsSingle().NonLazy();
        }
    }
}
