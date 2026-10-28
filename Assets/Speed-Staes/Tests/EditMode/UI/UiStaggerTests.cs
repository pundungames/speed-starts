using NUnit.Framework;
using SpeedStars.UI.Framework;

namespace SpeedStars.Tests.UI
{
    /// <summary>
    /// Stagger spacing rule (agent-kit/05 §4). The inputs are arbitrary probe values, not the tuned numbers (those live
    /// in UiMotionConfig.asset); only the relationships are asserted.
    /// </summary>
    public sealed class UiStaggerTests
    {
        private const float Target = 0.05f;
        private const float Cap = 0.6f;
        private const float Tolerance = 1e-5f;

        [Test]
        public void Interval_NeverExceedsTheWantedInterval()
        {
            for (int count = 0; count <= 200; count++)
                Assert.LessOrEqual(UiStagger.Interval(Target, Cap, count), Target + Tolerance, $"count {count}");
        }

        [Test]
        public void Interval_KeepsTheWholeListUnderTheCap()
        {
            for (int count = 1; count <= 200; count++)
                Assert.LessOrEqual(UiStagger.Interval(Target, Cap, count) * count, Cap + Tolerance, $"count {count}");
        }

        [Test]
        public void Interval_ShortListThatFitsTheCap_KeepsTheWantedInterval()
        {
            int fitting = (int)(Cap / Target);

            Assert.AreEqual(Target, UiStagger.Interval(Target, Cap, fitting), Tolerance);
        }

        [Test]
        public void Interval_LongerList_SpacesItemsCloser()
        {
            int fitting = (int)(Cap / Target);

            Assert.Less(UiStagger.Interval(Target, Cap, fitting * 4), UiStagger.Interval(Target, Cap, fitting));
        }

        [Test]
        public void Interval_WithoutCap_KeepsTheWantedInterval()
        {
            Assert.AreEqual(Target, UiStagger.Interval(Target, 0f, 500), Tolerance);
        }

        [Test]
        public void Interval_NegativeWantedInterval_IsTreatedAsZero()
        {
            Assert.AreEqual(0f, UiStagger.Interval(-1f, Cap, 3), Tolerance);
        }
    }
}
