using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Editor
{
    /// <summary>
    /// Checkable menu that switches the Edit-mode notch preview of every SafeAreaFitter. Off shows (and lets you edit)
    /// the authored, un-inset layout; on shows the layout inside the safe area of the simulated device.
    /// </summary>
    public static class SafeAreaPreviewMenu
    {
        private const string MenuPath = "Tools/Speed Stars/Notch Preview (Edit Mode)";

        [MenuItem(MenuPath, false, 200)]
        private static void Toggle()
        {
            bool on = !SafeAreaFitter.EditorPreview;
            SafeAreaFitter.SetEditorPreview(on);
            Menu.SetChecked(MenuPath, on);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, SafeAreaFitter.EditorPreview);
            return true;
        }
    }

    /// <summary>
    /// Unity serializes driven anchors as (0,0), so the preview must be dropped whenever the scene is serialized: before any
    /// editor save (here) and before entering Play mode (the play-entry scene backup does not go through OnWillSaveAssets).
    /// The preview is put back right after.
    /// </summary>
    public sealed class SafeAreaPreviewSaveGuard : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (Application.isPlaying) return paths;
            SafeAreaFitter.SuspendPreviewForSave();
            EditorApplication.delayCall += SafeAreaFitter.ResumePreviewAfterSave;
            return paths;
        }
    }

    /// <summary>Drops the preview when Play mode is about to back the scene up, and brings it back on return to Edit mode.</summary>
    [InitializeOnLoad]
    public static class SafeAreaPreviewPlayGuard
    {
        static SafeAreaPreviewPlayGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) SafeAreaFitter.SuspendPreviewForSave();
            else if (state == PlayModeStateChange.EnteredEditMode) SafeAreaFitter.ResumePreviewAfterSave();
        }
    }
}
