using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using SpeedStars.Themes;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Takes a full-screen picture (3D + UI) of every enabled Build Settings scene without entering Play mode, so layout
    /// can be checked from batchmode. Overlay canvases are not drawn into a camera's texture, so for the shot they are
    /// switched to Screen Space - Camera on the main camera; the scene is reopened without saving, nothing is kept.
    /// Batchmode (graphics on, no -nographics): -executeMethod SpeedStars.Tools.Builders.ShotTool.CaptureBuildScenes -shotDir <folder>
    /// </summary>
    public static class ShotTool
    {
        private const int Width = 2400;
        private const int Height = 1080;

        [MenuItem("Tools/Speed Stars/Capture build scene shots")]
        public static void CaptureBuildScenes()
        {
            string dir = ArgValue("-shotDir") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "shots");
            Directory.CreateDirectory(dir);
            foreach (var entry in EditorBuildSettings.scenes)
            {
                if (!entry.enabled) continue;
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                var camera = Camera.main;
                if (camera == null) { Debug.LogWarning($"SHOT skipped {entry.path}: no main camera"); continue; }

                SwitchOverlayCanvasesToCamera(camera);
                RenderToPng(camera, Path.Combine(dir, Path.GetFileNameWithoutExtension(entry.path) + ".png"));
                EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single); // drop the canvas changes, never saved
            }
        }

        /// <summary>
        /// Opens the built Home scene and renders its camera once per theme (showcase index 0..n-1) to
        /// home_theme_N.png, to check every framing. 3D world only; add -shotUi to draw the UI on top too.
        /// -executeMethod SpeedStars.Tools.Builders.ShotTool.CaptureHomeThemes -shotDir <folder outside Temp>
        /// </summary>
        public static void CaptureHomeThemes()
        {
            string dir = ArgValue("-shotDir") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "shots");
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(SceneBuilder.HomeScenePath, OpenSceneMode.Single);

            var showcase = UnityEngine.Object.FindObjectOfType<MainMenuThemeShowcase>(true);
            if (showcase == null) { Debug.LogError("SHOT_FAILED no MainMenuThemeShowcase in Home"); return; }
            Camera camera = showcase.previewCamera != null ? showcase.previewCamera : Camera.main;
            if (camera == null) { Debug.LogError("SHOT_FAILED no camera in Home"); return; }

            if (HasArg("-shotUi")) SwitchOverlayCanvasesToCamera(camera);
            int count = showcase.themeDisplays != null ? showcase.themeDisplays.Length : 0;
            for (int i = 0; i < count; i++)
            {
                showcase.SelectTheme(i);
                RenderToPng(camera, Path.Combine(dir, $"home_theme_{i}.png"));
            }
            EditorSceneManager.OpenScene(SceneBuilder.HomeScenePath, OpenSceneMode.Single); // never saved
        }

        /// <summary>Overlay canvases are not drawn into a camera's texture: show them as Screen Space - Camera.</summary>
        private static void SwitchOverlayCanvasesToCamera(Camera camera)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.1f + canvas.sortingOrder * 0.001f;
            }
            Canvas.ForceUpdateCanvases();
        }

        private static void RenderToPng(Camera camera, string file)
        {
            var rt = new RenderTexture(Width, Height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            camera.targetTexture = previous;
            RenderTexture.active = null;

            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            Debug.Log($"SHOT_OK {file}");
        }

        private static bool HasArg(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;

        private static string ArgValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
