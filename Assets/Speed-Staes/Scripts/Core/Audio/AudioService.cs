using System;
using SpeedStars.Meta.Contracts;
using Zenject;

namespace SpeedStars.Core.Audio
{
    /// <summary>
    /// Sound effects and music rules, free of the engine: what is heard is decided here from the config and the player's two
    /// switches, and handed to an <see cref="IAudioOutput"/>. A switch change applies at once (the settings window raises
    /// Changed). Rules: effects off = nothing plays; the same sound plays once per frame and not again within the config's
    /// repeat time; an id without a clip is silent; music off or no music clip = silent. Without a settings service both
    /// switches count as on.
    /// </summary>
    public sealed class AudioService : ISfx, IMusic, IDisposable
    {
        private readonly AudioConfig _config;
        private readonly IAudioOutput _output;
        private readonly IPlayerSettings _settings;
        private readonly IAudioClock _clock;
        private readonly int[] _lastFrame;
        private readonly float[] _lastTime;
        private bool _musicWanted = true;
        private bool _musicAudible;
        private bool _disposed;

        [Inject]
        public AudioService(AudioConfig config, IAudioOutput output, [InjectOptional] IPlayerSettings settings)
            : this(config, output, settings, new UnityAudioClock())
        {
        }

        public AudioService(AudioConfig config, IAudioOutput output, IPlayerSettings settings, IAudioClock clock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _settings = settings;

            int count = Enum.GetValues(typeof(SfxId)).Length;
            _lastFrame = new int[count];
            _lastTime = new float[count];
            for (int i = 0; i < count; i++)
            {
                _lastFrame[i] = int.MinValue; // "never played": frame 0 must still sound
                _lastTime[i] = float.NegativeInfinity;
            }

            if (_settings != null) _settings.Changed += OnSettingsChanged;
            ApplyMusic();
        }

        public bool IsAudible => _musicAudible;

        public void Play(SfxId id)
        {
            if (_disposed) return;
            if (_settings != null && !_settings.SfxOn) return;
            if (!_config.TryGetCue(id, out SfxCue cue) || cue.Clip == null) return;

            int index = (int)id;
            int frame = _clock.Frame;
            float now = _clock.Now;
            if (_lastFrame[index] == frame) return;
            if (now - _lastTime[index] < _config.MinRepeatSeconds) return;

            _lastFrame[index] = frame;
            _lastTime[index] = now;
            _output.PlayOneShot(cue.Clip, cue.Volume);
        }

        void IMusic.Play()
        {
            _musicWanted = true;
            ApplyMusic();
        }

        void IMusic.Stop()
        {
            _musicWanted = false;
            ApplyMusic();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_settings != null) _settings.Changed -= OnSettingsChanged;
            _output.SetMusic(null, 0f, false);
        }

        private void OnSettingsChanged()
        {
            if (!_disposed) ApplyMusic();
        }

        private void ApplyMusic()
        {
            UnityEngine.AudioClip clip = _config.MusicClip;
            _musicAudible = _musicWanted && clip != null && (_settings == null || _settings.MusicOn);
            _output.SetMusic(clip, _config.MusicVolume, _musicAudible);
        }
    }
}
