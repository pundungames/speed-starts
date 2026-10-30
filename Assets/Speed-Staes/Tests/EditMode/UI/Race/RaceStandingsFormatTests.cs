using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>Place suffix and table rows of the live race order. Names and times are test-chosen; assertions are relations.</summary>
    public sealed class RaceStandingsFormatTests
    {
        private static RaceEntry Runner(string name, bool finished, double time, string country = "TR") =>
            new RaceEntry(name, country, false, finished ? 100f : 50f, finished, time);

        private static RaceEntry Player(bool finished, double time) =>
            new RaceEntry("Me", "US", true, finished ? 100f : 50f, finished, time);

        private static ResultRowData PlayerRow(bool pb = false, bool db = false) =>
            new ResultRowData(1, "Ada", ResultsTexts.Time(13.5d), true, ResultTrophy.None, pb, db);

        [TestCase(1, "1st")]
        [TestCase(2, "2nd")]
        [TestCase(3, "3rd")]
        [TestCase(4, "4th")]
        [TestCase(10, "10th")]
        [TestCase(11, "11th")]
        [TestCase(12, "12th")]
        [TestCase(13, "13th")]
        [TestCase(21, "21st")]
        [TestCase(22, "22nd")]
        [TestCase(23, "23rd")]
        [TestCase(101, "101st")]
        [TestCase(111, "111th")]
        public void Ordinal_UsesEnglishSuffix(int place, string expected) =>
            Assert.AreEqual(expected, RaceStandingsFormat.Ordinal(place));

        [Test]
        public void Ordinal_BelowOne_ReadsAsFirst() =>
            Assert.AreEqual(RaceStandingsFormat.Ordinal(1), RaceStandingsFormat.Ordinal(0));

        [Test]
        public void PlaceText_AloneOnTrack_IsEmpty()
        {
            Assert.AreEqual(string.Empty, RaceStandingsFormat.PlaceText(1, 1));
            Assert.AreEqual(string.Empty, RaceStandingsFormat.PlaceText(1, 0));
        }

        [Test]
        public void PlaceText_WithOpponents_IsOrdinal()
        {
            Assert.AreEqual(RaceStandingsFormat.Ordinal(3), RaceStandingsFormat.PlaceText(3, 7));
            Assert.AreEqual(string.Empty, RaceStandingsFormat.PlaceText(0, 7));
        }

        [Test]
        public void TimeText_FinishedShowsTime_RunningShowsDash()
        {
            Assert.AreEqual(ResultsTexts.Time(27.69d), RaceStandingsFormat.TimeText(Runner("A", true, 27.69d)));
            Assert.AreEqual(ResultsTexts.NotFinished, RaceStandingsFormat.TimeText(Runner("A", false, 0d)));
        }

        [Test]
        public void Rows_KeepStandingsOrder_RankIsPosition()
        {
            var entries = new List<RaceEntry> { Runner("A", true, 12d), Player(true, 13.5d), Runner("B", false, 0d) };
            List<ResultRowData> rows = RaceStandingsFormat.Rows(entries, PlayerRow());

            Assert.AreEqual(3, rows.Count);
            for (int i = 0; i < rows.Count; i++) Assert.AreEqual(i + 1, rows[i].Rank);
            Assert.AreEqual("A", rows[0].Name);
            Assert.IsTrue(rows[1].IsPlayer);
            Assert.AreEqual("B", rows[2].Name);
        }

        [Test]
        public void Rows_PlayerRow_KeepsModelNameTimeAndBadges()
        {
            var entries = new List<RaceEntry> { Player(true, 13.5d), Runner("A", true, 14d) };
            ResultRowData row = RaceStandingsFormat.Rows(entries, PlayerRow(pb: true, db: true))[0];

            Assert.AreEqual("Ada", row.Name);
            Assert.AreEqual(ResultsTexts.Time(13.5d), row.TimeText);
            Assert.IsTrue(row.ShowPersonalBest);
            Assert.IsTrue(row.ShowDayBest);
        }

        [Test]
        public void Rows_RunnerStillRunning_HasDashAndNoCup()
        {
            var entries = new List<RaceEntry> { Runner("A", false, 0d) };
            ResultRowData row = RaceStandingsFormat.Rows(entries, PlayerRow())[0];

            Assert.AreEqual(ResultsTexts.NotFinished, row.TimeText);
            Assert.AreEqual(ResultTrophy.None, row.Trophy);
        }

        [Test]
        public void Rows_CupsOnlyFirstThreeFinished()
        {
            var entries = new List<RaceEntry>();
            for (int i = 0; i < 5; i++) entries.Add(Runner("R" + i, true, 10d + i));
            List<ResultRowData> rows = RaceStandingsFormat.Rows(entries, PlayerRow());

            Assert.AreEqual(ResultTrophy.Gold, rows[0].Trophy);
            Assert.AreEqual(ResultTrophy.Silver, rows[1].Trophy);
            Assert.AreEqual(ResultTrophy.Bronze, rows[2].Trophy);
            Assert.AreEqual(ResultTrophy.None, rows[3].Trophy);
        }

        [Test]
        public void Rows_AllRunnersListed_NoCap()
        {
            var entries = new List<RaceEntry>();
            for (int i = 0; i < ResultsModel.MaxRows + 4; i++) entries.Add(Runner("R" + i, true, 10d + i));
            Assert.AreEqual(entries.Count, RaceStandingsFormat.Rows(entries, PlayerRow()).Count);
        }

        [Test]
        public void Rows_CarryCountryCode()
        {
            var entries = new List<RaceEntry> { Runner("A", true, 12d, "DE"), Player(true, 13.5d) };
            List<ResultRowData> rows = RaceStandingsFormat.Rows(entries, PlayerRow());
            Assert.AreEqual("DE", rows[0].CountryCode);
            Assert.AreEqual("US", rows[1].CountryCode);
        }

        [Test]
        public void Rows_NullEntries_IsEmpty() =>
            Assert.AreEqual(0, RaceStandingsFormat.Rows(null, PlayerRow()).Count);
    }
}
