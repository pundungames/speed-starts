using SpeedStars.UI.Framework;
using UnityEngine;
using Zenject;

namespace SpeedStars.Installers
{
    /// <summary>
    /// What every scene's UI needs: its own screen stack, the root screen opened at start and the back key.
    /// Home and Race derive from this and add their own bindings.
    /// </summary>
    public abstract class SceneUiInstaller : MonoInstaller
    {
        [Tooltip("Screen shown when the scene opens; the only screen active in the scene at start.")]
        [SerializeField] private UiScreen rootScreen;

        public sealed override void InstallBindings()
        {
            Container.Bind<ScreenNavigator>().AsSingle();
            if (rootScreen != null)
                Container.BindInterfacesTo<RootScreenOpener>().AsSingle().WithArguments((IUiView)rootScreen);
            else
                Debug.LogError($"{GetType().Name}: root screen is not assigned.", this);
            Container.BindInterfacesTo<BackKeyReader>().AsSingle();

            InstallSceneBindings();
        }

        protected virtual void InstallSceneBindings()
        {
        }
    }

    /// <summary>Puts the scene's root screen on the stack once the scene is injected.</summary>
    public sealed class RootScreenOpener : IInitializable
    {
        private readonly ScreenNavigator _navigator;
        private readonly IUiView _root;

        public RootScreenOpener(ScreenNavigator navigator, IUiView root)
        {
            _navigator = navigator;
            _root = root;
        }

        public void Initialize() => _navigator.Push(_root);
    }

    /// <summary>
    /// Escape / Android back closes the top popup, then pops screens. Lives here because the UI assembly does not
    /// reference the Input System; at the root screen it does nothing.
    /// </summary>
    public sealed class BackKeyReader : ITickable
    {
        private readonly ScreenNavigator _navigator;

        public BackKeyReader(ScreenNavigator navigator) => _navigator = navigator;

        public void Tick()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) _navigator.Back();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) _navigator.Back();
#endif
        }
    }
}
