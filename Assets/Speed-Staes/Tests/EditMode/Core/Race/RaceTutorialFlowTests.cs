using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.Tests.RaceStart
{
    /// <summary>The order of the first race's tutorial (RaceTutorialFlow), against the config's own numbers. Pure: no scene, no clock.</summary>
    public sealed class RaceTutorialFlowTests
    {
        private const int Left = 0;
        private const int Right = 1;

        private RaceTutorialConfig _config;
        private RaceTutorialFlow _flow;
        private List<RaceTutorialStep> _seen;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RaceTutorialConfig>();
            _flow = new RaceTutorialFlow(_config);
            _seen = new List<RaceTutorialStep>();
            _flow.StepChanged += _seen.Add;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private void ToFirstPrompt()
        {
            _flow.Begin();
            _flow.OnGo();
        }

        /// <summary>Taps through the prompts on the right sides, the strides in between.</summary>
        private void ToKeepGoing()
        {
            ToFirstPrompt();
            for (int i = 0; i < _config.PromptedSteps; i++)
            {
                _flow.OnPress(i % 2 == 0 ? Left : Right);
                if (i < _config.PromptedSteps - 1) _flow.Tick(_config.StrideSeconds, _config.StrideSeconds);
            }
        }

        [Test]
        public void Begins_WaitingForGo_AndGoAsksForTheLeftSide()
        {
            _flow.Begin();
            Assert.AreEqual(RaceTutorialStep.WaitForGo, _flow.Step);
            Assert.AreEqual(1f, _flow.TimeScale, "the countdown runs at normal speed");

            _flow.OnGo();

            Assert.AreEqual(RaceTutorialStep.TapLeft, _flow.Step);
        }

        [Test]
        public void Go_WithoutBegin_DoesNothing()
        {
            _flow.OnGo();
            Assert.AreEqual(RaceTutorialStep.None, _flow.Step);
            Assert.IsFalse(_flow.IsActive);
        }

        [Test]
        public void Prompt_SlowsTheGameDown_AndGivesTheSpeedBackOnTheTap()
        {
            ToFirstPrompt();
            _flow.Tick(0f, _config.HoldBlendSeconds);
            Assert.AreEqual(_config.HoldTimeScale, _flow.TimeScale, 0.0001f);

            _flow.OnPress(Left);

            Assert.AreEqual(1f, _flow.TimeScale, 0.0001f);
        }

        [Test]
        public void Prompt_TakesOnlyItsOwnSide()
        {
            ToFirstPrompt();
            Assert.IsTrue(_flow.AcceptsPress(Left));
            Assert.IsFalse(_flow.AcceptsPress(Right));

            _flow.OnPress(Right);

            Assert.AreEqual(RaceTutorialStep.TapLeft, _flow.Step, "the wrong side changes nothing");
        }

        [Test]
        public void NextPrompt_ComesAsTheFootLands_OnTheOtherSide()
        {
            ToFirstPrompt();
            _flow.OnPress(Left);
            Assert.AreEqual(RaceTutorialStep.Stride, _flow.Step);

            _flow.Tick(_config.StrideSeconds * 0.5f, 1000f);
            Assert.AreEqual(RaceTutorialStep.Stride, _flow.Step, "the stride counts game time, not real time");

            _flow.Tick(_config.StrideSeconds, 0f);
            Assert.AreEqual(RaceTutorialStep.TapRight, _flow.Step);
            Assert.AreEqual(1, _flow.PromptIndex);
            Assert.IsTrue(_flow.AcceptsPress(Right));
            Assert.IsFalse(_flow.AcceptsPress(Left));
        }

        [Test]
        public void AfterThePrompts_EverySideCounts_AndTheKeysAreLearned()
        {
            Assert.IsFalse(_flow.KeysLearned);

            ToKeepGoing();

            Assert.AreEqual(RaceTutorialStep.KeepGoing, _flow.Step);
            Assert.IsTrue(_flow.KeysLearned);
            Assert.IsTrue(_flow.AcceptsPress(Left));
            Assert.IsTrue(_flow.AcceptsPress(Right));
            Assert.AreEqual(1f, _flow.TimeScale);
        }

        [Test]
        public void KeepGoing_EndsOnEnoughTaps()
        {
            ToKeepGoing();
            for (int i = 0; i < _config.KeepGoingPresses - 1; i++) _flow.OnPress(i % 2);
            Assert.AreEqual(RaceTutorialStep.KeepGoing, _flow.Step);

            _flow.OnPress(Left);

            Assert.AreEqual(RaceTutorialStep.Rhythm, _flow.Step);
        }

        [Test]
        public void KeepGoing_EndsAnywayAfterItsSeconds_ThenRhythm_ThenDone()
        {
            ToKeepGoing();

            _flow.Tick(0f, _config.KeepGoingMaxSeconds + 0.01f);
            Assert.AreEqual(RaceTutorialStep.Rhythm, _flow.Step);
            Assert.AreEqual(1f, _flow.TimeScale, "slow motion here would turn a steady hand into mashing");

            _flow.Tick(0f, _config.RhythmSeconds + 0.01f);
            Assert.AreEqual(RaceTutorialStep.Done, _flow.Step);
            Assert.IsFalse(_flow.IsActive);
        }

        [Test]
        public void Begin_AgainMidway_StartsFromTheTop()
        {
            ToFirstPrompt();
            _flow.OnPress(Left);

            _flow.Begin();
            _flow.OnGo();

            Assert.AreEqual(RaceTutorialStep.TapLeft, _flow.Step);
            Assert.AreEqual(0, _flow.PromptIndex);
        }

        [Test]
        public void Stop_LeavesNothingWaiting()
        {
            ToFirstPrompt();

            _flow.Stop();

            Assert.AreEqual(RaceTutorialStep.None, _flow.Step);
            Assert.AreEqual(1f, _flow.TimeScale);
            Assert.IsTrue(_flow.AcceptsPress(Right));
        }

        [Test]
        public void EveryStepChange_IsTold()
        {
            ToFirstPrompt();
            _flow.OnPress(Left);

            CollectionAssert.AreEqual(new[] { RaceTutorialStep.WaitForGo, RaceTutorialStep.TapLeft, RaceTutorialStep.Stride }, _seen);
        }
    }
}
