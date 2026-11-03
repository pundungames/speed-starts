using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Core.Race.Bots;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Settings;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Regression;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Settings
{
    /// <summary>
    /// T-088 settings v2: competitors, lane, hurdle sensitivity, particles, emote switches and the new resets. Config values are
    /// test inputs; assertions relate results to them or to each other, never to the shipped asset.
    /// </summary>
    public sealed class SettingsV2Tests
    {
        private RegressionHarness _h;

        [SetUp]
        public void SetUp() => _h = new RegressionHarness();

        [TearDown]
        public void TearDown() => _h.Dispose();

        // ---------------------------------------------------------------- defaults and old saves

        [Test]
        public void NewPlayer_ReadsEveryDefaultFromTheConfig()
        {
            ConfigFixture.SetBool(_h.SettingsConfig, "defaultTimeTrial", true);
            ConfigFixture.SetBool(_h.SettingsConfig, "defaultParticleEffects", false);
            ConfigFixture.SetBool(_h.SettingsConfig, "defaultShowEmoteButton", false);
            ConfigFixture.SetBool(_h.SettingsConfig, "defaultAutoEmoteSelf", true);
            ConfigFixture.SetBool(_h.SettingsConfig, "defaultAutoEmoteCompetitors", true);
            ConfigFixture.SetFloat(_h.SettingsConfig, "defaultCompetitorDifficulty", 0.2f);
            ConfigFixture.SetFloat(_h.SettingsConfig, "defaultCompetitorSkillRange", 0.7f);
            ConfigFixture.SetFloat(_h.SettingsConfig, "defaultHurdleSensitivity", 0.9f);

            IPlayerSettings s = _h.Boot().Settings;

            Assert.AreEqual(CompetitorType.TimeTrial, s.CompetitorType);
            Assert.IsFalse(s.ParticleEffects);
            Assert.IsFalse(s.ShowEmoteButton);
            Assert.IsTrue(s.AutoEmoteSelf);
            Assert.IsTrue(s.AutoEmoteCompetitors);
            Assert.AreEqual(0.2f, s.CompetitorDifficulty, 1e-5f);
            Assert.AreEqual(0.7f, s.CompetitorSkillRange, 1e-5f);
            Assert.AreEqual(0.9f, s.HurdleSensitivity, 1e-5f);
            Assert.AreEqual(0, s.LanePreference, "0 = automatic");
            Assert.IsFalse(s.HasCompetitorTuning, "untouched: the bot config decides");
        }

        [Test]
        public void AnOldSave_WithoutTheNewKeys_OpensWithTheConfigDefaults()
        {
            _h.Store.Write(ProgressRepository.SaveKey, "{\"schemaVersion\":2,\"cash\":5,\"musicChoice\":2}");

            var session = _h.Boot();
            IPlayerSettings s = session.Settings;

            Assert.AreEqual(ProgressLoadStatus.Loaded, session.Repository.Status);
            Assert.IsFalse(s.MusicOn, "an old setting is still read");
            Assert.IsFalse(s.HasCompetitorTuning);
            Assert.AreEqual(_h.SettingsConfig.DefaultCompetitorDifficulty, s.CompetitorDifficulty, 1e-5f);
            Assert.AreEqual(_h.SettingsConfig.DefaultHurdleSensitivity, s.HurdleSensitivity, 1e-5f);
            Assert.AreEqual(_h.SettingsConfig.DefaultParticleEffects, s.ParticleEffects);
            Assert.AreEqual(_h.SettingsConfig.DefaultShowEmoteButton, s.ShowEmoteButton);
            Assert.AreEqual(0, s.LanePreference);
            Assert.AreEqual(_h.SettingsConfig.DefaultTimeTrial ? CompetitorType.TimeTrial : CompetitorType.Bots, s.CompetitorType);
        }

        [Test]
        public void TheSettingsDefaults_MatchTheBotConfigDefaults()
        {
            var bots = ScriptableObject.CreateInstance<BotRaceConfig>();
            try
            {
                Assert.AreEqual(bots.Difficulty, _h.SettingsConfig.DefaultCompetitorDifficulty, 1e-5f);
                Assert.AreEqual(bots.SkillRange, _h.SettingsConfig.DefaultCompetitorSkillRange, 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(bots);
            }
        }

        // ---------------------------------------------------------------- saved and kept

        private static void SetAll(IPlayerSettings s)
        {
            s.SetCompetitorType(CompetitorType.TimeTrial);
            s.SetCompetitorDifficulty(0.15f);
            s.SetCompetitorSkillRange(0.85f);
            s.SetLanePreference(2);
            s.SetHurdleSensitivity(0.35f);
            s.SetParticleEffects(false);
            s.SetShowEmoteButton(false);
            s.SetAutoEmoteSelf(true);
            s.SetAutoEmoteCompetitors(true);
        }

        private static void AssertAll(IPlayerSettings s)
        {
            Assert.AreEqual(CompetitorType.TimeTrial, s.CompetitorType);
            Assert.AreEqual(0.15f, s.CompetitorDifficulty, 1e-5f);
            Assert.AreEqual(0.85f, s.CompetitorSkillRange, 1e-5f);
            Assert.IsTrue(s.HasCompetitorTuning);
            Assert.AreEqual(2, s.LanePreference);
            Assert.AreEqual(0.35f, s.HurdleSensitivity, 1e-5f);
            Assert.IsFalse(s.ParticleEffects);
            Assert.IsFalse(s.ShowEmoteButton);
            Assert.IsTrue(s.AutoEmoteSelf);
            Assert.IsTrue(s.AutoEmoteCompetitors);
        }

        [Test]
        public void EverySetting_SurvivesARestart_WithoutAnExplicitSave()
        {
            ConfigFixture.SetInt(_h.SettingsConfig, "laneChoiceCount", 4);
            SetAll(_h.Boot().Settings);

            AssertAll(_h.Boot().Settings);
        }

        [Test]
        public void RestartAllProgress_KeepsEverySetting()
        {
            ConfigFixture.SetInt(_h.SettingsConfig, "laneChoiceCount", 4);
            var s = _h.Boot();
            SetAll(s.Settings);

            s.Reset.ResetAll();

            AssertAll(_h.Boot().Settings);
        }

        [Test]
        public void ResetPersonalBests_KeepsEverySetting()
        {
            ConfigFixture.SetInt(_h.SettingsConfig, "laneChoiceCount", 4);
            var s = _h.Boot();
            SetAll(s.Settings);

            s.Reset.ResetPersonalBests();

            AssertAll(_h.Boot().Settings);
        }

        [Test]
        public void ChangingAValue_SavesAndRaisesChangedOnce_TheSameValueDoesNeither()
        {
            IPlayerSettings s = _h.Boot().Settings;
            int changed = 0;
            s.Changed += () => changed++;

            s.SetCompetitorDifficulty(0.4f);
            Assert.AreEqual(1, changed);
            s.SetCompetitorDifficulty(0.4f);
            s.SetParticleEffects(s.ParticleEffects);
            s.SetShowEmoteButton(s.ShowEmoteButton);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void ValuesOutsideTheirRange_AreKeptInside()
        {
            ConfigFixture.SetInt(_h.SettingsConfig, "laneChoiceCount", 3);
            IPlayerSettings s = _h.Boot().Settings;

            s.SetCompetitorDifficulty(4f);
            s.SetCompetitorSkillRange(-3f);
            s.SetHurdleSensitivity(9f);
            s.SetLanePreference(99);

            Assert.AreEqual(1f, s.CompetitorDifficulty, 1e-5f);
            Assert.AreEqual(0f, s.CompetitorSkillRange, 1e-5f);
            Assert.AreEqual(1f, s.HurdleSensitivity, 1e-5f);
            Assert.AreEqual(s.LaneChoiceCount, s.LanePreference);
            s.SetLanePreference(-5);
            Assert.AreEqual(0, s.LanePreference);
        }

        [Test]
        public void ASliderAtZero_IsATouchedValue_NotTheDefault()
        {
            ConfigFixture.SetFloat(_h.SettingsConfig, "defaultCompetitorDifficulty", 0.6f);
            IPlayerSettings s = _h.Boot().Settings;

            s.SetCompetitorDifficulty(0f);

            Assert.AreEqual(0f, s.CompetitorDifficulty, 1e-5f);
            Assert.IsTrue(s.HasCompetitorTuning);
            Assert.AreEqual(0f, _h.Boot().Settings.CompetitorDifficulty, 1e-5f);
        }

        // ---------------------------------------------------------------- hurdle sensitivity

        [Test]
        public void MoreSensitivity_MeansAShorterSwipe_InsideTheConfiguredRange()
        {
            IPlayerSettings s = _h.Boot().Settings;

            s.SetHurdleSensitivity(0f);
            float least = s.SwipeUpDistance;
            s.SetHurdleSensitivity(0.5f);
            float middle = s.SwipeUpDistance;
            s.SetHurdleSensitivity(1f);
            float most = s.SwipeUpDistance;

            Assert.Greater(least, middle);
            Assert.Greater(middle, most);
            Assert.AreEqual(_h.SettingsConfig.SwipeDistanceAtZeroSensitivity, least, 1e-5f);
            Assert.AreEqual(_h.SettingsConfig.SwipeDistanceAtFullSensitivity, most, 1e-5f);
        }

        [Test]
        public void SwipeDistanceRule_InterpolatesAndClamps()
        {
            Assert.AreEqual(0.1f, SettingsRules.SwipeDistance(0f, 0.02f, 0.1f), 1e-6f);
            Assert.AreEqual(0.02f, SettingsRules.SwipeDistance(1f, 0.02f, 0.1f), 1e-6f);
            Assert.AreEqual(0.06f, SettingsRules.SwipeDistance(0.5f, 0.02f, 0.1f), 1e-6f);
            Assert.AreEqual(0.02f, SettingsRules.SwipeDistance(7f, 0.02f, 0.1f), 1e-6f);
        }

        // ---------------------------------------------------------------- the new resets

        [Test]
        public void ResetOnePersonalBest_RemovesOnlyThatEventsRecordAndDayBests()
        {
            var s = _h.Boot();
            var a = new RaceEventId("ev-a");
            var b = new RaceEventId("ev-b");
            s.Progress.BestTimes.Add(new EventBestTime(a, 10.0));
            s.Progress.BestTimes.Add(new EventBestTime(b, 20.0));
            s.Progress.DayBests.Add(new DayBestEntry(a, 1L, 10000L, 2L));
            s.Progress.DayBests.Add(new DayBestEntry(b, 1L, 20000L, 2L));
            s.Progress.DayBests.Add(new DayBestEntry(b, 3L, 21000L, 4L));
            s.Progress.Cash = 4321;

            s.Reset.ResetPersonalBest(a);

            var r = _h.Boot();
            Assert.AreEqual(1, r.Progress.BestTimes.Count);
            Assert.AreEqual(b, r.Progress.BestTimes[0].EventId);
            Assert.AreEqual(2, r.Progress.DayBests.Count);
            Assert.AreEqual(b, r.Progress.DayBests[0].EventId);
            Assert.AreEqual(4321, r.Progress.Cash, "nothing else changes");
        }

        [Test]
        public void ResetOnePersonalBest_RaisesTheResetChanged()
        {
            var s = _h.Boot();
            int changed = 0;
            s.Reset.Changed += () => changed++;

            s.Reset.ResetPersonalBest(new RaceEventId("ev-a"));

            Assert.AreEqual(1, changed);
        }

        [Test]
        public void ClearCustomRacers_EmptiesThePresets_KeepsTheItemsAndTheSettings()
        {
            var s = _h.Boot();
            var preset = new CosmeticPreset();
            preset.SkinColorHex = "112233";
            s.Progress.CosmeticPresets.Add(preset);
            s.Progress.CosmeticPresets.Add(new CosmeticPreset());
            s.Progress.SelectedPresetIndex = 1;
            s.Progress.OwnedCosmeticIds.Add(RegressionHarness.HatId);
            s.Settings.SetParticleEffects(false);

            s.Reset.ClearCustomRacers();

            var r = _h.Boot();
            CollectionAssert.IsEmpty(r.Progress.CosmeticPresets, "the cosmetic service refills the default outfit lazily");
            Assert.AreEqual(0, r.Progress.SelectedPresetIndex);
            CollectionAssert.Contains(r.Progress.OwnedCosmeticIds, RegressionHarness.HatId);
            Assert.IsFalse(r.Settings.ParticleEffects);
        }
    }
}
