using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>Outcome of checking or saving a player name.</summary>
    public enum PlayerNameResult
    {
        /// <summary>Accepted (and saved, when it came from <c>TrySetName</c>).</summary>
        Valid,
        /// <summary>Fewer characters than the minimum after trimming (an empty name too).</summary>
        TooShort,
        /// <summary>More characters than the maximum after trimming.</summary>
        TooLong,
        /// <summary>Contains a character other than letters, digits, space, '_' or '-'.</summary>
        InvalidCharacters
    }

    /// <summary>The outside pages the settings window links to (their addresses live in the settings config).</summary>
    public enum SettingsLink
    {
        PrivacyPolicy,
        TermsOfUse,
        Support
    }

    /// <summary>
    /// The two resets of the settings window. Personal bests: every event record and day best is deleted. All progress: the
    /// save becomes a new player's (starting balances from the economy config) but the player's settings stay.
    /// <see cref="Changed"/> is raised after either, so screens that cache progress can refresh.
    /// </summary>
    public interface IProgressReset
    {
        event Action Changed;

        void ResetPersonalBests();

        void ResetAll();
    }

    /// <summary>One language the player can pick (code + its name in its own language).</summary>
    public readonly struct LanguageInfo
    {
        public readonly string Code;
        public readonly string DisplayName;

        public LanguageInfo(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }

    /// <summary>One player flag (country code, name and the picture shown in the grid and the field).</summary>
    public readonly struct FlagInfo
    {
        public readonly string Code;
        public readonly string DisplayName;
        public readonly Sprite Sprite;

        public FlagInfo(string code, string displayName, Sprite sprite)
        {
            Code = code;
            DisplayName = displayName;
            Sprite = sprite;
        }
    }

    /// <summary>
    /// Player settings (settings window): music, sound effects, language, flag and name. Every change is saved at once.
    /// Music and effects are separate switches. The flag and name are what High Scores will show later; the language
    /// is only stored until a translation system exists. Defaults and option lists come from the settings config asset.
    /// </summary>
    public interface IPlayerSettings
    {
        /// <summary>Raised after any value changed (the audio system listens for the switches).</summary>
        event Action Changed;

        bool MusicOn { get; }
        bool SfxOn { get; }
        string LanguageCode { get; }
        string FlagCode { get; }
        string PlayerName { get; }

        /// <summary>Longest name the player may type (for the input field's limit).</summary>
        int MaxNameLength { get; }

        IReadOnlyList<LanguageInfo> Languages { get; }
        IReadOnlyList<FlagInfo> Flags { get; }

        void SetMusic(bool on);
        void SetSfx(bool on);

        /// <summary>False for a code that is not in the language list; nothing changes then.</summary>
        bool SetLanguage(string code);

        /// <summary>False for a code that is not in the flag list; nothing changes then.</summary>
        bool SetFlag(string code);

        /// <summary>Checks a typed name without saving it.</summary>
        PlayerNameResult ValidateName(string raw);

        /// <summary>Trims, checks and saves a name. Anything but Valid changes nothing.</summary>
        PlayerNameResult TrySetName(string raw);
    }
}
