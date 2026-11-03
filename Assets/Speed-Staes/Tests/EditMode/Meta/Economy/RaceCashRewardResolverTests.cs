using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Settings;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class RaceCashRewardResolverTests
    {
        // Arbitrary test inputs; assertions are relations to these values, not shipped asset numbers.
        private const int Finish = 7;
        private const int Difficulty = 5;
        private const int OpponentFull = 12;
        private const int Runners = 5; // the player + 4 opponents

        private EconomyTestKit _k;

        [SetUp]
        public void SetUp()
        {
            _k = new EconomyTestKit();
            ConfigFixture.SetInt(_k.Kit.Economy, "raceFinishCash", Finish);
            ConfigFixture.SetInt(_k.Kit.Economy, "raceDifficultyBonusCash", Difficulty);
            ConfigFixture.SetInt(_k.Kit.Economy, "raceOpponentBonusCash", OpponentFull);
        }

        [TearDown]
        public void TearDown() => _k.Dispose();

        private RaceCashReward Pay(int rank, string id = "race-1", int runners = Runners)
            => _k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, rank, 12.5, runners), id);

        [Test]
        public void Winner_GetsFinishDifficultyAndFullOpponentBonus_AndTheWalletMatches()
        {
            RaceCashReward reward = Pay(1);

            Assert.AreEqual(Finish, reward.Finish);
            Assert.AreEqual(Difficulty, reward.Difficulty);
            Assert.AreEqual(OpponentFull, reward.Opponents);
            Assert.AreEqual(reward.Finish + reward.Difficulty + reward.Opponents, reward.Total);
            Assert.AreEqual(reward.Total, _k.Progress.Cash);
        }

        [Test]
        public void LastPlace_GetsNoOpponentBonus_ButStillFinishAndDifficulty()
        {
            RaceCashReward reward = Pay(Runners);

            Assert.AreEqual(0, reward.PassedOpponents);
            Assert.AreEqual(0, reward.Opponents);
            Assert.AreEqual(Finish + Difficulty, reward.Total);
        }

        [Test]
        public void OpponentBonus_IsTheShareOfOpponentsPassed_AndNeverFallsWithABetterRank()
        {
            int previous = -1;
            for (int rank = Runners; rank >= 1; rank--)
            {
                RaceCashReward reward = Pay(rank, "race-" + rank);
                int passed = Runners - rank;
                Assert.AreEqual(passed, reward.PassedOpponents);
                Assert.AreEqual(Runners - 1, reward.TotalOpponents);
                Assert.AreEqual((int)System.Math.Round((double)OpponentFull * passed / (Runners - 1), System.MidpointRounding.AwayFromZero), reward.Opponents);
                Assert.GreaterOrEqual(reward.Opponents, previous);
                previous = reward.Opponents;
            }
        }

        [Test]
        public void DifficultyBonus_IsTheConfiguredAmountTimesThePlayersDifficulty()
        {
            Assert.AreEqual(0, RaceCashRewardResolver.ScaledDifficultyBonus(Difficulty, 0f), "the lowest difficulty pays no bonus");
            Assert.AreEqual(Difficulty, RaceCashRewardResolver.ScaledDifficultyBonus(Difficulty, 1f));
            int half = RaceCashRewardResolver.ScaledDifficultyBonus(Difficulty, 0.5f);
            Assert.Greater(half, 0);
            Assert.Less(half, Difficulty);
            Assert.AreEqual(Difficulty, RaceCashRewardResolver.ScaledDifficultyBonus(Difficulty, 7f), "kept inside 0..1");
            Assert.AreEqual(0, RaceCashRewardResolver.ScaledDifficultyBonus(Difficulty, -2f));
        }

        [Test]
        public void ThePaidDifficultyBonus_FollowsTheSettingsDifficulty()
        {
            var settings = new PlayerSettingsService(_k.Kit.Fixture.Create<SettingsConfig>(), _k.Kit.Repository);
            var resolver = new RaceCashRewardResolver(_k.Events, _k.Kit.Economy, _k.Ledger, _k.Kit.Repository, settings);

            settings.SetCompetitorDifficulty(0f);
            RaceCashReward low = resolver.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1, 12.5, Runners), "low");
            settings.SetCompetitorDifficulty(1f);
            RaceCashReward high = resolver.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1, 12.5, Runners), "high");

            Assert.AreEqual(0, low.Difficulty);
            Assert.AreEqual(Difficulty, high.Difficulty);
            Assert.AreEqual(Finish + OpponentFull, low.Total, "finish and opponent lines do not depend on the difficulty");
        }

        [Test]
        public void OneOpponent_RankOne_GetsTheFullOpponentBonus()
        {
            RaceCashReward reward = Pay(1, "duel", runners: 2);

            Assert.AreEqual(OpponentFull, reward.Opponents);
            Assert.AreEqual(Finish + Difficulty + OpponentFull, reward.Total);
        }

        [Test]
        public void SameRaceTwice_PaysOnce()
        {
            RaceCashReward first = Pay(2, "race-1");
            RaceCashReward again = Pay(2, "race-1");

            Assert.Greater(first.Total, 0);
            Assert.IsTrue(again.IsEmpty);
            Assert.AreEqual(first.Total, _k.Progress.Cash);
        }

        [Test]
        public void DifferentRaces_EachPay()
        {
            int a = Pay(1, "race-1").Total;
            int b = Pay(1, "race-2").Total;
            Assert.AreEqual(a + b, _k.Progress.Cash);
        }

        [Test]
        public void EventWithoutATicketRule_StillPays_WhenThereAreOpponents()
        {
            RaceCashReward reward = _k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PlainEvent, 1, 12.5, Runners), "r");

            Assert.Greater(reward.Total, 0);
            Assert.AreEqual(reward.Total, _k.Progress.Cash);
        }

        [Test]
        public void RunAlone_PaysNothing_WhateverTheEvent()
        {
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1, 12.5, 1), "a").IsEmpty);
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PlainEvent, 1, 12.5, 1), "b").IsEmpty);
            Assert.AreEqual(0, _k.Progress.Cash);
            Assert.AreEqual(0, _k.Progress.ProcessedTransactionIds.Count);
        }

        [Test]
        public void UnknownEvent_UnfinishedOutcome_OrEmptyPayoutId_PayNothing()
        {
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(new RaceEventId("nope"), 1), "r1").IsEmpty);
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 0), "r2").IsEmpty);
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1, seconds: 0d), "r3").IsEmpty);
            Assert.IsTrue(_k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1), "").IsEmpty);
            Assert.AreEqual(0, _k.Progress.Cash);
        }

        [Test]
        public void AllAmountsZero_PaysNothingAndRecordsNothing()
        {
            ConfigFixture.SetInt(_k.Kit.Economy, "raceFinishCash", 0);
            ConfigFixture.SetInt(_k.Kit.Economy, "raceDifficultyBonusCash", 0);
            ConfigFixture.SetInt(_k.Kit.Economy, "raceOpponentBonusCash", 0);

            Assert.IsTrue(Pay(1).IsEmpty);
            Assert.AreEqual(0, _k.Progress.ProcessedTransactionIds.Count);
        }

        [Test]
        public void Payment_SurvivesARestart_AndTheSameIdStaysSpent()
        {
            RaceCashReward reward = Pay(1, "race-1");

            var reloaded = _k.Restart();

            Assert.AreEqual(reward.Total, reloaded.Current.Cash);
            Assert.IsTrue(Pay(1, "race-1").IsEmpty);
        }

        [Test]
        public void GemAndCashPayoutsShareARaceId_WithoutBlockingEachOther()
        {
            // The same payout id is used by both resolvers in the race flow.
            _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1");
            RaceCashReward cash = _k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PaidEvent, 1), "race-1");

            Assert.Greater(cash.Total, 0);
        }
    }
}
