using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Meta.Config;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>
    /// T-086: the bots' table per event. Relations only: an event without a profile plans exactly as before (100 m behaviour kept), an event
    /// with one uses its own table and fallback, the player's time is scaled only without a profile, the jump rule is deterministic and
    /// the hurdle layouts of the event assets leave room for the finish line. No balance number is asserted.
    /// </summary>
    public sealed class BotEventProfileTests
    {
        private const string FarEvent = "far";
        private BotRaceConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<BotRaceConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private void Set(string field, object value)
        {
            FieldInfo f = typeof(BotRaceConfig).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(f, field);
            f.SetValue(_config, value);
        }

        /// <summary>A profile whose times are the base table's times times a factor (a longer event).</summary>
        private BotEventProfile Longer(string id, float factor, float fallback = 0f)
            => new BotEventProfile(id, fallback, _config.Calibration.Select(p => new BotCalibrationPoint(p.skill, p.seconds * factor)));

        [Test]
        public void NoProfile_PlansExactlyAsBefore()
        {
            for (int seed = 0; seed < 5; seed++)
            {
                var without = BotFieldPlanner.Plan(_config, 18f, new System.Random(seed));
                var withNull = BotFieldPlanner.Plan(_config, 18f, new System.Random(seed), null);
                Assert.AreEqual(without.Count, withNull.Count);
                for (int i = 0; i < without.Count; i++)
                {
                    Assert.AreEqual(without[i].TargetSeconds, withNull[i].TargetSeconds);
                    Assert.AreEqual(without[i].Skill, withNull[i].Skill);
                    Assert.AreEqual(without[i].Name, withNull[i].Name);
                }
            }
        }

        [Test]
        public void ProfileOfAnotherEvent_IsNotFound_And100mKeepsTheBaseTable()
        {
            _config.SetProfile(Longer("200m", 2f));
            Assert.IsFalse(_config.TryGetProfile("100m", out _));
            Assert.IsNull(BotEventSetup.Find(_config, "100m"));
            Assert.IsNull(BotEventSetup.Find(_config, string.Empty));
            Assert.IsNull(BotEventSetup.Find(null, "200m"));
            Assert.IsNotNull(BotEventSetup.Find(_config, "200m"));
        }

        [Test]
        public void SetProfile_ReplacesTheSameEvent_AndEmptyTableCountsAsNone()
        {
            _config.SetProfile(Longer(FarEvent, 2f));
            _config.SetProfile(Longer(FarEvent, 3f));
            Assert.AreEqual(1, _config.EventProfiles.Count);
            Assert.IsTrue(_config.TryGetProfile(FarEvent, out BotEventProfile p));
            Assert.AreEqual(_config.Calibration[0].seconds * 3f, p.Calibration[0].seconds, 1e-3f);

            _config.SetProfile(new BotEventProfile("empty", 10f, new BotCalibrationPoint[0]));
            Assert.IsFalse(_config.TryGetProfile("empty", out _));
        }

        [Test]
        public void Profile_UsesItsOwnTable_TargetsAndSkillFollowTheLongerEvent()
        {
            Set("raceRandomRatio", 0f);
            Set("easyRaceChance", 0f);
            Set("hardRaceChance", 0f);
            BotEventProfile longer = Longer(FarEvent, 2f);
            var baseline = BotFieldPlanner.Plan(_config, 20f, new System.Random(4));
            var far = BotFieldPlanner.Plan(_config, 40f, new System.Random(4), longer);
            Assert.AreEqual(baseline.Count, far.Count);
            for (int i = 0; i < baseline.Count; i++)
            {
                Assert.AreEqual(baseline[i].TargetSeconds * 2f, far[i].TargetSeconds, 1e-3f);
                Assert.AreEqual(baseline[i].Skill, far[i].Skill, 1e-3f); // twice the length, twice the time: the same skill
            }
        }

        [Test]
        public void Profile_FallbackReference_ReplacesTheConfigsOnlyWhenSet()
        {
            Set("raceRandomRatio", 0f);
            float configFallback = _config.FallbackReferenceSeconds;
            var asConfig = BotFieldPlanner.Plan(_config, null, new System.Random(2));
            var noFallback = BotFieldPlanner.Plan(_config, null, new System.Random(2), Longer(FarEvent, 1f, 0f));
            for (int i = 0; i < asConfig.Count; i++) Assert.AreEqual(asConfig[i].TargetSeconds, noFallback[i].TargetSeconds, 1e-4f);

            var set = BotFieldPlanner.Plan(_config, null, new System.Random(2), Longer(FarEvent, 1f, configFallback * 2f));
            var asked = BotFieldPlanner.Plan(_config, configFallback * 2f, new System.Random(2), Longer(FarEvent, 1f, configFallback * 2f));
            for (int i = 0; i < set.Count; i++) Assert.AreEqual(asked[i].TargetSeconds, set[i].TargetSeconds, 1e-4f);
            Assert.AreNotEqual(asConfig[0].TargetSeconds, set[0].TargetSeconds);
        }

        [Test]
        public void ReferenceSeconds_IsScaledOnlyWithoutProfile()
        {
            BotEventProfile profile = Longer(FarEvent, 2f);
            Assert.AreEqual(50f, BotEventSetup.ReferenceSeconds(profile, 50.0, 400f).Value, 1e-4f);
            Assert.AreEqual(50f * BotEventSetup.BaseTableMeters / 400f, BotEventSetup.ReferenceSeconds(null, 50.0, 400f).Value, 1e-4f);
            Assert.AreEqual(17f, BotEventSetup.ReferenceSeconds(null, 17.0, BotEventSetup.BaseTableMeters).Value, 1e-4f); // 100 m: unchanged
            Assert.IsNull(BotEventSetup.ReferenceSeconds(profile, null, 400f));
            Assert.IsNull(BotEventSetup.ReferenceSeconds(profile, 0.0, 400f));
            Assert.IsNull(BotEventSetup.ReferenceSeconds(null, -3.0, 100f));
        }

        // ---- the bot's jump rule ----

        private static bool Ask(BotJumpDriver d, int index, float hurdle, float distance, float speed = 8f, bool inJump = false, float mistake = 0f)
            => d.ShouldJump(index, hurdle, distance, speed, inJump, windupSeconds: 0.1f, airSeconds: 0.2f, jitterSeconds: 0.02f, mistakeChance: mistake);

        [Test]
        public void Jump_NotAskedFarAway_AskedOnceNearTheHurdle()
        {
            var d = new BotJumpDriver(new System.Random(1));
            Assert.IsFalse(Ask(d, 0, 50f, 0f));
            int asked = 0;
            for (float x = 0f; x < 50f; x += 0.05f) if (Ask(d, 0, 50f, x)) asked++;
            Assert.AreEqual(1, asked);
        }

        [Test]
        public void Jump_NextHurdleIsPlannedAgain_AndReset_Forgets()
        {
            var d = new BotJumpDriver(new System.Random(1));
            for (float x = 0f; x < 50f; x += 0.05f) Ask(d, 0, 50f, x);
            Assert.IsFalse(Ask(d, 0, 50f, 49.9f));
            bool second = false;
            for (float x = 50f; x < 60f && !second; x += 0.05f) second = Ask(d, 1, 59f, x);
            Assert.IsTrue(second);

            d.Reset();
            bool again = false;
            for (float x = 0f; x < 50f && !again; x += 0.05f) again = Ask(d, 0, 50f, x);
            Assert.IsTrue(again);
        }

        [Test]
        public void Jump_NotWhileInTheAirOrStanding()
        {
            var d = new BotJumpDriver(new System.Random(1));
            Assert.IsFalse(Ask(d, 0, 50f, 49.9f, inJump: true));
            Assert.IsFalse(Ask(d, 0, 50f, 49.9f, speed: 0f));
        }

        [Test]
        public void Jump_SameSeed_SameTiming_AndAMistakeMovesItByAFlight()
        {
            float Timing(int seed, float mistake)
            {
                var d = new BotJumpDriver(new System.Random(seed));
                for (float x = 0f; x < 50f; x += 0.01f) if (Ask(d, 0, 50f, x, mistake: mistake)) return x;
                return -1f;
            }
            Assert.AreEqual(Timing(3, 0f), Timing(3, 0f));
            // Always a mistake: a flight (0.2 s at 8 m/s = 1.6 m) early or late, far beyond the 0.02 s jitter (0.16 m).
            float clean = Timing(3, 0f);
            Assert.Greater(System.Math.Abs(Timing(3, 1f) - clean), 1f);
        }

        // ---- hurdle layouts of the event assets ----

        private static IEnumerable<RaceEventDefinition> HurdleEvents()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RaceEventDefinition", new[] { "Assets/Speed-Staes/Data/Config/Events" }))
            {
                var def = AssetDatabase.LoadAssetAtPath<RaceEventDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && def.HurdleCount > 0) yield return def;
            }
        }

        [Test]
        public void HurdleEvents_LastHurdleIsBeforeTheFinishLine_WithRoomToRun()
        {
            var events = HurdleEvents().ToList();
            Assert.Greater(events.Count, 0, "no hurdle event assets found");
            foreach (RaceEventDefinition def in events)
            {
                float last = def.FirstHurdleMeters + def.HurdleSpacingMeters * (def.HurdleCount - 1);
                Assert.Greater(def.FirstHurdleMeters, 0f, def.Id.Value);
                Assert.Greater(def.HurdleSpacingMeters, 0f, def.Id.Value);
                Assert.Less(last, def.DistanceMeters, def.Id.Value + ": the last hurdle is on or past the finish line");
                Assert.Greater(def.DistanceMeters - last, 0.5f * def.HurdleSpacingMeters, def.Id.Value + ": hurdle too close to the line");
            }
        }
    }
}
