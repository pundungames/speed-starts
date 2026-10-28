using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SpeedStars.UI.Flow;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.Tests.UI
{
    /// <summary>SceneFlow rules with a fake loader: one move at a time, input locked for the whole move, names from settings.</summary>
    public sealed class SceneFlowTests
    {
        private FakeSceneLoader _loader;
        private SceneFlow _flow;
        private List<bool> _transitions;

        [SetUp]
        public void SetUp()
        {
            _loader = new FakeSceneLoader();
            _flow = new SceneFlow(_loader, new SceneFlowScenes("TestHome", "TestRace"));
            _transitions = new List<bool>();
            _flow.TransitionChanged += _transitions.Add;
        }

        [Test]
        public void GoTo_LoadsTheSceneNameFromTheSettings()
        {
            Assert.IsTrue(_flow.GoTo(GameScene.Race));

            CollectionAssert.AreEqual(new[] { "TestRace" }, _loader.Requested);
        }

        [Test]
        public void GoTo_WhileALoadRuns_IsRefusedAndLoadsOnce()
        {
            Assert.IsTrue(_flow.GoTo(GameScene.Race));

            Assert.IsFalse(_flow.GoTo(GameScene.Race));
            Assert.IsFalse(_flow.GoTo(GameScene.Home));

            Assert.AreEqual(1, _loader.Requested.Count);
        }

        [Test]
        public void GoTo_LocksInputUntilTheNewSceneIsActive()
        {
            _flow.GoTo(GameScene.Race);

            Assert.IsTrue(_flow.IsTransitioning);
            CollectionAssert.AreEqual(new[] { true }, _transitions);

            _loader.CompleteLast();

            Assert.IsFalse(_flow.IsTransitioning);
            CollectionAssert.AreEqual(new[] { true, false }, _transitions);
        }

        [Test]
        public void GoTo_AfterTheLoadCompletes_IsAcceptedAgain()
        {
            _flow.GoTo(GameScene.Race);
            _loader.CompleteLast();

            Assert.IsTrue(_flow.GoTo(GameScene.Home));

            CollectionAssert.AreEqual(new[] { "TestRace", "TestHome" }, _loader.Requested);
        }

        [Test]
        public void GoTo_WhenTheLoaderCannotStart_UnlocksAndReportsRefused()
        {
            _loader.Accepts = false;

            Assert.IsFalse(_flow.GoTo(GameScene.Race));

            Assert.IsFalse(_flow.IsTransitioning);
            CollectionAssert.AreEqual(new[] { true, false }, _transitions);

            _loader.Accepts = true;
            Assert.IsTrue(_flow.GoTo(GameScene.Race));
        }

        [Test]
        public void GoTo_WhenTheLoaderThrows_UnlocksAndRethrows()
        {
            _loader.Throws = true;

            Assert.Throws<InvalidOperationException>(() => _flow.GoTo(GameScene.Race));

            Assert.IsFalse(_flow.IsTransitioning);
        }

        [Test]
        public void LateCallbackOfAnEarlierMove_DoesNotUnlockTheCurrentMove()
        {
            _flow.GoTo(GameScene.Race);
            Action firstCompletion = _loader.LastCallback;
            firstCompletion();
            _flow.GoTo(GameScene.Home);

            firstCompletion();

            Assert.IsTrue(_flow.IsTransitioning);
            Assert.IsFalse(_flow.GoTo(GameScene.Race));
        }

        [Test]
        public void GoTo_SceneWithoutAName_IsRefusedWithAnError()
        {
            var flow = new SceneFlow(_loader, new SceneFlowScenes(string.Empty, "TestRace"));
            LogAssert.Expect(LogType.Error, new Regex(@"\[SceneFlow\].*Home"));

            Assert.IsFalse(flow.GoTo(GameScene.Home));

            Assert.IsFalse(flow.IsTransitioning);
            CollectionAssert.IsEmpty(_loader.Requested);
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new SceneFlow(null, new SceneFlowScenes()));
            Assert.Throws<ArgumentNullException>(() => new SceneFlow(_loader, null));
        }

        private sealed class FakeSceneLoader : ISceneLoader
        {
            public readonly List<string> Requested = new List<string>();
            public bool Accepts = true;
            public bool Throws;
            public Action LastCallback;

            public bool Load(string sceneName, Action onLoaded)
            {
                if (Throws) throw new InvalidOperationException("loader failed");
                if (!Accepts) return false;

                Requested.Add(sceneName);
                LastCallback = onLoaded;
                return true;
            }

            public void CompleteLast() => LastCallback?.Invoke();
        }
    }
}
