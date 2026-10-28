using NUnit.Framework;
using SpeedStars.Meta.Config;

namespace SpeedStars.Tests.Meta.Config
{
    public sealed class SevenDayRewardConfigTests
    {
        private ConfigFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ConfigFixture();

        [TearDown]
        public void TearDown() => fixture.DestroyAll();

        private static void SetDay(SevenDayRewardConfig config, int dayNumber, SevenDayRewardKind kind, int amount,
            CosmeticDefinition cosmetic = null)
        {
            ConfigFixture.Edit(config, so =>
            {
                var day = ConfigFixture.Find(so, "days").GetArrayElementAtIndex(dayNumber - 1);
                day.FindPropertyRelative("kind").intValue = (int)kind;
                day.FindPropertyRelative("amount").intValue = amount;
                day.FindPropertyRelative("cosmetic").objectReferenceValue = cosmetic;
            });
        }

        [Test]
        public void NewConfig_HasEveryDayEmpty()
        {
            var config = fixture.Create<SevenDayRewardConfig>();

            for (int day = 1; day <= SevenDayRewardConfig.DayCount; day++)
                Assert.IsTrue(config.GetDay(day).IsEmpty, $"day {day}");
        }

        [Test]
        public void EmptyLastDay_IsEmptyWhileTheOthersGiveTheirReward()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            int last = SevenDayRewardConfig.DayCount;
            for (int day = 1; day < last; day++)
                SetDay(config, day, SevenDayRewardKind.Cash, day * 10);
            SetDay(config, last, SevenDayRewardKind.None, 0);

            Assert.IsTrue(config.GetDay(last).IsEmpty);
            Assert.AreEqual(SevenDayRewardKind.None, config.GetDay(last).Kind);
            for (int day = 1; day < last; day++)
            {
                Assert.IsFalse(config.GetDay(day).IsEmpty, $"day {day}");
                Assert.AreEqual(SevenDayRewardKind.Cash, config.GetDay(day).Kind);
                Assert.AreEqual(day * 10, config.GetDay(day).Amount);
            }
        }

        [Test]
        public void EmptyDayInTheMiddle_DoesNotShiftTheFollowingDays()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            SetDay(config, 5, SevenDayRewardKind.TimedTicket, 2);
            SetDay(config, 6, SevenDayRewardKind.None, 0);
            SetDay(config, 7, SevenDayRewardKind.EntryTicket, 3);

            Assert.AreEqual(SevenDayRewardKind.TimedTicket, config.GetDay(5).Kind);
            Assert.IsTrue(config.GetDay(6).IsEmpty);
            Assert.AreEqual(SevenDayRewardKind.EntryTicket, config.GetDay(7).Kind);
            Assert.AreEqual(3, config.GetDay(7).Amount);
        }

        [Test]
        public void DaysOutsideTheCalendar_AreEmpty()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            for (int day = 1; day <= SevenDayRewardConfig.DayCount; day++)
                SetDay(config, day, SevenDayRewardKind.Cash, 5);

            Assert.IsTrue(config.GetDay(0).IsEmpty);
            Assert.IsTrue(config.GetDay(-1).IsEmpty);
            Assert.IsTrue(config.GetDay(SevenDayRewardConfig.DayCount + 1).IsEmpty);
        }

        [Test]
        public void ShortDayList_MissingDaysAreEmpty()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            ConfigFixture.Edit(config, so => ConfigFixture.Find(so, "days").arraySize = 2);
            SetDay(config, 1, SevenDayRewardKind.Cash, 5);

            Assert.IsFalse(config.GetDay(1).IsEmpty);
            Assert.IsTrue(config.GetDay(SevenDayRewardConfig.DayCount).IsEmpty);
        }

        [Test]
        public void AmountKindWithoutAmount_IsEmpty()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            SetDay(config, 1, SevenDayRewardKind.Cash, 0);
            SetDay(config, 2, SevenDayRewardKind.EntryTicket, 0);
            SetDay(config, 3, SevenDayRewardKind.TimedTicket, 0);

            Assert.IsTrue(config.GetDay(1).IsEmpty);
            Assert.IsTrue(config.GetDay(2).IsEmpty);
            Assert.IsTrue(config.GetDay(3).IsEmpty);
        }

        [Test]
        public void CosmeticDay_GivesItsItemAndIsEmptyWithoutOne()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            var item = fixture.Create<CosmeticDefinition>();
            SetDay(config, 6, SevenDayRewardKind.Cosmetic, 0);
            SetDay(config, 7, SevenDayRewardKind.Cosmetic, 0, item);

            Assert.IsTrue(config.GetDay(6).IsEmpty);
            Assert.IsFalse(config.GetDay(7).IsEmpty);
            Assert.AreSame(item, config.GetDay(7).Cosmetic);
        }

        [Test]
        public void CyclePolicy_ComesFromTheAsset()
        {
            var config = fixture.Create<SevenDayRewardConfig>();
            ConfigFixture.SetInt(config, "cyclePolicy", (int)SevenDayCyclePolicy.EndAfterLastDay);

            Assert.AreEqual(SevenDayCyclePolicy.EndAfterLastDay, config.CyclePolicy);
        }
    }
}
