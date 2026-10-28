namespace SpeedStars.UI.Race
{
    /// <summary>What the HUD's Exit and Restart buttons ask of the race flow. RaceFlow implements it (bound with its interfaces).</summary>
    public interface IRaceHudActions
    {
        /// <summary>Leave to the Home scene. No ticket is refunded.</summary>
        void RequestExit();

        /// <summary>Run the same event again through the entry gate (a ticket may be spent again), also while the race runs.</summary>
        void RequestRestart();
    }
}
