using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>Which sentence a tutorial step shows: every step with something to say has one; the first prompt of a side explains it.</summary>
    public sealed class RaceTutorialTextsTests
    {
        [TestCase(RaceTutorialStep.WaitForGo)]
        [TestCase(RaceTutorialStep.TapLeft)]
        [TestCase(RaceTutorialStep.TapRight)]
        [TestCase(RaceTutorialStep.KeepGoing)]
        [TestCase(RaceTutorialStep.Rhythm)]
        public void StepsWithSomethingToSay_HaveASentence(RaceTutorialStep step) =>
            Assert.IsNotEmpty(RaceTutorialTexts.For(step, 0));

        [TestCase(RaceTutorialStep.None)]
        [TestCase(RaceTutorialStep.Stride)]
        [TestCase(RaceTutorialStep.Done)]
        public void TheOthers_HaveNone(RaceTutorialStep step) =>
            Assert.IsEmpty(RaceTutorialTexts.For(step, 0));

        [Test]
        public void TheFirstPromptOfASide_ExplainsIt_TheNextOnlyNamesIt()
        {
            Assert.AreEqual(RaceTutorialTexts.TapLeftFirst, RaceTutorialTexts.For(RaceTutorialStep.TapLeft, 0));
            Assert.AreEqual(RaceTutorialTexts.TapRightFirst, RaceTutorialTexts.For(RaceTutorialStep.TapRight, 1));
            Assert.AreEqual(RaceTutorialTexts.TapLeft, RaceTutorialTexts.For(RaceTutorialStep.TapLeft, 2));
            Assert.AreEqual(RaceTutorialTexts.TapRight, RaceTutorialTexts.For(RaceTutorialStep.TapRight, 3));
        }
    }
}
