using NUnit.Framework;
using SpeedStars.Meta.Config;

namespace SpeedStars.Tests.Meta.Config
{
    public sealed class EconomyConfigTests
    {
        private ConfigFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ConfigFixture();

        [TearDown]
        public void TearDown() => fixture.DestroyAll();

        [Test]
        public void NewConfig_EveryTicketOptionIsClosed()
        {
            var config = fixture.Create<EconomyConfig>();

            Assert.IsFalse(config.TryGetEntryTicketCashPrice(out var entryPrice));
            Assert.AreEqual(0, entryPrice);
            Assert.IsFalse(config.TryGetTimedTicketCashPrice(out var timedPrice));
            Assert.AreEqual(0, timedPrice);
            Assert.IsFalse(config.TryGetRewardedAdEntryTickets(out var adTickets));
            Assert.AreEqual(0, adTickets);
        }

        [Test]
        public void TicketWithAPrice_IsOpenAtThatPrice()
        {
            var config = fixture.Create<EconomyConfig>();
            ConfigFixture.SetInt(config, "entryTicketCashPrice", 13);
            ConfigFixture.SetInt(config, "timedTicketCashPrice", 29);

            Assert.IsTrue(config.TryGetEntryTicketCashPrice(out var entryPrice));
            Assert.AreEqual(13, entryPrice);
            Assert.IsTrue(config.TryGetTimedTicketCashPrice(out var timedPrice));
            Assert.AreEqual(29, timedPrice);
        }

        [Test]
        public void OnePricedTicket_DoesNotOpenTheOther()
        {
            var config = fixture.Create<EconomyConfig>();
            ConfigFixture.SetInt(config, "timedTicketCashPrice", 29);

            Assert.IsFalse(config.TryGetEntryTicketCashPrice(out _));
            Assert.IsTrue(config.TryGetTimedTicketCashPrice(out _));
        }

        [TestCase(true, 0, false)]
        [TestCase(false, 4, false)]
        [TestCase(true, 4, true)]
        public void RewardedAd_NeedsBothEnabledAndAnAmount(bool enabled, int amount, bool expectedOpen)
        {
            var config = fixture.Create<EconomyConfig>();
            ConfigFixture.SetBool(config, "rewardedAdEnabled", enabled);
            ConfigFixture.SetInt(config, "rewardedAdEntryTickets", amount);

            Assert.AreEqual(expectedOpen, config.TryGetRewardedAdEntryTickets(out var tickets));
            Assert.AreEqual(expectedOpen ? amount : 0, tickets);
        }

        [Test]
        public void MinuteFields_BecomeTheSameDurations()
        {
            var config = fixture.Create<EconomyConfig>();
            ConfigFixture.SetFloat(config, "entryTicketRegenMinutes", 7.5f);
            ConfigFixture.SetFloat(config, "timedAccessMinutes", 95f);

            Assert.AreEqual(7.5, config.EntryTicketRegenInterval.TotalMinutes, 1e-6);
            Assert.AreEqual(95.0, config.TimedAccessDuration.TotalMinutes, 1e-6);
        }

        [Test]
        public void CapGiftStartingBalancesAndDayBoundary_ComeFromTheAsset()
        {
            var config = fixture.Create<EconomyConfig>();
            ConfigFixture.SetInt(config, "entryTicketCap", 9);
            ConfigFixture.SetInt(config, "dailyGiftEntryTickets", 2);
            ConfigFixture.SetInt(config, "startingCash", 101);
            ConfigFixture.SetInt(config, "startingPurpleGems", 102);
            ConfigFixture.SetInt(config, "startingEntryTickets", 103);
            ConfigFixture.SetInt(config, "startingTimedTickets", 104);
            ConfigFixture.SetInt(config, "dayBoundary", (int)DayBoundaryPolicy.UtcMidnight);

            Assert.AreEqual(9, config.EntryTicketCap);
            Assert.AreEqual(2, config.DailyGiftEntryTickets);
            Assert.AreEqual(101, config.StartingCash);
            Assert.AreEqual(102, config.StartingPurpleGems);
            Assert.AreEqual(103, config.StartingEntryTickets);
            Assert.AreEqual(104, config.StartingTimedTickets);
            Assert.AreEqual(DayBoundaryPolicy.UtcMidnight, config.DayBoundary);
        }
    }
}
