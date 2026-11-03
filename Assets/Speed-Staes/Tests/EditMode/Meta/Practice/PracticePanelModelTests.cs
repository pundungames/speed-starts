using System;
using NUnit.Framework;
using SpeedStars.Meta.Practice;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Practice
{
    /// <summary>What the Practice panel shows (T-090): visibility, labels, grey rows, and which change restarts the attempt.</summary>
    public sealed class PracticePanelModelTests
    {
        private PracticeConfig _config;
        private FakeControl _control;

        [SetUp]
        public void SetUp()
        {
            _config = PracticeConfig.CreateDefault();
            _control = new FakeControl(new PracticeOptions(_config, null));
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_config);

        [Test]
        public void The_Panel_Is_Visible_In_Practice_Only()
        {
            var model = new PracticePanelModel(_control);

            _control.IsPractice = false;
            Assert.IsFalse(model.Visible);
            _control.IsPractice = true;
            Assert.IsTrue(model.Visible);
            Assert.IsFalse(new PracticePanelModel(null).Visible);
        }

        [Test]
        public void Labels_Name_The_Distance_In_Meters()
        {
            var model = new PracticePanelModel(_control);

            Assert.AreEqual("100m", PracticePanelModel.DistanceLabel(100f));
            Assert.AreEqual(PracticePanelModel.DistanceLabel(_config.DefaultDistance), model.SelectedDistanceLabel);
            Assert.AreEqual(_config.Distances.Count, model.Distances.Count);
        }

        [Test]
        public void Rows_Without_A_Mechanic_Are_Grey_And_Read_Off_Even_When_Saved_On()
        {
            _control.Options.SetLockEnergy(true);
            _control.Options.SetStandingStart(true);
            var model = new PracticePanelModel(_control);

            Assert.IsFalse(model.LockEnergyEnabled);
            Assert.IsFalse(model.LockEnergyOn);
            Assert.IsFalse(model.StandingStartEnabled);
            Assert.IsFalse(model.StandingStartOn);
            Assert.IsFalse(model.ResetEnergyEnabled);
        }

        [Test]
        public void Rows_With_A_Mechanic_Follow_The_Saved_Choice()
        {
            _control.CanLockEnergy = true;
            _control.CanStandingStart = true;
            _control.CanResetEnergy = true;
            _control.Options.SetLockEnergy(true);
            var model = new PracticePanelModel(_control);

            Assert.IsTrue(model.LockEnergyEnabled);
            Assert.IsTrue(model.LockEnergyOn);
            Assert.IsTrue(model.StandingStartEnabled);
            Assert.IsFalse(model.StandingStartOn);
            Assert.IsTrue(model.ResetEnergyEnabled);
        }

        [Test]
        public void Hurdles_Row_Is_Always_Available()
        {
            var model = new PracticePanelModel(_control);

            Assert.IsFalse(model.HurdlesOn);
            _control.Options.SetHurdles(true);
            Assert.IsTrue(model.HurdlesOn);
        }

        [Test]
        public void Changes_That_Shape_The_Start_Restart_The_Attempt()
        {
            Assert.IsTrue(PracticePanelModel.RestartsAttempt(PracticeChange.Distance));
            Assert.IsTrue(PracticePanelModel.RestartsAttempt(PracticeChange.Hurdles));
            Assert.IsTrue(PracticePanelModel.RestartsAttempt(PracticeChange.StandingStart));
            Assert.IsFalse(PracticePanelModel.RestartsAttempt(PracticeChange.LockEnergy));
        }

        private sealed class FakeControl : IPracticeControl
        {
            public FakeControl(PracticeOptions options) => Options = options;

            public bool IsPractice { get; set; } = true;
            public PracticeOptions Options { get; }
            public bool CanLockEnergy { get; set; }
            public bool CanResetEnergy { get; set; }
            public bool CanStandingStart { get; set; }
            public void ResetEnergy() { }
            public event Action Changed { add { } remove { } }
        }
    }
}
