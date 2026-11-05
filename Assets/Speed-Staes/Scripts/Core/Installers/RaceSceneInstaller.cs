using SpeedStars.Bridge;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Home;
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

        [Tooltip("Entry gate modal in this scene; it opens when Race again finds no ticket. Empty = Race again only logs a warning.")]
        [SerializeField] private EntryGateModalView entryGateModal;

        [Tooltip("Race emotes on the player's runner root (Bridge): the HUD button / key plays the chosen emote; it also does the auto emotes. Empty = no emotes in the race.")]
        [SerializeField] private RaceEmotes raceEmotes;

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
            // T-081: left / right arrow = left / right foot of the player's runner, next to Malik's N/M and touch.
            Container.BindInterfacesTo<ArrowKeyRunnerInput>().AsSingle().WithArguments(raceSession).NonLazy();
            // T-120: the sound of the player's own steps.
            Container.BindInterfacesTo<RaceStepSounds>().AsSingle().WithArguments(raceSession).NonLazy();
            // T-075/T-076: the bots keep the live order the HUD and the results list read (both inject it as optional).
            if (raceBots != null) Container.Bind(typeof(IRaceStandings), typeof(IRaceTraces)).FromInstance(raceBots).AsSingle();
            Container.Bind<IRaceEventLookup>().To<CatalogRaceEventLookup>().AsSingle();
            if (entryGateModal != null) Container.Bind<IEntryGatePrompt>().To<RaceFlowEntryGatePrompt>().AsSingle().WithArguments(entryGateModal);
            // T-089: the chosen emote in the race (HUD button, key, auto emote at the finish).
            if (raceEmotes != null)
            {
                Container.Bind<IRaceEmoteControl>().FromInstance(raceEmotes).AsSingle();
                Container.Bind<RaceEmotes>().FromInstance(raceEmotes).AsSingle();
            }
            // The picture between the line and the Results.
            Container.Bind<IRaceFinishSequence>().To<RaceFinishSequence>().FromNewComponentOn(raceSession.gameObject).AsSingle();
            Container.BindInterfacesAndSelfTo<RaceFlow>().AsSingle().NonLazy();
        }
    }
}
