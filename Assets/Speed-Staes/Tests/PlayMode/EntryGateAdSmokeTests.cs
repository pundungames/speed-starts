using System.Collections;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// WATCH AD on the real entry gate (T-050): Home, Play with no ticket opens the gate, the button is pressed with a real
    /// raycast tap, the (dev fake) ad runs and the ticket count rises. Needs an ad service bound and the ad open in the
    /// economy config; when either is missing the test is ignored with the reason, not failed. No ticket count is asserted
    /// as a number: the count is compared with itself before and after. The save and the touched progress fields are put back.
    /// </summary>
    public sealed class EntryGateAdSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private int _entryTickets;
        private int _timedTickets;
        private long _regenDue;
        private long _timedExpires;
        private bool _captured;
        private RaceEventId _selected;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _captured = false;
            yield return LoadScene(HomeScene);

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            _entryTickets = progress.EntryTickets;
            _timedTickets = progress.TimedTickets;
            _regenDue = progress.EntryRegenDueUtcTicks;
            _timedExpires = progress.TimedAccessExpiresUtcTicks;
            _selected = Container.Resolve<IRaceSelection>().Selected;
            _captured = true;
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
                    progress.EntryTickets = _entryTickets;
                    progress.TimedTickets = _timedTickets;
                    progress.EntryRegenDueUtcTicks = _regenDue;
                    progress.TimedAccessExpiresUtcTicks = _timedExpires;
                    if (!_selected.IsEmpty) Container.Resolve<IRaceSelection>().Select(_selected);
                    Container.Resolve<IEntryTickets>().Refresh();
                }
            }
            _saveFiles.Restore();
        }

        [UnityTest]
        public IEnumerator WatchAd_RealTap_RunsTheAd_AndRaisesTheTicketCount()
        {
            if (Container.TryResolve<IRewardedAds>() == null) Assert.Ignore("No IRewardedAds is bound (the dev fake needs its WIRING): WATCH AD cannot be exercised.");
            if (!Container.Resolve<EconomyConfig>().TryGetRewardedAdEntryTickets(out _)) Assert.Ignore("The rewarded ad is closed in the economy config (rewardedAdEnabled / rewardedAdEntryTickets).");

            SelectTicketEvent();
            IEntryTickets tickets = Container.Resolve<IEntryTickets>();
            SetTickets(0); // below the cap, so the ad is open

            HomeScreenView home = Require<HomeScreenView>();
            EntryGateModalView gate = RequireField<EntryGateModalView>(home, "entryGateModal");
            yield return Tap(RequireField<Button>(home, "playButton"), "Play button");
            yield return WaitOpen(gate, "entry gate");

            int before = tickets.Count;
            Button watch = RequireField<Button>(gate, "watchAdButton");
            yield return Tap(watch, "WATCH AD");

            RectTransform spinner = RequireField<RectTransform>(gate, "watchAdSpinner");
            Assert.IsTrue(spinner.gameObject.activeSelf || tickets.Count > before, "the button shows the loading spinner while the ad runs");

            yield return WaitUntil(() => tickets.Count > before, "the ad ticket to arrive", 15f);
            yield return null;

            Assert.IsFalse(spinner.gameObject.activeSelf, "the spinner goes away when the ad ended");
            Assert.IsTrue(RequireField<Text>(gate, "watchAdLabel").gameObject.activeSelf, "the WATCH AD label is back");

            yield return Tap(CloseButtonOf(gate), "entry gate Close");
            yield return WaitClosed(gate, "entry gate");
        }

        // ---- helpers ----

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

        private static void SetTickets(int count)
        {
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            IEntryTickets tickets = Container.Resolve<IEntryTickets>();
            Assert.GreaterOrEqual(tickets.Cap, count, "test wants more tickets than the cap allows");
            progress.EntryTickets = count;
            progress.EntryRegenDueUtcTicks = 0;
            progress.TimedAccessExpiresUtcTicks = 0;
            tickets.Refresh();
        }
    }
}
