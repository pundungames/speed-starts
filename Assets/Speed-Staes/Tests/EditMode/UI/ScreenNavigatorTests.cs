using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.UI.Framework;

namespace SpeedStars.Tests.UI
{
    /// <summary>Screen stack and popup layer rules of ScreenNavigator, with fake views (no scene, no DOTween).</summary>
    public sealed class ScreenNavigatorTests
    {
        private List<string> _log;
        private ScreenNavigator _nav;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _nav = new ScreenNavigator();
        }

        private FakeView View(string id) => new FakeView(id, _log);

        [Test]
        public void Push_FirstScreen_ShowsItAndMakesItCurrent()
        {
            var home = View("home");

            _nav.Push(home);

            Assert.AreSame(home, _nav.CurrentScreen);
            Assert.AreEqual(1, _nav.ScreenCount);
            CollectionAssert.AreEqual(new[] { "home.Show" }, _log);
        }

        [Test]
        public void CurrentScreenChanged_RaisedForPushPopAndReplace_NotForPopups()
        {
            var home = View("home");
            var events = View("events");
            var themes = View("themes");
            var seen = new List<IUiView>();
            _nav.CurrentScreenChanged += seen.Add;

            _nav.Push(home);
            _nav.Push(events);
            _nav.Replace(themes);
            _nav.OpenPopup(View("settings"));
            _nav.Pop();

            CollectionAssert.AreEqual(new IUiView[] { home, events, themes, home }, seen);
        }

        [Test]
        public void Push_SecondScreen_HidesTheCoveredOneThenShowsTheNewOne()
        {
            var home = View("home");
            var events = View("events");
            _nav.Push(home);
            _log.Clear();

            _nav.Push(events);

            Assert.AreSame(events, _nav.CurrentScreen);
            Assert.AreEqual(2, _nav.ScreenCount);
            CollectionAssert.AreEqual(new[] { "home.Hide", "events.Show" }, _log);
        }

