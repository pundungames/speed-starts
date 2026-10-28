using System;
using NUnit.Framework;
using SpeedStars.UI.Costume;

namespace SpeedStars.Tests.UI.Costume
{
    public sealed class HsvColorTests
    {
        private const float Tolerance = 0.0005f;

        [TestCase("FF0000", 0f)]
        [TestCase("00FF00", 1f / 3f)]
        [TestCase("0000FF", 2f / 3f)]
        [TestCase("FFFF00", 1f / 6f)]
        public void PureColoursHaveTheirHue(string hex, float hue)
        {
            Assert.IsTrue(HsvColor.TryFromHex(hex, 0.9f, out HsvColor color));

            Assert.AreEqual(hue, color.Hue, Tolerance);
            Assert.AreEqual(1f, color.Saturation, Tolerance);
            Assert.AreEqual(1f, color.Value, Tolerance);
        }

        [Test]
        public void HsvToHexGivesThePureColours()
        {
            Assert.AreEqual("FF0000", new HsvColor(0f, 1f, 1f).ToHex());
            Assert.AreEqual("00FFFF", new HsvColor(0.5f, 1f, 1f).ToHex());
            Assert.AreEqual("FFFFFF", new HsvColor(0.3f, 0f, 1f).ToHex());
            Assert.AreEqual("000000", new HsvColor(0.3f, 0.7f, 0f).ToHex());
        }

        [Test]
        public void TheHueCircleCloses()
        {
            Assert.AreEqual(new HsvColor(0f, 1f, 1f).ToHex(), new HsvColor(1f, 1f, 1f).ToHex());
        }

        [TestCase("E53935")]
        [TestCase("1E88E5")]
        [TestCase("8E24AA")]
        [TestCase("212121")]
        [TestCase("F5F5F5")]
        [TestCase("FDD835")]
        public void HexSurvivesAPickerRoundTrip(string hex)
        {
            Assert.IsTrue(HsvColor.TryFromHex(hex, 0f, out HsvColor color));

            string back = color.ToHex();

            for (int i = 0; i < 6; i += 2)
            {
                int expected = Convert.ToInt32(hex.Substring(i, 2), 16);
                int actual = Convert.ToInt32(back.Substring(i, 2), 16);
                Assert.LessOrEqual(Math.Abs(expected - actual), 1, $"{hex} -> {back}");
            }
        }

        [TestCase("808080")]
        [TestCase("000000")]
        [TestCase("FFFFFF")]
        public void GreyHasNoHueSoTheFallbackStays(string hex)
        {
            const float fallback = 0.62f;

            Assert.IsTrue(HsvColor.TryFromHex(hex, fallback, out HsvColor color));

            Assert.AreEqual(fallback, color.Hue, Tolerance);
            Assert.AreEqual(0f, color.Saturation, Tolerance);
        }

        [Test]
        public void HexTextIsCaseAndHashTolerant()
        {
            Assert.IsTrue(HsvColor.TryFromHex("#e53935", 0f, out HsvColor lower));
            Assert.IsTrue(HsvColor.TryFromHex("E53935", 0f, out HsvColor upper));

            Assert.AreEqual(upper.ToHex(), lower.ToHex());
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("12345")]
        [TestCase("1234567")]
        [TestCase("GG0000")]
        public void NotAColourIsRefused(string hex)
        {
            Assert.IsFalse(HsvColor.TryFromHex(hex, 0.4f, out HsvColor color));
            Assert.AreEqual(0.4f, color.Hue, Tolerance, "a refused text leaves the fallback hue");
        }

        [Test]
        public void ValuesAreClampedIntoRange()
        {
            var color = new HsvColor(-0.5f, 3f, 2f);

            Assert.AreEqual(0f, color.Hue);
            Assert.AreEqual(1f, color.Saturation);
            Assert.AreEqual(1f, color.Value);
        }

        [Test]
        public void HueColourIsFullySaturatedAndBright()
        {
            new HsvColor(0.5f, 0.2f, 0.3f).HueToRgb(out float r, out float g, out float b);

            Assert.AreEqual(0f, r, Tolerance);
            Assert.AreEqual(1f, g, Tolerance);
            Assert.AreEqual(1f, b, Tolerance);
        }
    }
}
