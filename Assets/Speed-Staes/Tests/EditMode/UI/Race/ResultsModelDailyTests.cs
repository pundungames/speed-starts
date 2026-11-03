using NUnit.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>The Race of the Day cards of the Results screen: Daily Personal Best / Daily World Record (T-087).</summary>
    public sealed class ResultsModelDailyTests
    {
        private static ResultsInput Input(DailyResultsInput daily, bool newRecord = false) =>
            new ResultsInput("Race of the Day", 3, 12.34, "Ada", 40, true, 12.34, newRecord, null, false, daily);

        [Test]
        public void Ordinary_Race_KeepsTheWorldPositionCard()
        {
            ResultsData data = ResultsModel.Build(new ResultsInput("100m", 1, 10d, "Ada", 0, true, 10d, false, null));

            Assert.IsFalse(data.IsDaily);
            Assert.AreEqual(ResultsTexts.WorldPosition, data.WorldTitle);
            Assert.AreEqual(ResultsTexts.PersonalBest, data.PersonalBestTitle);
        }

        [Test]
        public void Daily_ShowsTheDaysBestAndTheDaysRecord()
        {
            var daily = new DailyResultsInput(true, 12340L, true, "Deniz", 9000L);

            ResultsData data = ResultsModel.Build(Input(daily));

            Assert.IsTrue(data.IsDaily);
            Assert.AreEqual(ResultsTexts.DailyPersonalBest, data.PersonalBestTitle);
            Assert.AreEqual(ResultsTexts.Time(12.34), data.PersonalBestValue);
            Assert.AreEqual(ResultsTexts.DailyWorldRecord, data.WorldTitle);
            Assert.AreEqual(ResultsModel.RecordLine("Deniz", 9000L), data.WorldPositionText);
            Assert.AreEqual(data.WorldPositionText, data.DailyBestText, "the Best of the day panel tells the same record");
        }

        [Test]
        public void Daily_WithoutResults_ReadsNone()
        {
            ResultsData data = ResultsModel.Build(Input(new DailyResultsInput(false, 0L, false, string.Empty, 0L)));

            Assert.AreEqual(ResultsTexts.NoneYet, data.PersonalBestValue);
            Assert.AreEqual(ResultsTexts.NoneYet, data.WorldPositionText);
        }

        [Test]
        public void Daily_NewRecord_KeepsTheNewRecordTitle()
        {
            ResultsData data = ResultsModel.Build(Input(new DailyResultsInput(true, 12340L, false, string.Empty, 0L), newRecord: true));

            Assert.IsTrue(data.IsNewRecord);
            Assert.AreEqual(ResultsTexts.NewRecord, data.PersonalBestTitle);
        }

        [Test]
        public void PickDailyRecord_TheFasterOfFieldAndOwn()
        {
            Assert.IsTrue(ResultsModel.PickDailyRecord(true, "Deniz", 9000L, true, "Ada", 8000L, out string name, out long ms));
            Assert.AreEqual("Ada", name);
            Assert.AreEqual(8000L, ms);

            Assert.IsTrue(ResultsModel.PickDailyRecord(true, "Deniz", 9000L, true, "Ada", 9500L, out name, out ms));
            Assert.AreEqual("Deniz", name);
            Assert.AreEqual(9000L, ms);
        }

        [Test]
        public void PickDailyRecord_EqualTimeGoesToTheField_AndSingleSourcesWork()
        {
            ResultsModel.PickDailyRecord(true, "Deniz", 9000L, true, "Ada", 9000L, out string name, out _);
            Assert.AreEqual("Deniz", name, "the player only takes it when strictly faster");

            Assert.IsTrue(ResultsModel.PickDailyRecord(false, string.Empty, 0L, true, "Ada", 9500L, out name, out long ms));
            Assert.AreEqual("Ada", name);
            Assert.AreEqual(9500L, ms);

            Assert.IsFalse(ResultsModel.PickDailyRecord(false, string.Empty, 0L, false, string.Empty, 0L, out _, out _));
        }
    }
}
