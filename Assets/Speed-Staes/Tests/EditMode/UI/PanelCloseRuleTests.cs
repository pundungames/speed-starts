using NUnit.Framework;
using SpeedStars.UI.Framework;

namespace SpeedStars.Tests.UI
{
    /// <summary>What a panel's Back / Close button does: a popup closes itself, a screen goes back one step.</summary>
    public sealed class PanelCloseRuleTests
    {
        private ScreenNavigator _navigator;
        private FakeView _root;
        private FakeView _screen;
        private FakeView _popup;

        [SetUp]
        public void SetUp()
        {
            _navigator = new ScreenNavigator();
            _root = new FakeView();
            _screen = new FakeView();
            _popup = new FakeView();
            _navigator.Push(_root);
        }

        [Test]
        public void Popup_ClosesItself()
        {
            _navigator.OpenPopup(_popup);

            bool closed = PanelCloseRule.Close(_navigator, _popup, isPopup: true);

            Assert.IsTrue(closed);
            Assert.AreEqual(0, _navigator.PopupCount);
            Assert.AreEqual(1, _popup.Hidden);
        }

        [Test]
        public void Popup_NotOpen_DoesNothing()
        {
            Assert.IsFalse(PanelCloseRule.Close(_navigator, _popup, isPopup: true));
            Assert.AreEqual(0, _popup.Hidden);
        }

        [Test]
        public void Screen_GoesBackToTheOneUnderIt()
        {
            _navigator.Push(_screen);

            bool closed = PanelCloseRule.Close(_navigator, _screen, isPopup: false);

            Assert.IsTrue(closed);
            Assert.AreSame(_root, _navigator.CurrentScreen);
        }

        [Test]
        public void Screen_WithAnOpenPopup_ClosesThePopupFirst()
        {
            _navigator.Push(_screen);
            _navigator.OpenPopup(_popup);

            PanelCloseRule.Close(_navigator, _screen, isPopup: false);

            Assert.AreEqual(0, _navigator.PopupCount);
            Assert.AreSame(_screen, _navigator.CurrentScreen);
        }

        [Test]
        public void RootScreen_StaysPut()
        {
            Assert.IsFalse(PanelCloseRule.Close(_navigator, _root, isPopup: false));
            Assert.AreSame(_root, _navigator.CurrentScreen);
        }

        [Test]
        public void WithoutANavigator_NothingHappens()
        {
            Assert.IsFalse(PanelCloseRule.Close(null, _popup, isPopup: true));
        }

        private sealed class FakeView : IUiView
        {
            public int Hidden;
            public void Show() { }
            public void Hide() => Hidden++;
        }
    }
}
