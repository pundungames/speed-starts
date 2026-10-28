using System;
using SpeedStars.Tools.Builders;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// "Canvas Dim" of the Home scene (costume editor, T-026): a Screen Space - Camera canvas on the Home camera with one
    /// full-screen dimscreen Image, placed BEHIND the menu character (plane farther than the character platform) so the
    /// scene is dimmed but the character is not. An overlay canvas cannot do this: it draws over the whole 3D scene.
    /// Starts inactive; CostumeEditorScreenView.dimBackdrop switches it on while the screen is open.
    /// </summary>
    internal static class SceneHomeDim
    {
        private const string Source = nameof(SceneHomeDim);
        public const string CanvasName = "Canvas Dim";
        public const string CameraName = "Main Menu Camera";
        private const string DimArtPath = UiArt.ArtRoot + "/Costume/dimscreen.png";

        /// <summary>How much farther than the character platform the plane sits (world units), so the character is in front of it.</summary>
        private const float PlaneMargin = 0.5f;

        /// <summary>Below every other camera-space canvas.</summary>
        private const int DimSortingOrder = -1;

        public static GameObject Create(Scene scene)
        {
            Transform cameraTransform = SceneHomeTheme.FindByName(scene, CameraName)
                ?? throw new InvalidOperationException($"'{CameraName}' not found in '{scene.path}'; the theme template changed. Update SceneHomeDim.");
            var camera = cameraTransform.GetComponent<Camera>()
                ?? throw new InvalidOperationException($"'{CameraName}' has no Camera.");
            Transform platform = SceneHomeTheme.FindByName(scene, SceneHomeCharacter.PlatformName)
                ?? throw new InvalidOperationException($"'{SceneHomeCharacter.PlatformName}' not found in '{scene.path}'.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DimArtPath)
                ?? throw new InvalidOperationException($"Sprite '{DimArtPath}' not found or not imported as a Sprite.");

            // Distance along the camera's view axis, so the plane (perpendicular to it) clears the platform.
            float distance = Vector3.Dot(platform.position - cameraTransform.position, cameraTransform.forward);
            if (distance <= camera.nearClipPlane)
                throw new InvalidOperationException($"'{SceneHomeCharacter.PlatformName}' is not in front of '{CameraName}'.");

            var go = SceneUiLayout.CreateRoot(scene, CanvasName, typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var canvas = AddRequired<Canvas>(go);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = distance + PlaneMargin;
            canvas.sortingOrder = DimSortingOrder;

            GameObject imageGo = SceneUiLayout.CreateChild(go.transform, "Dim", typeof(RectTransform));
            if (uiLayer >= 0) imageGo.layer = uiLayer;
            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = AddRequired<Image>(imageGo);
            image.sprite = sprite;
            image.raycastTarget = false;

            go.SetActive(false);
            Log(Source, $"Home: '{CanvasName}' on '{CameraName}', plane distance {canvas.planeDistance:0.##} (platform {distance:0.##} + {PlaneMargin}).");
            return go;
        }

        /// <summary>The scene's Canvas Dim (inactive, so found through the roots).</summary>
        public static GameObject Find(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == CanvasName) return root;
            throw new InvalidOperationException($"'{CanvasName}' not found in '{scene.path}'; SceneHomeDim.Create runs before SceneWiring.WireHome.");
        }
    }
}
