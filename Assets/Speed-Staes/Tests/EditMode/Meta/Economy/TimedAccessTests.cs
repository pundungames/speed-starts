using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class TimedAccessTests
    {
        private EconomyTestKit _k;

        [SetUp]
        public void SetUp() => _k = new EconomyTestKit(entryTickets: 2);

        [TearDown]
        public void TearDown() => _k.Dispose();

        [Test]
        public void Activate_SpendsOneStockTicket_AndOpensTheWindow()
        {
            _k.Progress.TimedTickets = 2;
            DateTime start = _k.Kit.Clock.UtcNow;

            Assert.AreEqual(TimedAccessResult.Activated, _k.Timed.TryActivate());

            Assert.AreEqual(1, _k.Progress.TimedTickets);
            Assert.IsTrue(_k.Wallet.TimedAccessActive);
            Assert.AreEqual(start + EconomyTestKit.TimedWindow, _k.Wallet.TimedAccessEndsUtc);
        }

        [Test]
        public void Activate_WithoutStock_ChangesNothing()
        {
            Assert.AreEqual(TimedAccessResult.NoTicket, _k.Timed.TryActivate());
            Assert.IsFalse(_k.Wallet.TimedAccessActive);
            Assert.AreEqual(0, _k.Progress.TimedTickets);
        }

        [Test]
        public void SecondUseWhileActive_IsRefused_NoSpend_NoStacking()
        {
            _k.Progress.TimedTickets = 2;
            _k.Timed.TryActivate();
            DateTime? ends = _k.Wallet.TimedAccessEndsUtc;
            _k.Kit.Clock.Advance(TimeSpan.FromTicks(EconomyTestKit.TimedWindow.Ticks / 2));

            Assert.AreEqual(TimedAccessResult.AlreadyActive, _k.Timed.TryActivate());

            Assert.AreEqual(1, _k.Progress.TimedTickets);
            Assert.AreEqual(ends, _k.Wallet.TimedAccessEndsUtc);
        }

        [Test]
        public void AfterTheWindow_AnotherTicketCanBeUsed()
        {
            _k.Progress.TimedTickets = 2;
            _k.Timed.TryActivate();
            _k.Kit.Clock.Advance(EconomyTestKit.TimedWindow);

            Assert.IsFalse(_k.Wallet.TimedAccessActive);
            Assert.AreEqual(TimedAccessResult.Activated, _k.Timed.TryActivate());
            Assert.AreEqual(0, _k.Progress.TimedTickets);
        }

        [Test]
        public void NoDuration_NothingIsSpent()
        {
            SpeedStars.Tests.Meta.Config.ConfigFixture.SetFloat(_k.Kit.Economy, "timedAccessMinutes", 0f);
            _k.Progress.TimedTickets = 1;

            Assert.AreEqual(TimedAccessResult.NotAvailable, _k.Timed.TryActivate());
            Assert.AreEqual(1, _k.Progress.TimedTickets);
        }

        [Test]
        public void Window_KeepsRunningWhileTheGameIsClosed()
        {
            _k.Progress.TimedTickets = 1;
            _k.Timed.TryActivate();

            // Quit, come back half a window later, then past the end.
            var reloaded = _k.Restart();
            _k.Kit.Clock.Advance(TimeSpan.FromTicks(EconomyTestKit.TimedWindow.Ticks / 2));
            Assert.IsTrue(_k.Wallet.TimedAccessActive);
            Assert.AreEqual(0, reloaded.Current.TimedTickets);

            _k.Kit.Clock.Advance(EconomyTestKit.TimedWindow);
            Assert.IsFalse(_k.Wallet.TimedAccessActive);
        }

        [Test]
        public void ActiveWindow_LetsTicketedEntryThrough_WithoutSpendingEntryTickets()
        {
            _k.Progress.TimedTickets = 1;
            _k.Timed.TryActivate();
            int before = _k.Progress.EntryTickets;

            Assert.AreEqual(EntryResult.EnteredWithTimedAccess, _k.Gate.Enter(EconomyTestKit.PaidEvent));
            Assert.AreEqual(before, _k.Progress.EntryTickets);
        }

        [Test]
        public void WindowEndingMidRace_DoesNotCancelTheRunningEntry()
        {
            _k.Progress.TimedTickets = 1;
            _k.Timed.TryActivate();
            Assert.AreEqual(EntryResult.EnteredWithTimedAccess, _k.Gate.Enter(EconomyTestKit.PaidEvent));
            Assert.IsTrue(_k.Gate.TryTakePending(out _)); // the race scene took the entry and starts running

            _k.Kit.Clock.Advance(EconomyTestKit.TimedWindow + TimeSpan.FromMinutes(1));

            // Nothing re-checks a race in progress; the gate only looks at the window on the next entry.
            Assert.IsFalse(_k.Wallet.TimedAccessActive);
            Assert.IsFalse(_k.Gate.HasPendingEntry);
            Assert.AreEqual(EntryResult.EnteredWithTicket, _k.Gate.Enter(EconomyTestKit.PaidEvent));
        }

        [Test]
        public void ConfirmedEntry_StaysValid_WhenWindowEndsBeforeTheSceneTakesIt()
        {
            _k.Progress.TimedTickets = 1;
            _k.Timed.TryActivate();
            _k.Gate.Enter(EconomyTestKit.PaidEvent);
            int tickets = _k.Progress.EntryTickets;

            _k.Kit.Clock.Advance(EconomyTestKit.TimedWindow + TimeSpan.FromMinutes(1));

            Assert.AreEqual(EntryResult.EnteredWithTimedAccess, _k.Gate.Enter(EconomyTestKit.PaidEvent));
            Assert.AreEqual(tickets, _k.Progress.EntryTickets);
        }
    }
}
