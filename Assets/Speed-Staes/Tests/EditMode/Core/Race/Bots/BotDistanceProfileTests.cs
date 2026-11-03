using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race.Bots;
using UnityEngine;

namespace SpeedStars.Tests.RaceBots
{
    /// <summary>Profile choice by race length for an event without its own table (Race of the Day, T-087). Relations only, no balance numbers.</summary>
    public sealed class BotDistanceProfileTests
    {
        private BotRaceConfig _config;
        private List<BotEventSetup.FlatEvent> _flat;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BotRaceConfig>();
            _config.SetCalibration(new[] { new BotCalibrationPoint(0f, 14f), new BotCalibrationPoint(1f, 10f) });
            _config.SetProfile(Table("200m", 2.1f));
            _config.SetProfile(Table("400m", 4.6f));
            _flat = new List<BotEventSetup.FlatEvent>
            {
                new BotEventSetup.FlatEvent("100m", 100f),
                new BotEventSetup.FlatEvent("200m", 200f),
                new BotEventSetup.FlatEvent("400m", 400f),
            };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private static BotEventProfile Table(string id, float factor) =>
            new BotEventProfile(id, 12f * factor, new[] { new BotCalibrationPoint(0f, 14f * factor), new BotCalibrationPoint(1f, 10f * factor) });

        private float Seconds(BotEventProfile profile, float skill) =>
            BotSkillCurve.SecondsForSkill(profile != null ? profile.Calibration : _config.Calibration, skill);

        [Test]
        public void AnEventWithItsOwnProfile_KeepsIt_WhateverTheLength()
        {
            BotEventProfile chosen = BotEventSetup.ForDistance(_config, "200m", 123f, _flat);

            Assert.AreSame(BotEventSetup.Find(_config, "200m"), chosen);
        }

        [Test]
        public void OrdinaryHundred_StaysOnTheBaseTable()
        {
            Assert.IsNull(BotEventSetup.ForDistance(_config, "100m", 100f, _flat));
            Assert.IsNull(BotEventSetup.ForDistance(_config, "race_of_the_day", 100f, _flat), "same length as the base table");
        }

        [Test]
        public void SameLengthAsAFlatEvent_GivesThatTable()
        {
            Assert.AreSame(BotEventSetup.Find(_config, "200m"), BotEventSetup.ForDistance(_config, "race_of_the_day", 200f, _flat));
            Assert.AreSame(BotEventSetup.Find(_config, "400m"), BotEventSetup.ForDistance(_config, "race_of_the_day", 400f, _flat));
        }

        [Test]
        public void BetweenTwoTables_TheTimesLieBetweenThem_AndGrowWithTheLength()
        {
            BotEventProfile at150 = BotEventSetup.ForDistance(_config, "race_of_the_day", 150f, _flat);
            BotEventProfile at300 = BotEventSetup.ForDistance(_config, "race_of_the_day", 300f, _flat);
            BotEventProfile p200 = BotEventSetup.Find(_config, "200m");
            BotEventProfile p400 = BotEventSetup.Find(_config, "400m");

            foreach (float skill in new[] { 0f, 0.5f, 1f })
            {
                Assert.Greater(Seconds(at150, skill), Seconds(null, skill));
                Assert.Less(Seconds(at150, skill), Seconds(p200, skill));
                Assert.Greater(Seconds(at300, skill), Seconds(p200, skill));
                Assert.Less(Seconds(at300, skill), Seconds(p400, skill));
                Assert.Less(Seconds(at150, skill), Seconds(at300, skill));
            }
            Assert.Greater(at150.FallbackReferenceSeconds, 0f);
        }

        [Test]
        public void ShorterThanTheBase_ScalesTheBaseTableByTheRatio()
        {
            BotEventProfile at60 = BotEventSetup.ForDistance(_config, "race_of_the_day", 60f, _flat);

            foreach (float skill in new[] { 0f, 0.5f, 1f })
                Assert.AreEqual(Seconds(null, skill) * 0.6f, Seconds(at60, skill), 1e-3f);
        }

        [Test]
        public void LongerThanTheLongest_ScalesTheLongestTable()
        {
            BotEventProfile at800 = BotEventSetup.ForDistance(_config, "race_of_the_day", 800f, _flat);

            Assert.AreEqual(Seconds(BotEventSetup.Find(_config, "400m"), 0.5f) * 2f, Seconds(at800, 0.5f), 1e-3f);
        }

        [Test]
        public void WithoutLengthOrFlatEvents_FallsBackToTheOldBehaviour()
        {
            Assert.IsNull(BotEventSetup.ForDistance(_config, "race_of_the_day", 0f, _flat));
            Assert.IsNull(BotEventSetup.ForDistance(null, "race_of_the_day", 150f, _flat));
            // No flat tables to interpolate with: only the base remains, so it is scaled by the ratio.
            BotEventProfile alone = BotEventSetup.ForDistance(_config, "race_of_the_day", 150f, null);
            Assert.AreEqual(Seconds(null, 0.5f) * 1.5f, Seconds(alone, 0.5f), 1e-3f);
        }
    }
}
