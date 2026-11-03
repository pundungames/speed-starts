namespace SpeedStars.UI.Home
{
    /// <summary>What the Home character card has to do with the menu character's outfit.</summary>
    public enum RacerCardAction
    {
        /// <summary>Nothing: the character wears the player's own preset and keeps doing so.</summary>
        None,

        /// <summary>Race of the Day is selected: the character wears the day's outfit (shown as a preview, never saved).</summary>
        ShowDailyOutfit,

        /// <summary>Another event was selected after the day's outfit was shown: back to the saved preset.</summary>
        RestoreOwnOutfit
    }

    /// <summary>
    /// Pure rule of the Home "Racer of the day" card: whether the day's outfit is on the character and whether it has to be taken off
    /// again. The player's own presets are never touched; only the preview layer (<c>ICosmeticPreview</c>) carries the day's outfit.
    /// </summary>
    public static class RacerCardRules
    {
        public static RacerCardAction Decide(bool dailySelected, bool dailyOutfitShown)
        {
            if (dailySelected) return RacerCardAction.ShowDailyOutfit;
            return dailyOutfitShown ? RacerCardAction.RestoreOwnOutfit : RacerCardAction.None;
        }
    }
}
