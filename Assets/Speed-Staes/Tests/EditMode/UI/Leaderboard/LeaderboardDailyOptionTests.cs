using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.UI.Leaderboard;

namespace SpeedStars.Tests.UI.Leaderboard
{
    /// <summary>The daily race as a category of the High Scores event filter (T-093).</summary>
    public sealed class LeaderboardDailyOptionTests
    {
        private static readonly RaceEventId Daily = new RaceEventId("daily_x");
        private static readonly RaceEventId Sprint = new RaceEventId("sprint_x");

        private static List<LeaderboardEventGroup> Groups() => new List<LeaderboardEventGroup>
        {
            new LeaderboardEventGroup("SPRINTS", new[] { new LeaderboardEventOption(Sprint, "100m") })
        };

        [Test]
        public void SetDaily_AddsOneCategory_AndFindsIt()
        {
            List<LeaderboardEventGroup> groups = Groups();
            LeaderboardEventGroups.SetDaily(groups, Daily, LeaderboardTexts.DailyEvent(150f));

            Assert.AreEqual(2, groups.Count);
            Assert.IsTrue(LeaderboardEventGroups.Find(groups, Daily, out int index, out LeaderboardEventOption option));
            Assert.AreEqual(1, index);
            StringAssert.Contains("150", option.Label);
            Assert.AreEqual(Daily, LeaderboardEventGroups.Resolve(groups, Daily), "opening with the daily race selected keeps it");
        }

        [Test]
        public void SetDaily_AgainRefreshesTheLabelInPlace_NoSecondCategory()
        {
            List<LeaderboardEventGroup> groups = Groups();
            LeaderboardEventGroups.SetDaily(groups, Daily, LeaderboardTexts.DailyEvent(150f));
            var picker = new LeaderboardEventPicker(groups);
            LeaderboardEventGroups.SetDaily(groups, Daily, LeaderboardTexts.DailyEvent(200f));

            Assert.AreEqual(2, groups.Count);
            StringAssert.Contains("200", LeaderboardEventGroups.FilterLabel(groups, Daily));
            picker.Begin(Daily);
            Assert.AreEqual(1, picker.Category);
            Assert.AreEqual(Daily, picker.Chosen, "a picker holding the list sees the refreshed category");
        }

        [Test]
        public void DailyEvent_NamesTheDistance_OnlyWhenThereIsOne()
        {
            StringAssert.Contains("150", LeaderboardTexts.DailyEvent(150f));
            Assert.AreNotEqual(LeaderboardTexts.DailyEvent(150f), LeaderboardTexts.DailyEvent(200f));
            Assert.IsFalse(LeaderboardTexts.DailyEvent(0f).Contains("0m"));
        }
    }
}
