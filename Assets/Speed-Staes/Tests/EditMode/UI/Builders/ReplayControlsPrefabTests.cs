using NUnit.Framework;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tests.UI.Builders
{
    /// <summary>
    /// The Watch Replay controls are a built prefab, not code-made UI (T-098): the node names the smoke test and the view rely on exist,
    /// every view field is bound to a node of the prefab and the canvas is the one the code used to set up. Red until the builders have
    /// run once after T-098.
    /// </summary>
    public sealed class ReplayControlsPrefabTests
    {
        private const string PrefabPath = "Assets/Speed-Staes/UI/Widgets/ReplayControls.prefab";

        private static readonly string[] DirectChildren =
        {
            "Clock", "Name", "Exit", "NextRunner", "Camera", "PrevRunner", "Play", "Timeline", "Rate"
        };

        private static readonly string[] ViewFields =
        {
            "timeText", "speedText", "nameText", "exitButton", "nextRunnerButton", "cameraButton", "prevRunnerButton",
            "playButton", "playText", "timeline", "rateButton", "rateText"
        };

        [Test]
        public void Prefab_HasTheControlsAsDirectChildren()
        {
            GameObject root = Load();
            foreach (string name in DirectChildren)
                Assert.IsNotNull(root.transform.Find(name), $"ReplayControls has no direct child '{name}' (the safe-area fitter and the smoke test need it there)");
        }

        [Test]
        public void Prefab_ViewFieldsAreAllBoundToNodesOfThePrefab()
        {
            GameObject root = Load();
            var view = root.GetComponent<ReplayControlsView>();
            Assert.IsNotNull(view, "ReplayControlsView missing on the prefab root");

            var so = new SerializedObject(view);
            foreach (string field in ViewFields)
            {
                Object target = so.FindProperty(field).objectReferenceValue;
                Assert.IsNotNull(target, $"ReplayControlsView.{field} is empty");
                var component = (Component)target;
                Assert.IsTrue(component.transform.IsChildOf(root.transform), $"ReplayControlsView.{field} points outside the prefab");
            }
        }

        [Test]
        public void Prefab_CanvasMatchesWhatTheCodeSetUp()
        {
            GameObject root = Load();
            var canvas = root.GetComponent<Canvas>();
            Assert.IsNotNull(canvas, "no Canvas on the root");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
            Assert.Greater(canvas.sortingOrder, 0, "above the race HUD and the screens");

            var scaler = root.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "no CanvasScaler on the root");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.Greater(scaler.referenceResolution.x, 0f, "the reference resolution is the user's to set; it only has to exist");

            Assert.IsNotNull(root.GetComponent<GraphicRaycaster>(), "taps need a GraphicRaycaster");
            Assert.IsNotNull(root.GetComponent<SpeedStars.UI.Framework.SafeAreaFitter>(), "the controls stay out of a notch");
        }

        [Test]
        public void Prefab_HoldsNoCodeBuiltLeftovers_ButtonsAreBackedByImages()
        {
            GameObject root = Load();
            foreach (string name in new[] { "Exit", "NextRunner", "Camera", "PrevRunner", "Play", "Rate" })
            {
                Transform node = root.transform.Find(name);
                Assert.IsNotNull(node, name);
                var button = node.GetComponent<Button>();
                Assert.IsNotNull(button, name + " is not a Button");
                Assert.AreSame(node.GetComponent<Image>(), button.targetGraphic, name + ": the button's own image is its target graphic");
            }

            var timeline = root.transform.Find("Timeline").GetComponent<Slider>();
            Assert.IsNotNull(timeline.fillRect, "timeline fill");
            Assert.IsNotNull(timeline.handleRect, "timeline handle");
            Assert.AreEqual(0f, timeline.minValue);
            Assert.AreEqual(1f, timeline.maxValue);
        }

        private static GameObject Load()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(go, PrefabPath + " not found: run the UI prefab builder");
            return go;
        }
    }
}
