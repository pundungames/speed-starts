using NUnit.Framework;
using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.UI
{
    /// <summary>
    /// Edit-mode notch preview: the fitted anchors are driven (never saved), the authored ones come back when the fitter is
    /// disabled or the preview is switched off, and a notch-less device drives nothing. Only relationships are asserted.
    /// </summary>
    public sealed class SafeAreaEditPreviewTests
    {
        private const string TempFolder = "Assets/_T103Temp";
        private static readonly Vector2 Screen2400x1080 = new Vector2(2400f, 1080f);
        private static readonly Vector2 AuthoredMin = new Vector2(0.8f, 0.2f);
        private static readonly Vector2 AuthoredMax = new Vector2(1f, 0.8f);

        private GameObject _root;
        private RectTransform _edge;
        private SafeAreaFitter _fitter;
        private bool _previousPreview;

        [SetUp]
        public void SetUp()
        {
            _previousPreview = SafeAreaFitter.EditorPreview;
            SafeAreaFitter.SetEditorPreview(true);
            BuildRoot();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            SafeAreaFitter.SetEditorPreview(_previousPreview);
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
        }

        private void BuildRoot()
        {
            _root = new GameObject("PreviewRoot", typeof(RectTransform));
            var rootRect = (RectTransform)_root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;

            _edge = new GameObject("Edge", typeof(RectTransform)).GetComponent<RectTransform>();
            _edge.SetParent(rootRect, false);
            _edge.anchorMin = AuthoredMin;
            _edge.anchorMax = AuthoredMax;
            _edge.anchoredPosition = new Vector2(5f, -7f);
            _edge.sizeDelta = new Vector2(40f, 30f);

            _fitter = _root.AddComponent<SafeAreaFitter>();
        }

        private static FixedSafeAreaSource Notch()
        {
            return new FixedSafeAreaSource(Screen2400x1080, new Rect(120f, 0f, 2160f, 1080f));
        }

        private static FixedSafeAreaSource NoNotch()
        {
            return new FixedSafeAreaSource(Screen2400x1080, new Rect(0f, 0f, 2400f, 1080f));
        }

        [Test]
        public void EditMode_Notch_InsetsTheAnchorsAndKeepsOffsets()
        {
            Assert.IsFalse(Application.isPlaying);
            _fitter.SetSource(Notch());

            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);
            Assert.Greater(_edge.anchorMin.x, 0f);
            Assert.AreEqual(5f, _edge.anchoredPosition.x, 1e-3f);
            Assert.AreEqual(30f, _edge.sizeDelta.y, 1e-3f);
        }

        [Test]
        public void Disabling_RestoresTheAuthoredAnchors()
        {
            _fitter.SetSource(Notch());
            Assert.AreNotEqual(AuthoredMax, _edge.anchorMax);

            _fitter.enabled = false;

            Assert.IsFalse(_fitter.IsDriving);
            Assert.AreEqual(AuthoredMin.x, _edge.anchorMin.x, 1e-5f);
            Assert.AreEqual(AuthoredMax.x, _edge.anchorMax.x, 1e-5f);
            Assert.AreEqual(5f, _edge.anchoredPosition.x, 1e-3f);

            _fitter.enabled = true;
            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);
        }

        [Test]
        public void SavedPrefab_HoldsTheAuthoredAnchors_NotThePreview()
        {
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);

            if (!AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.CreateFolder("Assets", "_T103Temp");
            string path = TempFolder + "/PreviewSave.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(_root, path);
            Assert.IsNotNull(saved);

            var savedEdge = (RectTransform)saved.transform.Find("Edge");
            Assert.AreEqual(AuthoredMin.x, savedEdge.anchorMin.x, 1e-5f);
            Assert.AreEqual(AuthoredMax.x, savedEdge.anchorMax.x, 1e-5f);

            // The same must hold after a reload from disk.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloaded = (RectTransform)AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("Edge");
            Assert.AreEqual(AuthoredMax.x, reloaded.anchorMax.x, 1e-5f);
            Assert.AreEqual(AuthoredMin.x, reloaded.anchorMin.x, 1e-5f);
        }

        [Test]
        public void NoNotch_DrivesNothing()
        {
            _fitter.SetSource(NoNotch());

            Assert.IsFalse(_fitter.IsDriving);
            Assert.AreEqual(AuthoredMin, _edge.anchorMin);
            Assert.AreEqual(AuthoredMax, _edge.anchorMax);
        }

        [Test]
        public void PreviewOff_NothingDrivenEvenWithANotch_OnAgainInsets()
        {
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);

            SafeAreaFitter.SetEditorPreview(false);
            Assert.IsFalse(_fitter.IsDriving);
            Assert.AreEqual(AuthoredMin, _edge.anchorMin);
            Assert.AreEqual(AuthoredMax, _edge.anchorMax);

            // A refit while off must keep showing the authored layout.
            _fitter.SetSource(Notch());
            Assert.IsFalse(_fitter.IsDriving);
            Assert.AreEqual(AuthoredMax, _edge.anchorMax);

            SafeAreaFitter.SetEditorPreview(true);
            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);
        }

        [Test]
        public void SafeAreaChange_WhileDriving_RefitsFromTheAuthoredAnchors()
        {
            _fitter.SetSource(Notch());
            float first = _edge.anchorMax.x;

            // A different notch must be mapped from the authored anchors, not from the already inset ones.
            _fitter.SetSource(new FixedSafeAreaSource(Screen2400x1080, new Rect(240f, 0f, 1920f, 1080f)));
            float second = _edge.anchorMax.x;
            Assert.Less(second, first);

            _fitter.SetSource(Notch());
            Assert.AreEqual(first, _edge.anchorMax.x, 1e-5f);
        }
    }
}
