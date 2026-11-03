using System;
using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using UnityEngine;

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
        public float CameraDistance => Progress.CameraDistance > 0f ? Progress.CameraDistance : _config.DefaultCameraDistance;
        public float CameraDistanceMin => _config.MinCameraDistance;
        public float CameraDistanceMax => _config.MaxCameraDistance;

        public CompetitorType CompetitorType => Resolve(Progress.TimeTrialChoice, _config.DefaultTimeTrial) ? CompetitorType.TimeTrial : CompetitorType.Bots;
        public float CompetitorDifficulty => SettingsRules.UnitOrFallback(Progress.CompetitorDifficulty, _config.DefaultCompetitorDifficulty);
        public float CompetitorSkillRange => SettingsRules.UnitOrFallback(Progress.CompetitorSkillRange, _config.DefaultCompetitorSkillRange);
        public bool HasCompetitorTuning => Progress.CompetitorDifficulty >= 0f || Progress.CompetitorSkillRange >= 0f;
        public int LanePreference => SettingsRules.ClampLane(Progress.LanePreference, _config.LaneChoiceCount);
        public int LaneChoiceCount => _config.LaneChoiceCount;
        public float HurdleSensitivity => SettingsRules.UnitOrFallback(Progress.HurdleSensitivity, _config.DefaultHurdleSensitivity);
        public float SwipeUpDistance => SettingsRules.SwipeDistance(HurdleSensitivity, _config.SwipeDistanceAtFullSensitivity, _config.SwipeDistanceAtZeroSensitivity);
        public bool ParticleEffects => Resolve(Progress.ParticleChoice, _config.DefaultParticleEffects);
        public bool ShowEmoteButton => Resolve(Progress.EmoteButtonChoice, _config.DefaultShowEmoteButton);
        public bool AutoEmoteSelf => Resolve(Progress.AutoEmoteSelfChoice, _config.DefaultAutoEmoteSelf);
        public bool AutoEmoteCompetitors => Resolve(Progress.AutoEmoteCompetitorsChoice, _config.DefaultAutoEmoteCompetitors);

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

        public void SetCameraDistance(float value)
        {
            float clamped = Mathf.Clamp(value, _config.MinCameraDistance, _config.MaxCameraDistance);
            if (Mathf.Approximately(CameraDistance, clamped)) return;
            Progress.CameraDistance = clamped;
            Commit();
        }

        public void SetCompetitorType(CompetitorType type)
        {
            if (CompetitorType == type) return;
            Progress.TimeTrialChoice = ToChoice(type == CompetitorType.TimeTrial);
            Commit();
        }

        public void SetCompetitorDifficulty(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Progress.CompetitorDifficulty >= 0f && Mathf.Approximately(Progress.CompetitorDifficulty, clamped)) return;
            Progress.CompetitorDifficulty = clamped;
            Commit();
        }

        public void SetCompetitorSkillRange(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Progress.CompetitorSkillRange >= 0f && Mathf.Approximately(Progress.CompetitorSkillRange, clamped)) return;
            Progress.CompetitorSkillRange = clamped;
            Commit();
        }

        public void SetLanePreference(int lane)
        {
            int clamped = SettingsRules.ClampLane(lane, _config.LaneChoiceCount);
            if (LanePreference == clamped) return;
            Progress.LanePreference = clamped;
            Commit();
        }

        public void SetHurdleSensitivity(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Progress.HurdleSensitivity >= 0f && Mathf.Approximately(Progress.HurdleSensitivity, clamped)) return;
            Progress.HurdleSensitivity = clamped;
            Commit();
        }

        public void SetParticleEffects(bool on)
        {
            if (ParticleEffects == on) return;
            Progress.ParticleChoice = ToChoice(on);
            Commit();
        }

        public void SetShowEmoteButton(bool on)
        {
            if (ShowEmoteButton == on) return;
            Progress.EmoteButtonChoice = ToChoice(on);
            Commit();
        }

        public void SetAutoEmoteSelf(bool on)
        {
            if (AutoEmoteSelf == on) return;
            Progress.AutoEmoteSelfChoice = ToChoice(on);
            Commit();
        }

        public void SetAutoEmoteCompetitors(bool on)
        {
            if (AutoEmoteCompetitors == on) return;
            Progress.AutoEmoteCompetitorsChoice = ToChoice(on);
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
