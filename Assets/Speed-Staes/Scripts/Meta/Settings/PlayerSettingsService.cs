using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Settings
{
    /// <summary>
    /// Player settings on top of <c>PlayerProgress</c>. A value the player never changed reads from the settings config
    /// (defaults); a saved value that is no longer in the config lists falls back to the default. Every change is saved
    /// at once and raises <see cref="Changed"/>; setting the value that is already there does neither.
    /// </summary>
    public sealed class PlayerSettingsService : IPlayerSettings
    {
        private readonly SettingsConfig _config;
        private readonly ProgressRepository _repository;

        public PlayerSettingsService(SettingsConfig config, ProgressRepository repository)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public event Action Changed;

        public bool MusicOn => Resolve(Progress.MusicChoice, _config.DefaultMusicOn);
        public bool SfxOn => Resolve(Progress.SfxChoice, _config.DefaultSfxOn);

        public string LanguageCode
        {
            get
            {
                string saved = Progress.LanguageCode;
                return ContainsLanguage(saved) ? saved : _config.StartLanguageCode;
            }
        }

        public string FlagCode
        {
            get
            {
                string saved = Progress.FlagCode;
                return ContainsFlag(saved) ? saved : _config.StartFlagCode;
            }
        }

        public string PlayerName
        {
            get
            {
                string saved = Progress.PlayerName;
                return string.IsNullOrEmpty(saved) ? _config.DefaultPlayerName : saved;
            }
        }

        public int MaxNameLength => _config.NameMaxLength;
        public IReadOnlyList<LanguageInfo> Languages => _config.GetLanguages();
        public IReadOnlyList<FlagInfo> Flags => _config.GetFlags();

        public void SetMusic(bool on)
        {
            if (MusicOn == on) return;
            Progress.MusicChoice = ToChoice(on);
            Commit();
        }

        public void SetSfx(bool on)
        {
            if (SfxOn == on) return;
            Progress.SfxChoice = ToChoice(on);
            Commit();
        }

        public bool SetLanguage(string code)
        {
            if (!ContainsLanguage(code)) return false;
            if (string.Equals(LanguageCode, code, StringComparison.Ordinal)) return true;
            Progress.LanguageCode = code;
            Commit();
            return true;
        }

        public bool SetFlag(string code)
        {
            if (!ContainsFlag(code)) return false;
            if (string.Equals(FlagCode, code, StringComparison.Ordinal)) return true;
            Progress.FlagCode = code;
            Commit();
            return true;
        }

        public PlayerNameResult ValidateName(string raw) =>
            PlayerNameRules.Validate(raw, _config.NameMinLength, _config.NameMaxLength);

        public PlayerNameResult TrySetName(string raw)
        {
            PlayerNameResult result = ValidateName(raw);
            if (result != PlayerNameResult.Valid) return result;

            string name = PlayerNameRules.Normalize(raw);
            if (string.Equals(PlayerName, name, StringComparison.Ordinal)) return result;
            Progress.PlayerName = name;
            Commit();
            return result;
        }

        private void Commit()
        {
            _repository.Save();
            Changed?.Invoke();
        }

        private static bool Resolve(SettingChoice choice, bool fallback) =>
            choice == SettingChoice.Default ? fallback : choice == SettingChoice.On;

        private static SettingChoice ToChoice(bool on) => on ? SettingChoice.On : SettingChoice.Off;

        private bool ContainsLanguage(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (LanguageInfo info in _config.GetLanguages())
                if (string.Equals(info.Code, code, StringComparison.Ordinal)) return true;
            return false;
        }

        private bool ContainsFlag(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            foreach (FlagInfo info in _config.GetFlags())
                if (string.Equals(info.Code, code, StringComparison.Ordinal)) return true;
            return false;
        }

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the settings service is used.");
    }
}
