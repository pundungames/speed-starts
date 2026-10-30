using NUnit.Framework;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.Tests.UI
{
    /// <summary>
    /// Safe-area rules. The screen and the notch are probe values (a wide landscape phone); only relationships are asserted:
    /// the notch side moves in by the notch share, the other side stays, a notch-less device changes nothing.
    /// </summary>
    public sealed class SafeAreaTests
    {
        private static readonly Vector2 Screen2400x1080 = new Vector2(2400f, 1080f);
        private const float Tolerance = 1e-4f;

        // ---- SafeAnchors ----

        [Test]
        public void SafeAnchors_NoNotch_IsTheWholeScreen()
        {
            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(0f, 0f, 2400f, 1080f), false, 1f, out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void SafeAnchors_NotchOnTheLeft_MovesOnlyTheLeftEdge()
        {
            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(120f, 0f, 2280f, 1080f), false, 1f, out Vector2 min, out Vector2 max);
            Assert.AreEqual(120f / 2400f, min.x, Tolerance);
            Assert.AreEqual(1f, max.x, Tolerance);
            Assert.AreEqual(0f, min.y, Tolerance);
            Assert.AreEqual(1f, max.y, Tolerance);
        }

        [Test]
        public void SafeAnchors_NotchOnTheRight_MovesOnlyTheRightEdge()
        {
            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(0f, 0f, 2280f, 1080f), false, 1f, out Vector2 min, out Vector2 max);
            Assert.AreEqual(0f, min.x, Tolerance);
            Assert.AreEqual(2280f / 2400f, max.x, Tolerance);
        }

        [Test]
        public void SafeAnchors_BothSides_MovesBothEdges()
        {
            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(100f, 0f, 2200f, 1080f), false, 1f, out Vector2 min, out Vector2 max);
            Assert.AreEqual(100f / 2400f, min.x, Tolerance);
            Assert.AreEqual(1f - 100f / 2400f, max.x, Tolerance);
        }

        [Test]
        public void SafeAnchors_VerticalInset_IsIgnoredUnlessAsked()
        {
            var safe = new Rect(0f, 60f, 2400f, 960f);
            SafeAreaMath.SafeAnchors(Screen2400x1080, safe, false, 1f, out Vector2 minOff, out Vector2 maxOff);
            Assert.AreEqual(0f, minOff.y, Tolerance);
            Assert.AreEqual(1f, maxOff.y, Tolerance);

            SafeAreaMath.SafeAnchors(Screen2400x1080, safe, true, 1f, out Vector2 minOn, out Vector2 maxOn);
            Assert.AreEqual(60f / 1080f, minOn.y, Tolerance);
            Assert.AreEqual(1f - 60f / 1080f, maxOn.y, Tolerance);
        }

        [Test]
        public void SafeAnchors_InsetWithinTolerance_CountsAsNone()
        {
            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(1f, 0f, 2398f, 1080f), false, 1f, out Vector2 min, out Vector2 max);
            Assert.AreEqual(0f, min.x);
            Assert.AreEqual(1f, max.x);
        }

        [Test]
        public void SafeAnchors_NonsenseInput_ShowsEverything()
        {
            SafeAreaMath.SafeAnchors(Vector2.zero, new Rect(10f, 10f, 5f, 5f), false, 1f, out Vector2 min0, out Vector2 max0);
            Assert.AreEqual(Vector2.zero, min0);
            Assert.AreEqual(Vector2.one, max0);

            SafeAreaMath.SafeAnchors(Screen2400x1080, new Rect(900f, 0f, 0f, 1080f), false, 1f, out Vector2 min1, out Vector2 max1);
            Assert.AreEqual(Vector2.zero, min1);
            Assert.AreEqual(Vector2.one, max1);
        }

        // ---- Remap / full-bleed detection ----

        [Test]
        public void Remap_KeepsTheEdgesAtTheSafeEdges_AndTheCentreInTheMiddle()
        {
            var safeMin = new Vector2(0.05f, 0f);
            var safeMax = Vector2.one;
            Assert.AreEqual(safeMin.x, SafeAreaMath.Remap(Vector2.zero, safeMin, safeMax).x, Tolerance);
            Assert.AreEqual(1f, SafeAreaMath.Remap(Vector2.one, safeMin, safeMax).x, Tolerance);
            Assert.AreEqual((safeMin.x + 1f) * 0.5f, SafeAreaMath.Remap(new Vector2(0.5f, 0.5f), safeMin, safeMax).x, Tolerance);
            Assert.AreEqual(0.7f, SafeAreaMath.Remap(new Vector2(0.5f, 0.7f), safeMin, safeMax).y, Tolerance);
        }

        [Test]
        public void LooksFullBleed_OnlyForAStretchedChildThatDraws()
        {
            Assert.IsTrue(SafeAreaMath.LooksFullBleed(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true, 1f));
            Assert.IsFalse(SafeAreaMath.LooksFullBleed(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, false, 1f), "a container only holds content");
            Assert.IsFalse(SafeAreaMath.LooksFullBleed(new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, true, 1f), "a corner item");
            Assert.IsFalse(SafeAreaMath.LooksFullBleed(Vector2.zero, Vector2.one, new Vector2(40f, 0f), Vector2.zero, true, 1f), "an inset panel");
        }

        // ---- SafeAreaFitter ----

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private SafeAreaFitter NewScreen(out RectTransform corner, out RectTransform background, out RectTransform forced)
        {
            _root = new GameObject("Screen", typeof(RectTransform));
            var root = (RectTransform)_root.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;

            background = NewRect("Background", root);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = Vector2.zero;
            background.offsetMax = Vector2.zero;
            background.gameObject.AddComponent<Image>();

            corner = NewRect("Corner", root);
            corner.anchorMin = new Vector2(0f, 1f);
            corner.anchorMax = new Vector2(0f, 1f);
            corner.anchoredPosition = new Vector2(30f, -20f);
            corner.sizeDelta = new Vector2(200f, 80f);

            forced = NewRect("Forced", root);
            forced.anchorMin = new Vector2(0f, 0f);
            forced.anchorMax = new Vector2(0f, 1f);
            forced.sizeDelta = new Vector2(50f, 0f);
            forced.gameObject.AddComponent<FullBleed>();

            return _root.AddComponent<SafeAreaFitter>();
        }

        [Test]
        public void Fitter_MovesTheCornerIn_KeepsBackgroundAndMarkedChildFull_AndKeepsOffsets()
        {
            SafeAreaFitter fitter = NewScreen(out RectTransform corner, out RectTransform background, out RectTransform forced);
            var source = new FixedSafeAreaSource(Screen2400x1080, new Rect(120f, 0f, 2280f, 1080f));

            fitter.SetSource(source);

            Assert.AreEqual(120f / 2400f, corner.anchorMin.x, Tolerance, "corner moved to the safe edge");
            Assert.AreEqual(corner.anchorMin.x, corner.anchorMax.x, Tolerance);
            Assert.AreEqual(1f, corner.anchorMin.y, Tolerance);
            Assert.AreEqual(new Vector2(30f, -20f), corner.anchoredPosition, "authored offset kept");
            Assert.AreEqual(new Vector2(200f, 80f), corner.sizeDelta);
            Assert.AreEqual(Vector2.zero, background.anchorMin, "stretched graphic stays full-bleed");
            Assert.AreEqual(Vector2.one, background.anchorMax);
            Assert.AreEqual(Vector2.zero, forced.anchorMin, "FullBleed marker keeps authored anchors");
        }

        [Test]
        public void Fitter_WritesOnlyWhenTheSafeAreaChanges()
        {
            SafeAreaFitter fitter = NewScreen(out _, out _, out _);
            var source = new FixedSafeAreaSource(Screen2400x1080, new Rect(120f, 0f, 2280f, 1080f));
            fitter.SetSource(source);
            int writes = fitter.WriteCount;

            Assert.IsFalse(fitter.Refresh(), "same input, nothing to write");
            Assert.AreEqual(writes, fitter.WriteCount);

            source.SafeArea = new Rect(200f, 0f, 2200f, 1080f);
            Assert.IsTrue(fitter.Refresh());
            Assert.AreEqual(writes + 1, fitter.WriteCount);
        }

        [Test]
        public void Fitter_ReturningToFullScreen_RestoresTheAuthoredAnchors()
        {
            SafeAreaFitter fitter = NewScreen(out RectTransform corner, out _, out _);
            var source = new FixedSafeAreaSource(Screen2400x1080, new Rect(120f, 0f, 2280f, 1080f));
            fitter.SetSource(source);

            source.SafeArea = new Rect(0f, 0f, 2400f, 1080f);
            fitter.Refresh();

            Assert.AreEqual(new Vector2(0f, 1f), corner.anchorMin);
            Assert.AreEqual(new Vector2(0f, 1f), corner.anchorMax);
            Assert.AreEqual(new Vector2(30f, -20f), corner.anchoredPosition);
        }

        [Test]
        public void Fitter_SelfMode_FitsItsOwnRect()
        {
            _root = new GameObject("Bar", typeof(RectTransform));
            var rect = (RectTransform)_root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -10f);
            var fitter = _root.AddComponent<SafeAreaFitter>();
            fitter.Configure(SafeAreaMode.Self, false);

            fitter.SetSource(new FixedSafeAreaSource(Screen2400x1080, new Rect(120f, 0f, 2280f, 1080f)));

            Assert.AreEqual(120f / 2400f, rect.anchorMin.x, Tolerance);
            Assert.AreEqual(new Vector2(10f, -10f), rect.anchoredPosition);
        }
    }
}
