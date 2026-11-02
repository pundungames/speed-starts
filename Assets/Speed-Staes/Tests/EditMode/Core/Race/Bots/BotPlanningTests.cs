using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using UnityEngine;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>
    /// Bot field planning and press schedule. Relations only (harder = faster, wider range = wider spread, order of events); the config
    /// numbers are read from the config instance, no balance value is asserted. Per-race randomness is switched off where a relation needs it.
    /// </summary>
    public sealed class BotPlanningTests
    {
        private BotRaceConfig _config;

        [SetUp]
        public void SetUp() { _config = ScriptableObject.CreateInstance<BotRaceConfig>(); }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_config); }

        private void Set(string field, object value)
        {
            FieldInfo f = typeof(BotRaceConfig).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f, field);
            f.SetValue(_config, value);
        }

        private static IReadOnlyList<BotPlan> Plan(BotRaceConfig c, int seed, float? reference = null)
            => BotFieldPlanner.Plan(c, reference, new System.Random(seed));

        [Test]
        public void HigherDifficulty_ShortensEveryTarget()
        {
            Set("raceRandomRatio", 0f);
            Set("difficulty", 0.1f);
            var easy = Plan(_config, 5, 20f);
            Set("difficulty", 0.9f);
            var hard = Plan(_config, 5, 20f);
            for (int i = 0; i < easy.Count; i++) Assert.Less(hard[i].TargetSeconds, easy[i].TargetSeconds);
        }

        [Test]
        public void WiderSkillRange_WidensTheSpread()
        {
            Set("raceRandomRatio", 0f);
            Set("skillRange", 0.1f);
            var narrow = Plan(_config, 5, 20f);
            Set("skillRange", 0.9f);
            var wide = Plan(_config, 5, 20f);
            Assert.Greater(Spread(wide), Spread(narrow));
        }

        private static float Spread(IReadOnlyList<BotPlan> plans)
            => plans.Max(p => p.TargetSeconds) - plans.Min(p => p.TargetSeconds);

        [Test]
        public void BotCount_MatchesConfig_AndIsCappedByNamePool()
        {
            Set("botCount", 3);
            Assert.AreEqual(3, Plan(_config, 1).Count);
            Set("botCount", _config.Identities.Count + 10);
            Assert.AreEqual(_config.Identities.Count, Plan(_config, 1).Count);
        }

        [Test]
        public void Names_AreUnique_ForManySeeds()
        {
            Set("botCount", 12);
            for (int seed = 0; seed < 20; seed++)
            {
                var plans = Plan(_config, seed);
                Assert.AreEqual(plans.Count, plans.Select(p => p.Name).Distinct().Count());
            }
        }

        [Test]
        public void SameSeed_SamePlan_DifferentSeed_DifferentTimes()
        {
            var a = Plan(_config, 42, 20f);
            var b = Plan(_config, 42, 20f);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Name, b[i].Name);
                Assert.AreEqual(a[i].TargetSeconds, b[i].TargetSeconds);
                Assert.AreEqual(a[i].PressPeriodSeconds, b[i].PressPeriodSeconds);
            }
            var c = Plan(_config, 43, 20f);
            Assert.IsTrue(Enumerable.Range(0, a.Count).Any(i => a[i].TargetSeconds != c[i].TargetSeconds));
        }

        [Test]
        public void MissingPlayerTime_UsesFallback()
        {
            Set("raceRandomRatio", 0f);
            var noTime = Plan(_config, 3, null);
            var withTime = Plan(_config, 3, _config.FallbackReferenceSeconds);
            for (int i = 0; i < noTime.Count; i++) Assert.AreEqual(withTime[i].TargetSeconds, noTime[i].TargetSeconds);
        }

        [Test]
        public void FasterTarget_MeansHigherSkill_AndBetterDriver()
        {
            var plans = Plan(_config, 9, 20f); // fastest first
            for (int i = 1; i < plans.Count; i++)
            {
                Assert.GreaterOrEqual(plans[i - 1].Skill, plans[i].Skill);
                Assert.LessOrEqual(plans[i - 1].ReactionSeconds, plans[i].ReactionSeconds);
                Assert.LessOrEqual(plans[i - 1].PressPeriodSeconds, plans[i].PressPeriodSeconds);
                Assert.LessOrEqual(plans[i - 1].MistakeChance, plans[i].MistakeChance);
            }
        }

        [Test]
        public void SkillCurve_IsInverseOfTable()
        {
            var table = _config.Calibration;
            foreach (BotCalibrationPoint p in table)
                Assert.AreEqual(p.skill, BotSkillCurve.SkillForSeconds(table, p.seconds), 1e-4f);
            Assert.AreEqual(0.25f, BotSkillCurve.SkillForSeconds(table, BotSkillCurve.SecondsForSkill(table, 0.25f)), 1e-4f);
        }

        // ---- press schedule ----

        private static BotPlan SamplePlan(float chance = 0f)
            => new BotPlan("A", "TR", 15f, 0.8f, reactionSeconds: 0.3f, pressPeriodSeconds: 0.32f, holdSeconds: 0.1f,
                jitterSeconds: 0.03f, mistakeChance: chance, mistakeShiftRatio: 0.5f);

        [Test]
        public void Schedule_LegsAlternate_FirstPressAfterReaction()
        {
            var plan = SamplePlan(0.3f);
            var s = new BotPressSchedule(plan, new System.Random(1), 10.0);
            var presses = s.Events.Where(e => e.IsPress).ToList();
            Assert.Greater(presses.Count, 5);
            Assert.GreaterOrEqual(presses[0].Time, plan.ReactionSeconds);
            for (int i = 0; i < presses.Count; i++) Assert.AreEqual(i % 2, presses[i].Leg);
        }

        [Test]
        public void Schedule_TimesMonotonic_ReleaseAfterPress_HoldMatches()
        {
            var plan = SamplePlan(0.5f);
            for (int seed = 0; seed < 20; seed++)
            {
                var ev = new BotPressSchedule(plan, new System.Random(seed), 12.0).Events;
                for (int i = 1; i < ev.Count; i++) Assert.Greater(ev[i].Time, ev[i - 1].Time);
                for (int i = 0; i + 1 < ev.Count; i += 2)
                {
                    Assert.IsTrue(ev[i].IsPress);
                    Assert.IsFalse(ev[i + 1].IsPress);
                    Assert.AreEqual(ev[i].Leg, ev[i + 1].Leg);
                    Assert.AreEqual(plan.HoldSeconds, ev[i + 1].Time - ev[i].Time, 1e-6);
                }
            }
        }

        [Test]
        public void Schedule_WithoutMistakes_JitterStaysBounded()
        {
            var plan = SamplePlan(0f);
            var presses = new BotPressSchedule(plan, new System.Random(7), 10.0).Events.Where(e => e.IsPress).ToList();
            for (int k = 0; k < presses.Count; k++)
            {
                double nominal = plan.ReactionSeconds + k * plan.PressPeriodSeconds;
                Assert.LessOrEqual(System.Math.Abs(presses[k].Time - nominal), plan.JitterSeconds + 1e-6);
            }
        }

        [Test]
        public void Schedule_Mistakes_MoveSomePressesBeyondJitter()
        {
            var plan = SamplePlan(1f);
            var presses = new BotPressSchedule(plan, new System.Random(3), 10.0).Events.Where(e => e.IsPress).ToList();
            int moved = 0;
            for (int k = 0; k < presses.Count; k++)
                if (System.Math.Abs(presses[k].Time - (plan.ReactionSeconds + k * plan.PressPeriodSeconds)) > plan.JitterSeconds + 1e-6) moved++;
            Assert.Greater(moved, 0);
        }

        [Test]
        public void Schedule_SameSeed_SameEvents()
        {
            var plan = SamplePlan(0.4f);
            var a = new BotPressSchedule(plan, new System.Random(11), 8.0).Events;
            var b = new BotPressSchedule(plan, new System.Random(11), 8.0).Events;
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].Time, b[i].Time);
        }
    }
}
