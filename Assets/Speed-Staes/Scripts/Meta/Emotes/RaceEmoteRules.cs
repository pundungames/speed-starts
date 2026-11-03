using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// The race emote decisions without any Unity object: when the button shows, when a press is accepted, who auto-plays and
    /// which clip a bot takes. The Bridge component only asks.
    /// </summary>
    public static class RaceEmoteRules
    {
        /// <summary>The HUD button shows when the player wants it and the equipped emote has an animation (None has nothing to play).</summary>
        public static bool ButtonVisible(bool showOption, bool hasEmote) => showOption && hasEmote;

        /// <summary>
        /// A press is accepted from GO on, also after the finish (free then); never before GO or while the last emote still
        /// plays. A session without a start signal (<c>null</c>) counts as started.
        /// </summary>
        public static bool CanPlay(bool hasEmote, RaceStartPhase? phase, bool busy) =>
            hasEmote && !busy && (phase == null || phase == RaceStartPhase.Go);

        /// <summary>The player's runner plays by itself at the finish when the switch is on and an animation is equipped.</summary>
        public static bool AutoSelfWanted(bool autoOption, bool hasEmote) => autoOption && hasEmote;

        /// <summary>A runner has crossed the line (a race with no length never finishes).</summary>
        public static bool CrossedLine(float distance, float target) => target > 0f && distance >= target;

        /// <summary>The clip a bot takes: bots spread over the clips, and the round moves the start so a bot does not repeat itself.</summary>
        public static int BotClipIndex(int botIndex, int round, int clipCount) =>
            clipCount <= 0 ? -1 : (int)(((long)botIndex + round) % clipCount);
    }
}
