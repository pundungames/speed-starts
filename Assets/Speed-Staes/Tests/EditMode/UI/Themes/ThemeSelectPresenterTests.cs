using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Themes;

namespace SpeedStars.Tests.UI.Themes
{
    public sealed class ThemeSelectPresenterTests
    {
        /// <summary>Test double of the service: 3 themes, price per theme, a Cash balance, ownership set, a saved (active) theme.</summary>
        private sealed class FakeThemes : IThemeService
        {
            private readonly List<ThemeInfo> _themes = new List<ThemeInfo>();
            private readonly HashSet<string> _owned = new HashSet<string>();
            private int _shown;

            public int Cash;
            public int BeginCalls;
            public int CancelCalls;

            public FakeThemes()
            {
                _themes.Add(new ThemeInfo("a", "Alpha", 0, 0));
                _themes.Add(new ThemeInfo("b", "Beta", 1, 50));
                _themes.Add(new ThemeInfo("c", "Gamma", 2, 0));
                _owned.Add("a");
                ActiveId = "a";
            }

            public IReadOnlyList<ThemeInfo> Themes => _themes;
            public string ActiveId { get; private set; }
            public int ActiveShowcaseIndex => IndexOf(ActiveId);
            public string ShownId => _themes[_shown].Id;
            public int ShownShowcaseIndex => _shown;
            public int ShownListIndex => _shown;
            public bool IsBrowsing { get; private set; }

            public event Action ShownChanged;
            public event Action ActiveChanged;
            public event Action OwnershipChanged;
            public event Action BrowsingChanged;

            public bool IsOwned(string id) => _owned.Contains(id);

            public void BeginBrowse()
            {
                BeginCalls++;
                IsBrowsing = true;
                _shown = IndexOf(ActiveId);
            }

            public bool Preview(string id)
            {
                int index = IndexOf(id);
                if (index < 0) return false;
                _shown = index;
                ShownChanged?.Invoke();
                return true;
            }

            public bool CanStep(int direction)
            {
                int target = _shown + direction;
                return target >= 0 && target < _themes.Count;
            }

            public bool Step(int direction) => CanStep(direction) && Preview(_themes[_shown + direction].Id);

            public void CancelBrowse()
            {
                CancelCalls++;
                IsBrowsing = false;
                _shown = IndexOf(ActiveId);
            }

            public ThemeSelectResult Use(string id)
            {
                if (IndexOf(id) < 0) return ThemeSelectResult.UnknownTheme;
                if (!_owned.Contains(id)) return ThemeSelectResult.NotUsable;
                if (ActiveId == id) return ThemeSelectResult.AlreadySelected;
                ActiveId = id;
                ActiveChanged?.Invoke();
                return ThemeSelectResult.Selected;
            }

            public ThemePurchaseResult TryPurchase(string id)
            {
                int index = IndexOf(id);
                if (index < 0) return ThemePurchaseResult.UnknownTheme;
                if (_owned.Contains(id)) return ThemePurchaseResult.AlreadyOwned;
                int price = _themes[index].CashPrice;
                if (price <= 0) return ThemePurchaseResult.NotForSale;
                if (Cash < price) return ThemePurchaseResult.NotEnoughCash;
                Cash -= price;
                _owned.Add(id);
                OwnershipChanged?.Invoke();
                return ThemePurchaseResult.Purchased;
            }

            public ThemeActionState GetActionState(string id)
            {
                int index = IndexOf(id);
                if (_owned.Contains(id)) return ActiveId == id ? ThemeActionState.Active : ThemeActionState.OwnedInactive;
                if (_themes[index].CashPrice <= 0) return ThemeActionState.NotForSale;
                return Cash >= _themes[index].CashPrice ? ThemeActionState.UnownedAffordable : ThemeActionState.UnownedInsufficient;
            }

            private int IndexOf(string id) => _themes.FindIndex(t => t.Id == id);
        }

        private static ThemeSelectPresenter Open(FakeThemes themes)
        {
            var presenter = new ThemeSelectPresenter(themes);
            presenter.Open();
            return presenter;
        }

