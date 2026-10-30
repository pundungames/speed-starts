using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Bots;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>Order rules of the live standings: finished by time first, then on-track by distance; Changed only when the order or a finish changes.</summary>
    public sealed class RaceStandingsModelTests
    {
        private static RaceEntry Run(string name, float distance, bool player = false) => new RaceEntry(name, "TR", player, distance, false, 0.0);
        private static RaceEntry Done(string name, double time, bool player = false) => new RaceEntry(name, "TR", player, 100f, true, time);

        private RaceStandingsModel _model;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _model = new RaceStandingsModel();
            _changed = 0;
            _model.Changed += () => _changed++;
        }

        [Test]
        public void RunningRunners_AreOrderedByDistance_PlayerRankFollows()
        {
            _model.Update(new[] { Run("P", 10f, true), Run("A", 30f), Run("B", 20f) });
            Assert.AreEqual("A", _model.Entries[0].Name);
            Assert.AreEqual("B", _model.Entries[1].Name);
            Assert.AreEqual(3, _model.PlayerRank);
            Assert.AreEqual(3, _model.RunnerCount);
        }

        [Test]
        public void Finished_StandAheadOfRunners_AndSortByTime()
        {
            _model.Update(new[] { Run("P", 95f, true), Done("A", 14.0), Done("B", 12.5) });
            Assert.AreEqual("B", _model.Entries[0].Name);
            Assert.AreEqual("A", _model.Entries[1].Name);
            Assert.AreEqual("P", _model.Entries[2].Name);
            Assert.AreEqual(3, _model.PlayerRank);
        }

        [Test]
        public void Changed_FiresOnlyOnOrderOrFinishChange()
        {
            _model.Update(new[] { Run("P", 10f, true), Run("A", 20f) });
            int afterFirst = _changed;
            Assert.Greater(afterFirst, 0);

            _model.Update(new[] { Run("P", 12f, true), Run("A", 25f) }); // distances move, order same
            Assert.AreEqual(afterFirst, _changed);

            _model.Update(new[] { Run("P", 30f, true), Run("A", 26f) }); // player overtakes
            Assert.AreEqual(afterFirst + 1, _changed);
            Assert.AreEqual(1, _model.PlayerRank);

            _model.Update(new[] { Done("P", 13.0, true), Run("A", 60f) }); // player finishes, same order
            Assert.AreEqual(afterFirst + 2, _changed);
        }

        [Test]
        public void Reset_ReturnsToStart_AndNextUpdateRaises()
        {
            _model.Update(new[] { Run("P", 10f, true), Run("A", 20f) });
            _model.Reset();
            Assert.AreEqual(0, _model.RunnerCount);
            Assert.AreEqual(0, _model.PlayerRank);
            int before = _changed;
            _model.Update(new[] { Run("P", 0f, true), Run("A", 0f) });
            Assert.Greater(_changed, before);
        }

        [Test]
        public void Ties_KeepInputOrder()
        {
            _model.Update(new[] { Run("A", 0f), Run("P", 0f, true), Run("B", 0f) });
            Assert.AreEqual("A", _model.Entries[0].Name);
            Assert.AreEqual("P", _model.Entries[1].Name);
            Assert.AreEqual(2, _model.PlayerRank);
        }
    }
}
