using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.UI.Flow
{
    /// <summary>Plays the departure motion before loading the next scene in single mode.</summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public const float DepartureDuration = 3.5f;
        private const float DepartureZoom = 0.87f;

        public bool Load(string sceneName, Action onLoaded)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneFlow] Scene '{sceneName}' is not in Build Settings; the move is cancelled.");
                return false;
            }

            _ = AnimateAndLoad(sceneName, onLoaded);
            return true;
        }

        private static async Task AnimateAndLoad(string sceneName, Action onLoaded)
        {
            Camera camera = Camera.main;
            bool orthographic = camera && camera.orthographic;
            float startLens = camera ? (orthographic ? camera.orthographicSize : camera.fieldOfView) : 0f;
            try
            {
                float elapsed = 0f;
                while (Application.isPlaying && elapsed < DepartureDuration)
                {
                    await Task.Yield();
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / DepartureDuration);
                    float eased = Mathf.SmoothStep(0f, 1f, progress);
                    SetLens(camera, orthographic, Mathf.Lerp(startLens, startLens * DepartureZoom, eased));
                }

                // Present the final pose before beginning the load, which may stall the next frame.
                await Task.Yield();
                if (!Application.isPlaying) return;

                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    Debug.LogError($"[SceneFlow] Loading '{sceneName}' could not start.");
                    return;
                }

                while (!operation.isDone)
                    await Task.Yield();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                SetLens(camera, orthographic, startLens);
                onLoaded?.Invoke();
            }
        }

        private static void SetLens(Camera camera, bool orthographic, float value)
        {
            if (!camera) return;
            if (orthographic) camera.orthographicSize = value;
            else camera.fieldOfView = value;
        }
    }
}
