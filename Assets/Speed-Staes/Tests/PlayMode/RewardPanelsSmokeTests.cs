using System.Collections;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Home;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// Daily gift popup and 7 day panel on the real Home scene, pressed the way a finger does (a real raycast through Tap).
    /// The gift is made due by clearing the saved day key and giving the player room for a ticket; the save folder and the few
    /// progress fields the tests touch are put back afterwards. No balance number is asserted: the ticket count is compared
    /// with itself before and after.
    /// </summary>
    public sealed class RewardPanelsSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;
        private string _dayKey;
        private int _entryTickets;
        private long _regenDue;
        private int _timedTickets;
        private bool _captured;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            _captured = false;
            yield return LoadScene(HomeScene);

            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            _dayKey = progress.DailyGiftDayKey;
            _entryTickets = progress.EntryTickets;
            _regenDue = progress.EntryRegenDueUtcTicks;
            _timedTickets = progress.TimedTickets;
            _captured = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (ProjectContext.HasInstance && _captured)
            {
                PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
                progress.DailyGiftDayKey = _dayKey;
                progress.EntryTickets = _entryTickets;
                progress.EntryRegenDueUtcTicks = _regenDue;
                progress.TimedTickets = _timedTickets;
            }
            _saveFiles.Restore();
            yield break;
        }

        // ---- daily gift ----

        [UnityTest]
        public IEnumerator DailyGift_WhenDue_OpensOnHome_ClaimClosesItAndAddsATicket()
        {
            // Due today with room in the inventory, then Home opens again: it must open the gift by itself.
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;
            progress.DailyGiftDayKey = string.Empty;
            progress.EntryTickets = 1;
            progress.EntryRegenDueUtcTicks = 0;
            IEntryTickets tickets = Container.Resolve<IEntryTickets>();
            tickets.Refresh();
            Assume.That(tickets.Cap, Is.GreaterThan(tickets.Count), "the test needs room for one more ticket");
            int before = tickets.Count;

            yield return LoadScene(HomeScene, dismissDailyGift: false);
            DailyGiftPopupView popup = Require<DailyGiftPopupView>();
            yield return WaitOpen(popup, "daily gift popup (opens by itself on Home)");
            Assert.IsNull(Field<Button>(popup, "closeButton"), "the claimable gift has no X");

            yield return Tap(RequireField<Button>(popup, "actionButton"), "daily gift CLAIM");
            yield return WaitClosed(popup, "daily gift popup after CLAIM");

            TicketFlightView flight = RequireField<TicketFlightView>(popup, "flight");
            yield return WaitUntil(() => !flight.gameObject.activeSelf, "the ticket flight to land", 10f);
            Assert.AreEqual(before + 1, Container.Resolve<IEntryTickets>().Count, "CLAIM must add exactly one Entry Ticket");
            Assert.AreEqual(DailyGiftState.NotDue, Container.Resolve<IDailyGift>().State, "the day's gift is taken");
        }

        // ---- 7 day panel ----

        [UnityTest]
        public IEnumerator SevenDay_Panel_ShowsSevenCards_AndClaimAndRedDotFollowTheService()
        {
            HomeScreenView home = Require<HomeScreenView>();
            SevenDayPanelView panel = Require<SevenDayPanelView>();
            ISevenDayRewards rewards = Container.Resolve<ISevenDayRewards>();

            yield return Tap(RequireField<Button>(home, "sevenDayButton"), "7-day button");
            yield return WaitOpen(panel, "7 day panel");

            var cards = RequireField<RewardDayCardView[]>(panel, "dayCards");
            Assert.AreEqual(7, cards.Length, "seven day cards");
            Assert.AreEqual(7, rewards.Cards.Count, "the service has seven cards");
            foreach (RewardDayCardView card in cards) Assert.IsNotNull(card, "a day card is not bound in the panel");

            Button claim = RequireField<Button>(panel, "claimButton");
            Assert.AreEqual(rewards.HasClaimable, claim.interactable, "CLAIM works exactly while a reward is claimable");
            GameObject dot = Field<GameObject>(home, "sevenDayRedDot");
            if (dot != null) Assert.AreEqual(rewards.HasClaimable, dot.activeSelf, "the calendar's red dot follows HasClaimable");

            yield return Tap(CloseButtonOf(panel), "7 day panel X");
            yield return WaitClosed(panel, "7 day panel");
        }
    }
}
