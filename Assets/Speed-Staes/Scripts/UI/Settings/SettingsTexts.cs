using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// Every fixed sentence of the settings window in one place (no string table asset exists yet; a later localization
    /// step replaces this class). Plain ASCII: the legacy UI font must have every glyph.
    /// </summary>
    public static class SettingsTexts
    {
        public const string Title = "SETTINGS";
        public const string Music = "Music";
        public const string Language = "Language";
        public const string Flag = "Flag";
        public const string PlayerName = "Player Name";
        public const string Done = "Done";
        public const string On = "ON";
        public const string Off = "OFF";
        public const string NamePlaceholder = "Enter name";

        /// <summary>The red line under the rows for a refused name; empty for a valid one.</summary>
        public static string NameError(PlayerNameResult result, int maxLength)
        {
            switch (result)
            {
                case PlayerNameResult.TooShort: return "Name is too short";
                case PlayerNameResult.TooLong: return $"Name can have {maxLength} characters at most";
                case PlayerNameResult.InvalidCharacters: return "Use letters, numbers, space, - or _";
                default: return string.Empty;
            }
        }

        public static string OnOff(bool on) => on ? On : Off;
    }
}
