using System;
using NUnit.Framework;
using SpeedStars.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Tickets
{
    public sealed class EntryTicketServiceTests
    {
        private TicketTestKit _kit;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private EntryTicketService Service(int starting)
        {
            _kit = new TicketTestKit(starting);
            return new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
        }

        [Test]
        public void Spend_FromTwo_LeavesOneAndStartsTimerWhenBelowCap()
        {
            var s = Service(2);
            Assert.IsTrue(s.TrySpend());
            Assert.AreEqual(1, s.Count);
            Assert.IsNotNull(s.NextTicketUtc);
        }

        [Test]
        public void Spend_WithNone_ReturnsFalseAndChangesNothing()
        {
            var s = Service(0);
            Assert.IsFalse(s.TrySpend());
            Assert.AreEqual(0, s.Count);
        }

        [Test]
        public void AtCap_TimeDoesNotAddTickets_AndTimerIsStopped()
        {
            var s = Service(TicketTestKit.Cap);
            _kit.Clock.Advance(TimeSpan.FromTicks(TicketTestKit.Interval.Ticks * 10));
            Assert.AreEqual(TicketTestKit.Cap, s.Count);
            Assert.IsTrue(s.IsFull);
            Assert.IsNull(s.NextTicketUtc);
            Assert.AreEqual(0, _kit.Progress.EntryRegenDueUtcTicks);
        }

        [Test]
        public void DroppingBelowCap_StartsFreshFullInterval()
        {
            var s = Service(TicketTestKit.Cap);
            _kit.Clock.Advance(TimeSpan.FromHours(3));
            Assert.IsTrue(s.TrySpend());
            Assert.AreEqual(_kit.Clock.UtcNow + TicketTestKit.Interval, s.NextTicketUtc);

            _kit.Clock.Advance(TicketTestKit.Interval - TimeSpan.FromSeconds(1));
            Assert.AreEqual(TicketTestKit.Cap - 1, s.Count);
            _kit.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.AreEqual(TicketTestKit.Cap, s.Count);
            Assert.IsNull(s.NextTicketUtc);
        }

        [Test]
        public void ManyIntervalsWhileClosed_FillUpToCapButNotBeyond()
        {
            var s = Service(1);
            Assert.AreEqual(1, s.Count); // starts the timer
            _kit.Clock.Advance(TimeSpan.FromTicks(TicketTestKit.Interval.Ticks * 3 + 5));
            Assert.AreEqual(4, s.Count);
            Assert.AreEqual(_kit.Clock.UtcNow.Ticks - 5 + TicketTestKit.Interval.Ticks, s.NextTicketUtc.Value.Ticks);

            _kit.Clock.Advance(TimeSpan.FromDays(30));
            Assert.AreEqual(TicketTestKit.Cap, s.Count);
        }

        [Test]
        public void RegenSurvivesRestart_NewServiceOnReloadedSave()
        {
            var s = Service(1);
            Assert.AreEqual(1, s.Count);
            var due = s.NextTicketUtc;

            var reloaded = new SpeedStars.Meta.Persistence.ProgressRepository(_kit.Store,
                () => new SpeedStars.Meta.Persistence.StartingBalances(0, 0, 99, 0));
            reloaded.Load();
            var s2 = new EntryTicketService(reloaded, _kit.Clock, _kit.Economy);
            _kit.Clock.Advance(TicketTestKit.Interval);

            Assert.AreEqual(2, s2.Count);
            Assert.IsNotNull(due);
        }

        [Test]
        public void ClockMovingBack_DoesNotProduceTickets()
        {
            var s = Service(1);
            Assert.AreEqual(1, s.Count);
            _kit.Clock.Advance(-TimeSpan.FromDays(2));
            Assert.AreEqual(1, s.Count);
        }

        [Test]
        public void Changed_FiresOnSpendAndOnRegen()
        {
            var s = Service(2);
            int fired = 0;
            s.Changed += () => fired++;

            s.TrySpend();
            Assert.Greater(fired, 0);

            int afterSpend = fired;
            _kit.Clock.Advance(TicketTestKit.Interval);
            Assert.AreEqual(2, s.Count);
            Assert.Greater(fired, afterSpend);
        }

        [Test]
        public void RegenOff_WhenIntervalIsZero()
        {
            var s = Service(1);
            SpeedStars.Tests.Meta.Config.ConfigFixture.SetFloat(_kit.Economy, "entryTicketRegenMinutes", 0f);
            _kit.Clock.Advance(TimeSpan.FromDays(5));
            Assert.AreEqual(1, s.Count);
            Assert.IsNull(s.NextTicketUtc);
        }
    }
}
