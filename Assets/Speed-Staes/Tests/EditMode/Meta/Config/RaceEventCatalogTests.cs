using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;

namespace SpeedStars.Tests.Meta.Config
{
    public sealed class RaceEventCatalogTests
    {
        private ConfigFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ConfigFixture();

        [TearDown]
        public void TearDown() => fixture.DestroyAll();

        private RaceEventDefinition Event(string id)
        {
            var definition = fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(definition, "eventId", id);
            return definition;
        }

        private RaceEventCatalog Catalog(params RaceEventDefinition[] events)
        {
            var catalog = fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(catalog, "events", events);
            return catalog;
        }

        [Test]
        public void Definition_IdConvertsToCoreRaceEventId()
        {
            var definition = Event("event-a");

            Assert.AreEqual(new RaceEventId("event-a"), definition.Id);
        }

        [Test]
        public void Definition_ExposesItsSerializedValues()
        {
            var definition = Event("event-a");
            ConfigFixture.SetString(definition, "displayName", "Event A");
            ConfigFixture.SetFloat(definition, "distanceMeters", 42.5f);
            ConfigFixture.SetBool(definition, "requiresTicket", true);
            ConfigFixture.SetBool(definition, "awardsPurpleGems", true);
            ConfigFixture.SetBool(definition, "isPlayable", true);

            Assert.AreEqual("Event A", definition.DisplayName);
            Assert.AreEqual(42.5f, definition.DistanceMeters, 1e-4f);
            Assert.IsTrue(definition.RequiresTicket);
            Assert.IsTrue(definition.AwardsPurpleGems);
            Assert.IsTrue(definition.IsPlayable);
        }

        [Test]
        public void NewDefinition_IsOffEverywhere()
        {
            var definition = fixture.Create<RaceEventDefinition>();

            Assert.IsTrue(definition.Id.IsEmpty);
            Assert.IsFalse(definition.RequiresTicket);
            Assert.IsFalse(definition.AwardsPurpleGems);
            Assert.IsFalse(definition.IsPlayable);
        }

        [Test]
        public void TryGet_FindsTheEventWithThatId()
        {
            var a = Event("event-a");
            var b = Event("event-b");
            var catalog = Catalog(a, b);

            Assert.IsTrue(catalog.TryGet(new RaceEventId("event-b"), out var found));
            Assert.AreSame(b, found);
        }

        [Test]
        public void TryGet_UnknownOrEmptyId_ReturnsFalse()
        {
            var catalog = Catalog(Event("event-a"), Event(""));

            Assert.IsFalse(catalog.TryGet(new RaceEventId("missing"), out var unknown));
            Assert.IsNull(unknown);
            Assert.IsFalse(catalog.TryGet(new RaceEventId(""), out var empty));
            Assert.IsNull(empty);
        }

        [Test]
        public void TryGet_IsOrdinal()
        {
            var catalog = Catalog(Event("event-a"));

            Assert.IsFalse(catalog.TryGet(new RaceEventId("EVENT-A"), out _));
        }

        [Test]
        public void GetEvents_KeepsCatalogOrderAndSkipsEmptySlots()
        {
            var a = Event("event-a");
            var b = Event("event-b");
            var c = Event("event-c");
            var catalog = Catalog(c, null, a, b);

            CollectionAssert.AreEqual(new[] { c, a, b }, catalog.GetEvents());
        }

        [Test]
        public void DuplicateId_IsReportedAndLookupReturnsTheFirst()
        {
            var first = Event("event-a");
            var second = Event("event-a");
            var catalog = Catalog(first, Event("event-b"), second);

            CollectionAssert.AreEqual(new[] { "event-a" }, catalog.FindDuplicateIds());
            Assert.IsTrue(catalog.TryGet(new RaceEventId("event-a"), out var found));
            Assert.AreSame(first, found);
        }

        [Test]
        public void UniqueIds_ReportNoDuplicates()
        {
            var catalog = Catalog(Event("event-a"), Event("event-b"), Event(""), Event(""));

            CollectionAssert.IsEmpty(catalog.FindDuplicateIds());
        }
    }
}
