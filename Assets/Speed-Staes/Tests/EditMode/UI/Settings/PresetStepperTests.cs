using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Home;
using SpeedStars.UI.Settings;

namespace SpeedStars.Tests.UI.Settings
{
    public sealed class PresetStepperTests
    {
        private const int Count = 4; // test-chosen; the rule works for any count

        [Test]
        public void Next_WrapsFromLastToFirst()
        {
            Assert.AreEqual(1, PresetStepper.Step(0, Count, +1));
            Assert.AreEqual(0, PresetStepper.Step(Count - 1, Count, +1));
        }

        [Test]
        public void Previous_WrapsFromFirstToLast()
        {
            Assert.AreEqual(Count - 1, PresetStepper.Step(0, Count, -1));
            Assert.AreEqual(0, PresetStepper.Step(1, Count, -1));
        }

        [Test]
        public void SinglePreset_StaysPut_AndNoPresetsGivesZero()
        {
            Assert.AreEqual(0, PresetStepper.Step(0, 1, +1));
            Assert.AreEqual(0, PresetStepper.Step(0, 1, -1));
            Assert.AreEqual(0, PresetStepper.Step(3, 0, +1));
        }

        [Test]
        public void NoDirection_KeepsTheIndex()
        {
            Assert.AreEqual(2, PresetStepper.Step(2, Count, 0));
        }

        [Test]
        public void NameError_IsEmptyOnlyForAValidName()
        {
            Assert.IsEmpty(SettingsTexts.NameError(PlayerNameResult.Valid, 5));
            Assert.IsNotEmpty(SettingsTexts.NameError(PlayerNameResult.TooShort, 5));
            Assert.IsNotEmpty(SettingsTexts.NameError(PlayerNameResult.InvalidCharacters, 5));
            StringAssert.Contains("5", SettingsTexts.NameError(PlayerNameResult.TooLong, 5));
        }
    }
}
