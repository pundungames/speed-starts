using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.DailyRace;

namespace SpeedStars.Tests.Meta.RaceOfTheDay
{
    /// <summary>The pure day rule of Race of the Day: same day same pick, every day a different one, every entry in turn (T-087).</summary>
    public sealed class RaceOfTheDayRuleTests
    {
        private const int Days = 1500;
        private static readonly long Start = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc).Ticks / TimeSpan.TicksPerDay;

        [Test]
        public void DayKey_IsTheUtcDay_NotTheTime()
        {
            var morning = new DateTime(2026, 11, 3, 0, 0, 1, DateTimeKind.Utc);
            var night = new DateTime(2026, 11, 3, 23, 59, 59, DateTimeKind.Utc);
            var next = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(RaceOfTheDayRule.DayKey(morning), RaceOfTheDayRule.DayKey(night));
            Assert.AreEqual(RaceOfTheDayRule.DayKey(night) + 1, RaceOfTheDayRule.DayKey(next));
        }

        [Test]
        public void IndexFor_EmptyListHasNoIndex_SingleEntryIsAlwaysZero()
        {
            Assert.AreEqual(-1, RaceOfTheDayRule.IndexFor(Start, 0, RaceOfTheDayRule.DistanceSalt));
            for (int d = 0; d < 50; d++) Assert.AreEqual(0, RaceOfTheDayRule.IndexFor(Start + d, 1, RaceOfTheDayRule.DistanceSalt));
        }

        [Test]
        public void IndexFor_SameDaySameAnswer()
        {
            for (int count = 1; count <= 9; count++)
                for (int d = 0; d < 40; d++)
                    Assert.AreEqual(RaceOfTheDayRule.IndexFor(Start + d, count, RaceOfTheDayRule.LookSalt),
                        RaceOfTheDayRule.IndexFor(Start + d, count, RaceOfTheDayRule.LookSalt));
        }

        [Test]
        public void IndexFor_NeighbouringDaysNeverPickTheSameEntry()
        {
            for (int count = 2; count <= 9; count++)
            {
                foreach (int salt in new[] { RaceOfTheDayRule.DistanceSalt, RaceOfTheDayRule.LookSalt })
                {
                    for (int d = 0; d < Days; d++)
                    {
                        int today = RaceOfTheDayRule.IndexFor(Start + d, count, salt);
                        int tomorrow = RaceOfTheDayRule.IndexFor(Start + d + 1, count, salt);
                        Assert.AreNotEqual(today, tomorrow, $"count {count}, salt {salt}, day +{d}");
                    }
                }
            }
        }

        [Test]
        public void IndexFor_StaysInRange_AndEveryCycleVisitsEveryEntryOnce()
        {
            for (int count = 1; count <= 9; count++)
            {
                for (int cycle = 0; cycle < 60; cycle++)
                {
                    long first = (Start / count + cycle) * count; // a cycle starts on a multiple of count
                    var seen = new HashSet<int>();
                    for (int i = 0; i < count; i++)
                    {
                        int index = RaceOfTheDayRule.IndexFor(first + i, count, RaceOfTheDayRule.DistanceSalt);
                        Assert.That(index, Is.InRange(0, count - 1));
                        seen.Add(index);
                    }
                    Assert.AreEqual(count, seen.Count, $"count {count}, cycle {cycle}");
                }
            }
        }

        [Test]
        public void IndexFor_TheOrderIsNotJustCountingUp()
        {
            int ascending = 0;
            for (int d = 0; d < 200; d++)
                if (RaceOfTheDayRule.IndexFor(Start + d + 1, 5, RaceOfTheDayRule.DistanceSalt)
                    == (RaceOfTheDayRule.IndexFor(Start + d, 5, RaceOfTheDayRule.DistanceSalt) + 1) % 5) ascending++;

            Assert.Less(ascending, 150, "the days walk a shuffle, not the list in order");
        }

        [Test]
        public void IndexFor_DistanceAndLookDoNotMoveTogether()
        {
            int same = 0;
            for (int d = 0; d < 300; d++)
                if (RaceOfTheDayRule.IndexFor(Start + d, 5, RaceOfTheDayRule.DistanceSalt)
                    == RaceOfTheDayRule.IndexFor(Start + d, 5, RaceOfTheDayRule.LookSalt)) same++;

            Assert.Less(same, 250);
        }

        [Test]
        public void RecordId_HasTheEventAndTheRoundedDistance()
        {
            Assert.AreEqual("race_of_the_day:150m", RaceOfTheDayRule.RecordId("race_of_the_day", 150f));
            Assert.AreEqual("race_of_the_day:150m", RaceOfTheDayRule.RecordId("race_of_the_day", 149.9f));
            Assert.AreNotEqual(RaceOfTheDayRule.RecordId("race_of_the_day", 100f), RaceOfTheDayRule.RecordId("race_of_the_day", 150f));
        }
    }
}
