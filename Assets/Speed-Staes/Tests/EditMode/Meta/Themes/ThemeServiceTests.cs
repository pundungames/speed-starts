using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Themes;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Themes
{
    /// <summary>Prices and cash are test-chosen values; tests assert relations, never the shipped catalog.</summary>
    public sealed class ThemeServiceTests
    {
        private const int PriceB = 40;
        private const int PriceC = 90;

        private TicketTestKit _kit;
        private ThemeCatalog _catalog;
        private WalletService _wallet;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        // Themes in showcase order: a (default, free), b (PriceB), c (PriceC), d (no price).
        private ThemeService Service(int cash, bool allOwned = false)
        {
            _kit = new TicketTestKit(0);
            _kit.Progress.Cash = cash;
            _catalog = _kit.Fixture.Create<ThemeCatalog>();
            ConfigFixture.Edit(_catalog, so =>
            {
                var list = ConfigFixture.Find(so, "themes");
                string[] ids = { "a", "b", "c", "d" };
                int[] prices = { 0, PriceB, PriceC, 0 };
                list.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("themeId").stringValue = ids[i];
                    e.FindPropertyRelative("displayName").stringValue = ids[i].ToUpperInvariant();
                    e.FindPropertyRelative("showcaseIndex").intValue = i;
                    e.FindPropertyRelative("cashPrice").intValue = prices[i];
                }
                ConfigFixture.Find(so, "defaultThemeId").stringValue = "a";
            });
            _wallet = new WalletService(_kit.Repository, _kit.Clock);
            return new ThemeService(_catalog, _kit.Repository, new EconomyLedger(_kit.Repository, _wallet, _kit.Economy), allOwned);
        }

        [Test]
        public void ActionState_FollowsOwnershipPriceAndCash()
        {
            var s = Service(PriceB);
            Assert.AreEqual(ThemeActionState.Active, s.GetActionState("a"));
            Assert.AreEqual(ThemeActionState.UnownedAffordable, s.GetActionState("b"));   // cash == price
            Assert.AreEqual(ThemeActionState.UnownedInsufficient, s.GetActionState("c")); // cash < price
            Assert.AreEqual(ThemeActionState.NotForSale, s.GetActionState("d"));
            s.TryPurchase("b");
            Assert.AreEqual(ThemeActionState.OwnedInactive, s.GetActionState("b"));
            s.Use("b");
            Assert.AreEqual(ThemeActionState.Active, s.GetActionState("b"));
            Assert.AreEqual(ThemeActionState.OwnedInactive, s.GetActionState("a"));
        }

        [Test]
        public void Preview_ChangesShownOnly_NothingSavedOrSpent()
        {
            var s = Service(PriceC);
            int writes = _kit.Store.Writes;
            s.BeginBrowse();
            Assert.IsTrue(s.Preview("c"));
            Assert.AreEqual("c", s.ShownId);
            Assert.AreEqual("a", s.ActiveId);
            Assert.AreEqual(PriceC, _kit.Progress.Cash);
            Assert.AreEqual(writes, _kit.Store.Writes);
        }

        [Test]
        public void CancelBrowse_RestoresActiveTheme()
        {
            var s = Service(0);
            int shownEvents = 0;
            s.ShownChanged += () => shownEvents++;
            s.BeginBrowse();
            s.Preview("c");
            s.CancelBrowse();
            Assert.AreEqual("a", s.ShownId);
            Assert.IsFalse(s.IsBrowsing);
            Assert.AreEqual(2, shownEvents); // preview, then restore
        }

        [Test]
        public void CancelBrowse_AfterUse_KeepsTheNewActiveTheme()
        {
            var s = Service(PriceB);
            s.BeginBrowse();
            s.Preview("b");
            s.TryPurchase("b");
            Assert.AreEqual(ThemeSelectResult.Selected, s.Use("b"));
            s.CancelBrowse();
            Assert.AreEqual("b", s.ShownId);
            Assert.AreEqual("b", s.ActiveId);
        }

        [Test]
        public void Step_HasNoWrapAndArrowsReportTheEnds()
        {
            var s = Service(0);
            s.BeginBrowse();
            Assert.IsFalse(s.CanStep(-1));
            Assert.IsFalse(s.Step(-1));
            Assert.IsTrue(s.Step(+1));
            Assert.AreEqual("b", s.ShownId);
            s.Step(+1); s.Step(+1);
            Assert.AreEqual("d", s.ShownId);
            Assert.IsFalse(s.CanStep(+1));
            Assert.IsFalse(s.Step(+1));
            Assert.AreEqual("d", s.ShownId);
        }

        [Test]
        public void Purchase_SpendsOnce_AndRepeatDoesNotChargeAgain()
        {
            var s = Service(PriceB * 3);
            int walletEvents = 0;
            _wallet.Changed += () => walletEvents++;

            Assert.AreEqual(ThemePurchaseResult.Purchased, s.TryPurchase("b"));
            Assert.AreEqual(PriceB * 2, _kit.Progress.Cash);
            Assert.AreEqual(ThemePurchaseResult.AlreadyOwned, s.TryPurchase("b"));
            Assert.AreEqual(PriceB * 2, _kit.Progress.Cash);
            Assert.AreEqual(1, walletEvents);
            Assert.IsTrue(s.IsOwned("b"));
        }

        [Test]
        public void Purchase_Failures_ChangeNothing()
        {
            var s = Service(PriceC - 1);
            Assert.AreEqual(ThemePurchaseResult.NotEnoughCash, s.TryPurchase("c"));
            Assert.AreEqual(ThemePurchaseResult.NotForSale, s.TryPurchase("d"));
            Assert.AreEqual(ThemePurchaseResult.UnknownTheme, s.TryPurchase("zzz"));
            Assert.AreEqual(PriceC - 1, _kit.Progress.Cash);
            Assert.IsFalse(s.IsOwned("c"));
            Assert.AreEqual(0, _kit.Progress.OwnedThemeIds.Count);
        }

        [Test]
        public void Use_UnownedTheme_IsRefusedUnlessDevUnlock()
        {
            var s = Service(0);
            Assert.AreEqual(ThemeSelectResult.NotUsable, s.Use("c"));
            Assert.AreEqual("a", s.ActiveId);

            var dev = Service(0, allOwned: true);
            Assert.AreEqual(ThemeSelectResult.Selected, dev.Use("c"));
            Assert.AreEqual("c", dev.ActiveId);
            Assert.AreEqual(ThemeActionState.Active, dev.GetActionState("c"));
            Assert.AreEqual(ThemeActionState.OwnedInactive, dev.GetActionState("d"));
        }

        [Test]
        public void ActiveTheme_IsRestoredFromSave()
        {
            var s = Service(PriceB);
            s.TryPurchase("b");
            s.Use("b");

            var reloaded = new ProgressRepository(_kit.Store, () => new StartingBalances(0, 0, 0, 0));
            reloaded.Load();
            var again = new ThemeService(_catalog, reloaded,
                new EconomyLedger(reloaded, new WalletService(reloaded, _kit.Clock), _kit.Economy), false);
            Assert.AreEqual("b", again.ActiveId);
            Assert.AreEqual("b", again.ShownId);
            Assert.AreEqual(1, again.ShownShowcaseIndex);
        }

        [Test]
        public void SavedThemeThatIsNotOwned_FallsBackToDefault()
        {
            var s = Service(0);
            _kit.Progress.SelectedThemeId = "c";
            Assert.AreEqual("a", s.ActiveId);
            _kit.Progress.SelectedThemeId = "gone";
            Assert.AreEqual("a", s.ActiveId);
        }

        [Test]
        public void ActionRules_AreAPureTable()
        {
            Assert.AreEqual(ThemeActionState.Active, ThemeActionRules.Decide(true, true, 10, 0));
            Assert.AreEqual(ThemeActionState.OwnedInactive, ThemeActionRules.Decide(true, false, 10, 0));
            Assert.AreEqual(ThemeActionState.NotForSale, ThemeActionRules.Decide(false, false, 0, 999));
            Assert.AreEqual(ThemeActionState.UnownedAffordable, ThemeActionRules.Decide(false, false, 10, 10));
            Assert.AreEqual(ThemeActionState.UnownedInsufficient, ThemeActionRules.Decide(false, false, 10, 9));
        }

        private const int GemPrice = 25;

        // Themes: a (default, free), p (premium, GemPrice Purple Gems), q (premium, no price yet).
        private ThemeService PremiumService(int gems, int cash = 0)
        {
            _kit = new TicketTestKit(0);
            _kit.Progress.Cash = cash;
            _kit.Progress.PurpleGems = gems;
            _catalog = _kit.Fixture.Create<ThemeCatalog>();
            ConfigFixture.Edit(_catalog, so =>
            {
                var list = ConfigFixture.Find(so, "themes");
                string[] ids = { "a", "p", "q" };
                int[] prices = { 0, GemPrice, 0 };
                list.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("themeId").stringValue = ids[i];
                    e.FindPropertyRelative("displayName").stringValue = ids[i];
                    e.FindPropertyRelative("showcaseIndex").intValue = i;
                    e.FindPropertyRelative("cashPrice").intValue = prices[i];
                    e.FindPropertyRelative("premium").boolValue = i > 0;
                    e.FindPropertyRelative("priceCurrency").enumValueIndex = i > 0 ? (int)ThemePriceCurrency.PurpleGem : (int)ThemePriceCurrency.Cash;
                }
                ConfigFixture.Find(so, "defaultThemeId").stringValue = "a";
            });
            _wallet = new WalletService(_kit.Repository, _kit.Clock);
            return new ThemeService(_catalog, _kit.Repository, new EconomyLedger(_kit.Repository, _wallet, _kit.Economy), false);
        }

        [Test]
        public void Premium_ThemesListTheirCurrencyAndFlag()
        {
            var s = PremiumService(0);
            Assert.IsFalse(s.Themes[0].IsPremium);
            Assert.IsTrue(s.Themes[1].IsPremium);
            Assert.AreEqual(ThemePriceCurrency.PurpleGem, s.Themes[1].Currency);
            Assert.AreEqual(0, s.Themes[1].CashPrice, "a gem price is not a cash price");
        }

        [Test]
        public void Premium_Unpriced_CannotBeBought()
        {
            var s = PremiumService(1000, cash: 1000);
            Assert.AreEqual(ThemeActionState.NotForSale, s.GetActionState("q"));
            Assert.AreEqual(ThemePurchaseResult.NotForSale, s.TryPurchase("q"));
            Assert.AreEqual(1000, _kit.Progress.PurpleGems);
            Assert.AreEqual(1000, _kit.Progress.Cash);
        }

        [Test]
        public void Premium_PaidInGems_NotCash_AndIdempotent()
        {
            var s = PremiumService(GemPrice * 3, cash: 5);
            Assert.AreEqual(ThemeActionState.UnownedAffordable, s.GetActionState("p"));

            Assert.AreEqual(ThemePurchaseResult.Purchased, s.TryPurchase("p"));
            Assert.AreEqual(GemPrice * 2, _kit.Progress.PurpleGems);
            Assert.AreEqual(5, _kit.Progress.Cash, "cash is untouched");
            Assert.AreEqual(ThemePurchaseResult.AlreadyOwned, s.TryPurchase("p"));
            Assert.AreEqual(GemPrice * 2, _kit.Progress.PurpleGems, "a repeat never pays twice");
            Assert.AreEqual(ThemeActionState.OwnedInactive, s.GetActionState("p"));
        }

        [Test]
        public void Premium_NotEnoughGems_ChangesNothing_EvenWithPlentyOfCash()
        {
            var s = PremiumService(GemPrice - 1, cash: 100000);
            Assert.AreEqual(ThemeActionState.UnownedInsufficient, s.GetActionState("p"));
            Assert.AreEqual(ThemePurchaseResult.NotEnoughCash, s.TryPurchase("p"));
            Assert.AreEqual(GemPrice - 1, _kit.Progress.PurpleGems);
            Assert.AreEqual(100000, _kit.Progress.Cash);
            Assert.IsFalse(s.IsOwned("p"));
        }
    }
}
