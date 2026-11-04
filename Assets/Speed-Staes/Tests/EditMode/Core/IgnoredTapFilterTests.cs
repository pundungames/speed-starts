using NUnit.Framework;
using SpeedStars.Core.Race;

namespace SpeedStars.Tests.RaceStart
{
    /// <summary>A finger on a button is not a step: its press and its release are dropped, the other leg and the next step are not.</summary>
    public sealed class IgnoredTapFilterTests
    {
        private const int Left = 0;
        private const int Right = 1;

        [Test]
        public void Nothing_Marked_NothingDropped()
        {
            var filter = new IgnoredTapFilter();
            Assert.IsFalse(filter.ShouldDrop(Left, true));
            Assert.IsFalse(filter.ShouldDrop(Right, false));
        }

        [Test]
        public void AMark_DropsOneEvent_OfThatLegAndKindOnly()
        {
            var filter = new IgnoredTapFilter();
            filter.IgnorePress(Left);

            Assert.IsFalse(filter.ShouldDrop(Right, true), "the other leg still steps");
            Assert.IsFalse(filter.ShouldDrop(Left, false), "a release is not a press");
            Assert.IsTrue(filter.ShouldDrop(Left, true));
            Assert.IsFalse(filter.ShouldDrop(Left, true), "the next step counts");
        }

        [Test]
        public void TwoFingers_OnButtons_OfOneSide_AreBothDropped()
        {
            var filter = new IgnoredTapFilter();
            filter.IgnoreRelease(Right);
            filter.IgnoreRelease(Right);

            Assert.IsTrue(filter.ShouldDrop(Right, false));
            Assert.IsTrue(filter.ShouldDrop(Right, false));
            Assert.IsFalse(filter.ShouldDrop(Right, false));
        }

        [Test]
        public void Clear_ForgetsMarksWhoseEventIsGone()
        {
            var filter = new IgnoredTapFilter();
            filter.IgnorePress(Left);
            filter.IgnoreRelease(Left);

            filter.Clear();

            Assert.IsFalse(filter.ShouldDrop(Left, true));
            Assert.IsFalse(filter.ShouldDrop(Left, false));
        }
    }
}
