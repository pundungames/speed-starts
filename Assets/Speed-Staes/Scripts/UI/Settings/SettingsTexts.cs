using SpeedStars.Meta.Contracts;
using UnityEngine;

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
        public const string CameraDistance = "Camera";
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
        public const string ResetPersonalBests = "Reset All Personal Bests";
        public const string ResetAllProgress = "Reset All Progress";
        public const string Cancel = "Cancel";
        public const string Continue = "Continue";
        public const string ResetButton = "RESET";

        // Sections and rows added with settings v2 (the reference game's Competitors / Gameplay / Visuals / Emote / Reset options).
        public const string SectionCompetitors = "Competitors";
        public const string SectionGameplay = "Gameplay";
        public const string SectionVisuals = "Visuals";
        public const string SectionEmote = "Emote";
        public const string SectionResetOptions = "Reset options";

        public const string CompetitorType = "Competitor type";
        public const string CompetitorBots = "Bots";
        public const string CompetitorTimeTrial = "Time trial";
        public const string Difficulty = "Difficulty";
        public const string SkillRange = "Skill range";
        public const string LanePreference = "Lane preference";
        public const string LaneAuto = "?";
        public const string HurdleSensitivity = "Hurdle input sensitivity";
        public const string ParticleEffects = "Particle effects";
        public const string ShowEmoteButton = "Show emote button";
        public const string EmoteMyself = "Emote myself";
        public const string EmoteCompetitors = "Emote competitors";
        public const string ClearCustomRacers = "Clear custom racers";
        public const string PersonalBestOf = "Personal best of";
        public const string ResetAPersonalBest = "Reset a personal best";

        /// <summary>Option names of the competitor type row, in <see cref="CompetitorType"/> order.</summary>
        public static readonly string[] CompetitorTypeOptions = { CompetitorBots, CompetitorTimeTrial };

        /// <summary>Lane picker options: Auto first, then 1..<paramref name="laneCount"/>.</summary>
        public static string[] LaneOptions(int laneCount)
        {
            var options = new string[Mathf.Max(0, laneCount) + 1];
            options[0] = LaneAuto;
            for (int i = 1; i < options.Length; i++) options[i] = i.ToString();
            return options;
        }

        /// <summary>Title of the confirmation window for the question on screen.</summary>
        public static string ConfirmTitle(ResetKind kind, ResetStep step)
        {
            if (kind == ResetKind.OnePersonalBest) return "Reset this personal best?";
            if (kind == ResetKind.CustomRacers) return "Clear custom racers?";
            if (kind == ResetKind.PersonalBests) return "Reset personal bests?";
            if (kind == ResetKind.AllProgress) return step == ResetStep.Second ? "This cannot be undone" : "Are you sure?";
            return string.Empty;
        }

        /// <summary>Explanation under the title of the confirmation window.</summary>
        public static string ConfirmBody(ResetKind kind, ResetStep step, string eventName = "")
        {
            if (kind == ResetKind.OnePersonalBest) return "The record time and daily bests of " + eventName + " will be deleted.";
            if (kind == ResetKind.CustomRacers) return "All custom racers go back to the default look. Your items stay.";
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
