using System;

namespace SpeedStars.UI.Flow
{
    /// <summary>
    /// Starts a scene load and reports when the new scene is active. Abstracted so SceneFlow's rules (one move at a
    /// time, input locked until the end) are tested without real scenes.
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>
        /// Begins loading <paramref name="sceneName"/> (replacing the current scene). Returns false when the load could
        /// not start (e.g. the scene is not in Build Settings); then <paramref name="onLoaded"/> is never called.
        /// </summary>
        bool Load(string sceneName, Action onLoaded);
    }
}
