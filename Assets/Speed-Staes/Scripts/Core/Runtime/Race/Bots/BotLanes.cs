using System;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// Where the runners stand on the start line. Slots are counted from the camera side (slot 0 nearest); the player has one
    /// slot among the bots. The player's runner never moves (its start is the scene's), so a lane is expressed as an offset
    /// from the player: negative = nearer the camera, positive = farther. Pure.
    /// </summary>
    public static class BotLanes
    {
        /// <summary>
        /// The player's slot (0-based) for a lane preference (1-based; 0 or less = automatic = slot 0, the camera side, as before).
        /// A preference past the last lane is the last lane.
        /// </summary>
        public static int PlayerSlot(int preference, int botCount)
        {
            if (preference <= 0 || botCount <= 0) return 0;
            return Math.Min(preference - 1, botCount);
        }

        /// <summary>
        /// Lane offset (in lane widths, from the player) of the bot with this index (0-based): the bots fill every slot but the
        /// player's, in order. With the player in slot 0 the offsets are 1, 2, 3... (the old layout).
        /// </summary>
        public static int BotOffset(int botIndex, int playerSlot)
        {
            int slot = botIndex < playerSlot ? botIndex : botIndex + 1;
            return slot - playerSlot;
        }
    }
}
