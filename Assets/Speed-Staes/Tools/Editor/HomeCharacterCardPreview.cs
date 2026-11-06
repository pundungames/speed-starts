using SpeedStars.UI.Home;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools
{
    /// <summary>Preview the responsive card without running the screen's gameplay lifecycle.</summary>
    [InitializeOnLoad]
    internal static class HomeCharacterCardPreview
    {
        private static double _nextRefresh;

        static HomeCharacterCardPreview()
        {
            EditorApplication.update += Refresh;
        }

        private static void Refresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + 0.1;
            foreach (var view in Object.FindObjectsOfType<HomeScreenView>())
            {
                if (!view.gameObject.scene.IsValid()
                    || UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(view.gameObject.scene)) continue;
                view.RefreshCharacterCardLayout();
            }
        }
    }
}
