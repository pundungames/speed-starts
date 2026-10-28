namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Optional side of the Results screen: the Purple Gems the race paid. RaceFlow calls it before the screen opens
    /// (0 clears the line of a previous race). Separate from IResultsView so the result contract stays unchanged.
    /// </summary>
    public interface IGemRewardView
    {
        void ShowGemReward(int gems);
    }
}
