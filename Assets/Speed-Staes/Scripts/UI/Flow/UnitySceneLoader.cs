using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.UI.Flow
{
    /// <summary>
    /// ISceneLoader over SceneManager.LoadSceneAsync (single mode). The new scene's SceneContext picks up the
    /// ProjectContext on its own, so session services (SceneFlow included) survive the move.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public bool Load(string sceneName, Action onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                UnityEngine.Debug.LogError($"[SceneFlow] Scene '{sceneName}' is not in Build Settings; the move is cancelled.");
                return false;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null) return false;

            operation.completed += _ => onLoaded?.Invoke();
            return true;
        }
    }
}
