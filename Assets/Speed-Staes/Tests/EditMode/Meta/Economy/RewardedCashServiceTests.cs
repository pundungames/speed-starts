using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Economy;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class RewardedCashServiceTests
    {
        // Arbitrary test inputs; assertions are relations to these values.
        private const int Amount = 40;
        private const int Limit = 3;

        private EconomyTestKit _k;

        [SetUp]
        public void SetUp()
        {
            _k = new EconomyTestKit();
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCash", Amount);
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCashDailyLimit", Limit);
            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown() => _k.Dispose();

        private RewardedCashOutcome Watch(IRewardedCash service)
        {
            RewardedCashOutcome? outcome = null;
            Assert.IsTrue(service.TryWatch(o => outcome = o));
            Assert.IsNotNull(outcome);
            return outcome.Value;
        }

        [Test]
        public void CompletedAd_PaysTheConfiguredCash_AndUsesOneRight()
        {
            Assert.AreEqual(Limit, _k.CashAds.RemainingToday);

            Assert.AreEqual(RewardedCashOutcome.Granted, Watch(_k.CashAds));

            Assert.AreEqual(Amount, _k.Progress.Cash);
            Assert.AreEqual(Limit - 1, _k.CashAds.RemainingToday);
        }

        [Test]
        public void AfterTheDailyLimit_NoFurtherAdIsShown_AndNothingIsPaid()
        {
            for (int i = 0; i < Limit; i++) Assert.AreEqual(RewardedCashOutcome.Granted, Watch(_k.CashAds));
            int shown = _k.Ads.Shown;

            Assert.IsFalse(_k.CashAds.IsAvailable);
            Assert.IsFalse(_k.CashAds.TryWatch(_ => Assert.Fail("no ad past the limit")));

            Assert.AreEqual(shown, _k.Ads.Shown);
            Assert.AreEqual(0, _k.CashAds.RemainingToday);
            Assert.AreEqual(Amount * Limit, _k.Progress.Cash);
        }

        [Test]
        public void NextUtcDay_RestoresTheRights()
        {
            for (int i = 0; i < Limit; i++) Watch(_k.CashAds);

            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 2, 0, 0, 1, DateTimeKind.Utc);

            Assert.AreEqual(Limit, _k.CashAds.RemainingToday);
            Assert.AreEqual(RewardedCashOutcome.Granted, Watch(_k.CashAds));
            Assert.AreEqual(Limit - 1, _k.CashAds.RemainingToday);
        }

        [Test]
        public void ClockWoundBackToAnEarlierDay_GivesNoRightsBack()
        {
            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 5, 12, 0, 0, DateTimeKind.Utc);
            for (int i = 0; i < Limit; i++) Watch(_k.CashAds);

            _k.Kit.Clock.UtcNow = new DateTime(2030, 3, 1, 12, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(0, _k.CashAds.RemainingToday);
            Assert.IsFalse(_k.CashAds.IsAvailable);
        }

        [Test]
        public void SkippedOrFailedAd_PaysNothingAndKeepsTheRight()
        {
            _k.Ads.Result = AdResult.Skipped;
            Assert.AreEqual(RewardedCashOutcome.Skipped, Watch(_k.CashAds));
            _k.Ads.Result = AdResult.Failed;
            Assert.AreEqual(RewardedCashOutcome.Failed, Watch(_k.CashAds));

            Assert.AreEqual(0, _k.Progress.Cash);
            Assert.AreEqual(Limit, _k.CashAds.RemainingToday);
        }

        [Test]
        public void OneAdAtATime_AndARepeatedCallbackPaysOnce()
        {
            _k.Ads.Hold = true;
            int calls = 0;
            Assert.IsTrue(_k.CashAds.TryWatch(_ => calls++));
            Assert.IsTrue(_k.CashAds.IsBusy);
            Assert.IsFalse(_k.CashAds.TryWatch(_ => Assert.Fail("second ad while one runs")));

            _k.Ads.Pending(AdResult.Completed);
            _k.Ads.Pending(AdResult.Completed); // an SDK answering twice

            Assert.AreEqual(1, calls);
            Assert.AreEqual(Amount, _k.Progress.Cash);
            Assert.IsFalse(_k.CashAds.IsBusy);
        }

        [Test]
        public void NoAdService_NotReady_OrClosedConfig_MeansNoOffer()
        {
            _k.Ads.Ready = false;
            Assert.IsFalse(_k.CashAds.IsAvailable);
            _k.Ads.Ready = true;

            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCash", 0);
            Assert.IsFalse(_k.CashAds.IsAvailable);
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCash", Amount);
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCashDailyLimit", 0);
            Assert.IsFalse(_k.CashAds.IsAvailable);

            var noAds = new RewardedCashService(null, _k.Kit.Repository, _k.Kit.Clock, _k.Kit.Economy, _k.Ledger);
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdCashDailyLimit", Limit);
            Assert.IsFalse(noAds.IsAvailable);
        }

        [Test]
        public void Counter_SurvivesARestart()
        {
            Watch(_k.CashAds);
            Watch(_k.CashAds);

            var reloaded = _k.Restart();

            Assert.AreEqual(Limit - 2, _k.CashAds.RemainingToday);
            Assert.AreEqual(Amount * 2, reloaded.Current.Cash);
        }

        [Test]
        public void ChangedFiresAfterPayment()
        {
            int changed = 0;
            _k.CashAds.Changed += () => changed++;

            Watch(_k.CashAds);

            Assert.AreEqual(1, changed);
        }

        [Test]
        public void OldSaveWithoutTheCounter_OpensWithAllRights()
        {
            // A save written before economy v2 has no adCash fields: JsonUtility leaves the defaults.
            var progress = new SpeedStars.Meta.Persistence.PlayerProgress();
            UnityEngine.JsonUtility.FromJsonOverwrite("{\"schemaVersion\":2,\"cash\":5}", progress);

            Assert.AreEqual(string.Empty, progress.AdCashDayKey);
            Assert.AreEqual(0, progress.AdCashWatched);
            Assert.AreEqual(5, progress.Cash);
        }
    }
}
