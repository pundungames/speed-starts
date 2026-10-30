using NUnit.Framework;
using SpeedStars.Meta.Settings;

namespace SpeedStars.Tests.Meta.Settings
{
    public sealed class LinkRulesTests
    {
        [TestCase("https://example.com/privacy", true)]
        [TestCase("  http://example.com  ", true)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase(null, false)]
        [TestCase("example.com", false)]
        [TestCase("javascript:alert(1)", false)]
        [TestCase("file:///c:/secret.txt", false)]
        public void IsOpenable_AcceptsOnlyAbsoluteHttpPages(string url, bool expected)
        {
            Assert.AreEqual(expected, LinkRules.IsOpenable(url));
        }
    }
}
