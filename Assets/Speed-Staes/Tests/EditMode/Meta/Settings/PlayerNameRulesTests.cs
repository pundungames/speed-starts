using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Settings;

namespace SpeedStars.Tests.Meta.Settings
{
    /// <summary>Limits are test-chosen; the tests assert how the rules relate to them, not the shipped asset.</summary>
    public sealed class PlayerNameRulesTests
    {
        private const int Min = 2;
        private const int Max = 6;

        private static PlayerNameResult Check(string name) => PlayerNameRules.Validate(name, Min, Max);

        [Test]
        public void Length_AtLimits_IsValid_OutsideIsNot()
        {
            Assert.AreEqual(PlayerNameResult.Valid, Check(new string('a', Min)));
            Assert.AreEqual(PlayerNameResult.Valid, Check(new string('a', Max)));
            Assert.AreEqual(PlayerNameResult.TooShort, Check(new string('a', Min - 1)));
            Assert.AreEqual(PlayerNameResult.TooLong, Check(new string('a', Max + 1)));
        }

        [Test]
        public void Spaces_AroundTheName_AreCutBeforeCounting()
        {
            Assert.AreEqual(PlayerNameResult.Valid, Check("  " + new string('a', Max) + "  "));
            Assert.AreEqual(PlayerNameResult.TooShort, Check("   "));
            Assert.AreEqual(PlayerNameResult.TooShort, Check(null));
            Assert.AreEqual(PlayerNameResult.TooShort, Check(string.Empty));
            Assert.AreEqual("abc", PlayerNameRules.Normalize("  abc "));
        }

        [Test]
        public void AllowedCharacters_AnyScriptDigitsSpaceUnderscoreDash()
        {
            Assert.AreEqual(PlayerNameResult.Valid, Check("a b_c-"));
            Assert.AreEqual(PlayerNameResult.Valid, Check("İşi")); // Turkish letters
            Assert.AreEqual(PlayerNameResult.Valid, Check("小明")); // Chinese
        }

        [Test]
        public void SurrogatePair_CountsAsOneCharacter()
        {
            string rare = char.ConvertFromUtf32(0x20000); // CJK extension B, two UTF-16 units
            Assert.AreEqual(PlayerNameResult.Valid, Check(rare + rare));
            Assert.AreEqual(PlayerNameResult.TooShort, Check(rare));
        }

        [TestCase("ab!")]
        [TestCase("a\nb")]
        [TestCase("a\tb")]
        [TestCase("ab@c")]
        [TestCase("ab\U0001F600")] // emoji: a pair that is not a letter
        public void ForbiddenCharacters_AreRejected_EvenWhenLengthIsFine(string name)
        {
            Assert.AreEqual(PlayerNameResult.InvalidCharacters, Check(name));
        }
    }
}
