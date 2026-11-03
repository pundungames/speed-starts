using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Core.Race.Relay;
using UnityEngine;

namespace SpeedStars.Tests.Relay
{
    /// <summary>
    /// Relay exchange rules and the planner's team count. Relations and geometry only; the tuning numbers live on the relay component.
    /// </summary>
    public sealed class RelayRulesTests
    {
        [Test]
        public void FirstLeg_StartsAtTheRaceStart_OthersAZoneBeforeTheirLine()
        {
            Assert.AreEqual(0f, RelayRules.LegStartMeters(0, 100f, 20f));
            Assert.AreEqual(80f, RelayRules.LegStartMeters(1, 100f, 20f));
            Assert.AreEqual(280f, RelayRules.LegStartMeters(3, 100f, 20f));
        }

        [Test]
        public void NextRunner_StartsWithinTheGap_TakesTheBatonWithinReach()
        {
            Assert.IsFalse(RelayRules.ShouldStart(21f, 20f));
            Assert.IsTrue(RelayRules.ShouldStart(20f, 20f));
            Assert.IsFalse(RelayRules.ShouldExchange(1.5f, 0.8f));
            Assert.IsTrue(RelayRules.ShouldExchange(0.5f, 0.8f));
        }

        [Test]
        public void NextRunner_HoldsBack_OnlyWhenAsFastAsTheIncomingRunner()
        {
            Assert.IsFalse(RelayRules.ShouldHoldBack(5f, 9f, 0.4f));
            Assert.IsTrue(RelayRules.ShouldHoldBack(8.7f, 9f, 0.4f));
        }

        [Test]
        public void CrossingTime_IsInterpolatedInsideTheFrame()
        {
            Assert.AreEqual(10.5, RelayRules.CrossingTime(100f, 99f, 10.0, 101f, 11.0), 1e-9);
            Assert.AreEqual(11.0, RelayRules.CrossingTime(100f, 100f, 10.0, 100f, 11.0), 1e-9); // no movement: the frame's end
        }

        [Test]
        public void Planner_PlansTheAskedCount_CappedByTheNamePool()
        {
            var config = ScriptableObject.CreateInstance<BotRaceConfig>();
            try
            {
                int pool = config.Identities.Count;
                Assert.AreEqual(Mathf.Min(8, pool), BotFieldPlanner.Plan(config, 15f, new System.Random(1), count: 8).Count);
                Assert.AreEqual(config.BotCount <= pool ? config.BotCount : pool, BotFieldPlanner.Plan(config, 15f, new System.Random(1)).Count);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
