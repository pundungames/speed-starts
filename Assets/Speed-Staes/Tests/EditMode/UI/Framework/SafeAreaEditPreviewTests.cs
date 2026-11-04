using System.Collections;
using System.IO;
using NUnit.Framework;
using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SpeedStars.Tests.UI
{
    /// <summary>
    /// Edit-mode notch preview: the fitted anchors are driven, the authored ones come back when the fitter is disabled or the
    /// preview is switched off, and a notch-less device drives nothing. Unity writes (0,0) for driven anchors, so the saved
    /// FILES must hold the authored anchors, also across Play-mode entry and domain reload; hand edits must survive;
    /// preview scenes (prefab mode) are never driven. Only relationships are asserted.
    /// </summary>
    public sealed class SafeAreaEditPreviewTests
    {
        private const string TempFolder = "Assets/_T103Temp";
        // SessionState survives the domain reloads some tests trigger (instance fields do not).
        private const string PrevKey = "T103.PreviousPreview";
        private static readonly Vector2 Screen2400x1080 = new Vector2(2400f, 1080f);
        private static readonly Vector2 AuthoredMin = new Vector2(0.8f, 0.2f);
        private static readonly Vector2 AuthoredMax = new Vector2(1f, 0.8f);

        private GameObject _root;
        private RectTransform _edge;
        private SafeAreaFitter _fitter;
        private bool _usedTempScene;

        [SetUp]
        public void SetUp()
        {
            SessionState.SetBool(PrevKey, SafeAreaFitter.EditorPreview);
            SafeAreaFitter.ResumePreviewAfterSave();
            SafeAreaFitter.SetEditorPreview(true);
            BuildRoot();
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            GameObject leftover = GameObject.Find("PreviewRoot");
            if (leftover != null) Object.DestroyImmediate(leftover);
            SafeAreaFitter.ResumePreviewAfterSave();
            SafeAreaFitter.SetEditorPreview(SessionState.GetBool(PrevKey, true));
            if (_usedTempScene) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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

        private static void EnsureTempFolder()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.CreateFolder("Assets", "_T103Temp");
        }

        private Scene NewTempScene()
        {
            _usedTempScene = true;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildRoot();
            EnsureTempFolder();
            return scene;
        }

        private static void AssertFileHoldsAuthored(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string text = File.ReadAllText(Path.Combine(projectRoot, assetPath));
            StringAssert.Contains("m_AnchorMin: {x: 0.8, y: 0.2}", text);
            StringAssert.Contains("m_AnchorMax: {x: 1, y: 0.8}", text);
            StringAssert.DoesNotContain("m_AnchorMax: {x: 0, y: 0}", text);
        }

        private static RectTransform FindEdge()
        {
            GameObject root = GameObject.Find("PreviewRoot");
            Assert.IsNotNull(root, "PreviewRoot not found");
            return (RectTransform)root.transform.Find("Edge");
        }

        private static void AssertAuthored(RectTransform edge)
        {
            Assert.AreEqual(AuthoredMin.x, edge.anchorMin.x, 1e-4f);
            Assert.AreEqual(AuthoredMin.y, edge.anchorMin.y, 1e-4f);
            Assert.AreEqual(AuthoredMax.x, edge.anchorMax.x, 1e-4f);
            Assert.AreEqual(AuthoredMax.y, edge.anchorMax.y, 1e-4f);
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
        public void SavedPrefab_FileHoldsTheAuthoredAnchors_NotThePreview()
        {
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);

            EnsureTempFolder();
            string path = TempFolder + "/PreviewSave.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(_root, path);
            Assert.IsNotNull(saved);

            // The file itself (not the in-memory asset) must hold the authored anchors.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssertFileHoldsAuthored(path);

            var reloaded = (RectTransform)AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("Edge");
            Assert.AreEqual(AuthoredMax.x, reloaded.anchorMax.x, 1e-5f);
            Assert.AreEqual(AuthoredMin.x, reloaded.anchorMin.x, 1e-5f);
        }

        [Test]
        public void SceneSave_FileHoldsTheAuthoredAnchors()
        {
            Scene scene = NewTempScene();
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);

            string path = TempFolder + "/PreviewSave.unity";
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, path));
            AssertFileHoldsAuthored(path);

            // The save hook defers its resume to delayCall; do it by hand and the preview is back.
            SafeAreaFitter.ResumePreviewAfterSave();
            Assert.IsTrue(_fitter.IsDriving);
            Assert.Less(_edge.anchorMax.x, AuthoredMax.x);
        }

        [UnityTest]
        public IEnumerator PlayEntry_KeepsTheAuthoredAnchors_BeforeAndAfter()
        {
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);

            yield return new EnterPlayMode();
            AssertAuthored(FindEdge());

            yield return new ExitPlayMode();
            AssertAuthored(FindEdge());
        }

        [Test]
        public void HandEdit_WhileNotDriven_IsNotLostOnRefit()
        {
            _fitter.SetSource(NoNotch());
            Assert.IsFalse(_fitter.IsDriving);

            var hand = new Vector2(0.7f, 0.9f);
            _edge.anchorMin = new Vector2(hand.x, AuthoredMin.y);
            _edge.anchorMax = new Vector2(hand.y, AuthoredMax.y);

            // The screen size changes: the hand-edited value stays.
            _fitter.SetSource(new FixedSafeAreaSource(new Vector2(1920f, 1080f), new Rect(0f, 0f, 1920f, 1080f)));
            Assert.AreEqual(hand.x, _edge.anchorMin.x, 1e-4f);
            Assert.AreEqual(hand.y, _edge.anchorMax.x, 1e-4f);

            // A notch is mapped from the hand-edited value, not from the old authored one (120 px inset on each side).
            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);
            float inset = 120f / Screen2400x1080.x;
            Assert.AreEqual(inset + (1f - 2f * inset) * hand.x, _edge.anchorMin.x, 1e-3f);
            Assert.AreEqual(inset + (1f - 2f * inset) * hand.y, _edge.anchorMax.x, 1e-3f);

            _fitter.SetSource(NoNotch());
            Assert.IsFalse(_fitter.IsDriving);
            Assert.AreEqual(hand.x, _edge.anchorMin.x, 1e-4f);
            Assert.AreEqual(hand.y, _edge.anchorMax.x, 1e-4f);
        }

        [UnityTest]
        public IEnumerator DomainReload_PreviewStillWorks_AndDisableRestores()
        {
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            // Everything Unity held is dead; find again. The reloaded fitter must drive for real, not just claim to.
            SafeAreaFitter fitter = GameObject.Find("PreviewRoot").GetComponent<SafeAreaFitter>();
            fitter.SetSource(Notch());
            RectTransform edge = FindEdge();
            Assert.IsTrue(fitter.IsDriving);
            Assert.Less(edge.anchorMax.x, AuthoredMax.x);
            Assert.Greater(edge.anchorMin.x, 0f);

            // A second reload while driving: disabling afterwards gives the authored anchors back.
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            fitter = GameObject.Find("PreviewRoot").GetComponent<SafeAreaFitter>();
            fitter.enabled = false;
            Assert.IsFalse(fitter.IsDriving);
            AssertAuthored(FindEdge());
        }

        [Test]
        public void TogglingThePreview_DoesNotDirtyASavedScene()
        {
            Scene scene = NewTempScene();
            Assert.IsTrue(EditorSceneManager.SaveScene(scene, TempFolder + "/Dirty.unity"));
            SafeAreaFitter.ResumePreviewAfterSave();
            Assert.IsFalse(scene.isDirty);

            _fitter.SetSource(Notch());
            Assert.IsTrue(_fitter.IsDriving);
            Assert.IsFalse(scene.isDirty, "driving the anchors dirtied the scene");

            SafeAreaFitter.SetEditorPreview(false);
            Assert.IsFalse(_fitter.IsDriving);
            Assert.IsFalse(scene.isDirty, "releasing the anchors dirtied the scene");

            SafeAreaFitter.SetEditorPreview(true);
            Assert.IsFalse(scene.isDirty, "re-driving the anchors dirtied the scene");
        }

        [Test]
        public void PreviewScene_NeverDrives()
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                SceneManager.MoveGameObjectToScene(_root, preview);
                _fitter.SetSource(Notch());

                Assert.IsFalse(_fitter.IsDriving);
                Assert.AreEqual(AuthoredMin, _edge.anchorMin);
                Assert.AreEqual(AuthoredMax, _edge.anchorMax);
            }
            finally
            {
                Object.DestroyImmediate(_root);
                EditorSceneManager.ClosePreviewScene(preview);
            }
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
