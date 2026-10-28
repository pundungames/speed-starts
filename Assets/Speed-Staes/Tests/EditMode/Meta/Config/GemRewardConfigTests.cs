using NUnit.Framework;
using SpeedStars.Meta.Config;

namespace SpeedStars.Tests.Meta.Config
{
    public sealed class GemRewardConfigTests
    {
        private ConfigFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ConfigFixture();

        [TearDown]
        public void TearDown() => fixture.DestroyAll();

        private GemRewardConfig Table(params int[] gemsByRank)
        {
            var config = fixture.Create<GemRewardConfig>();
            ConfigFixture.SetInts(config, "purpleGemsByRank", gemsByRank);
            return config;
        }

        [Test]
        public void RankInsideTable_ReturnsThatRow()
        {
            int[] rows = { 11, 6, 4 };
            var config = Table(rows);

            for (int rank = 1; rank <= rows.Length; rank++)
                Assert.AreEqual(rows[rank - 1], config.GetPurpleGems(rank), $"rank {rank}");
        }

        [Test]
        public void RankPastTable_PaysNothing()
        {
            int[] rows = { 11, 6, 4 };
            var config = Table(rows);

            Assert.AreEqual(0, config.GetPurpleGems(rows.Length + 1));
            Assert.AreEqual(0, config.GetPurpleGems(rows.Length + 50));
        }

        [Test]
        public void RankBelowOne_PaysNothing()
        {
            var config = Table(11, 6, 4);

            Assert.AreEqual(0, config.GetPurpleGems(0));
            Assert.AreEqual(0, config.GetPurpleGems(-1));
        }

        [Test]
        public void EmptyTable_PaysNothingForAnyRank()
        {
            var config = fixture.Create<GemRewardConfig>();

            Assert.AreEqual(0, config.GetPurpleGems(1));
            Assert.AreEqual(0, config.GetPurpleGems(2));
        }
    }
}
