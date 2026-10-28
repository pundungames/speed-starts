using System;
using UnityEditor;
using UnityEngine;
using Zenject.Internal;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Zenject's own validation over every enabled Build Settings scene: resolves all bindings without running the game,
    /// so a missing binding shows up here instead of as a Play-mode exception. Batchmode:
    /// -executeMethod SpeedStars.Tools.Builders.ZenjectSmoke.ValidateBuildScenes
    /// </summary>
    public static class ZenjectSmoke
    {
        public const string OkMarker = "ZENJECT_VALIDATE_OK";

        [MenuItem("Tools/Speed Stars/Validate Zenject (build scenes)")]
        public static void ValidateBuildScenes()
        {
            try
            {
                int count = ZenUnityEditorUtil.ValidateAllActiveScenes();
                Debug.Log($"{OkMarker} scenes={count}");
            }
            catch (Exception e)
            {
                Debug.LogError("ZENJECT_VALIDATE_FAILED: " + e.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
