using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Race;
using SpeedStars.Meta.Tickets;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;
using UnityEditor;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// T-082 scenario 3: which events cost a ticket and which run alone, read from the shipped event catalog (rules, not event names).
    /// The real entry gate runs over the catalog; the ticket counts are test inputs, the expectations are relations to the flags.
    /// </summary>
    public sealed class EventEntryRulesRegressionTests
    {
        private const string CatalogPath = "Assets/Speed-Staes/Data/Config/RaceEventCatalog.asset";
        private const int TicketsInStock = 3;

        private TicketTestKit _kit;
        private RaceEventCatalog _catalog;
        private RaceEntryGate _gate;

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        private void Setup(int tickets)
        {
            _catalog = AssetDatabase.LoadAssetAtPath<RaceEventCatalog>(CatalogPath);
            Assert.IsNotNull(_catalog, "the shipped event catalog must be at " + CatalogPath);
            _kit = new TicketTestKit(tickets);
            var tickService = new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
            _gate = new RaceEntryGate(_kit.Repository, _kit.Clock, _catalog, tickService);
        }

        private List<RaceEventDefinition> Where(System.Func<RaceEventDefinition, bool> filter)
        {
            var list = new List<RaceEventDefinition>();
            foreach (RaceEventDefinition e in _catalog.GetEvents())
                if (filter(e)) list.Add(e);
            return list;
        }

        [Test]
        public void PlayableEventsWithoutATicketRule_AreEnteredFree_EvenWithNoTickets_AndSpendNothing()
        {
            Setup(0);
            var free = Where(e => e.IsPlayable && !e.RequiresTicket);
            Assert.IsNotEmpty(free, "the catalog is expected to hold at least one free playable event (Practice)");

            foreach (RaceEventDefinition e in free)
            {
                Assert.AreEqual(EntryResult.EnteredFree, _gate.Enter(e.Id), e.Id.Value);
                _gate.TryTakePending(out _);
            }

            Assert.AreEqual(0, _kit.Progress.EntryTickets);
        }

        [Test]
        public void FreeEvents_DoNotSpendAStockedTicket_AndDoNotSave()
        {
            Setup(TicketsInStock);
            int writes = _kit.Store.Writes;

            foreach (RaceEventDefinition e in Where(x => x.IsPlayable && !x.RequiresTicket))
            {
                _gate.Enter(e.Id);
                _gate.TryTakePending(out _);
            }

            Assert.AreEqual(TicketsInStock, _kit.Progress.EntryTickets);
            Assert.AreEqual(writes, _kit.Store.Writes);
        }

        [Test]
        public void PlayableEventsWithATicketRule_NeedATicket_AndSpendExactlyOne()
        {
            Setup(TicketsInStock);
            var paid = Where(e => e.IsPlayable && e.RequiresTicket);
            Assert.IsNotEmpty(paid, "the catalog is expected to hold at least one ticketed playable event");

            EventDefinitionAssert.SpendsOne(_gate, _kit, paid[0]);

            _kit.Dispose();
            Setup(0);
            Assert.AreEqual(EntryResult.NoTicket, _gate.Enter(paid[0].Id));
            Assert.AreEqual(0, _kit.Progress.EntryTickets);
            Assert.IsFalse(_gate.HasPendingEntry);
        }

        [Test]
        public void EventsThatAreNotPlayable_AreRefused_AndSpendNothing()
        {
            Setup(TicketsInStock);
            var locked = Where(e => !e.IsPlayable);
            Assert.IsNotEmpty(locked, "the catalog is expected to still hold locked (coming soon) events");

            foreach (RaceEventDefinition e in locked)
                Assert.AreEqual(EntryResult.NotPlayable, _gate.Enter(e.Id), e.Id.Value);

            Assert.AreEqual(TicketsInStock, _kit.Progress.EntryTickets);
            Assert.IsFalse(_gate.HasPendingEntry);
        }

        [Test]
        public void EventsWithoutOpponents_AreSoloRuns_FreePlayableOverARealDistance_AndPayNoGems()
        {
            Setup(0);
            var solo = Where(e => !e.HasOpponents);
            Assert.IsNotEmpty(solo, "the catalog is expected to hold a solo event (Practice)");

            foreach (RaceEventDefinition e in solo)
            {
                Assert.IsFalse(e.RequiresTicket, e.Id.Value + ": a run with no one to race costs no ticket");
                Assert.IsTrue(e.IsPlayable, e.Id.Value + ": a solo event is opened");
                Assert.Greater(e.DistanceMeters, 0f, e.Id.Value + ": a solo run needs a distance");
                Assert.IsFalse(e.AwardsPurpleGems, e.Id.Value + ": nothing to win alone");
                Assert.AreEqual(EntryResult.EnteredFree, _gate.Enter(e.Id), e.Id.Value);
                _gate.TryTakePending(out _);
            }
        }

        [Test]
        public void TicketedEvents_HaveOpponents()
        {
            Setup(0);
            foreach (RaceEventDefinition e in Where(x => x.RequiresTicket))
                Assert.IsTrue(e.HasOpponents, e.Id.Value + ": a paid race is a race against bots");
        }

        [Test]
        public void EveryPlayableEvent_HasADistance()
        {
            Setup(0);
            foreach (RaceEventDefinition e in Where(x => x.IsPlayable))
                Assert.Greater(e.DistanceMeters, 0f, e.Id.Value);
        }

        [Test]
        public void ANewEventDefinition_HasOpponents_UnlessSwitchedOff()
        {
            var fixture = new ConfigFixture();
            try
            {
                var fresh = fixture.Create<RaceEventDefinition>();
                Assert.IsTrue(fresh.HasOpponents, "an event asset saved before the flag existed must keep its bots");
                ConfigFixture.SetBool(fresh, "hasOpponents", false);
                Assert.IsFalse(fresh.HasOpponents);
            }
            finally
            {
                fixture.DestroyAll();
            }
        }

        private static class EventDefinitionAssert
        {
            public static void SpendsOne(RaceEntryGate gate, TicketTestKit kit, RaceEventDefinition e)
            {
                int before = kit.Progress.EntryTickets;
                EntryResult result = gate.Enter(e.Id);
                Assert.AreEqual(EntryResult.EnteredWithTicket, result, e.Id.Value);
                Assert.AreEqual(before - 1, kit.Progress.EntryTickets);
                gate.TryTakePending(out _);
            }
        }
    }
}
