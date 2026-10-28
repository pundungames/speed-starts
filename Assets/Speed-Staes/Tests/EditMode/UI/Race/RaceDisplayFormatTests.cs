using NUnit.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>HUD / Results text and fraction rules.</summary>
    public sealed class RaceDisplayFormatTests
    {
        [TestCase(0d, "0.00")]
        [TestCase(9.58d, "9.58")]
        [TestCase(9.589d, "9.58")]
        [TestCase(10d, "10.00")]
        [TestCase(59.999d, "59.99")]
        [TestCase(60d, "1:00.00")]
        [TestCase(62.05d, "1:02.05")]
        [TestCase(-3d, "0.00")]
        [TestCase(double.NaN, "0.00")]
        [TestCase(double.PositiveInfinity, "0.00")]
        public void Time_FormatsLikeAStopwatch(double seconds, string expected)
        {
            Assert.AreEqual(expected, RaceDisplayFormat.Time(seconds));
        }

        [Test]
        public void Time_NeverShowsABetterTimeThanTheRealOne()
        {
            Assert.LessOrEqual(RaceDisplayFormat.ToCentiseconds(9.5899d), 958L);
        }

        [TestCase(0f, 100f, 0f)]
        [TestCase(50f, 100f, 0.5f)]
        [TestCase(100f, 100f, 1f)]
        [TestCase(130f, 100f, 1f)]
        [TestCase(-5f, 100f, 0f)]
        [TestCase(10f, 0f, 0f)]
        [TestCase(10f, -1f, 0f)]
        public void Progress_IsClampedToZeroOne(float distance, float target, float expected)
        {
            Assert.AreEqual(expected, RaceDisplayFormat.Progress(distance, target), 1e-5f);
        }

        [Test]
        public void Progress_WithNaN_IsZero()
        {
            Assert.AreEqual(0f, RaceDisplayFormat.Progress(float.NaN, 100f));
            Assert.AreEqual(0f, RaceDisplayFormat.Progress(10f, float.NaN));
        }

        [TestCase(37.9f, 100f, 37)]
        [TestCase(120f, 100f, 100)]
        [TestCase(-2f, 100f, 0)]
        [TestCase(12.5f, 0f, 12)]
        public void WholeMeters_FloorsAndNeverPassesTheTarget(float distance, float target, int expected)
        {
            Assert.AreEqual(expected, RaceDisplayFormat.WholeMeters(distance, target));
        }

        [Test]
        public void Meters_AddsTheUnit()
        {
            Assert.AreEqual("37 m", RaceDisplayFormat.Meters(37));
        }
    }
}
