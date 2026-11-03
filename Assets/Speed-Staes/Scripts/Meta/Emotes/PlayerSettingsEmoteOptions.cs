using System;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// The race emote switches (<see cref="IEmoteRaceOptions"/>) read straight from the player's settings (T-088), so the emote code
    /// never depends on the settings contract. Every read is live; <c>Changed</c> is the settings' own event, passed on.
    /// </summary>
    public sealed class PlayerSettingsEmoteOptions : IEmoteRaceOptions
    {
        private readonly IPlayerSettings _settings;

        public PlayerSettingsEmoteOptions(IPlayerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool ShowEmoteButton => _settings.ShowEmoteButton;
        public bool AutoEmoteSelf => _settings.AutoEmoteSelf;
        public bool AutoEmoteCompetitors => _settings.AutoEmoteCompetitors;

        public event Action Changed
        {
            add => _settings.Changed += value;
            remove => _settings.Changed -= value;
        }
    }
}
