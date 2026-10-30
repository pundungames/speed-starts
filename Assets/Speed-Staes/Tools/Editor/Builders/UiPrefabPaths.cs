namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Where every UI prefab lives (contract §5). The prefab builder writes these, the scene builder places them;
    /// one list so the two never disagree about a path.
    /// </summary>
    public static class UiPrefabPaths
    {
        public const string Root = "Assets/Speed-Staes/UI";

        public const string HomeScreen = Root + "/Screens/HomeScreen.prefab";
        public const string EventSelectScreen = Root + "/Screens/EventSelectScreen.prefab";
        public const string ThemeSelectScreen = Root + "/Screens/ThemeSelectScreen.prefab";
        public const string CosmeticsScreen = Root + "/Screens/CosmeticsScreen.prefab";
        public const string HighScoresScreen = Root + "/Screens/HighScoresScreen.prefab";
        public const string EmoteScreen = Root + "/Screens/EmoteScreen.prefab";
        public const string RaceHud = Root + "/Screens/RaceHud.prefab";
        public const string ResultsScreen = Root + "/Screens/ResultsScreen.prefab";
        public const string AnalysisScreen = Root + "/Screens/AnalysisScreen.prefab";

        public const string EntryGateModal = Root + "/Popups/EntryGateModal.prefab";
        public const string DailyGiftPopup = Root + "/Popups/DailyGiftPopup.prefab";
        public const string SevenDayCheckInPanel = Root + "/Popups/SevenDayCheckInPanel.prefab";
        public const string SettingsPopup = Root + "/Popups/SettingsPopup.prefab";

        public const string ResourceBar = Root + "/Widgets/ResourceBar.prefab";
        public const string RedDot = Root + "/Widgets/RedDot.prefab";
        public const string RewardFlight = Root + "/Widgets/RewardFlight.prefab";
        public const string RaceStart = Root + "/Widgets/RaceStart.prefab";

        public const string EventRow = Root + "/Rows/EventRow.prefab";
        public const string CosmeticCell = Root + "/Rows/CosmeticCell.prefab";
        public const string SevenDayCell = Root + "/Rows/SevenDayCell.prefab";
        public const string HighScoreRow = Root + "/Rows/HighScoreRow.prefab";
        public const string SettingsRow = Root + "/Rows/SettingsRow.prefab";
        public const string LanguageRow = Root + "/Rows/LanguageRow.prefab";
        public const string FlagCell = Root + "/Rows/FlagCell.prefab";

        /// <summary>Home scene screens, in the order they sit under "Canvas Screens" (first one is the root screen).</summary>
        public static readonly string[] HomeScreens = { HomeScreen, EventSelectScreen, ThemeSelectScreen, CosmeticsScreen, HighScoresScreen, EmoteScreen };

        /// <summary>Home scene popups under "Canvas Popups".</summary>
        public static readonly string[] HomePopups = { EntryGateModal, DailyGiftPopup, SevenDayCheckInPanel, SettingsPopup };
    }
}
