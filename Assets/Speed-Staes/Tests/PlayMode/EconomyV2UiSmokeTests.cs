using System.Collections;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Home;
using SpeedStars.UI.Race;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Economy v2 screens on the real scenes (T-085), pressed with real raycast taps: the Cash "+" of the resource bar opens the
    /// Cash offer window and a (dev fake) ad pays; a premium event's entry gate sells the Timed Ticket window for Cash in one tap;
    /// the Results screen shows or hides the Cash breakdown. A piece that is not in the scene yet (prefab not rebuilt, service not
    /// bound) ignores the test with the reason instead of failing it. No price or amount is asserted as a number: values are read
    /// from the configs and compared with themselves. The save and the touched progress fields are put back.
    /// </summary>
    public sealed class EconomyV2UiSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private bool _captured;
        private int _cash;
        private int _entryTickets;
        private int _timedTickets;
        private long _timedExpires;
        private long _regenDue;
        private string _adDay;
        private int _adWatched;
        private RaceEventId _selected;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _captured = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance)
            {
                SceneFlow flow = Container.TryResolve<SceneFlow>();
                if (flow != null) yield return WaitUntil(() => !flow.IsTransitioning, "scene move to finish", 15f);

                Container.Resolve<IRaceEntryGate>().TryTakePending(out _);
                if (_captured)
                {
                    PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
                    progress.Cash = _cash;
                    progress.EntryTickets = _entryTickets;
                    progress.TimedTickets = _timedTickets;
                    progress.TimedAccessExpiresUtcTicks = _timedExpires;
                    progress.EntryRegenDueUtcTicks = _regenDue;
                    progress.AdCashDayKey = _adDay;
                    progress.AdCashWatched = _adWatched;
                    if (!_selected.IsEmpty) Container.Resolve<IRaceSelection>().Select(_selected);
                    Container.Resolve<IEntryTickets>().Refresh();
                }
            }
            _saveFiles.Restore();
        }

        private void Capture()
        {
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            _cash = progress.Cash;
            _entryTickets = progress.EntryTickets;
            _timedTickets = progress.TimedTickets;
            _timedExpires = progress.TimedAccessExpiresUtcTicks;
            _regenDue = progress.EntryRegenDueUtcTicks;
            _adDay = progress.AdCashDayKey;
            _adWatched = progress.AdCashWatched;
            _selected = Container.Resolve<IRaceSelection>().Selected;
            _captured = true;
        }

        // ---------------------------------------------------------------- Cash "+" -> offer window

        [UnityTest]
        public IEnumerator CashPlus_RealTap_OpensTheOfferWindow_AndAnAdPaysCash()
        {
            yield return LoadScene(HomeScene);
            Capture();

            ResourceBarView bar = Require<ResourceBarView>();
            Button plus = Field<Button>(bar, "cashPlusButton");
            if (plus == null) Assert.Ignore("SCENE: the ResourceBar prefab has no Cash/Plus yet (rebuild UiPrefabBuilder.BuildAll(overwrite: true)).");
            CashOfferModalView modal = Field<CashOfferModalView>(bar, "cashOfferModal");
            if (modal == null) Assert.Ignore("SCENE: no CashOfferModal instance is bound to the ResourceBar (cashOfferModal); the Cash '+' stays closed.");
            IRewardedCash cash = Container.TryResolve<IRewardedCash>();
            if (cash == null) Assert.Ignore("WIRING: IRewardedCash is not bound; the offer window cannot start an ad.");

            // Start from a fresh day so rights are left; the counter fields are put back in TearDown.
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.AdCashDayKey = string.Empty;
            progress.AdCashWatched = 0;
            if (cash.Amount <= 0 || cash.DailyLimit <= 0) Assert.Ignore("The Cash ad is closed in the economy config (amount / daily limit 0).");
            if (Container.TryResolve<IRewardedAds>() == null) Assert.Ignore("No IRewardedAds is bound (the dev fake needs its WIRING).");

            yield return Tap(plus, "Cash plus");
            yield return WaitOpen(modal, "Cash offer window");

            Button watch = RequireField<Button>(modal, "watchAdButton");
            int cashBefore = Container.Resolve<IWallet>().Cash;
            int remainingBefore = cash.RemainingToday;
            yield return Tap(watch, "WATCH AD");

            yield return WaitUntil(() => cash.RemainingToday < remainingBefore, "the ad to count against today's limit", 15f);
            Assert.AreEqual(cashBefore + cash.Amount, Container.Resolve<IWallet>().Cash, "the ad paid its configured amount");

            // The window shows the new counter, and (once the diamond landed) the new balance.
            Text counter = RequireField<Text>(modal, "counterText");
            Assert.AreEqual($"{cash.DailyLimit - cash.RemainingToday}/{cash.DailyLimit}", counter.text);
            Text balance = RequireField<Text>(modal, "balanceText");
            yield return WaitUntil(() => balance.text == Container.Resolve<IWallet>().Cash.ToString(), "the balance to count up to the wallet value", 10f);

            yield return Tap(CloseButtonOf(modal), "Cash offer window Close");
            yield return WaitClosed(modal, "Cash offer window");
        }

        [UnityTest]
        public IEnumerator CashOffer_WithNoRightsLeft_HasAGreyButton_AndSaysWhy()
        {
            yield return LoadScene(HomeScene);
            Capture();

            ResourceBarView bar = Require<ResourceBarView>();
            Button plus = Field<Button>(bar, "cashPlusButton");
            CashOfferModalView modal = Field<CashOfferModalView>(bar, "cashOfferModal");
            if (plus == null || modal == null) Assert.Ignore("SCENE: Cash '+' or the CashOfferModal instance is not in the Home scene yet.");
            IRewardedCash cash = Container.TryResolve<IRewardedCash>();
            if (cash == null) Assert.Ignore("WIRING: IRewardedCash is not bound.");
            if (cash.DailyLimit <= 0) Assert.Ignore("The Cash ad is closed in the economy config.");

            // Use every right of today up (today's key from the service's own view: the saved counter counts for today at once).
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.AdCashDayKey = "99991231";
            progress.AdCashWatched = cash.DailyLimit;
            Assert.AreEqual(0, cash.RemainingToday);

            yield return Tap(plus, "Cash plus");
            yield return WaitOpen(modal, "Cash offer window");
            yield return null;

            Assert.IsFalse(RequireField<Button>(modal, "watchAdButton").interactable, "no rights left: the button is closed");
            Assert.IsTrue(RequireField<Text>(modal, "noteText").gameObject.activeSelf, "and a line says why");

            yield return Tap(CloseButtonOf(modal), "Cash offer window Close");
            yield return WaitClosed(modal, "Cash offer window");
        }

        // ---------------------------------------------------------------- premium event -> Spend Cash, unlock

        [UnityTest]
        public IEnumerator PremiumGate_SpendCash_RealTap_StartsTheTimedWindow_AndChargesThePrice()
        {
            yield return LoadScene(HomeScene);
            Capture();

            if (Container.TryResolve<ITimedTicketShop>() == null) Assert.Ignore("WIRING: ITimedTicketShop is not bound; the Cash unlock stays closed.");
            if (!Container.Resolve<EconomyConfig>().TryGetTimedTicketCashPrice(out int price)) Assert.Ignore("The Timed Ticket has no Cash price in the economy config.");
            SelectTicketEvent();

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.EntryTickets = 0;
            progress.TimedAccessExpiresUtcTicks = 0;
            progress.EntryRegenDueUtcTicks = 0;
            progress.Cash = price + 7;
            Container.Resolve<IEntryTickets>().Refresh();

            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            Button unlock = Field<Button>(gate, "useTimedCashButton");
            if (unlock == null) Assert.Ignore("SCENE: the EntryGateModal prefab has no UnlockWithCash button yet (rebuild UiPrefabBuilder.BuildAll(overwrite: true)).");

            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");

            IWallet wallet = Container.Resolve<IWallet>();
            Assert.IsFalse(wallet.TimedAccessActive);
            int before = wallet.Cash;
            yield return Tap(unlock, "Spend Cash - unlock");

            yield return WaitUntil(() => wallet.TimedAccessActive, "the Timed Ticket window to start", 10f);
            Assert.AreEqual(before - price, wallet.Cash, "exactly the configured price was charged");
            yield return null;
            Assert.IsFalse(unlock.gameObject.activeSelf, "no second purchase while the window runs (time does not stack)");

            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
        }

        [UnityTest]
        public IEnumerator PremiumGate_WithoutTheCash_TheUnlockButtonIsGrey()
        {
            yield return LoadScene(HomeScene);
            Capture();

            if (Container.TryResolve<ITimedTicketShop>() == null) Assert.Ignore("WIRING: ITimedTicketShop is not bound.");
            if (!Container.Resolve<EconomyConfig>().TryGetTimedTicketCashPrice(out int price)) Assert.Ignore("The Timed Ticket has no Cash price in the economy config.");
            SelectTicketEvent();

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.EntryTickets = 0;
            progress.TimedAccessExpiresUtcTicks = 0;
            progress.EntryRegenDueUtcTicks = 0;
            progress.Cash = price - 1;
            Container.Resolve<IEntryTickets>().Refresh();

            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            Button unlock = Field<Button>(gate, "useTimedCashButton");
            if (unlock == null) Assert.Ignore("SCENE: the EntryGateModal prefab has no UnlockWithCash button yet.");

            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");
            yield return null;

            Assert.IsTrue(unlock.gameObject.activeSelf, "offered while there is no window");
            Assert.IsFalse(unlock.interactable, "not enough Cash: the button is closed");

            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
        }

        // ---------------------------------------------------------------- Results: Cash breakdown

        [UnityTest]
        public IEnumerator Results_ShowsTheCashBreakdownLines_AndHidesItForAnEmptyReward()
        {
            yield return LoadScene(RaceScene);
            Capture();

            ResultsScreenView results = Require<ResultsScreenView>();
            CashBreakdownView breakdown = Field<CashBreakdownView>(results, "cashBreakdown");
            if (breakdown == null) Assert.Ignore("SCENE: the ResultsScreen prefab has no CashBreakdown yet (rebuild UiPrefabBuilder.BuildAll(overwrite: true)).");

            var paid = new RaceCashReward(3, 0, 5, 2, 4);
            results.ShowCashReward(paid);
            Assert.IsTrue(breakdown.IsShown, "a race that paid Cash shows the block");
            Assert.AreEqual($"+{paid.Total}", RequireField<Text>(breakdown, "totalText").text);

            Text[] lines = RequireField<Text[]>(breakdown, "lineTexts");
            CanvasGroup[] groups = RequireField<CanvasGroup[]>(breakdown, "lineGroups");
            Assert.AreEqual($"Finished race +{paid.Finish}", lines[0].text);
            Assert.IsTrue(groups[0].gameObject.activeSelf);
            Assert.IsFalse(groups[1].gameObject.activeSelf, "a line that paid nothing is hidden");
            Assert.AreEqual($"Rivals beaten +{paid.Opponents}", lines[2].text);
            Assert.IsTrue(groups[2].gameObject.activeSelf);

            results.ShowCashReward(RaceCashReward.None); // a solo run after it
            Assert.IsFalse(breakdown.IsShown, "an empty reward hides the block (and clears the previous race's)");
            yield return null;
        }

        // ---------------------------------------------------------------- helpers

        private static void SelectTicketEvent()
        {
            RaceEventCatalog catalog = Container.Resolve<RaceEventCatalog>();
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable || !definition.RequiresTicket) continue;
                Container.Resolve<IRaceSelection>().Select(definition.Id);
                return;
            }
            Assert.Ignore("The event catalog has no playable event that requires a ticket; the entry gate cannot be opened.");
        }
    }
}
