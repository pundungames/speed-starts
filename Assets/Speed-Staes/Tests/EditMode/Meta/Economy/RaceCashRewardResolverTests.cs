using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
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
        public void SoloRace_NoOpponents_PaysNoOpponentBonus()
        {
            RaceCashReward reward = Pay(1, "solo", runners: 1);

            Assert.AreEqual(0, reward.Opponents);
            Assert.AreEqual(Finish + Difficulty, reward.Total);
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
        public void FreeEvent_PaysNothing()
        {
            RaceCashReward reward = _k.CashRewards.Resolve(_k.Outcome(EconomyTestKit.PlainEvent, 1), "r");

            Assert.IsTrue(reward.IsEmpty);
            Assert.AreEqual(0, _k.Progress.Cash);
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
