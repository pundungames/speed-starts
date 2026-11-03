using System.Linq;
using NUnit.Framework;
using SpeedStars.UI.Race;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.UI.Builders
{
    /// <summary>
    /// Malik's 03.11 hand edits must survive a builder re-run (T-091): the built prefabs are read back, so a builder change
    /// that drops one of these goes red. Red until the builders have run once after T-091.
    /// </summary>
    public sealed class BuilderHandEditsTests
    {
        private const string HighScores = "Assets/Speed-Staes/UI/Screens/HighScoresScreen.prefab";
        private const string SevenDay = "Assets/Speed-Staes/UI/Popups/SevenDayCheckInPanel.prefab";
        private const string Results = "Assets/Speed-Staes/UI/Screens/ResultsScreen.prefab";

        [Test]
        public void EventModalClose_IsPinnedToTheWindowRightEdge()
        {
            AssertPinnedRight(Close(HighScores, "EventModal", "Window"));
        }

        [Test]
        public void SevenDayClose_IsPinnedToTheWindowRightEdge()
        {
            AssertPinnedRight(Close(SevenDay, null, "Window"));
        }

        [Test]
        public void ResultsScreenView_HasReplaySkinBound()
        {
            GameObject root = Load(Results);
            var view = root.GetComponentInChildren<ResultsScreenView>(true);
            Assert.IsNotNull(view, "ResultsScreenView missing");
            var skin = new SerializedObject(view).FindProperty("replaySkin").objectReferenceValue;
            Assert.IsNotNull(skin, "replaySkin is empty: the builder must bind ReplayControlsSkin");
            Assert.IsInstanceOf<ReplayControlsSkin>(skin);
        }

        private static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(go, path + " not found");
            return go;
        }

        private static RectTransform Close(string path, string grandParent, string parent)
        {
            RectTransform found = Load(path).GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rt =>
                rt.name == "Close" && rt.parent != null && rt.parent.name == parent
                && (grandParent == null || (rt.parent.parent != null && rt.parent.parent.name == grandParent)));
            Assert.IsNotNull(found, $"{path}: no {grandParent}/{parent}/Close");
            return found;
        }

        private static void AssertPinnedRight(RectTransform close)
        {
            Assert.AreEqual(1f, close.anchorMin.x, 0.0001f, "anchorMin.x");
            Assert.AreEqual(1f, close.anchorMax.x, 0.0001f, "anchorMax.x");
            Assert.AreEqual(1f, close.pivot.x, 0.0001f, "pivot.x");
            Assert.AreEqual(0f, close.anchoredPosition.x, 0.01f, "anchoredPosition.x");
            Assert.AreEqual(0f, close.anchoredPosition.y, 0.01f, "anchoredPosition.y");
        }
    }
}
