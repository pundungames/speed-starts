using System;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// Where the runners stand on the start line. Slots are counted from the camera side (slot 0 nearest); the player has one
    /// slot among the bots. The field stands around the road's middle line, whichever slot the player has, so nobody ends up off
    /// the road. Pure.
    /// </summary>
    public static class BotLanes
    {
        /// <summary>
        /// The player's slot (0-based) for a lane preference (1-based; 0 or less = automatic: the given slot, which the caller draws).
        /// A preference past the last lane is the last lane. Alone on the track there is one slot.
        /// </summary>
        public static int PlayerSlot(int preference, int botCount, int automaticSlot)
        {
            if (botCount <= 0) return 0;
            if (preference <= 0) return Math.Max(0, Math.Min(automaticSlot, botCount));
            return Math.Min(preference - 1, botCount);
        }

        /// <summary>
        /// The slot of the bot with this index (0-based): the bots fill every slot but the player's, in order.
        /// </summary>
        public static int BotSlot(int botIndex, int playerSlot) => botIndex < playerSlot ? botIndex : botIndex + 1;

        /// <summary>A slot's distance from the road's middle, in lane widths (negative = nearer the camera): the slots spread evenly around it.</summary>
        public static float FromMiddle(int slot, int laneCount) => slot - (laneCount - 1) * 0.5f;
    }
}
