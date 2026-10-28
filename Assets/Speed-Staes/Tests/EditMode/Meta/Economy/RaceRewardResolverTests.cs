using NUnit.Framework;
using SpeedStars.Core.Race;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class RaceRewardResolverTests
    {
        // The kit's reward table is { 9, 6, 3 } for ranks 1..3; read it back through the config, not by literal.
        private EconomyTestKit _k;

        [SetUp]
        public void SetUp() => _k = new EconomyTestKit();

        [TearDown]
        public void TearDown() => _k.Dispose();

        [Test]
        public void GemEvent_PaysTheRankRow()
        {
            int paid = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 2), "race-1");

            Assert.AreEqual(_k.Gems.GetPurpleGems(2), paid);
            Assert.Greater(paid, 0);
            Assert.AreEqual(paid, _k.Progress.PurpleGems);
        }

        [Test]
        public void BetterRank_PaysAtLeastAsMuch()
        {
            int first = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "a");
            int third = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 3), "b");
            Assert.GreaterOrEqual(first, third);
        }

        [Test]
        public void SameRaceTwice_PaysOnce()
        {
            int first = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1");
            int again = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1");

            Assert.Greater(first, 0);
            Assert.AreEqual(0, again);
            Assert.AreEqual(first, _k.Progress.PurpleGems);
        }

        [Test]
        public void DifferentRaces_EachPay()
        {
            int a = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1");
            int b = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-2");
            Assert.AreEqual(a + b, _k.Progress.PurpleGems);
        }

        [Test]
        public void EventWithoutGemFlag_PaysNothing()
        {
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.PlainEvent, 1), "r"));
            Assert.AreEqual(0, _k.Progress.PurpleGems);
        }

        [Test]
        public void UnknownEvent_PaysNothing()
        {
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(new RaceEventId("nope"), 1), "r"));
        }

        [Test]
        public void RankPastTheTable_OrBelowOne_PaysNothing()
        {
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 4), "r1"));
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 0), "r2"));
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, -1), "r3"));
            Assert.AreEqual(0, _k.Progress.PurpleGems);
        }

        [Test]
        public void UnfinishedOutcome_PaysNothing_AndDoesNotBurnThePayoutId()
        {
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1, seconds: 0d), "r"));
            Assert.AreEqual(0, _k.Progress.PurpleGems);

            Assert.Greater(_k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "r"), 0);
        }

        [Test]
        public void MissingPayoutId_PaysNothing()
        {
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), ""));
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), null));
        }

        [Test]
        public void Payout_IsSaved_AndStillIdempotentAfterRestart()
        {
            int paid = _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1");

            var reloaded = _k.Restart();

            Assert.AreEqual(paid, reloaded.Current.PurpleGems);
            Assert.AreEqual(0, _k.Rewards.Resolve(_k.Outcome(EconomyTestKit.GemEvent, 1), "race-1"));
        }
    }
}
