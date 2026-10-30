using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// Scenarios 1 and 2 (T-053): Entry Ticket regen and the Timed Ticket window across an app kill / reopen and a moving device
    /// clock. The whole stack is rebuilt from the save with <c>Boot()</c>; the clock is the real <c>ClockService</c>.
    /// </summary>
    public sealed class RestartTimersRegressionTests
    {
        private RegressionHarness _h;

        [SetUp]
        public void SetUp() => _h = new RegressionHarness();

        [TearDown]
        public void TearDown() => _h.Dispose();

        /// <summary>Spends <paramref name="count"/> tickets at the current instant and saves, like the entry gate does.</summary>
        private static void Spend(RegressionHarness.Session s, int count)
        {
            for (int i = 0; i < count; i++) Assert.IsTrue(s.Tickets.TrySpend(), "spend " + (i + 1));
            s.Repository.Save();
        }

        // ---- scenario 1: tickets ----------------------------------------------------------------------------------

        [Test]
        public void SpentTicket_SurvivesRestart_WithSameCountAndSameRegenDeadline()
        {
            var s = _h.Boot();
            int full = s.Tickets.Cap;
            Assert.AreEqual(full, s.Tickets.Count, "the test starts with a full inventory");
            DateTime spentAt = _h.Device;
            Spend(s, 1);
            DateTime? due = s.Tickets.NextTicketUtc;
            Assert.IsNotNull(due);
            Assert.AreEqual(spentAt + _h.RegenInterval, due.Value, "a drop below the cap starts a fresh full interval");

            var r = _h.Boot();

            Assert.AreEqual(full - 1, r.Tickets.Count);
            Assert.AreEqual(due, r.Tickets.NextTicketUtc);
        }

        [Test]
        public void RestartMidInterval_DoesNotRestartTheTimer()
        {
            var s = _h.Boot();
            Spend(s, 1);
            DateTime? due = s.Tickets.NextTicketUtc;
            _h.Device += TimeSpan.FromTicks(_h.RegenInterval.Ticks / 2);

            var r = _h.Boot();

            Assert.AreEqual(due, r.Tickets.NextTicketUtc, "reopening the app must not push the next ticket out");
            Assert.AreEqual(s.Tickets.Cap - 1, r.Tickets.Count, "half an interval gives nothing yet");
        }

        [Test]
        public void ClosedForSeveralIntervals_RegensThatManyTickets_AndNoMore()
        {
            var s = _h.Boot();
            int cap = s.Tickets.Cap;
            Spend(s, cap - 1); // one ticket left
            const int before = 1;
            TimeSpan closedFor = TimeSpan.FromTicks(_h.RegenInterval.Ticks * 5 / 2); // two whole intervals and a half
            long wholeIntervals = closedFor.Ticks / _h.RegenInterval.Ticks;
            _h.Device += closedFor;

            var r = _h.Boot();

            Assert.AreEqual(before + (int)wholeIntervals, r.Tickets.Count);
            Assert.Less(r.Tickets.Count, cap, "the test must not reach the cap by accident");
            Assert.IsNotNull(r.Tickets.NextTicketUtc);
            Assert.Greater(r.Tickets.NextTicketUtc.Value, _h.Device, "the next ticket is still ahead");
        }

        [Test]
        public void ClosedLongerThanItTakesToRefill_StopsAtTheCap_AndTheTimerStops()
        {
            var s = _h.Boot();
            int cap = s.Tickets.Cap;
            Spend(s, cap);
            Assert.AreEqual(0, s.Tickets.Count);
            _h.Device += TimeSpan.FromTicks(_h.RegenInterval.Ticks * (cap * 4));

            var r = _h.Boot();

            Assert.AreEqual(cap, r.Tickets.Count);
            Assert.IsNull(r.Tickets.NextTicketUtc, "at the cap no timer runs");
            Assert.IsTrue(r.Tickets.IsFull);
            Assert.AreEqual(TicketGrantResult.Full, r.Tickets.TryGrant(TicketGrantSource.DailyGift));
        }

        [Test]
        public void ClockWoundBackWhileClosed_GivesNoTickets_AndKeepsTheDeadline()
        {
            var s = _h.Boot();
            DateTime spentAt = _h.Device;
            Spend(s, 3);
            DateTime? due = s.Tickets.NextTicketUtc;
            int count = s.Tickets.Count;
            _h.Device -= TimeSpan.FromTicks(_h.RegenInterval.Ticks * 10);

            var r = _h.Boot();

            Assert.AreEqual(count, r.Tickets.Count);
            Assert.AreEqual(due, r.Tickets.NextTicketUtc, "the clock holds at the latest time seen, so the deadline does not move");
            Assert.AreEqual(spentAt, r.Clock.UtcNow, "the clock holds at the latest time it handed out");
            Assert.IsTrue(r.Clock.IsRolledBack);
        }

        [Test]
        public void ClockWoundBackThenForwardPastTheDeadline_PaysEachTicketOnlyOnce()
        {
            var s = _h.Boot();
            Spend(s, 3);
            int count = s.Tickets.Count;
            DateTime spentAt = _h.Device;

            _h.Device = spentAt - TimeSpan.FromHours(5); // wound back
            var back = _h.Boot();
            Assert.AreEqual(count, back.Tickets.Count);

            _h.Device = spentAt + _h.RegenInterval + TimeSpan.FromTicks(_h.RegenInterval.Ticks / 4); // genuinely one interval later
            var forward = _h.Boot();

            Assert.AreEqual(count + 1, forward.Tickets.Count, "winding back and forth earns nothing extra");
        }

        // ---- scenario 2: timed ticket -----------------------------------------------------------------------------

        [Test]
        public void ActiveTimedWindow_SurvivesRestart_AndRemainingTimeFollowsTheRealClock()
        {
            var s = _h.Boot();
            DateTime activatedAt = _h.Device;
            Assert.AreEqual(TimedAccessResult.Activated, s.Timed.TryActivate());
            DateTime endsAt = activatedAt + _h.TimedWindow;
            Assert.AreEqual(endsAt, s.Wallet.TimedAccessEndsUtc);

            TimeSpan closedFor = TimeSpan.FromTicks(_h.TimedWindow.Ticks / 3);
            _h.Device = activatedAt + closedFor;
            var r = _h.Boot();

            Assert.IsTrue(r.Wallet.TimedAccessActive);
            Assert.AreEqual(endsAt, r.Wallet.TimedAccessEndsUtc, "the deadline is a UTC stamp, not a countdown that restarts");
            Assert.AreEqual(_h.TimedWindow - closedFor, r.Wallet.TimedAccessEndsUtc.Value - r.Clock.UtcNow);
        }

        [Test]
        public void TimedWindow_IsClosedAtItsEndInstant_AndStaysClosedAfterRestart()
        {
            var s = _h.Boot();
            DateTime activatedAt = _h.Device;
            s.Timed.TryActivate();

            _h.Device = activatedAt + _h.TimedWindow - TimeSpan.FromTicks(1);
            Assert.IsTrue(_h.Boot().Wallet.TimedAccessActive, "one tick before the end it is still open");

            _h.Device = activatedAt + _h.TimedWindow;
            var atEnd = _h.Boot();
            Assert.IsFalse(atEnd.Wallet.TimedAccessActive, "at the end instant it is closed");
            Assert.IsNull(atEnd.Wallet.TimedAccessEndsUtc);

            _h.Device = activatedAt + _h.TimedWindow + TimeSpan.FromDays(3);
            Assert.IsFalse(_h.Boot().Wallet.TimedAccessActive);
        }

        [Test]
        public void SecondActivation_AfterRestart_IsRefused_AndSpendsNothing()
        {
            var s = _h.Boot();
            s.Timed.TryActivate();
            int stockAfterFirst = s.Wallet.TimedTickets;
            _h.Device += TimeSpan.FromTicks(_h.TimedWindow.Ticks / 2);

            var r = _h.Boot();

            Assert.AreEqual(stockAfterFirst, r.Wallet.TimedTickets, "the spent ticket stayed spent across the restart");
            Assert.AreEqual(TimedAccessResult.AlreadyActive, r.Timed.TryActivate());
            Assert.AreEqual(stockAfterFirst, r.Wallet.TimedTickets);
        }

        [Test]
        public void ClockWoundBackDuringTheWindow_DoesNotExtendIt()
        {
            var s = _h.Boot();
            DateTime activatedAt = _h.Device;
            s.Timed.TryActivate();
            DateTime endsAt = s.Wallet.TimedAccessEndsUtc.Value;
            _h.Device = activatedAt + TimeSpan.FromTicks(_h.TimedWindow.Ticks / 2);
            var mid = _h.Boot();
            DateTime seen = mid.Clock.UtcNow;
            mid.Repository.Save(); // the guard stamp is written with the next save, as in the game

            _h.Device = activatedAt - TimeSpan.FromHours(1);
            var r = _h.Boot();

            Assert.AreEqual(endsAt, r.Wallet.TimedAccessEndsUtc);
            Assert.GreaterOrEqual(r.Clock.UtcNow, seen, "the clock does not go back, so the remaining time does not grow");
            Assert.LessOrEqual(r.Wallet.TimedAccessEndsUtc.Value - r.Clock.UtcNow, endsAt - seen);
        }

        [Test]
        public void TimedWindowEnded_ThenNoStockLeft_CannotBeActivatedAgain()
        {
            var s = _h.Boot();
            DateTime activatedAt = _h.Device;
            s.Timed.TryActivate();
            Assert.AreEqual(0, s.Wallet.TimedTickets, "the harness gives one Timed Ticket");

            _h.Device = activatedAt + _h.TimedWindow + TimeSpan.FromMinutes(1);
            var r = _h.Boot();

            Assert.IsFalse(r.Wallet.TimedAccessActive);
            Assert.AreEqual(TimedAccessResult.NoTicket, r.Timed.TryActivate());
        }
    }
}
