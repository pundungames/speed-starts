using NUnit.Framework;
using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.Tests.RaceStart
{
    /// <summary>
    /// When things happen between the line and the Results (RaceFinishTimeline), against the config's own numbers: relations only, never a
    /// fixed value. Pure: no scene, no clock but the seconds given here.
    /// </summary>
    public sealed class RaceFinishTimelineTests
    {
        private const float Fast = 100f;
        private const float Stopped = 0f;
        private const float Step = 0.02f;

        private RaceFinishConfig _config;
        private RaceFinishTimeline _timeline;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<RaceFinishConfig>();
            _timeline = new RaceFinishTimeline(_config);
            _timeline.Begin();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        private void Run(float seconds, float speed)
        {
            for (float t = 0f; t < seconds; t += Step) _timeline.Tick(Step, speed);
        }

        /// <summary>To the moment the emote is asked for, answered with this length.</summary>
        private void ReachEmote(float emoteSeconds)
        {
            Run(_config.SlowMotionSeconds + Step, Stopped);
            Assert.IsTrue(_timeline.TakeEmoteDue(), "a stopped runner gets its emote right after the slow motion");
            _timeline.EmoteStarted(emoteSeconds);
        }

        [Test]
        public void SlowMotion_HoldsAtTheLine_ThenGivesTheNormalSpeedBack()
        {
            Assert.AreEqual(RaceFinishPhase.SlowMotion, _timeline.Phase);
            Assert.AreEqual(_config.SlowMotionScale, _timeline.TimeScale, 0.0001f);
            Assert.Less(_timeline.TimeScale, 1f);

            Run(_config.SlowMotionSeconds + _config.SlowMotionReturnSeconds + Step, Fast);

            Assert.AreEqual(1f, _timeline.TimeScale, 0.0001f);
        }

        [Test]
        public void Camera_WaitsForTheSlowMotion_ThenSwingsAllTheWay()
        {
            Run(_config.SlowMotionSeconds - Step, Fast);
            Assert.AreEqual(0f, _timeline.CameraProgress, 0.0001f);

            Run(_config.CameraMoveSeconds + 2f * Step, Fast);
            Assert.AreEqual(1f, _timeline.CameraProgress, 0.0001f);
        }

        [Test]
        public void Emote_WaitsForTheRunnerToSlowDown()
        {
            Run(_config.SlowMotionSeconds + Step, Fast);
            Assert.IsFalse(_timeline.TakeEmoteDue(), "still running");
            Assert.AreEqual(RaceFinishPhase.Slowdown, _timeline.Phase);

            _timeline.Tick(Step, _config.EmoteFromSpeed);

            Assert.IsTrue(_timeline.TakeEmoteDue());
            Assert.AreEqual(RaceFinishPhase.Celebrate, _timeline.Phase);
            Assert.IsFalse(_timeline.TakeEmoteDue(), "asked once");
        }

        [Test]
        public void Emote_ComesAnywayWhenTheRunnerNeverSlowsDown()
        {
            Run(_config.SlowMotionSeconds + _config.SlowdownMaxSeconds + Step, Fast);

            Assert.IsTrue(_timeline.TakeEmoteDue());
        }

        [Test]
        public void Results_WaitForTheEmote()
        {
            float emote = _config.MaxEmoteWaitSeconds * 0.5f;
            ReachEmote(emote);

            Run(emote - 2f * Step, Stopped);
            Assert.IsFalse(_timeline.TakeResultsDue());

            Run(3f * Step, Stopped);
            Assert.IsTrue(_timeline.TakeResultsDue());
            Assert.AreEqual(RaceFinishPhase.Holding, _timeline.Phase);
            Assert.IsFalse(_timeline.TakeResultsDue(), "opened once");
        }

        [Test]
        public void Results_DoNotWaitForALongEmoteBeyondTheLimit()
        {
            ReachEmote(_config.MaxEmoteWaitSeconds * 10f);

            Run(_config.MaxEmoteWaitSeconds + 2f * Step, Stopped);

            Assert.IsTrue(_timeline.TakeResultsDue());
        }

        [Test]
        public void Results_WithoutAnEmote_ComeAfterTheShortPause()
        {
            ReachEmote(0f);

            Run(_config.NoEmoteSeconds + 2f * Step, Stopped);

            Assert.IsTrue(_timeline.TakeResultsDue());
        }

        [Test]
        public void Skip_IsIgnoredWhileTheFingerMayStillBeMashing()
        {
            _timeline.Skip();
            Assert.IsFalse(_timeline.TakeResultsDue());
            Assert.AreEqual(RaceFinishPhase.SlowMotion, _timeline.Phase);
        }

        [Test]
        public void Skip_OpensTheResultsOnce_AtNormalSpeed()
        {
            Run(_config.SkipLockSeconds + Step, Fast);

            _timeline.Skip();
            _timeline.Skip();

            Assert.IsTrue(_timeline.TakeResultsDue());
            Assert.IsFalse(_timeline.TakeResultsDue());
            Assert.AreEqual(1f, _timeline.TimeScale, 0.0001f);
        }

        [Test]
        public void Skip_StillLetsTheRunnerEmoteWhenItHasStopped()
        {
            Run(_config.SkipLockSeconds + Step, Fast);
            _timeline.Skip();
            Assert.IsFalse(_timeline.TakeEmoteDue(), "the Results open while it still runs");

            _timeline.Tick(Step, Stopped);

            Assert.IsTrue(_timeline.TakeEmoteDue());
            Assert.AreEqual(RaceFinishPhase.Holding, _timeline.Phase, "it does not take the Results back");
        }

        [Test]
        public void Stop_EndsEverything()
        {
            _timeline.Stop();

            Run(_config.SlowMotionSeconds + _config.SlowdownMaxSeconds + _config.NoEmoteSeconds + Step, Stopped);

            Assert.AreEqual(RaceFinishPhase.Idle, _timeline.Phase);
            Assert.IsFalse(_timeline.TakeEmoteDue());
            Assert.IsFalse(_timeline.TakeResultsDue());
            Assert.AreEqual(1f, _timeline.TimeScale, 0.0001f);
        }
    }
}
