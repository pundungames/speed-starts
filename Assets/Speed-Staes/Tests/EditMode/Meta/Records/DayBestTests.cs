using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Records;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Records
{
    /// <summary>Per-(event, UTC day) bests behind the leaderboard: one entry per day, capped, saved, old saves still open. Times and caps are test-chosen.</summary>
    public sealed class DayBestTests
    {
        private static readonly RaceEventId EventA = new RaceEventId("a");
        private static readonly RaceEventId EventB = new RaceEventId("b");
        private static readonly DateTime Noon = new DateTime(2030, 1, 16, 12, 0, 0, DateTimeKind.Utc);

        private const double Base = 12.34d;

        private TicketTestKit _kit;
        private LeaderboardConfig _config;
        private PersonalBestService _service;
        private int _raceCounter;

        [SetUp]
        public void SetUp()
        {
            _kit = new TicketTestKit(0);
            _kit.Clock.UtcNow = Noon;
            _config = _kit.Fixture.Create<LeaderboardConfig>();
            _service = new PersonalBestService(_kit.Repository, _kit.Clock, _config);
            _raceCounter = 0;
        }

        [TearDown]
        public void TearDown() => _kit.Dispose();

        private PersonalBestUpdate Race(RaceEventId id, double seconds, int dayOffset = 0)
        {
            _kit.Clock.UtcNow = Noon.AddDays(dayOffset);
            return _service.Record(id, seconds, "race" + _raceCounter++);
        }

        private List<DayBestRecord> Bests(RaceEventId id)
        {
            var list = new List<DayBestRecord>();
            _service.CopyDayBests(id, list);
            return list;
        }

        [Test]
        public void EveryFinish_KeepsOneEntryPerDay_AndOnlyTheFasterReplaces()
        {
            Race(EventA, Base);
            Race(EventA, Base + 1d); // slower, same day: not stored
            Assert.AreEqual(1, Bests(EventA).Count);
            long first = Bests(EventA)[0].ElapsedMilliseconds;

            Race(EventA, Base - 1d); // faster, same day: replaces
            Assert.AreEqual(1, Bests(EventA).Count);
            Assert.Less(Bests(EventA)[0].ElapsedMilliseconds, first);

            Race(EventA, Base, 1); // another day: a second entry
            Assert.AreEqual(2, Bests(EventA).Count);
        }

        [Test]
        public void DayBest_IsStoredEvenWhenItIsNotAPersonalBest()
        {
            Race(EventA, Base - 5d, -3); // a fast race earlier
            PersonalBestUpdate update = Race(EventA, Base); // slower today: no new PB, still today's best

            Assert.IsFalse(update.IsNewRecord);
            Assert.AreEqual(2, Bests(EventA).Count);
        }

        [Test]
        public void Events_AreSeparated()
        {
            Race(EventA, Base);
            Assert.AreEqual(0, Bests(EventB).Count);
        }

        [Test]
        public void InvalidTimes_StoreNothing()
        {
            Race(EventA, 0d);
            Race(EventA, double.NaN);
            Race(EventA, -2d);
            Assert.AreEqual(0, Bests(EventA).Count);
        }

        [Test]
        public void RepeatedRaceId_DoesNotAddAnEntry()
        {
            _service.Record(EventA, Base, "same");
            _service.Record(EventA, Base - 1d, "same");
            Assert.AreEqual(1, Bests(EventA).Count);
        }

        [Test]
        public void ChangedEvent_FiresOnNewDayBest_NotOnASlowerRace()
        {
            int fired = 0;
            _service.DayBestChanged += () => fired++;

            Race(EventA, Base);
            Assert.AreEqual(1, fired);
            Race(EventA, Base + 1d);
            Assert.AreEqual(1, fired);
            Race(EventA, Base - 1d);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void Cap_DropsTheOldest_ButKeepsTheEventsFastestEntry()
        {
            const int cap = 3;
            ConfigFixture.SetInt(_config, "maxDayBestEntries", cap);

            Race(EventA, Base - 5d, -10); // the oldest day AND the all-time best
            for (int day = -9; day <= -3; day++) Race(EventA, Base + 1d + (day + 10) * 0.1d, day);

            List<DayBestRecord> kept = Bests(EventA);
            Assert.LessOrEqual(kept.Count, cap);
            long fastest = long.MaxValue;
            foreach (DayBestRecord r in kept) fastest = Math.Min(fastest, r.ElapsedMilliseconds);
            Assert.AreEqual((long)Math.Round((Base - 5d) * 1000d), fastest);

            bool newestKept = false;
            foreach (DayBestRecord r in kept) newestKept |= r.DayUtcTicks == Noon.AddDays(-3).Date.Ticks;
            Assert.IsTrue(newestKept, "the newest day must survive the cap");
        }

        [Test]
        public void DayBests_SurviveAReload()
        {
            Race(EventA, Base);

            var reloaded = new ProgressRepository(_kit.Store, () => new StartingBalances(0, 0, 0, 0));
            reloaded.Load();
            var service = new PersonalBestService(reloaded, _kit.Clock, _config);

            var list = new List<DayBestRecord>();
            service.CopyDayBests(EventA, list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(Bests(EventA)[0].ElapsedMilliseconds, list[0].ElapsedMilliseconds);
            Assert.AreEqual(Bests(EventA)[0].AchievedUtcTicks, list[0].AchievedUtcTicks);
        }

        [Test]
        public void OlderSave_WithoutTheList_OpensWithAnEmptyList()
        {
            // A schema-2 save written before the leaderboard: no "dayBests" key at all.
            var store = new MemoryStore();
            store.Write(ProgressRepository.SaveKey, "{\"schemaVersion\":2,\"cash\":7}");
            var repository = new ProgressRepository(store, () => new StartingBalances(0, 0, 0, 0));

            PlayerProgress progress = repository.Load();

            Assert.AreEqual(ProgressLoadStatus.Loaded, repository.Status);
            Assert.AreEqual(7, progress.Cash);
            Assert.IsNotNull(progress.DayBests);
            Assert.AreEqual(0, progress.DayBests.Count);

            var service = new PersonalBestService(repository, _kit.Clock, _config);
            service.Record(EventA, Base, "r1");
            var list = new List<DayBestRecord>();
            service.CopyDayBests(EventA, list);
            Assert.AreEqual(1, list.Count);
        }
    }
}
