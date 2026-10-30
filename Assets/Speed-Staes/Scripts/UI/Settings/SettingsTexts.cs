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

        public const string PrivacyPolicy = "Privacy Policy";
        public const string TermsOfUse = "Terms of Use";
        public const string Support = "Support";
        public const string ResetPersonalBests = "Reset Personal Bests";
        public const string ResetAllProgress = "Reset All Progress";
        public const string Cancel = "Cancel";
        public const string Continue = "Continue";
        public const string ResetButton = "RESET";

        /// <summary>Title of the confirmation window for the question on screen.</summary>
        public static string ConfirmTitle(ResetKind kind, ResetStep step)
        {
            if (kind == ResetKind.PersonalBests) return "Reset personal bests?";
            if (kind == ResetKind.AllProgress) return step == ResetStep.Second ? "This cannot be undone" : "Are you sure?";
            return string.Empty;
        }

        /// <summary>Explanation under the title of the confirmation window.</summary>
        public static string ConfirmBody(ResetKind kind, ResetStep step)
        {
            if (kind == ResetKind.PersonalBests) return "All your record times and daily bests will be deleted.";
            if (kind == ResetKind.AllProgress)
                return step == ResetStep.Second
                    ? "Everything you earned and own will be lost. Your settings stay."
                    : "You will start again like a new player.";
            return string.Empty;
        }

        /// <summary>Label of the right-hand button: the last question says RESET.</summary>
        public static string ConfirmButton(bool isFinalStep) => isFinalStep ? ResetButton : Continue;

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
