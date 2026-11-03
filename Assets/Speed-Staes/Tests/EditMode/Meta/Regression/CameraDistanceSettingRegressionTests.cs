using NUnit.Framework;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// T-082 scenario 1: the camera distance of the settings window across a restart and the progress reset. The range and the
    /// default are test inputs written into the harness' settings config; tests assert relations to them, never the shipped values.
    /// </summary>
    public sealed class CameraDistanceSettingRegressionTests
    {
        private const float TestMin = 0.5f;
        private const float TestMax = 2f;
        private const float TestDefault = 0.8f;

        private RegressionHarness _h;

        [SetUp]
        public void SetUp()
        {
            _h = new RegressionHarness();
            ConfigFixture.SetFloat(_h.SettingsConfig, "minCameraDistance", TestMin);
            ConfigFixture.SetFloat(_h.SettingsConfig, "maxCameraDistance", TestMax);
            ConfigFixture.SetFloat(_h.SettingsConfig, "defaultCameraDistance", TestDefault);
        }

        [TearDown]
        public void TearDown() => _h.Dispose();

        /// <summary>A value inside the range that is not the default, so a read that fell back to the default would show.</summary>
        private float Chosen => (_h.SettingsConfig.MinCameraDistance + _h.SettingsConfig.MaxCameraDistance) * 0.5f + 0.1f;

        [Test]
        public void NeverTouched_ReadsTheConfigDefault_AfterARestart()
        {
            _h.Boot();

            var r = _h.Boot();

            Assert.AreEqual(_h.SettingsConfig.DefaultCameraDistance, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void Chosen_IsInsideTheRangeAndNotTheDefault_SoTheTestsBelowMeanSomething()
        {
            Assert.GreaterOrEqual(Chosen, _h.SettingsConfig.MinCameraDistance);
            Assert.LessOrEqual(Chosen, _h.SettingsConfig.MaxCameraDistance);
            Assert.Greater(System.Math.Abs(_h.SettingsConfig.DefaultCameraDistance - Chosen), 1e-4f);
        }

        [Test]
        public void SetDistance_SurvivesARestart()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(Chosen);

            var r = _h.Boot();

            Assert.AreEqual(Chosen, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void SetDistance_IsSavedWithoutAnExplicitSave()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(Chosen);

            // No Repository.Save() here: killing the app right after the slider moves must keep the value.
            var r = _h.Boot();

            Assert.AreEqual(Chosen, r.Progress.CameraDistance, 1e-5f);
        }

        [Test]
        public void ResetAll_ThenRestart_KeepsTheCameraDistance()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(Chosen);

            s.Reset.ResetAll();
            var r = _h.Boot();

            Assert.AreEqual(Chosen, r.Settings.CameraDistance, 1e-5f, "the progress reset keeps the player's own settings");
        }

        [Test]
        public void ResetPersonalBests_ThenRestart_KeepsTheCameraDistance()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(Chosen);

            s.Reset.ResetPersonalBests();
            var r = _h.Boot();

            Assert.AreEqual(Chosen, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void ResetAll_WhenNeverTouched_StillReadsTheConfigDefault()
        {
            var s = _h.Boot();

            s.Reset.ResetAll();
            var r = _h.Boot();

            Assert.AreEqual(_h.SettingsConfig.DefaultCameraDistance, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void ValueAboveTheRange_IsKeptAtTheMax_AlsoAfterARestart()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(_h.SettingsConfig.MaxCameraDistance * 10f);

            Assert.AreEqual(_h.SettingsConfig.MaxCameraDistance, s.Settings.CameraDistance, 1e-5f);
            var r = _h.Boot();
            Assert.AreEqual(_h.SettingsConfig.MaxCameraDistance, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void ValueBelowTheRange_IsKeptAtTheMin_AlsoAfterARestart()
        {
            var s = _h.Boot();
            s.Settings.SetCameraDistance(-5f);

            Assert.AreEqual(_h.SettingsConfig.MinCameraDistance, s.Settings.CameraDistance, 1e-5f);
            var r = _h.Boot();
            Assert.AreEqual(_h.SettingsConfig.MinCameraDistance, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void BothRangeEnds_AreReachable_AndTheRangeIsOrdered()
        {
            Assert.Less(_h.SettingsConfig.MinCameraDistance, _h.SettingsConfig.MaxCameraDistance);
            var s = _h.Boot();

            s.Settings.SetCameraDistance(_h.SettingsConfig.MaxCameraDistance);
            Assert.AreEqual(_h.SettingsConfig.MaxCameraDistance, s.Settings.CameraDistance, 1e-5f);
            s.Settings.SetCameraDistance(_h.SettingsConfig.MinCameraDistance);
            Assert.AreEqual(_h.SettingsConfig.MinCameraDistance, s.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void ANonPositiveStoredValue_IsReadAsNeverTouched_NotAsADistance()
        {
            var s = _h.Boot();
            s.Progress.CameraDistance = -3f; // a damaged or hand-edited save
            s.Repository.Save();

            var r = _h.Boot();

            Assert.AreEqual(_h.SettingsConfig.DefaultCameraDistance, r.Settings.CameraDistance, 1e-5f);
        }

        [Test]
        public void ChangingTheCameraDistance_DoesNotTouchTheOtherSettings()
        {
            var s = _h.Boot();
            s.Settings.SetMusic(false);
            s.Settings.TrySetName("Ada");

            s.Settings.SetCameraDistance(Chosen);
            var r = _h.Boot();

            Assert.IsFalse(r.Settings.MusicOn);
            Assert.AreEqual("Ada", r.Settings.PlayerName);
            Assert.AreEqual(Chosen, r.Settings.CameraDistance, 1e-5f);
        }
    }
}
