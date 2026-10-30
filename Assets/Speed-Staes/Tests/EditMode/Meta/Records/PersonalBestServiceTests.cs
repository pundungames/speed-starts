using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Records;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Records
{
    /// <summary>Times are test-chosen; the tests assert how they relate (better, equal, worse), never fixed records.</summary>
    public sealed class PersonalBestServiceTests
    {
        private static readonly RaceEventId EventA = new RaceEventId("a");
        private static readonly RaceEventId EventB = new RaceEventId("b");

        private const double Base = 12.34d;
        private const double Better = Base - 1d;
        private const double Worse = Base + 1d;

        private TicketTestKit _kit;
        private PersonalBestService _service;

        [SetUp]
        public void SetUp()
        {
            _kit = new TicketTestKit(0);
            _service = new PersonalBestService(_kit.Repository, _kit.Clock, _kit.Fixture.Create<LeaderboardConfig>());
        }

        [TearDown]
        public void TearDown() => _kit.Dispose();

        [Test]
        public void FirstValidRace_IsANewRecord_AndStored()
        {
            Assert.IsFalse(_service.TryGetBest(EventA, out _));

            PersonalBestUpdate update = _service.Record(EventA, Base, "r1");

            Assert.IsTrue(update.IsNewRecord);
            Assert.IsTrue(update.HasBest);
            Assert.AreEqual(Base, update.BestSeconds, 1e-9);
            Assert.IsTrue(_service.TryGetBest(EventA, out double stored));
            Assert.AreEqual(Base, stored, 1e-9);
        }

        [Test]
        public void BetterRace_ReplacesTheBest_WorseOrEqualDoesNot()
        {
            _service.Record(EventA, Base, "r1");

            PersonalBestUpdate worse = _service.Record(EventA, Worse, "r2");
            Assert.IsFalse(worse.IsNewRecord);
            Assert.AreEqual(Base, worse.BestSeconds, 1e-9);

            PersonalBestUpdate equal = _service.Record(EventA, Base, "r3");
            Assert.IsFalse(equal.IsNewRecord);

            PersonalBestUpdate better = _service.Record(EventA, Better, "r4");
            Assert.IsTrue(better.IsNewRecord);
            Assert.AreEqual(Better, better.BestSeconds, 1e-9);
            _service.TryGetBest(EventA, out double stored);
            Assert.AreEqual(Better, stored, 1e-9);
        }

        [Test]
        public void SameCentisecond_IsNotARecord()
        {
            _service.Record(EventA, 9.587d, "r1");

            PersonalBestUpdate update = _service.Record(EventA, 9.581d, "r2"); // both show 9.58

            Assert.IsFalse(update.IsNewRecord);
            _service.TryGetBest(EventA, out double stored);
            Assert.AreEqual(9.587d, stored, 1e-9);
        }

        [Test]
        public void SameRaceId_WritesOnce_AndRepeatsTheFirstResult()
        {
            PersonalBestUpdate first = _service.Record(EventA, Base, "r1");
            int writes = _kit.Store.Writes;

            PersonalBestUpdate again = _service.Record(EventA, Better, "r1"); // a repeated finish of the same race

            Assert.AreEqual(first.IsNewRecord, again.IsNewRecord);
            Assert.AreEqual(first.BestSeconds, again.BestSeconds, 1e-9);
            Assert.AreEqual(writes, _kit.Store.Writes);
            _service.TryGetBest(EventA, out double stored);
            Assert.AreEqual(Base, stored, 1e-9);
        }

        [Test]
        public void Events_KeepSeparateBests()
        {
            _service.Record(EventA, Base, "r1");
            PersonalBestUpdate other = _service.Record(EventB, Worse, "r2");

            Assert.IsTrue(other.IsNewRecord); // the first race of event B, though slower than A's best
            _service.TryGetBest(EventA, out double a);
            _service.TryGetBest(EventB, out double b);
            Assert.AreEqual(Base, a, 1e-9);
            Assert.AreEqual(Worse, b, 1e-9);
        }

        [Test]
        public void InvalidTimes_AreIgnored()
        {
            PersonalBestUpdate none = _service.Record(EventA, 0d, "r1");
            Assert.IsFalse(none.IsNewRecord);
            Assert.IsFalse(none.HasBest);
            Assert.IsFalse(_service.TryGetBest(EventA, out _));

            _service.Record(EventA, Base, "r2");
            PersonalBestUpdate nan = _service.Record(EventA, double.NaN, "r3");
            Assert.IsFalse(nan.IsNewRecord);
            Assert.IsTrue(nan.HasBest);
            Assert.AreEqual(Base, nan.BestSeconds, 1e-9);
            Assert.IsFalse(_service.Record(EventA, -1d, "r4").IsNewRecord);
            Assert.IsFalse(_service.Record(EventA, double.PositiveInfinity, "r5").IsNewRecord);
        }

        [Test]
        public void Best_SurvivesAReload()
        {
            _service.Record(EventA, Base, "r1");

            var reloaded = new ProgressRepository(_kit.Store, () => new StartingBalances(0, 0, 0, 0));
            reloaded.Load();
            var service = new PersonalBestService(reloaded, _kit.Clock, _kit.Fixture.Create<LeaderboardConfig>());

            Assert.IsTrue(service.TryGetBest(EventA, out double stored));
            Assert.AreEqual(Base, stored, 1e-9);
        }
    }
}
