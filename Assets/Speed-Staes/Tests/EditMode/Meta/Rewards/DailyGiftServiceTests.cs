using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Rewards;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Economy;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Rewards
{
    public sealed class DailyGiftServiceTests
    {
        private const int GiftAmount = 2; // arbitrary test input

        private EconomyTestKit _k;
        private DailyGiftService _gift;

        [SetUp]
        public void SetUp()
        {
            _k = new EconomyTestKit(entryTickets: 0);
            ConfigFixture.SetInt(_k.Kit.Economy, "dailyGiftEntryTickets", GiftAmount);
            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 1, 10, 0, 0, DateTimeKind.Utc);
            _gift = new DailyGiftService(_k.Kit.Repository, _k.Kit.Clock, _k.Kit.Economy, _k.Tickets, _k.Tickets);
        }

        [TearDown]
        public void TearDown() => _k.Dispose();

        [Test]
        public void Available_ThenClaim_AddsConfiguredAmount_AndCannotBeClaimedTwiceToday()
        {
            Assert.AreEqual(DailyGiftState.Available, _gift.State);
            Assert.AreEqual(GiftAmount, _gift.Amount);

            Assert.AreEqual(DailyGiftResult.Granted, _gift.Claim());

            Assert.AreEqual(GiftAmount, _k.Progress.EntryTickets);
            Assert.AreEqual(DailyGiftState.NotDue, _gift.State);
            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, _gift.Claim());
            Assert.AreEqual(GiftAmount, _k.Progress.EntryTickets);
        }

        [Test]
        public void Claim_SavesTheDay_SoARestartDoesNotOfferItAgain()
        {
            _gift.Claim();

            var reloaded = _k.Restart();
            var again = new DailyGiftService(reloaded, _k.Kit.Clock, _k.Kit.Economy, _k.Tickets, _k.Tickets);

            Assert.AreEqual(DailyGiftState.NotDue, again.State);
            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, again.Claim());
        }

        [Test]
        public void NextUtcDay_OffersTheGiftAgain()
        {
            _gift.Claim();
            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 2, 0, 0, 1, DateTimeKind.Utc);

            Assert.AreEqual(DailyGiftState.Available, _gift.State);
            Assert.AreEqual(DailyGiftResult.Granted, _gift.Claim());
        }

        [Test]
        public void SameUtcDay_StaysClaimed_UntilMidnightUtc()
        {
            _gift.Claim();
            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 1, 23, 59, 59, DateTimeKind.Utc);

            Assert.AreEqual(DailyGiftState.NotDue, _gift.State);
        }

        [Test]
        public void FullInventory_GivesNothing_AndKeepsTheDaysRight()
        {
            _k.Progress.EntryTickets = TicketTestKit.Cap;

            Assert.AreEqual(DailyGiftState.Full, _gift.State);
            Assert.AreEqual(DailyGiftResult.Full, _gift.Claim());
            Assert.AreEqual(TicketTestKit.Cap, _k.Progress.EntryTickets);
            Assert.AreEqual(string.Empty, _k.Progress.DailyGiftDayKey);

            _gift.MarkSeenFull();
            Assert.AreEqual(DailyGiftState.Full, _gift.State, "seeing the notice does not use up the day");
            Assert.IsTrue(_gift.FullNoticeSeenToday);
        }

        [Test]
        public void FullInventory_ThenATicketIsUsed_SameDayClaimWorks()
        {
            _k.Progress.EntryTickets = TicketTestKit.Cap;
            Assert.AreEqual(DailyGiftResult.Full, _gift.Claim());

            Assert.IsTrue(_k.Tickets.TrySpend());

            Assert.AreEqual(DailyGiftState.Available, _gift.State);
            Assert.AreEqual(DailyGiftResult.Granted, _gift.Claim());
            Assert.AreEqual(TicketTestKit.Cap, _k.Progress.EntryTickets, "clamped to the cap");
        }

        [Test]
        public void ZeroAmount_IsNotDue_AndChangesNothing()
        {
            ConfigFixture.SetInt(_k.Kit.Economy, "dailyGiftEntryTickets", 0);

            Assert.AreEqual(DailyGiftState.NotDue, _gift.State);
            Assert.AreEqual(DailyGiftResult.NotDue, _gift.Claim());
            Assert.AreEqual(0, _k.Progress.EntryTickets);
            Assert.AreEqual(string.Empty, _k.Progress.DailyGiftDayKey);
        }

        [Test]
        public void Claim_RaisesChanged_OnlyWhenGranted()
        {
            int raised = 0;
            _gift.Changed += () => raised++;

            _gift.Claim();
            _gift.Claim();

            Assert.AreEqual(1, raised);
        }
    }
}
