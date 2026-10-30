using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// Scenarios 3 and 4 (T-053): the daily gift and the 7-day check-in across an app kill / reopen, a device clock wound back
    /// and a clock jumped forward. Regen is switched off in the gift tests so only the day rule moves the ticket count.
    /// </summary>
    public sealed class RestartDayRewardsRegressionTests
    {
        private RegressionHarness _h;

        [TearDown]
        public void TearDown() => _h.Dispose();

        // Starts below the cap (gift needs room) with ticket regen off, so no timer can add tickets behind the test's back.
        private void GiftHarness() => _h = new RegressionHarness(startEntryTickets: 0, regenMinutes: 0f);

        private void SevenDayHarness() => _h = new RegressionHarness();

        // ---- scenario 3: daily gift -------------------------------------------------------------------------------

        [Test]
        public void ClaimedGift_CannotBeClaimedAgainTheSameDay_AfterRestart()
        {
            GiftHarness();
            var s = _h.Boot();
            Assert.AreEqual(DailyGiftResult.Granted, s.Gift.Claim());
            int tickets = s.Tickets.Count;
            Assert.AreEqual(s.Gift.Amount, tickets);

            _h.Device += TimeSpan.FromHours(3); // still the same UTC day
            var r = _h.Boot();

            Assert.AreEqual(DailyGiftState.NotDue, r.Gift.State);
            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, r.Gift.Claim());
            Assert.AreEqual(tickets, r.Tickets.Count);
        }

        [Test]
        public void ClockWoundBackADay_AfterTheClaim_StillRefusesTheGift_AndMakesNoTicket()
        {
            GiftHarness();
            var s = _h.Boot();
            s.Gift.Claim();
            int tickets = s.Tickets.Count;

            _h.Device -= TimeSpan.FromDays(1);
            var r = _h.Boot();

            Assert.AreEqual(DailyGiftState.NotDue, r.Gift.State, "yesterday on the device clock must not look like a new day");
            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, r.Gift.Claim());
            Assert.AreEqual(tickets, r.Tickets.Count);
            Assert.IsNull(r.Tickets.NextTicketUtc, "no regen timer exists in this setup");
        }

        [Test]
        public void ClockWoundBackWithoutRestart_AlsoCannotReclaim()
        {
            GiftHarness();
            var s = _h.Boot();
            s.Gift.Claim();
            int tickets = s.Tickets.Count;

            _h.Device -= TimeSpan.FromDays(2);

            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, s.Gift.Claim());
            Assert.AreEqual(tickets, s.Tickets.Count);
        }

        [Test]
        public void NextUtcDay_OffersTheGiftAgain_AfterRestart_AndAddsExactlyTheConfiguredAmount()
        {
            GiftHarness();
            var s = _h.Boot();
            s.Gift.Claim();
            int tickets = s.Tickets.Count;

            _h.Device = _h.Device.Date.AddDays(1).AddMinutes(1); // just after the next UTC midnight
            var r = _h.Boot();

            Assert.AreEqual(DailyGiftState.Available, r.Gift.State);
            Assert.AreEqual(DailyGiftResult.Granted, r.Gift.Claim());
            Assert.AreEqual(tickets + r.Gift.Amount, r.Tickets.Count);
            Assert.AreEqual(DailyGiftResult.AlreadyClaimed, r.Gift.Claim());
        }

        [Test]
        public void JustBeforeUtcMidnight_StaysClaimed_JustAfterItIsOffered()
        {
            GiftHarness();
            var s = _h.Boot();
            s.Gift.Claim();
            DateTime midnight = _h.Device.Date.AddDays(1);

            _h.Device = midnight - TimeSpan.FromSeconds(1);
            Assert.AreEqual(DailyGiftState.NotDue, _h.Boot().Gift.State);

            _h.Device = midnight;
            Assert.AreEqual(DailyGiftState.Available, _h.Boot().Gift.State);
        }

        [Test]
        public void FullInventory_ClaimIsRefused_ButTheDayStaysOpen_AcrossRestart()
        {
            _h = new RegressionHarness(startEntryTickets: RegressionHarness.TicketCap, regenMinutes: 0f);
            var s = _h.Boot();
            Assert.AreEqual(DailyGiftResult.Full, s.Gift.Claim());

            var r = _h.Boot();
            Assert.AreEqual(DailyGiftState.Full, r.Gift.State);
            Assert.IsTrue(r.Tickets.TrySpend());
            r.Repository.Save();

            var later = _h.Boot();
            Assert.AreEqual(DailyGiftState.Available, later.Gift.State, "the refused claim did not use up the day");
            Assert.AreEqual(DailyGiftResult.Granted, later.Gift.Claim());
        }

        // ---- scenario 4: 7-day check-in ---------------------------------------------------------------------------

        private static int Count(RegressionHarness.Session s, SevenDayCardState state)
        {
            int n = 0;
            foreach (SevenDayCard card in s.SevenDay.Cards) if (card.State == state) n++;
            return n;
        }

        [Test]
        public void FirstClaim_IsSavedWithItsReward_AndNotRepeatableTheSameDayAfterRestart()
        {
            SevenDayHarness();
            var s = _h.Boot();
            int cash = s.Wallet.Cash;
            Assert.AreEqual(SevenDayClaimResult.Claimed, s.SevenDay.Claim());
            Assert.AreEqual(cash + RegressionHarness.CashDayOne, s.Wallet.Cash);

            _h.Device += TimeSpan.FromHours(2);
            var r = _h.Boot();

            Assert.AreEqual(cash + RegressionHarness.CashDayOne, r.Wallet.Cash);
            Assert.IsFalse(r.SevenDay.HasClaimable);
            Assert.AreEqual(SevenDayClaimResult.NothingAvailable, r.SevenDay.Claim());
            Assert.AreEqual(1, Count(r, SevenDayCardState.Claimed));
            Assert.AreEqual(cash + RegressionHarness.CashDayOne, r.Wallet.Cash);
        }

        [Test]
        public void ClockJumpedThreeDaysAhead_AllowsOneClaim_NotThree_AndMissedDaysDoNotResetTheProgress()
        {
            SevenDayHarness();
            var s = _h.Boot();
            s.SevenDay.Claim();
            int cash = s.Wallet.Cash;

            _h.Device += TimeSpan.FromDays(3);
            var r = _h.Boot();

            Assert.IsTrue(r.SevenDay.HasClaimable);
            Assert.AreEqual(1, Count(r, SevenDayCardState.Claimed), "the missed days did not reset the calendar");
            Assert.AreEqual(SevenDayClaimResult.Claimed, r.SevenDay.Claim());
            Assert.AreEqual(SevenDayClaimResult.NothingAvailable, r.SevenDay.Claim(), "one card per day, however long the gap");
            Assert.AreEqual(2, Count(r, SevenDayCardState.Claimed));
            Assert.AreEqual(2, r.Progress.SevenDay.ClaimedCount);
            Assert.AreEqual(cash + RegressionHarness.CashDayTwo, r.Wallet.Cash, "day 2 pays the day 2 reward");
        }

        [Test]
        public void ClockWoundBack_NeverMovesTheCalendarBackwards_NorOffersAnotherCard()
        {
            SevenDayHarness();
            var s = _h.Boot();
            s.SevenDay.Claim();
            _h.Device += TimeSpan.FromDays(3);
            s = _h.Boot();
            s.SevenDay.Claim();
            int claimed = s.Progress.SevenDay.ClaimedCount;
            int cash = s.Wallet.Cash;

            _h.Device -= TimeSpan.FromDays(5);
            var r = _h.Boot();

            Assert.AreEqual(claimed, r.Progress.SevenDay.ClaimedCount);
            Assert.IsFalse(r.SevenDay.HasClaimable);
            Assert.AreEqual(SevenDayClaimResult.NothingAvailable, r.SevenDay.Claim());
            Assert.AreEqual(claimed, Count(r, SevenDayCardState.Claimed));
            Assert.AreEqual(cash, r.Wallet.Cash);
        }

        [Test]
        public void AfterTheRollback_TheRealNextDayStillWorks()
        {
            SevenDayHarness();
            var s = _h.Boot();
            s.SevenDay.Claim();
            DateTime firstDay = _h.Device;

            _h.Device = firstDay - TimeSpan.FromDays(4); // rolled back
            _h.Boot();
            _h.Device = firstDay.Date.AddDays(1).AddHours(1); // then genuinely tomorrow
            var r = _h.Boot();

            Assert.IsTrue(r.SevenDay.HasClaimable);
            Assert.AreEqual(SevenDayClaimResult.Claimed, r.SevenDay.Claim());
            Assert.AreEqual(2, r.Progress.SevenDay.ClaimedCount);
        }

        [Test]
        public void SevenConsecutiveDays_WithARestartEachDay_CompleteTheRound_AndThePremiumDayOwnsTheItem()
        {
            SevenDayHarness();
            var s = _h.Boot();
            int days = SpeedStars.Meta.Config.SevenDayRewardConfig.DayCount;
            for (int day = 0; day < days; day++)
            {
                Assert.AreEqual(SevenDayClaimResult.Claimed, s.SevenDay.Claim(), "day " + (day + 1));
                _h.Device = _h.Device.Date.AddDays(1).AddHours(1);
                s = _h.Boot();
            }

            Assert.IsTrue(s.Cosmetics.IsOwned(RegressionHarness.HatId), "day 7 hands out the item and it is still owned after the restart");
            Assert.AreEqual(days, s.Progress.SevenDay.ClaimedCount);
        }
    }
}