        [Test]
        public void Open_ShowsTheActiveTheme_AsInUse()
        {
            var themes = new FakeThemes();
            ThemeSelectSnapshot s = Open(themes).Snapshot();

            Assert.AreEqual(1, themes.BeginCalls);
            Assert.AreEqual("Alpha", s.Name);
            Assert.AreEqual(ThemeActionBackground.InUse, s.Action.Background);
            Assert.AreEqual(ThemeActionLabel.InUse, s.Action.Label);
            Assert.AreEqual(ThemeActionTone.Light, s.Action.Tone);
            Assert.IsFalse(s.Action.Interactable);
            Assert.IsFalse(s.Action.ShowsCurrencyIcon);
        }

        [Test]
        public void Arrows_AreOffAtTheEnds_NoWrap()
        {
            var themes = new FakeThemes();
            ThemeSelectPresenter presenter = Open(themes);

            Assert.IsFalse(presenter.Snapshot().CanPrevious);
            Assert.IsTrue(presenter.Snapshot().CanNext);

            presenter.Step(1);
            Assert.IsTrue(presenter.Snapshot().CanPrevious);
            Assert.IsTrue(presenter.Snapshot().CanNext);

            presenter.Step(1);
            Assert.IsTrue(presenter.Snapshot().CanPrevious);
            Assert.IsFalse(presenter.Snapshot().CanNext);
            Assert.IsFalse(presenter.Step(1), "no step past the last theme");
            Assert.AreEqual("Gamma", presenter.Snapshot().Name);
        }

        [Test]
        public void UnownedAffordable_ShowsPriceWithIcon_AndIsTappable()
        {
            var themes = new FakeThemes { Cash = 1000 };
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);
            ThemeSelectSnapshot s = presenter.Snapshot();

            Assert.AreEqual(themes.Themes[1].CashPrice, s.Price);
            Assert.AreEqual(ThemeActionBackground.Purchase, s.Action.Background);
            Assert.AreEqual(ThemeActionLabel.Price, s.Action.Label);
            Assert.IsTrue(s.Action.ShowsCurrencyIcon);
            Assert.IsTrue(s.Action.Interactable);
        }

        [Test]
        public void UnownedInsufficient_IsGreyAndNotTappable()
        {
            var themes = new FakeThemes { Cash = 0 };
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);
            ThemeSelectSnapshot s = presenter.Snapshot();

