using System;

namespace SpeedStars.UI.Flow
{
    /// <summary>
    /// Home ↔ Race moves for the whole session (bound once, in the project installer). One move at a time: a request
    /// while a load runs is refused, so a double tap on a button loads once. From the request until the new scene is
    /// active IsTransitioning is true and TransitionChanged fires, so each scene's SceneTransitionBlocker swallows UI
    /// input for the whole move. Scene names come from SceneFlowScenes; loading goes through ISceneLoader (tests use a fake).
    /// </summary>
    public sealed class SceneFlow
    {
        private readonly ISceneLoader _loader;
        private readonly SceneFlowScenes _scenes;
        private int _loadToken;

        public SceneFlow(ISceneLoader loader, SceneFlowScenes scenes)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }

        /// <summary>True from an accepted request until its scene is active. UI input is locked meanwhile.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>Raised with true when a move starts and false when it ends (or could not start).</summary>
        public event Action<bool> TransitionChanged;

        /// <summary>
        /// Moves to <paramref name="scene"/>. False when refused: another move is running, the scene has no name set,
        /// or the loader could not start it. Moving to the current scene reloads it.
        /// </summary>
        public bool GoTo(GameScene scene)
        {
            if (IsTransitioning) return false;

            string sceneName = _scenes.NameOf(scene);
            if (string.IsNullOrEmpty(sceneName))
            {
                UnityEngine.Debug.LogError($"[SceneFlow] No scene name is set for {scene}; the move is cancelled.");
                return false;
            }

            int token = ++_loadToken;
            SetTransitioning(true);

            bool started = false;
            try
            {
                started = _loader.Load(sceneName, () => OnLoaded(token));
            }
            finally
            {
                if (!started) OnLoaded(token); // refused or threw: unlock, the move never happened
            }
            return started;
        }

        private void OnLoaded(int token)
        {
            // A late or repeated callback of an older move must not unlock the move that runs now.
            if (token != _loadToken) return;
            SetTransitioning(false);
        }

        private void SetTransitioning(bool value)
        {
            if (IsTransitioning == value) return;
            IsTransitioning = value;
            TransitionChanged?.Invoke(value);
        }
    }
}
