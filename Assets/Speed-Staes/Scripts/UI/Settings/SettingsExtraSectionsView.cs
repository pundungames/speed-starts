using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// The settings v2 sections of the settings window (one node of the prefab holding their headings and rows): Competitors,
    /// Gameplay, Visuals, Emote and the new Reset options rows. Binds the rows of the prefab to <see cref="IPlayerSettings"/>
    /// (every change is saved at once by the service) and builds nothing. The reset rows only ask the window to confirm: the
    /// window owns the confirmation dialog and runs the reset.
    /// </summary>
    public sealed class SettingsExtraSectionsView : MonoBehaviour
    {
        [Header("Competitors")]
        [Tooltip("Competitor type row: Bots or Time trial.")]
        [SerializeField] private SettingsChoiceRowView competitorTypeRow;

        [Tooltip("Difficulty slider row.")]
        [SerializeField] private SettingsSliderRowView difficultyRow;

        [Tooltip("Skill range slider row.")]
        [SerializeField] private SettingsSliderRowView skillRangeRow;

        [Header("Gameplay")]
        [Tooltip("Lane preference row: Auto, 1, 2...")]
        [SerializeField] private SettingsChoiceRowView laneRow;

        [Tooltip("Hurdle input sensitivity slider row.")]
        [SerializeField] private SettingsSliderRowView hurdleRow;

        [Header("Visuals")]
        [Tooltip("Particle effects switch row.")]
        [SerializeField] private SettingsToggleRowView particlesRow;

        [Header("Emote")]
        [Tooltip("Show emote button switch row.")]
        [SerializeField] private SettingsToggleRowView showEmoteRow;

        [Tooltip("Emote automatically: myself, switch row.")]
        [SerializeField] private SettingsToggleRowView autoSelfRow;

        [Tooltip("Emote automatically: competitors, switch row.")]
        [SerializeField] private SettingsToggleRowView autoCompetitorsRow;

        [Header("Reset options")]
        [Tooltip("Clear custom racers (tap area).")]
        [SerializeField] private SettingsActionRowView clearRacersRow;

        [Tooltip("Row that picks the event whose personal best the next row resets.")]
        [SerializeField] private SettingsChoiceRowView resetEventRow;

        [Tooltip("Reset a personal best (tap area): resets the event picked above.")]
        [SerializeField] private SettingsActionRowView resetBestRow;

        private IPlayerSettings _settings;
        private readonly List<RaceEventId> _eventIds = new List<RaceEventId>();
        private readonly List<string> _eventNames = new List<string>();

        /// <summary>The player asked to clear the custom racers (the window confirms, then runs it).</summary>
        public event Action ClearRacersRequested;

        /// <summary>The player asked to reset one event's personal best: the event id and its name.</summary>
        public event Action<RaceEventId, string> ResetBestRequested;

        /// <summary>Binds every row. <paramref name="events"/> may be null (the personal best pick then stays empty and unusable).</summary>
        public void Bind(IPlayerSettings settings, RaceEventCatalog events, bool canReset)
        {
            Unbind();
            _settings = settings;
            if (_settings == null) return;

            competitorTypeRow.Bind(SettingsTexts.CompetitorType, SettingsTexts.CompetitorTypeOptions, (int)_settings.CompetitorType,
                index => _settings.SetCompetitorType((CompetitorType)index));
            difficultyRow.Bind(SettingsTexts.Difficulty, 0f, 1f, _settings.CompetitorDifficulty, _settings.SetCompetitorDifficulty);
            skillRangeRow.Bind(SettingsTexts.SkillRange, 0f, 1f, _settings.CompetitorSkillRange, _settings.SetCompetitorSkillRange);

            laneRow.Bind(SettingsTexts.LanePreference, SettingsTexts.LaneOptions(_settings.LaneChoiceCount), _settings.LanePreference,
                _settings.SetLanePreference);
            hurdleRow.Bind(SettingsTexts.HurdleSensitivity, 0f, 1f, _settings.HurdleSensitivity, _settings.SetHurdleSensitivity);

            particlesRow.Bind(SettingsTexts.ParticleEffects, _settings.ParticleEffects, _settings.SetParticleEffects);
            showEmoteRow.Bind(SettingsTexts.ShowEmoteButton, _settings.ShowEmoteButton, _settings.SetShowEmoteButton);
            autoSelfRow.Bind(SettingsTexts.EmoteMyself, _settings.AutoEmoteSelf, _settings.SetAutoEmoteSelf);
            autoCompetitorsRow.Bind(SettingsTexts.EmoteCompetitors, _settings.AutoEmoteCompetitors, _settings.SetAutoEmoteCompetitors);

            CollectEvents(events);
            resetEventRow.Bind(SettingsTexts.PersonalBestOf, _eventNames, 0, null);
            clearRacersRow.Bind(SettingsTexts.ClearCustomRacers, () => ClearRacersRequested?.Invoke(), canReset);
            resetBestRow.Bind(SettingsTexts.ResetAPersonalBest, OnResetBest, canReset && _eventIds.Count > 0);

            _settings.Changed += Refresh;
        }

        private void OnDisable() => Unbind();

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_settings != null) _settings.Changed -= Refresh;
            _settings = null;
        }

        /// <summary>Shows the saved values again (a value changed elsewhere).</summary>
        private void Refresh()
        {
            if (_settings == null) return;
            competitorTypeRow.SetIndex((int)_settings.CompetitorType);
            difficultyRow.SetValue(_settings.CompetitorDifficulty);
            skillRangeRow.SetValue(_settings.CompetitorSkillRange);
            laneRow.SetIndex(_settings.LanePreference);
            hurdleRow.SetValue(_settings.HurdleSensitivity);
            particlesRow.SetOn(_settings.ParticleEffects);
            showEmoteRow.SetOn(_settings.ShowEmoteButton);
            autoSelfRow.SetOn(_settings.AutoEmoteSelf);
            autoCompetitorsRow.SetOn(_settings.AutoEmoteCompetitors);
        }

        private void CollectEvents(RaceEventCatalog events)
        {
            _eventIds.Clear();
            _eventNames.Clear();
            if (events == null) return;
            foreach (var definition in events.GetEvents())
            {
                if (definition.Id.IsEmpty) continue;
                _eventIds.Add(definition.Id);
                _eventNames.Add(definition.DisplayName);
            }
        }

        private void OnResetBest()
        {
            int index = resetEventRow.Index;
            if (index < 0 || index >= _eventIds.Count) return;
            ResetBestRequested?.Invoke(_eventIds[index], _eventNames[index]);
        }
    }
}