        [Test]
        public void Push_TopScreenAgain_DoesNothing()
        {
            var home = View("home");
            _nav.Push(home);
            _log.Clear();

            _nav.Push(home);

            Assert.AreEqual(1, _nav.ScreenCount);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void Push_ScreenAlreadyBelow_UnwindsBackToItWithoutASecondCopy()
        {
            var home = View("home");
            var events = View("events");
            var cosmetics = View("cosmetics");
            _nav.Push(home);
            _nav.Push(events);
            _nav.Push(cosmetics);
            _log.Clear();

            _nav.Push(home);

            Assert.AreSame(home, _nav.CurrentScreen);
            Assert.AreEqual(1, _nav.ScreenCount);
            CollectionAssert.AreEqual(new[] { "cosmetics.Hide", "home.Show" }, _log);
        }

        [Test]
        public void Pop_HidesTopAndShowsTheScreenBelow()
        {
            var home = View("home");
            var events = View("events");
            _nav.Push(home);
            _nav.Push(events);
            _log.Clear();

            bool popped = _nav.Pop();

            Assert.IsTrue(popped);
            Assert.AreSame(home, _nav.CurrentScreen);
            CollectionAssert.AreEqual(new[] { "events.Hide", "home.Show" }, _log);
        }

        [Test]
        public void Pop_AtRoot_IsRefusedAndKeepsTheRootVisible()
        {
            var home = View("home");
            _nav.Push(home);
            _log.Clear();

            bool popped = _nav.Pop();

            Assert.IsFalse(popped);
            Assert.AreSame(home, _nav.CurrentScreen);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void Pop_OnEmptyStack_IsRefused()
        {
            Assert.IsFalse(_nav.Pop());
            Assert.IsNull(_nav.CurrentScreen);
        }

        [Test]
        public void Replace_SwapsTheTopScreenAndKeepsTheDepth()
        {
            var home = View("home");
            var events = View("events");
            var highScores = View("highScores");
            _nav.Push(home);
            _nav.Push(events);
            _log.Clear();

            _nav.Replace(highScores);

            Assert.AreSame(highScores, _nav.CurrentScreen);
            Assert.AreEqual(2, _nav.ScreenCount);
            CollectionAssert.AreEqual(new[] { "events.Hide", "highScores.Show" }, _log);

            Assert.IsTrue(_nav.Pop());
            Assert.AreSame(home, _nav.CurrentScreen);
        }

        [Test]
        public void Replace_OnEmptyStack_ActsLikePush()
        {
            var home = View("home");

            _nav.Replace(home);

            Assert.AreSame(home, _nav.CurrentScreen);
            CollectionAssert.AreEqual(new[] { "home.Show" }, _log);
        }

        [Test]
        public void Back_ClosesPopupsNewestFirstBeforeAnyScreen()
        {
            var home = View("home");
            var events = View("events");
            var gate = View("gate");
            var gift = View("gift");
            _nav.Push(home);
            _nav.Push(events);
            _nav.OpenPopup(gate);
            _nav.OpenPopup(gift);
            _log.Clear();

            Assert.IsTrue(_nav.Back());
            CollectionAssert.AreEqual(new[] { "gift.Hide" }, _log);
            Assert.AreSame(events, _nav.CurrentScreen);

            Assert.IsTrue(_nav.Back());
            CollectionAssert.AreEqual(new[] { "gift.Hide", "gate.Hide" }, _log);
            Assert.AreEqual(0, _nav.PopupCount);
            Assert.AreSame(events, _nav.CurrentScreen);

            Assert.IsTrue(_nav.Back());
            Assert.AreSame(home, _nav.CurrentScreen);
        }

        [Test]
        public void Back_AtRootWithoutPopups_ReportsNothingToGoBackFrom()
        {
            var home = View("home");
            _nav.Push(home);
            _log.Clear();

            Assert.IsFalse(_nav.Back());
            Assert.AreSame(home, _nav.CurrentScreen);
            CollectionAssert.IsEmpty(_log);
        }

        [Test]
        public void OpenPopup_AlreadyOpen_IsRefusedAndShownOnlyOnce()
        {
            var gate = View("gate");

            Assert.IsTrue(_nav.OpenPopup(gate));
            Assert.IsFalse(_nav.OpenPopup(gate));

            Assert.AreEqual(1, _nav.PopupCount);
            Assert.AreEqual(1, gate.ShowCount);
        }

        [Test]
        public void ScreenMoves_LeaveOpenPopupsAlone()
        {
            var home = View("home");
            var events = View("events");
            var gate = View("gate");
            _nav.Push(home);
            _nav.OpenPopup(gate);
            _log.Clear();

            _nav.Push(events);
            _nav.Pop();

            Assert.AreEqual(1, _nav.PopupCount);
            Assert.AreSame(gate, _nav.TopPopup);
            CollectionAssert.DoesNotContain(_log, "gate.Hide");
        }

        [Test]
        public void ClosePopup_NotOnTop_ClosesOnlyThatOne()
        {
            var gate = View("gate");
            var gift = View("gift");
            _nav.OpenPopup(gate);
            _nav.OpenPopup(gift);
            _log.Clear();

            Assert.IsTrue(_nav.ClosePopup(gate));
            Assert.IsFalse(_nav.ClosePopup(gate));

            Assert.AreSame(gift, _nav.TopPopup);
            Assert.AreEqual(1, _nav.PopupCount);
            CollectionAssert.AreEqual(new[] { "gate.Hide" }, _log);
        }

        [Test]
        public void CloseAllPopups_HidesEveryPopupNewestFirst()
        {
            var gate = View("gate");
            var gift = View("gift");
            _nav.OpenPopup(gate);
            _nav.OpenPopup(gift);
            _log.Clear();

            _nav.CloseAllPopups();

            Assert.AreEqual(0, _nav.PopupCount);
            Assert.IsNull(_nav.TopPopup);
            CollectionAssert.AreEqual(new[] { "gift.Hide", "gate.Hide" }, _log);
        }

        [Test]
        public void NullViews_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => _nav.Push(null));
            Assert.Throws<ArgumentNullException>(() => _nav.Replace(null));
            Assert.Throws<ArgumentNullException>(() => _nav.OpenPopup(null));
            Assert.Throws<ArgumentNullException>(() => _nav.ClosePopup(null));
        }

        private sealed class FakeView : IUiView
        {
            private readonly string _id;
            private readonly List<string> _log;

            public FakeView(string id, List<string> log)
            {
                _id = id;
                _log = log;
            }

            public int ShowCount { get; private set; }

            public void Show()
            {
                ShowCount++;
                _log.Add(_id + ".Show");
            }

            public void Hide()
            {
                _log.Add(_id + ".Hide");
            }
        }
    }
}
