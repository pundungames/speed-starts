using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;
using UnityEngine;

namespace SpeedStars.Installers
{
    /// <summary>
    /// Race scene: IRaceSession is the Bridge adapter over the runner's RaceController (scene instance, Inspector reference),
    /// RaceFlow drives start / finish / results, the Results screen is bound as its view.
    /// </summary>
    public sealed class RaceSceneInstaller : SceneUiInstaller
    {
        [Tooltip("Adapter over the runner's RaceController / RunnerBody in this scene (SpeedStars.Bridge).")]
        [SerializeField] private RaceSessionAdapter raceSession;

        [Tooltip("Results screen in this scene; RaceFlow opens it when the race finishes.")]
        [SerializeField] private ResultsScreenView results;

        protected override void InstallSceneBindings()
        {
            if (raceSession == null || results == null)
            {
                Debug.LogError("RaceSceneInstaller: raceSession and results must be assigned.", this);
                return;
            }
            Container.Bind<IRaceSession>().FromInstance(raceSession).AsSingle();
            Container.Bind<IResultsView>().FromInstance(results).AsSingle();
            Container.Bind<IRaceEventLookup>().To<CatalogRaceEventLookup>().AsSingle();
            Container.BindInterfacesAndSelfTo<RaceFlow>().AsSingle().NonLazy();
        }
    }
}
