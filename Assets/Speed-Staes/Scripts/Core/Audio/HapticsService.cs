using System;
using SpeedStars.Meta.Contracts;
using Zenject;

namespace SpeedStars.Core.Audio
{
    /// <summary>
    /// The light buzz of a button press. There is no separate haptics switch (supervisor decision): it follows the
    /// sound-effects switch. Length and strength come from the config; buzzes closer than the config's interval are dropped
    /// so quick tapping does not become one long vibration.
    /// </summary>
    public sealed class HapticsService : IHaptics
    {
        private readonly AudioConfig _config;
        private readonly IHapticsDevice _device;
        private readonly IPlayerSettings _settings;
        private readonly IAudioClock _clock;
        private float _lastTime = float.NegativeInfinity;

        [Inject]
        public HapticsService(AudioConfig config, IHapticsDevice device, [InjectOptional] IPlayerSettings settings)
            : this(config, device, settings, new UnityAudioClock())
        {
        }

        public HapticsService(AudioConfig config, IHapticsDevice device, IPlayerSettings settings, IAudioClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _settings = settings;
        }

        public void Tap()
        {
            if (_settings != null && !_settings.SfxOn) return;
            if (_config.HapticMilliseconds <= 0) return;

            float now = _clock.Now;
            if (now - _lastTime < _config.HapticMinIntervalSeconds) return;

            _lastTime = now;
            _device.Vibrate(_config.HapticMilliseconds, _config.HapticAmplitude);
        }
    }
}
