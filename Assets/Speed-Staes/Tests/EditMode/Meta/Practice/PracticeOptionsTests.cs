using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Practice;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Practice
{
    /// <summary>
    /// The Practice choices (T-090): snapping to the configured distances, saving between runs, and the hurdle layout of the chosen
    /// distance. Numbers are read from the config, never written here.
    /// </summary>
    public sealed class PracticeOptionsTests
    {
        private PracticeConfig _config;

        [SetUp]
        public void SetUp() => _config = PracticeConfig.CreateDefault();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void New_Options_Start_At_The_Configured_Default_Distance_With_Everything_Off()
        {
            var options = new PracticeOptions(_config, null);

            Assert.AreEqual(_config.DefaultDistance, options.DistanceMeters);
            Assert.AreEqual(0, options.DistanceIndex);
            Assert.IsFalse(options.Hurdles);
            Assert.IsFalse(options.LockEnergy);
            Assert.IsFalse(options.StandingStart);
        }

        [Test]
        public void Every_Configured_Distance_Can_Be_Chosen_By_Index()
        {
            var options = new PracticeOptions(_config, null);

            for (int i = 0; i < _config.Distances.Count; i++)
            {
                options.SetDistanceIndex(i);
                Assert.AreEqual(_config.Distances[i], options.DistanceMeters);
                Assert.AreEqual(i, options.DistanceIndex);
            }
        }

        [Test]
        public void An_Odd_Distance_Snaps_To_The_Nearest_Configured_One()
        {
            var options = new PracticeOptions(_config, null);
            float first = _config.Distances[0];
            float second = _config.Distances[1];

            options.SetDistance(second + 1f);

            Assert.AreEqual(second, options.DistanceMeters);
            Assert.AreNotEqual(first, options.DistanceMeters);
        }

        [Test]
        public void Index_Outside_The_List_Is_Refused()
        {
            var options = new PracticeOptions(_config, null);

            Assert.IsFalse(options.SetDistanceIndex(-1));
            Assert.IsFalse(options.SetDistanceIndex(_config.Distances.Count));
            Assert.AreEqual(_config.DefaultDistance, options.DistanceMeters);
        }

        [Test]
        public void Changed_Fires_Once_Per_Real_Change_Only()
        {
            var options = new PracticeOptions(_config, null);
            int raised = 0;
            options.Changed += () => raised++;

            Assert.IsTrue(options.SetHurdles(true));
            Assert.IsFalse(options.SetHurdles(true));
            Assert.IsTrue(options.SetDistanceIndex(1));
            Assert.IsFalse(options.SetDistanceIndex(1));

            Assert.AreEqual(2, raised);
        }

        [Test]
        public void Choices_Are_Remembered_By_The_Next_Options()
        {
            var store = new MemoryStore();
            var first = new PracticeOptions(_config, store);
            first.SetDistanceIndex(_config.Distances.Count - 1);
            first.SetHurdles(true);
            first.SetLockEnergy(true);
            first.SetStandingStart(true);

            var second = new PracticeOptions(_config, store);

            Assert.AreEqual(_config.Distances[_config.Distances.Count - 1], second.DistanceMeters);
            Assert.IsTrue(second.Hurdles);
            Assert.IsTrue(second.LockEnergy);
            Assert.IsTrue(second.StandingStart);
        }

        [Test]
        public void A_Saved_Distance_That_Left_The_Config_Snaps_To_A_Listed_One()
        {
            var store = new MemoryStore();
            store.Write(PracticeOptions.SaveKey, JsonUtility.ToJson(new PracticeSelection { distanceMeters = _config.Distances[1] + 3f }));

            var options = new PracticeOptions(_config, store);

            Assert.AreEqual(_config.Distances[1], options.DistanceMeters);
        }

        [Test]
        public void A_Broken_Save_Reads_As_The_Defaults()
        {
            var store = new MemoryStore();
            store.Write(PracticeOptions.SaveKey, "{not json");

            var options = new PracticeOptions(_config, store);

            Assert.AreEqual(_config.DefaultDistance, options.DistanceMeters);
            Assert.IsFalse(options.Hurdles);
        }

        [Test]
        public void Hurdle_Layout_Appears_Only_With_The_Box_On()
        {
            var options = new PracticeOptions(_config, null);

            Assert.IsFalse(options.TryGetHurdles(out _));
            options.SetHurdles(true);
            Assert.IsTrue(options.TryGetHurdles(out PracticeConfig.HurdleLayout layout));
            Assert.Greater(layout.Count, 0);
        }

        [Test]
        public void Every_Distance_Has_A_Hurdle_Layout_That_Fits_Before_The_Finish()
        {
            var options = new PracticeOptions(_config, null);
            options.SetHurdles(true);

            for (int i = 0; i < _config.Distances.Count; i++)
            {
                options.SetDistanceIndex(i);
                Assert.IsTrue(options.TryGetHurdles(out PracticeConfig.HurdleLayout layout), $"no layout for {_config.Distances[i]}");
                Assert.Greater(layout.FirstMeters, 0f);
                Assert.Greater(layout.SpacingMeters, 0f);
                Assert.Less(layout.LastMeters, _config.Distances[i], $"last hurdle past the finish at {_config.Distances[i]}");
            }
        }

        [Test]
        public void Longer_Races_Do_Not_Pack_Hurdles_Closer_Than_The_Shortest_One()
        {
            var shortest = new PracticeOptions(_config, null);
            shortest.SetHurdles(true);
            shortest.TryGetHurdles(out PracticeConfig.HurdleLayout shortLayout);

            var longest = new PracticeOptions(_config, null);
            longest.SetHurdles(true);
            longest.SetDistanceIndex(_config.Distances.Count - 1);
            longest.TryGetHurdles(out PracticeConfig.HurdleLayout longLayout);

            Assert.GreaterOrEqual(longLayout.SpacingMeters, shortLayout.SpacingMeters);
        }

        private sealed class MemoryStore : ISaveStore
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public bool TryRead(string key, out string contents) => _files.TryGetValue(key, out contents);
            public void Write(string key, string contents) => _files[key] = contents;
            public void Delete(string key) => _files.Remove(key);
        }
    }
}
