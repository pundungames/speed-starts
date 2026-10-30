using NUnit.Framework;
using SpeedStars.Meta.Themes;

namespace SpeedStars.Tests.Meta.Themes
{
    /// <summary>Slot counts are test-chosen; the tests assert the mapping rule, never how many themes ship.</summary>
    public sealed class RaceThemeSlotsTests
    {
        [Test]
        public void InRangeIndexIsItsOwnSlot()
        {
            int count = RaceThemeSlots.SlotCountFor(3);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, RaceThemeSlots.SlotFor(i, count));
        }

        [Test]
        public void OutOfRangeIndexFallsBackToStandard()
        {
            int count = RaceThemeSlots.SlotCountFor(3);
            Assert.AreEqual(RaceThemeSlots.StandardSlot, RaceThemeSlots.SlotFor(count, count));
            Assert.AreEqual(RaceThemeSlots.StandardSlot, RaceThemeSlots.SlotFor(count + 10, count));
            Assert.AreEqual(RaceThemeSlots.StandardSlot, RaceThemeSlots.SlotFor(-1, count));
        }

        [Test]
        public void NoSlotsFallsBackToStandard()
        {
            Assert.AreEqual(RaceThemeSlots.StandardSlot, RaceThemeSlots.SlotFor(2, 0));
        }

        [Test]
        public void StandardSlotIsNotAThemedWorld()
        {
            Assert.AreEqual(-1, RaceThemeSlots.ThemedIndexOf(RaceThemeSlots.StandardSlot));
        }

        [Test]
        public void ThemedSlotsMapToConsecutiveThemedIndices()
        {
            int count = RaceThemeSlots.SlotCountFor(4);
            for (int slot = RaceThemeSlots.StandardSlot + 1; slot < count; slot++)
                Assert.AreEqual(slot - 1, RaceThemeSlots.ThemedIndexOf(slot));
        }

        [Test]
        public void SlotCountIsStandardPlusThemed()
        {
            Assert.AreEqual(RaceThemeSlots.SlotCountFor(0) + 5, RaceThemeSlots.SlotCountFor(5));
            Assert.AreEqual(RaceThemeSlots.SlotCountFor(0), RaceThemeSlots.SlotCountFor(-3));
        }
    }
}
