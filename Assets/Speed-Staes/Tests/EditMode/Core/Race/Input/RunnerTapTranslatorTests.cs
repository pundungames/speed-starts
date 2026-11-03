using NUnit.Framework;
using SpeedStars.Core.Race;

namespace SpeedStars.Tests.RaceInput
{
    /// <summary>The arrow-key gate and the event-time conversion (T-081). Relations only, no balance numbers.</summary>
    public sealed class RunnerTapTranslatorTests
    {
        [Test]
        public void Press_PassesOnlyWhenTheRunnerAcceptsInput()
        {
            Assert.IsTrue(RunnerTapTranslator.ShouldForward(true, true));
            Assert.IsFalse(RunnerTapTranslator.ShouldForward(true, false));
        }

        [Test]
        public void Release_AlwaysPasses()
        {
            Assert.IsTrue(RunnerTapTranslator.ShouldForward(false, true));
            Assert.IsTrue(RunnerTapTranslator.ShouldForward(false, false));
        }

        [Test]
        public void OlderEvent_LandsEarlierOnGameTime()
        {
            double newer = RunnerTapTranslator.ToGameTime(99.9, 100.0, 50.0);
            double older = RunnerTapTranslator.ToGameTime(99.5, 100.0, 50.0);
            Assert.Less(older, newer);
            Assert.LessOrEqual(newer, 50.0);
        }

        [Test]
        public void EventAge_IsKeptBetweenTheTwoTimelines()
        {
            double game = RunnerTapTranslator.ToGameTime(10.25, 10.75, 3.0);
            Assert.AreEqual(3.0 - 0.5, game, 1e-9);
        }

        [Test]
        public void EventFromTheFuture_NeverLandsAfterNow()
        {
            double game = RunnerTapTranslator.ToGameTime(10.5, 10.0, 3.0);
            Assert.AreEqual(3.0, game, 1e-9);
        }
    }
}
