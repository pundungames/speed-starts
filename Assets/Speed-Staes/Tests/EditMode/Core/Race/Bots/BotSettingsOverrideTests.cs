using System.Linq;
using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using UnityEngine;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>T-088: the player's difficulty and skill range replace the config's in the field plan, and the lane preference moves the player among the bots.</summary>
    public sealed class BotSettingsOverrideTests
    {
        private BotRaceConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<BotRaceConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private void Set(string field, object value)
        {
            var f = typeof(BotRaceConfig).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(f, field);
            f.SetValue(_config, value);
        }

        [Test]
        public void NoOverride_PlansExactlyAsTheConfigDoes()
        {
            Set("raceRandomRatio", 0f);
            var plain = BotFieldPlanner.Plan(_config, 20f, new System.Random(3));
            var same = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, null, null);

            for (int i = 0; i < plain.Count; i++) Assert.AreEqual(plain[i].TargetSeconds, same[i].TargetSeconds, 1e-6f);
        }

        [Test]
        public void ThePlayersDifficulty_ReplacesTheConfigs()
        {
            Set("raceRandomRatio", 0f);
            Set("difficulty", 0.5f);
            var easy = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, 0.1f, null);
            var hard = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, 0.9f, null);

            for (int i = 0; i < easy.Count; i++) Assert.Less(hard[i].TargetSeconds, easy[i].TargetSeconds);
        }

        [Test]
        public void ThePlayersSkillRange_ReplacesTheConfigs()
        {
            Set("raceRandomRatio", 0f);
            Set("skillRange", 0.5f);
            var narrow = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, null, 0.1f);
            var wide = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, null, 0.9f);

            Assert.Greater(wide.Max(p => p.TargetSeconds) - wide.Min(p => p.TargetSeconds),
                narrow.Max(p => p.TargetSeconds) - narrow.Min(p => p.TargetSeconds));
        }

        [Test]
        public void AnOutOfRangeOverride_IsKeptInside()
        {
            Set("raceRandomRatio", 0f);
            var top = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, 1f, 1f);
            var beyond = BotFieldPlanner.Plan(_config, 20f, new System.Random(3), null, 50f, 50f);

            for (int i = 0; i < top.Count; i++) Assert.AreEqual(top[i].TargetSeconds, beyond[i].TargetSeconds, 1e-6f);
        }

        // ---------------------------------------------------------------- lanes

        [Test]
        public void AutomaticLane_KeepsThePlayerOnSlotZero_AndTheBotsInOrderBehindIt()
        {
            Assert.AreEqual(0, BotLanes.PlayerSlot(0, 5));
            for (int i = 0; i < 5; i++) Assert.AreEqual(i + 1, BotLanes.BotOffset(i, 0));
        }

        [Test]
        public void ALaneChoice_PutsThePlayerThere_AndTheBotsFillEveryOtherSlotOnce()
        {
            const int bots = 5;
            for (int preference = 1; preference <= bots + 1; preference++)
            {
                int slot = BotLanes.PlayerSlot(preference, bots);
                Assert.AreEqual(preference - 1, slot);
                var taken = Enumerable.Range(0, bots).Select(i => BotLanes.BotOffset(i, slot) + slot).ToList();
                CollectionAssert.AllItemsAreUnique(taken);
                CollectionAssert.DoesNotContain(taken, slot, "no bot stands in the player's lane");
                Assert.GreaterOrEqual(taken.Min(), 0);
                Assert.LessOrEqual(taken.Max(), bots);
            }
        }

        [Test]
        public void ALaneChoicePastTheLastLane_IsTheLastLane()
        {
            Assert.AreEqual(5, BotLanes.PlayerSlot(99, 5));
            Assert.AreEqual(0, BotLanes.PlayerSlot(3, 0));
        }
    }
}
