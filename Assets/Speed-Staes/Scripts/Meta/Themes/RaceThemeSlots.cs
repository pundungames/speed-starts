namespace SpeedStars.Meta.Themes
{
    /// <summary>
    /// Which streaming world the race scene opens for a showcase index (no Unity, no service). World slots follow the
    /// showcase order: slot 0 is the standard city (one LoopWayGenerator), slots 1..N-1 are the themed worlds
    /// (ThemedLoopWay array 0..N-2). An index outside the available slots falls back to the standard city.
    /// </summary>
    public static class RaceThemeSlots
    {
        /// <summary>The standard city's slot, also the fallback.</summary>
        public const int StandardSlot = 0;

        /// <summary>World slot for a showcase index; <paramref name="slotCount"/> is how many worlds the scene holds.</summary>
        public static int SlotFor(int showcaseIndex, int slotCount)
        {
            if (slotCount <= 0) return StandardSlot;
            return showcaseIndex >= 0 && showcaseIndex < slotCount ? showcaseIndex : StandardSlot;
        }

        /// <summary>Index into the themed worlds for a slot, or -1 for the standard city.</summary>
        public static int ThemedIndexOf(int slot) => slot > StandardSlot ? slot - 1 : -1;

        /// <summary>Total slots for a scene with the standard city plus this many themed worlds.</summary>
        public static int SlotCountFor(int themedWorldCount) => 1 + (themedWorldCount < 0 ? 0 : themedWorldCount);
    }
}
