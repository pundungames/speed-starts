using System;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Race;
using SpeedStars.Meta.Tickets;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Race
{
    public sealed class RaceEntryGateTests
    {
        private static readonly RaceEventId Free = new RaceEventId("test-free");
        private static readonly RaceEventId Paid = new RaceEventId("test-paid");
        private static readonly RaceEventId Soon = new RaceEventId("test-soon");
        private static readonly RaceEventId Missing = new RaceEventId("test-missing");

        private TicketTestKit _kit;
        private RaceEventCatalog _catalog;
        private EntryTicketService _tickets;
        private RaceEntryGate _gate;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private RaceEventDefinition Event(string id, bool requiresTicket, bool playable)
        {
            var e = _kit.Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(e, "eventId", id);
            ConfigFixture.SetBool(e, "requiresTicket", requiresTicket);
            ConfigFixture.SetBool(e, "isPlayable", playable);
            return e;
        }

        private void Setup(int tickets)
        {
            _kit = new TicketTestKit(tickets);
            _catalog = _kit.Fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(_catalog, "events",
                Event("test-soon", true, false), Event("test-free", false, true), Event("test-paid", true, true));
            _tickets = new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
            _gate = new RaceEntryGate(_kit.Repository, _kit.Clock, _catalog, _tickets);
        }

        [Test]
        public void Enter_PaidEvent_SpendsOneAndSavesImmediately()
        {
            Setup(2);
            int writes = _kit.Store.Writes;

            Assert.AreEqual(EntryResult.EnteredWithTicket, _gate.Enter(Paid));

            Assert.AreEqual(1, _kit.Progress.EntryTickets);
            Assert.Greater(_kit.Store.Writes, writes);
            var reloaded = new SpeedStars.Meta.Persistence.ProgressRepository(_kit.Store,
                () => new SpeedStars.Meta.Persistence.StartingBalances(0, 0, 99, 0));
            Assert.AreEqual(1, reloaded.Load().EntryTickets);
        }

        [Test]
        public void Enter_DoubleTap_PaysOnce()
        {
            Setup(3);

            var first = _gate.Enter(Paid);
            var second = _gate.Enter(Paid);

            Assert.AreEqual(EntryResult.EnteredWithTicket, first);
            Assert.AreEqual(first, second);
            Assert.AreEqual(2, _kit.Progress.EntryTickets);
        }

        [Test]
        public void Enter_AfterPendingTaken_PaysAgain()
        {
            Setup(3);
            _gate.Enter(Paid);
            Assert.IsTrue(_gate.TryTakePending(out RaceEventId id));
            Assert.AreEqual(Paid, id);

            _gate.Enter(Paid);

            Assert.AreEqual(1, _kit.Progress.EntryTickets);
        }

        [Test]
        public void TryTakePending_IsOneShot()
        {
            Setup(1);
            Assert.IsFalse(_gate.HasPendingEntry);
            _gate.Enter(Paid);
            Assert.IsTrue(_gate.HasPendingEntry);

            Assert.IsTrue(_gate.TryTakePending(out _));
            Assert.IsFalse(_gate.TryTakePending(out _));
            Assert.IsFalse(_gate.HasPendingEntry);
        }

        [Test]
        public void Enter_FreeEvent_SpendsNothing()
        {
            Setup(2);
            int writes = _kit.Store.Writes;

            Assert.AreEqual(EntryResult.EnteredFree, _gate.Enter(Free));

            Assert.AreEqual(2, _kit.Progress.EntryTickets);
            Assert.AreEqual(writes, _kit.Store.Writes);
        }

        [Test]
        public void Enter_TimedAccessActive_SpendsNothing()
        {
            Setup(2);
            _kit.Progress.TimedAccessExpiresUtcTicks = (_kit.Clock.UtcNow + TimeSpan.FromMinutes(1)).Ticks;

            Assert.AreEqual(EntryResult.EnteredWithTimedAccess, _gate.Enter(Paid));
            Assert.AreEqual(2, _kit.Progress.EntryTickets);
        }

        [Test]
        public void Enter_TimedAccessExpired_FallsBackToTicket()
        {
            Setup(2);
            _kit.Progress.TimedAccessExpiresUtcTicks = (_kit.Clock.UtcNow - TimeSpan.FromMinutes(1)).Ticks;

            Assert.AreEqual(EntryResult.EnteredWithTicket, _gate.Enter(Paid));
            Assert.AreEqual(1, _kit.Progress.EntryTickets);
        }

        [Test]
        public void Enter_NoTicket_ChangesNothingAndHasNoPending()
        {
            Setup(0);
            Assert.AreEqual(EntryResult.NoTicket, _gate.Enter(Paid));

            Assert.AreEqual(0, _kit.Progress.EntryTickets);
            Assert.IsFalse(_gate.HasPendingEntry);
            Assert.IsFalse(_gate.TryTakePending(out _));
        }

        [Test]
        public void Enter_NotPlayableAndUnknown_ReportAndSpendNothing()
        {
            Setup(2);

            Assert.AreEqual(EntryResult.NotPlayable, _gate.Enter(Soon));
            Assert.AreEqual(EntryResult.UnknownEvent, _gate.Enter(Missing));
            Assert.AreEqual(EntryResult.UnknownEvent, _gate.Enter(default));

            Assert.AreEqual(2, _kit.Progress.EntryTickets);
            Assert.IsFalse(_gate.HasPendingEntry);
        }

        [Test]
        public void Enter_TicketBecomesAvailableByRegen()
        {
            Setup(0);
            Assert.AreEqual(EntryResult.NoTicket, _gate.Enter(Paid));

            _kit.Clock.Advance(TicketTestKit.Interval);

            Assert.AreEqual(EntryResult.EnteredWithTicket, _gate.Enter(Paid));
        }

        [Test]
        public void Selection_StartsOnFirstPlayableEvent_AndRaisesChangedOnlyOnChange()
        {
            Setup(0);
            var selection = new RaceSelection(_catalog);
            Assert.AreEqual(Free, selection.Selected);

            int fired = 0;
            selection.Changed += () => fired++;
            selection.Select(Free);
            Assert.AreEqual(0, fired);
            selection.Select(Paid);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(Paid, selection.Selected);
        }

        [Test]
        public void Selection_NoPlayableEvent_IsEmpty()
        {
            _kit = new TicketTestKit(0);
            var catalog = _kit.Fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(catalog, "events", Event("test-soon", true, false));

            Assert.IsTrue(new RaceSelection(catalog).Selected.IsEmpty);
        }

        [Test]
        public void Wallet_ReadsProgress_AndTimedAccessFollowsClock()
        {
            Setup(0);
            _kit.Progress.Cash = 11;
            _kit.Progress.PurpleGems = 22;
            _kit.Progress.TimedTickets = 3;
            var wallet = new WalletService(_kit.Repository, _kit.Clock);

            Assert.AreEqual(11, wallet.Cash);
            Assert.AreEqual(22, wallet.PurpleGems);
            Assert.AreEqual(3, wallet.TimedTickets);
            Assert.IsFalse(wallet.TimedAccessActive);
            Assert.IsNull(wallet.TimedAccessEndsUtc);

            DateTime ends = _kit.Clock.UtcNow + TimeSpan.FromMinutes(5);
            _kit.Progress.TimedAccessExpiresUtcTicks = ends.Ticks;
            Assert.IsTrue(wallet.TimedAccessActive);
            Assert.AreEqual(ends, wallet.TimedAccessEndsUtc);

            _kit.Clock.Advance(TimeSpan.FromMinutes(5));
            Assert.IsFalse(wallet.TimedAccessActive);

            int fired = 0;
            wallet.Changed += () => fired++;
            wallet.NotifyChanged();
            Assert.AreEqual(1, fired);
        }
    }
}
