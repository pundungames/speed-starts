using NUnit.Framework;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>The player name line under the Home card buttons (T-097): empty name = empty line, a change shows the new name.</summary>
    public sealed class PlayerNameLineTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void NoName_IsAnEmptyLine(string name)
        {
            Assert.AreEqual(string.Empty, RacerCardRules.PlayerNameLine(name));
        }

        [Test]
        public void Name_IsShownAsIs()
        {
            Assert.AreEqual("Speedy Joe", RacerCardRules.PlayerNameLine("Speedy Joe"));
        }

        [Test]
        public void Name_LosesOuterSpacesOnly()
        {
            Assert.AreEqual("A  B", RacerCardRules.PlayerNameLine("  A  B "));
        }

        [Test]
        public void ChangedName_GivesTheNewLine()
        {
            string before = RacerCardRules.PlayerNameLine("Kim");
            string after = RacerCardRules.PlayerNameLine("Lee");
            Assert.AreNotEqual(before, after);
            Assert.AreEqual("Lee", after);
        }
    }
}
