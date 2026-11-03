using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using UnityEngine;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>
    /// T-082 scenario 5: the easy / normal / hard race draw of <c>BotFieldPlanner</c>. The draw is one random number taken before
    /// anything else, so one seed gives the same draw under every config: plans of the same seed differ only by the race multiplier.
    /// Chances and multipliers are test inputs written into a fresh config; tests read them back and assert relations.
    /// </summary>
    public sealed class BotRaceDrawTests
    {
        private const float Reference = 20f;
        private const int Seed = 17;

        private BotRaceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BotRaceConfig>();
            Set("raceRandomRatio", 0.02f); // kept on: the per-bot randomness must not break the shift
            Set("easyRaceMultiplier", 1.2f);
            Set("hardRaceMultiplier", 0.8f);
            SetDraw(0f, 0f);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_config); }

        private void Set(string field, object value)
        {
            FieldInfo f = typeof(BotRaceConfig).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f, field);
            f.SetValue(_config, value);
        }

        private void SetDraw(float easyChance, float hardChance)
        {
            Set("easyRaceChance", easyChance);
            Set("hardRaceChance", hardChance);
        }

        private IReadOnlyList<BotPlan> Plan(int seed = Seed) =>
            BotFieldPlanner.Plan(_config, Reference, new System.Random(seed));

        private static void AssertEveryTargetScaled(IReadOnlyList<BotPlan> shifted, IReadOnlyList<BotPlan> neutral, float factor)
        {
            Assert.AreEqual(neutral.Count, shifted.Count);
            Assert.Greater(neutral.Count, 0);
            for (int i = 0; i < neutral.Count; i++)
                Assert.AreEqual(neutral[i].TargetSeconds * factor, shifted[i].TargetSeconds, neutral[i].TargetSeconds * 1e-4f, "bot " + i);
        }

        [Test]
        public void NoChance_LeavesTheFieldUntouched_WhateverTheMultipliersSay()
        {
            var baseline = Plan();
            Set("easyRaceMultiplier", 3f);
            Set("hardRaceMultiplier", 0.3f);

            var plan = Plan();

            AssertEveryTargetScaled(plan, baseline, 1f);
        }

        [Test]
        public void EasyDrawAlways_SlowsEveryBotByTheConfigMultiplier()
        {
            var neutral = Plan();
            SetDraw(1f, 0f);

            var easy = Plan();

            Assert.Greater(_config.EasyRaceMultiplier, 1f, "the test input must be a slower field");
            AssertEveryTargetScaled(easy, neutral, _config.EasyRaceMultiplier);
        }

        [Test]
        public void HardDrawAlways_SpeedsUpEveryBotByTheConfigMultiplier()
        {
            var neutral = Plan();
            SetDraw(0f, 1f);

            var hard = Plan();

            Assert.Less(_config.HardRaceMultiplier, 1f, "the test input must be a faster field");
            AssertEveryTargetScaled(hard, neutral, _config.HardRaceMultiplier);
        }

        [Test]
        public void AnEasyRace_IsSlowerThanANormalOne_WhichIsSlowerThanAHardOne_BotForBot()
        {
            var neutral = Plan();
            SetDraw(1f, 0f);
            var easy = Plan();
            SetDraw(0f, 1f);
            var hard = Plan();

            for (int i = 0; i < neutral.Count; i++)
            {
                Assert.Greater(easy[i].TargetSeconds, neutral[i].TargetSeconds);
                Assert.Less(hard[i].TargetSeconds, neutral[i].TargetSeconds);
            }
        }

        [Test]
        public void TheShift_KeepsTheSpreadOfTheFieldInProportion()
        {
            var neutral = Plan();
            SetDraw(1f, 0f);
            var easy = Plan();

            float neutralSpan = neutral[neutral.Count - 1].TargetSeconds - neutral[0].TargetSeconds;
            float easySpan = easy[easy.Count - 1].TargetSeconds - easy[0].TargetSeconds;
            Assert.AreEqual(neutralSpan * _config.EasyRaceMultiplier, easySpan, neutralSpan * 1e-3f);
        }

        [Test]
        public void TheShift_MovesTheWholeFieldTogether_SoTheOrderOfBotsIsKept()
        {
            SetDraw(1f, 0f);
            var easy = Plan();

            for (int i = 1; i < easy.Count; i++)
                Assert.GreaterOrEqual(easy[i].TargetSeconds, easy[i - 1].TargetSeconds, "fastest first");
        }

        [Test]
        public void EasyDrawSlowsTheBots_SoTheirSkillFallsAndTheirDriverGetsWorse()
        {
            var neutral = Plan();
            SetDraw(1f, 0f);
            var easy = Plan();

            for (int i = 0; i < neutral.Count; i++)
            {
                Assert.LessOrEqual(easy[i].Skill, neutral[i].Skill);
                Assert.GreaterOrEqual(easy[i].PressPeriodSeconds, neutral[i].PressPeriodSeconds);
            }
        }

        [Test]
        public void SameSeedAndSameConfig_GiveTheSamePlan_AlsoWithTheDrawOn()
        {
            SetDraw(0.4f, 0.3f);

            var a = Plan(5);
            var b = Plan(5);

            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Name, b[i].Name);
                Assert.AreEqual(a[i].TargetSeconds, b[i].TargetSeconds);
            }
        }

        [Test]
        public void TheDrawFollowsTheChances_OverManySeeds()
        {
            const int races = 400;
            Set("raceRandomRatio", 0f);
            var neutral = Plan();
            SetDraw(0.3f, 0.2f);
            int easy = 0, hard = 0;

            for (int seed = 0; seed < races; seed++)
            {
                float ratio = Plan(seed)[0].TargetSeconds / neutral[0].TargetSeconds;
                if (ratio > 1f + 1e-3f) easy++;
                else if (ratio < 1f - 1e-3f) hard++;
            }

            // Wide margins: this guards the direction and the rough share, not an exact count.
            Assert.Greater(easy, 0);
            Assert.Greater(hard, 0);
            Assert.Less(easy + hard, races, "some races stay normal");
            Assert.Greater(easy, hard, "the config gives the easy draw the larger chance");
        }

        [Test]
        public void ChancesSummingAboveOne_NeverBreakThePlan()
        {
            SetDraw(0.9f, 0.9f);

            for (int seed = 0; seed < 50; seed++)
            {
                var plan = Plan(seed);
                Assert.AreEqual(_config.BotCount, plan.Count);
                foreach (BotPlan p in plan) Assert.Greater(p.TargetSeconds, 0f);
            }
        }
    }
}
