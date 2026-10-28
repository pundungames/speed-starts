using DG.Tweening;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Clock;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Race;
using SpeedStars.Meta.Tickets;
using SpeedStars.Meta.Wallet;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using Zenject;

namespace SpeedStars.Installers
{
    /// <summary>
    /// Session-wide composition root, on the ProjectContext prefab. Config assets are Inspector references so every
    /// number stays in its asset; services are plain C# objects created once here and shared by both scenes.
    /// Lives for the whole session, so it also writes the save when the app is paused or closed.
    /// </summary>
    public sealed class ProjectInstaller : MonoInstaller
    {
        [Header("Config assets (Data/Config)")]
        [SerializeField] private EconomyConfig economy;
        [SerializeField] private RaceEventCatalog raceEvents;
        [SerializeField] private GemRewardConfig gemRewards;
        [SerializeField] private SevenDayRewardConfig sevenDayRewards;
        [SerializeField] private CosmeticCatalog cosmetics;
        [SerializeField] private UiMotionConfig uiMotion;

        [Header("Scenes")]
        [SerializeField] private SceneFlowScenes scenes = new SceneFlowScenes();

        [Header("DOTween pools (technical, not balance)")]
        [Tooltip("Tweeners allocated up front so the pool does not grow (and allocate) mid-game. Defaults = DOTween's own.")]
        [SerializeField, Min(1)] private int tweenCapacity = 200;
        [Tooltip("Sequences allocated up front.")]
        [SerializeField, Min(1)] private int sequenceCapacity = 50;

        private ProgressRepository _progress;

        public override void InstallBindings()
        {
            // Zenject validation (editor, not playing) runs installers too; DOTween.Init returns null there.
            if (!Container.IsValidating) DOTween.Init()?.SetCapacity(tweenCapacity, sequenceCapacity);

            BindConfig(economy);
            BindConfig(raceEvents);
            BindConfig(gemRewards);
            BindConfig(sevenDayRewards);
            BindConfig(cosmetics);
            BindConfig(uiMotion);

            Container.Bind<ISaveStore>().FromMethod(_ => FileSaveStore.CreateDefault()).AsSingle();
            Container.Bind<ProgressRepository>().FromMethod(CreateProgress).AsSingle();
            Container.Bind(typeof(IClock), typeof(ClockService)).FromMethod(CreateClock).AsSingle();

            Container.BindInstance(scenes);
            Container.Bind<ISceneLoader>().To<UnitySceneLoader>().AsSingle();
            Container.Bind<SceneFlow>().AsSingle();

            // Faz 2 (T-010): one EntryTicketService instance serves the UI (IEntryTickets) and the gate (concrete TrySpend).
            Container.Bind(typeof(IEntryTickets), typeof(EntryTicketService)).To<EntryTicketService>().AsSingle();
            Container.Bind(typeof(IWallet), typeof(WalletService)).To<WalletService>().AsSingle();
            Container.Bind<IRaceSelection>().To<RaceSelection>().AsSingle();
            Container.Bind<IRaceEntryGate>().To<RaceEntryGate>().AsSingle();
            // Project-wide because the Entry Gate modal sits in both scenes (Play on Home, Race again on Race).
            Container.Bind<HomeEntryFlow>().AsSingle();
        }

        private void BindConfig<T>(T asset) where T : ScriptableObject
        {
            if (asset == null)
            {
                Debug.LogError($"ProjectInstaller: {typeof(T).Name} asset is not assigned.", this);
                return;
            }
            Container.BindInstance(asset);
        }

        private ProgressRepository CreateProgress(InjectContext context)
        {
            // Starting balances only matter for a brand-new save; they come from the economy asset, never from code.
            _progress = new ProgressRepository(context.Container.Resolve<ISaveStore>(), () => economy != null
                ? new StartingBalances(economy.StartingCash, economy.StartingPurpleGems, economy.StartingEntryTickets, economy.StartingTimedTickets)
                : default);
            _progress.Load();
            return _progress;
        }

        private ClockService CreateClock(InjectContext context)
        {
            // Read through the repository every time: Load can replace Current, the latest seen time must follow it.
            var progress = context.Container.Resolve<ProgressRepository>();
            return new ClockService(() => progress.Current.LastSeenUtcTicks, ticks => progress.Current.LastSeenUtcTicks = ticks);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) _progress?.Save();
        }

        private void OnApplicationQuit()
        {
            _progress?.Save();
        }
    }
}
