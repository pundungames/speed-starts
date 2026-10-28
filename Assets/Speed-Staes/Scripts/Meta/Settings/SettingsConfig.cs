using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.Settings
{
    /// <summary>One selectable language: a stable code written to saves and its name in its own language.</summary>
    [Serializable]
    public sealed class LanguageOption
    {
        [Tooltip("Stable code written to saves (en, tr, zh...). Never change it once players have saves.")]
        [SerializeField] private string languageCode;

        [Tooltip("Name shown in the list, in the language itself (English, Turkce).")]
        [SerializeField] private string displayName;

        public string Code => languageCode ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
    }

    /// <summary>One selectable player flag.</summary>
    [Serializable]
    public sealed class FlagOption
    {
        [Tooltip("Country code written to saves (TR, US...). Never change it once players have saves.")]
        [SerializeField] private string flagCode;

        [Tooltip("Country name (kept for tooling and later screens).")]
        [SerializeField] private string displayName;

        [Tooltip("Picture on the 160x112 flag canvas of the settings art.")]
        [SerializeField] private Sprite sprite;

        public string Code => flagCode ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public Sprite Sprite => sprite;
    }

    /// <summary>
    /// Settings window numbers and lists: first-run defaults, name length limits, the language list and the flag list.
    /// Adding a language or a flag (for example CN) is one entry here, no code. Defaults only apply until the player
    /// changes a value; after that the save wins.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Settings", fileName = "SettingsConfig")]
    public sealed class SettingsConfig : ScriptableObject
    {
        [Header("Defaults (until the player changes them)")]
        [Tooltip("Music switch of a new player.")]
        [SerializeField] private bool defaultMusicOn = true;

        [Tooltip("Sound effects switch of a new player.")]
        [SerializeField] private bool defaultSfxOn = true;

        [Tooltip("Language code of a new player. Empty or unknown = the first language in the list.")]
        [SerializeField] private string defaultLanguageCode;

        [Tooltip("Flag code of a new player. Empty or unknown = the first flag in the list.")]
        [SerializeField] private string defaultFlagCode;

        [Tooltip("Name of a new player.")]
        [SerializeField] private string defaultPlayerName;

        [Header("Player name rules")]
        [Tooltip("Fewest characters a name may have after trimming.")]
        [SerializeField, Min(1)] private int nameMinLength = 1;

        [Tooltip("Most characters a name may have.")]
        [SerializeField, Min(1)] private int nameMaxLength = 16;

        [Header("Lists")]
        [Tooltip("Languages in list order.")]
        [SerializeField] private LanguageOption[] languages = new LanguageOption[0];

        [Tooltip("Flags in grid order.")]
        [SerializeField] private FlagOption[] flags = new FlagOption[0];

        public bool DefaultMusicOn => defaultMusicOn;
        public bool DefaultSfxOn => defaultSfxOn;
        public string DefaultPlayerName => defaultPlayerName ?? string.Empty;
        public int NameMinLength => Mathf.Max(1, nameMinLength);
        public int NameMaxLength => Mathf.Max(NameMinLength, nameMaxLength);

        public IReadOnlyList<LanguageInfo> GetLanguages()
        {
            var list = new List<LanguageInfo>();
            if (languages == null) return list;
            foreach (LanguageOption option in languages)
                if (option != null && !string.IsNullOrEmpty(option.Code)) list.Add(new LanguageInfo(option.Code, option.DisplayName));
            return list;
        }

        public IReadOnlyList<FlagInfo> GetFlags()
        {
            var list = new List<FlagInfo>();
            if (flags == null) return list;
            foreach (FlagOption option in flags)
                if (option != null && !string.IsNullOrEmpty(option.Code)) list.Add(new FlagInfo(option.Code, option.DisplayName, option.Sprite));
            return list;
        }

        /// <summary>The start language code: the configured one if listed, else the first listed, else empty.</summary>
        public string StartLanguageCode
        {
            get
            {
                IReadOnlyList<LanguageInfo> all = GetLanguages();
                foreach (LanguageInfo item in all)
                    if (string.Equals(item.Code, defaultLanguageCode, StringComparison.Ordinal)) return item.Code;
                return all.Count > 0 ? all[0].Code : string.Empty;
            }
        }

        /// <summary>The start flag code: the configured one if listed, else the first listed, else empty.</summary>
        public string StartFlagCode
        {
            get
            {
                IReadOnlyList<FlagInfo> all = GetFlags();
                foreach (FlagInfo item in all)
                    if (string.Equals(item.Code, defaultFlagCode, StringComparison.Ordinal)) return item.Code;
                return all.Count > 0 ? all[0].Code : string.Empty;
            }
        }
    }
}
