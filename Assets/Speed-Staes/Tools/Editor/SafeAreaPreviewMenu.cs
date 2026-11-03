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
    /// Safety net: before any editor save the driven preview anchors are dropped (so no inset value can reach a scene or
    /// prefab file), and the preview is put back right after.
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
}