            Assert.AreEqual(ThemeActionBackground.Disabled, s.Action.Background);
            Assert.AreEqual(ThemeActionLabel.Price, s.Action.Label);
            Assert.IsTrue(s.Action.ShowsCurrencyIcon);
            Assert.IsFalse(s.Action.Interactable);
            Assert.AreEqual(ThemeActionOutcome.Nothing, presenter.PressAction());
        }

        [Test]
        public void NotForSale_IsGreyWithoutIcon_AndNotTappable()
        {
            var themes = new FakeThemes { Cash = 1000 };
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);
            presenter.Step(1); // Gamma: no price, not owned
            ThemeSelectSnapshot s = presenter.Snapshot();

            Assert.AreEqual(ThemeActionBackground.Disabled, s.Action.Background);
            Assert.IsFalse(s.Action.ShowsCurrencyIcon);
            Assert.IsFalse(s.Action.Interactable);
            Assert.AreEqual(ThemeActionOutcome.Nothing, presenter.PressAction());
        }

        [Test]
        public void Purchase_ChargesOnce_ThenShowsUse()
        {
            var themes = new FakeThemes { Cash = 120 };
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);
            int price = themes.Themes[1].CashPrice;

            Assert.AreEqual(ThemeActionOutcome.Purchased, presenter.PressAction());
            Assert.AreEqual(120 - price, themes.Cash);

            ThemeSelectSnapshot s = presenter.Snapshot();
            Assert.AreEqual(ThemeActionBackground.Use, s.Action.Background);
            Assert.AreEqual(ThemeActionLabel.Use, s.Action.Label);
            Assert.IsFalse(s.Action.ShowsCurrencyIcon);
            Assert.IsTrue(s.Action.Interactable);
            Assert.AreEqual(themes.Cash, 120 - price, "the second state does not charge again");
        }

        [Test]
        public void Use_MakesTheThemeActive_ThenShowsInUse()
        {
            var themes = new FakeThemes { Cash = 1000 };
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);
            presenter.PressAction(); // buy

            Assert.AreEqual(ThemeActionOutcome.Used, presenter.PressAction());
            Assert.AreEqual("b", themes.ActiveId);
            Assert.AreEqual(ThemeActionBackground.InUse, presenter.Snapshot().Action.Background);
            Assert.AreEqual(ThemeActionOutcome.Nothing, presenter.PressAction(), "IN USE does nothing");
        }

        [Test]
        public void Close_WhileBrowsing_CancelsThePreview()
        {
            var themes = new FakeThemes();
            ThemeSelectPresenter presenter = Open(themes);
            presenter.Step(1);

            presenter.Close();

            Assert.AreEqual(1, themes.CancelCalls);
            Assert.AreEqual("a", themes.ShownId, "the saved theme is back");
        }

        [Test]
        public void Close_WhenNotBrowsing_DoesNothing()
        {
            var themes = new FakeThemes();
            var presenter = new ThemeSelectPresenter(themes);

            presenter.Close();

            Assert.AreEqual(0, themes.CancelCalls);
        }

        [Test]
        public void LookFor_EveryState_PicksOneBackgroundAndMatchingClick()
        {
            foreach (ThemeActionState state in Enum.GetValues(typeof(ThemeActionState)))
            {
                ThemeActionLook look = ThemeSelectPresenter.LookFor(state);
                Assert.AreEqual(look.Interactable, look.Click != ThemeActionClick.None, state.ToString());
            }
        }

        [Test]
        public void Premium_NormalIsGold_InUseIsRed_NeverBoth()
        {
            foreach (ThemeActionState state in Enum.GetValues(typeof(ThemeActionState)))
            {
                bool interactable = ThemeSelectPresenter.LookFor(state).Interactable;
                PremiumLook look = PremiumLook.For(true, ThemePriceCurrency.PurpleGem, state, interactable);

                Assert.IsTrue(look.IsPremium);
                Assert.IsFalse(look.ShowsGold && look.ShowsRed, state.ToString());
                Assert.AreEqual(state == ThemeActionState.Active, look.ShowsRed, state.ToString());
                Assert.AreEqual(state != ThemeActionState.Active, look.ShowsGold, state.ToString());
            }
        }

        [Test]
        public void Premium_OverlayOnlyWhenNotPressableAndNotInUse()
        {
            foreach (ThemeActionState state in Enum.GetValues(typeof(ThemeActionState)))
            {
                bool interactable = ThemeSelectPresenter.LookFor(state).Interactable;
                PremiumLook look = PremiumLook.For(true, ThemePriceCurrency.PurpleGem, state, interactable);
                Assert.AreEqual(!interactable && state != ThemeActionState.Active, look.ActionOverlay, state.ToString());
            }
        }

        [Test]
        public void Premium_ActionTextIsAlwaysDark_PlainKeepsItsStateTone()
        {
            foreach (ThemeActionState state in Enum.GetValues(typeof(ThemeActionState)))
            {
                ThemeActionLook plain = ThemeSelectPresenter.LookFor(state);
                PremiumLook premium = PremiumLook.For(true, ThemePriceCurrency.PurpleGem, state, plain.Interactable);
                PremiumLook normal = PremiumLook.For(false, ThemePriceCurrency.Cash, state, plain.Interactable);

                Assert.AreEqual(ThemeActionTone.Dark, premium.ToneFor(plain.Tone), state.ToString());
                Assert.AreEqual(plain.Tone, normal.ToneFor(plain.Tone), state.ToString());
            }
        }

        [Test]
        public void NonPremium_HasNoPremiumLayers()
        {
            foreach (ThemeActionState state in Enum.GetValues(typeof(ThemeActionState)))
            {
                PremiumLook look = PremiumLook.For(false, ThemePriceCurrency.Cash, state, false);
                Assert.IsFalse(look.IsPremium);
                Assert.AreEqual(PremiumFrame.None, look.Frame);
                Assert.IsFalse(look.ActionOverlay);
            }
        }
    }
}
