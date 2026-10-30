using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>Texts, rows and cups of the Results screen. Names and times are test-chosen; the assertions are relations.</summary>
    public sealed class ResultsModelTests
    {
        private const string Event = "Test event";
        private const double Time = 12.34d;

        private static ResultsInput Input(int rank = 1, string name = "Ada", int cash = 250, bool hasBest = true,
            double best = Time, bool record = false, IReadOnlyList<ResultEntry> others = null) =>
            new ResultsInput(Event, rank, Time, name, cash, hasBest, best, record, others);

        private static ResultRowData PlayerRow(bool record, bool dayBest) =>
            ResultsModel.Build(new ResultsInput(Event, 1, Time, "Ada", 0, true, Time, record, null, dayBest)).Rows[0];

        [Test]
        public void Badges_FirstRun_IsPersonalBestAndDayBest()
        {
            bool dayBest = ResultsModel.IsDayBest(true, (long)System.Math.Round(Time * 1000d), Time);
            ResultRowData row = PlayerRow(record: true, dayBest: dayBest);
            Assert.IsTrue(row.ShowPersonalBest);
            Assert.IsTrue(row.ShowDayBest);
        }

        [Test]
        public void Badges_SlowerRun_ShowsNone()
        {
            bool dayBest = ResultsModel.IsDayBest(true, (long)System.Math.Round((Time - 1d) * 1000d), Time);
            ResultRowData row = PlayerRow(record: false, dayBest: dayBest);
            Assert.IsFalse(row.ShowPersonalBest);
            Assert.IsFalse(row.ShowDayBest);
        }

        [Test]
        public void Badges_DayBestSlowerThanOldRecord_ShowsOnlyDayBest()
        {
            ResultRowData row = PlayerRow(record: false, dayBest: true);
            Assert.IsFalse(row.ShowPersonalBest);
            Assert.IsTrue(row.ShowDayBest);
        }

        [Test]
        public void IsDayBest_NoStoredValueOrInvalidTime_IsFalse()
        {
            Assert.IsFalse(ResultsModel.IsDayBest(false, 0L, Time));
            Assert.IsFalse(ResultsModel.IsDayBest(true, 0L, 0d));
            Assert.IsFalse(ResultsModel.IsDayBest(true, 0L, double.NaN));
        }

        [Test]
        public void Badges_OtherRunners_NeverShowBadges()
        {
            var others = new List<ResultEntry> { new ResultEntry(2, "Bo", Time + 1d) };
            ResultsData data = ResultsModel.Build(new ResultsInput(Event, 1, Time, "Ada", 0, true, Time, true, others, true));
            Assert.IsFalse(data.Rows[1].ShowPersonalBest);
            Assert.IsFalse(data.Rows[1].ShowDayBest);
        }

        [Test]
        public void TrophyFor_FirstThreePlaces_AreDistinct_AndNoneAfter()
        {
            Assert.AreEqual(ResultTrophy.Gold, ResultsModel.TrophyFor(1));
            Assert.AreEqual(ResultTrophy.Silver, ResultsModel.TrophyFor(2));
            Assert.AreEqual(ResultTrophy.Bronze, ResultsModel.TrophyFor(3));
            Assert.AreEqual(ResultTrophy.None, ResultsModel.TrophyFor(4));
            Assert.AreEqual(ResultTrophy.None, ResultsModel.TrophyFor(ResultsModel.MaxRows));
            Assert.AreEqual(ResultTrophy.None, ResultsModel.TrophyFor(0));
        }

        [Test]
        public void SingleRunner_ShowsOnlyThePlayerRow_WithRankNameAndTime()
        {
            ResultsData data = ResultsModel.Build(Input(rank: 2));

            Assert.AreEqual(1, data.Rows.Count);
            ResultRowData row = data.Rows[0];
            Assert.IsTrue(row.IsPlayer);
            Assert.AreEqual(2, row.Rank);
            Assert.AreEqual("Ada", row.Name);
            Assert.AreEqual(ResultsTexts.Time(Time), row.TimeText);
            Assert.AreEqual(ResultsModel.TrophyFor(2), row.Trophy);
        }

        [Test]
        public void EmptyPlayerName_FallsBackToYou()
        {
            Assert.AreEqual(ResultsTexts.DefaultPlayerName, ResultsModel.Build(Input(name: "")).Rows[0].Name);
            Assert.AreEqual(ResultsTexts.DefaultPlayerName, ResultsModel.Build(Input(name: "   ")).Rows[0].Name);
            Assert.AreEqual(ResultsTexts.DefaultPlayerName, ResultsModel.Build(Input(name: null)).Rows[0].Name);
        }

        [Test]
        public void BadRank_CountsAsFirst()
        {
            Assert.AreEqual(1, ResultsModel.Build(Input(rank: 0)).Rows[0].Rank);
            Assert.AreEqual(1, ResultsModel.Build(Input(rank: -3)).Rows[0].Rank);
        }

        [Test]
        public void OtherRunners_AreSortedByRank_AroundThePlayer()
        {
            var others = new List<ResultEntry>
            {
                new ResultEntry(3, "C", Time + 2d),
                new ResultEntry(1, "A", Time - 1d)
            };

            ResultsData data = ResultsModel.Build(Input(rank: 2, others: others));

            Assert.AreEqual(3, data.Rows.Count);
            Assert.AreEqual(1, data.Rows[0].Rank);
            Assert.AreEqual(2, data.Rows[1].Rank);
            Assert.AreEqual(3, data.Rows[2].Rank);
            Assert.IsTrue(data.Rows[1].IsPlayer);
            Assert.IsFalse(data.Rows[0].IsPlayer);
            Assert.AreEqual("A", data.Rows[0].Name);
        }

        [Test]
        public void TooManyRows_DropsTheWorstOthers_NeverThePlayer()
        {
            var others = new List<ResultEntry>();
            for (int i = 1; i <= ResultsModel.MaxRows + 3; i++) others.Add(new ResultEntry(i, "R" + i, Time + i));

            // The player sits last: still kept, the worst other is dropped instead.
            ResultsData data = ResultsModel.Build(Input(rank: ResultsModel.MaxRows + 4, others: others));

            Assert.AreEqual(ResultsModel.MaxRows, data.Rows.Count);
            Assert.IsTrue(data.Rows[data.Rows.Count - 1].IsPlayer);
            Assert.AreEqual(1, data.Rows[0].Rank);
        }

        [Test]
        public void NewRecord_ChangesTheCardTitle_AndKeepsTheBestTime()
        {
            ResultsData record = ResultsModel.Build(Input(record: true));
            ResultsData normal = ResultsModel.Build(Input(record: false));

            Assert.IsTrue(record.IsNewRecord);
            Assert.AreEqual(ResultsTexts.NewRecord, record.PersonalBestTitle);
            Assert.IsFalse(normal.IsNewRecord);
            Assert.AreEqual(ResultsTexts.PersonalBest, normal.PersonalBestTitle);
            Assert.AreEqual(ResultsTexts.Time(Time), record.PersonalBestValue);
            Assert.AreEqual(Event, record.PersonalBestEvent);
        }

        [Test]
        public void NoBestTime_ShowsADash_AndNeverARecord()
        {
            ResultsData data = ResultsModel.Build(Input(hasBest: false, record: true));

            Assert.IsFalse(data.IsNewRecord);
            Assert.AreEqual(ResultsTexts.PersonalBest, data.PersonalBestTitle);
            Assert.AreEqual(ResultsTexts.NoValue, data.PersonalBestValue);
        }

        [Test]
        public void WorldAndPercentile_HaveNoData_SoTheyShowADash()
        {
            ResultsData data = ResultsModel.Build(Input());

            Assert.AreEqual(ResultsTexts.NoValue, data.WorldPositionText);
            Assert.AreEqual(ResultsTexts.NoValue, data.PercentileText);
            Assert.AreEqual(ResultsTexts.NoDailyBest, data.DailyBestText);
        }

        [Test]
        public void Cash_IsPlainNumber_AndNeverNegative()
        {
            Assert.AreEqual("250", ResultsModel.Build(Input(cash: 250)).CashText);
            Assert.AreEqual("0", ResultsModel.Build(Input(cash: -5)).CashText);
        }

        [Test]
        public void Time_HasTheUnit_AndTruncatesLikeTheStopwatch()
        {
            Assert.AreEqual(RaceDisplayFormat.Time(9.589d) + "s", ResultsTexts.Time(9.589d));
            Assert.IsTrue(ResultsTexts.Time(9.589d).EndsWith("s"));
        }

        [Test]
        public void GemReward_NamesTheAmount()
        {
            StringAssert.Contains("3", ResultsTexts.GemReward(3));
            StringAssert.StartsWith("+", ResultsTexts.GemReward(3));
        }
    }
}
