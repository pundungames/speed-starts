using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Audio;
using SpeedStars.Meta.Contracts;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.Audio
{
    /// <summary>
    /// The sound and haptic rules (T-052) with a fake settings service, fake output and fake clock: effects off = silent, music follows
    /// its own switch at once, the same sound once per frame and not within the repeat time, empty clips are safe, buzzes keep their
    /// interval. Loudness and millisecond values come from the test's own config, so no shipped number is asserted.
    /// </summary>
    public sealed class AudioServiceTests
    {
        private sealed class FakeSettings : IPlayerSettings
        {
            public bool Music = true;
            public bool Sfx = true;
            public event Action Changed;

            public bool MusicOn => Music;
            public bool SfxOn => Sfx;
            public string LanguageCode => "en";
            public string FlagCode => "TR";
            public string PlayerName => "Player";
            public int MaxNameLength => 16;
            public IReadOnlyList<LanguageInfo> Languages => new List<LanguageInfo>();
            public IReadOnlyList<FlagInfo> Flags => new List<FlagInfo>();

            public void SetMusic(bool on) { Music = on; Changed?.Invoke(); }
            public void SetSfx(bool on) { Sfx = on; Changed?.Invoke(); }
            public bool SetLanguage(string code) => false;
            public bool SetFlag(string code) => false;
            public PlayerNameResult ValidateName(string raw) => PlayerNameResult.Valid;
            public PlayerNameResult TrySetName(string raw) => PlayerNameResult.Valid;
        }

        private sealed class FakeOutput : IAudioOutput
        {
            public readonly List<AudioClip> Played = new List<AudioClip>();
            public readonly List<float> Volumes = new List<float>();
            public AudioClip MusicClip;
            public float MusicVolume;
            public bool MusicAudible;
            public int MusicCalls;

            public void PlayOneShot(AudioClip clip, float volume)
            {
                Played.Add(clip);
                Volumes.Add(volume);
            }

            public void SetMusic(AudioClip clip, float volume, bool audible)
            {
                MusicClip = clip;
                MusicVolume = volume;
                MusicAudible = audible;
                MusicCalls++;
            }
        }

        private sealed class FakeDevice : IHapticsDevice
        {
            public readonly List<int> Milliseconds = new List<int>();
            public readonly List<int> Amplitudes = new List<int>();

            public void Vibrate(int milliseconds, int amplitude)
            {
                Milliseconds.Add(milliseconds);
                Amplitudes.Add(amplitude);
            }
        }

        private sealed class FakeClock : IAudioClock
        {
            public int Frame { get; set; }
            public float Now { get; set; }
        }

        private const float RepeatSeconds = 0.5f;
        private const float BuzzIntervalSeconds = 0.25f;

        private readonly List<UnityEngine.Object> _made = new List<UnityEngine.Object>();
        private FakeSettings _settings;
        private FakeOutput _output;
        private FakeClock _clock;
        private AudioClip _tapClip;
        private AudioClip _musicClip;

        [SetUp]
        public void SetUp()
        {
            _settings = new FakeSettings();
            _output = new FakeOutput();
            _clock = new FakeClock { Frame = 10, Now = 100f };
            _tapClip = MakeClip("tap");
            _musicClip = MakeClip("music");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object made in _made)
                if (made != null) UnityEngine.Object.DestroyImmediate(made);
            _made.Clear();
        }

        private AudioClip MakeClip(string name)
        {
            AudioClip clip = AudioClip.Create(name, 64, 1, 44100, false);
            _made.Add(clip);
            return clip;
        }

        /// <summary>A config with a Tap cue (and an empty Denied cue); music clip, haptic length and intervals as given.</summary>
        private AudioConfig MakeConfig(AudioClip music, int hapticMs = 20)
        {
            var config = ScriptableObject.CreateInstance<AudioConfig>();
            _made.Add(config);
            var so = new SerializedObject(config);
            SerializedProperty cues = so.FindProperty("cues");
            cues.arraySize = 2;
            SetCue(cues.GetArrayElementAtIndex(0), SfxId.Tap, _tapClip, 0.7f);
            SetCue(cues.GetArrayElementAtIndex(1), SfxId.Denied, null, 1f); // empty clip: must stay silent, not throw
            so.FindProperty("minRepeatSeconds").floatValue = RepeatSeconds;
            so.FindProperty("musicClip").objectReferenceValue = music;
            so.FindProperty("musicVolume").floatValue = 0.4f;
            so.FindProperty("hapticMilliseconds").intValue = hapticMs;
            so.FindProperty("hapticAmplitude").intValue = 90;
            so.FindProperty("hapticMinIntervalSeconds").floatValue = BuzzIntervalSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static void SetCue(SerializedProperty cue, SfxId id, AudioClip clip, float volume)
        {
            cue.FindPropertyRelative("id").enumValueIndex = (int)id;
            cue.FindPropertyRelative("clip").objectReferenceValue = clip;
            cue.FindPropertyRelative("volume").floatValue = volume;
        }

        private AudioService MakeService(AudioConfig config, IPlayerSettings settings)
        {
            return new AudioService(config, _output, settings, _clock);
        }

        // ---------------------------------------------------------------- effects

        [Test]
        public void Sfx_Off_DoesNotPlay_AndPlaysAgainWhenSwitchedOn()
        {
            AudioService service = MakeService(MakeConfig(null), _settings);

            _settings.SetSfx(false);
            service.Play(SfxId.Tap);
            Assert.IsEmpty(_output.Played);

            _settings.SetSfx(true);
            service.Play(SfxId.Tap);
            Assert.AreEqual(1, _output.Played.Count);
            Assert.AreSame(_tapClip, _output.Played[0]);
        }

        [Test]
        public void Sfx_UsesTheCueVolumeFromTheConfig()
        {
            AudioConfig config = MakeConfig(null);
            AudioService service = MakeService(config, _settings);

            service.Play(SfxId.Tap);

            config.TryGetCue(SfxId.Tap, out SfxCue cue);
            Assert.AreEqual(cue.Volume, _output.Volumes[0], 1e-5f);
        }

        [Test]
        public void Sfx_EmptyClipAndMissingCue_AreSilentAndSafe()
        {
            AudioService service = MakeService(MakeConfig(null), _settings);

            Assert.DoesNotThrow(() => service.Play(SfxId.Denied)); // cue with no clip
            Assert.DoesNotThrow(() => service.Play(SfxId.Reward)); // no cue at all
            Assert.IsEmpty(_output.Played);
        }

        [Test]
        public void Sfx_SameFrame_PlaysOnce()
        {
            AudioConfig config = MakeConfig(null);
            AudioService service = MakeService(config, _settings);

            service.Play(SfxId.Tap);
            _clock.Now += config.MinRepeatSeconds * 4f; // far past the repeat time: only the frame rule can stop it
            service.Play(SfxId.Tap);

            Assert.AreEqual(1, _output.Played.Count);
        }

        [Test]
        public void Sfx_NextFrame_RespectsTheRepeatTime()
        {
            AudioConfig config = MakeConfig(null);
            AudioService service = MakeService(config, _settings);

            service.Play(SfxId.Tap);
            _clock.Frame++;
            _clock.Now += config.MinRepeatSeconds * 0.5f; // too soon
            service.Play(SfxId.Tap);
            Assert.AreEqual(1, _output.Played.Count);

            _clock.Frame++;
            _clock.Now += config.MinRepeatSeconds; // long enough now
            service.Play(SfxId.Tap);
            Assert.AreEqual(2, _output.Played.Count);
        }

        [Test]
        public void Sfx_FirstPlayOnFrameZero_Sounds()
        {
            _clock.Frame = 0;
            _clock.Now = 0f;
            AudioService service = MakeService(MakeConfig(null), _settings);

            service.Play(SfxId.Tap);

            Assert.AreEqual(1, _output.Played.Count);
        }

        [Test]
        public void Sfx_WithoutSettings_Plays()
        {
            AudioService service = MakeService(MakeConfig(null), null);

            service.Play(SfxId.Tap);

            Assert.AreEqual(1, _output.Played.Count);
        }

        [Test]
        public void Sfx_AfterDispose_IsSilent()
        {
            AudioService service = MakeService(MakeConfig(null), _settings);
            service.Dispose();

            service.Play(SfxId.Tap);

            Assert.IsEmpty(_output.Played);
        }

        // ---------------------------------------------------------------- music

        [Test]
        public void Music_FollowsItsOwnSwitch_AtOnce()
        {
            AudioService service = MakeService(MakeConfig(_musicClip), _settings);
            Assert.IsTrue(_output.MusicAudible);
            Assert.AreSame(_musicClip, _output.MusicClip);

            _settings.SetMusic(false);
            Assert.IsFalse(_output.MusicAudible);
            Assert.IsFalse(((IMusic)service).IsAudible);

            _settings.SetMusic(true);
            Assert.IsTrue(_output.MusicAudible);
            Assert.IsTrue(((IMusic)service).IsAudible);
        }

        [Test]
        public void Music_Switch_DoesNotMuteEffects_AndEffectsSwitchDoesNotMuteMusic()
        {
            AudioService service = MakeService(MakeConfig(_musicClip), _settings);

            _settings.SetMusic(false);
            service.Play(SfxId.Tap);
            Assert.AreEqual(1, _output.Played.Count);

            _settings.SetMusic(true);
            _settings.SetSfx(false);
            Assert.IsTrue(_output.MusicAudible);
        }

        [Test]
        public void Music_NoClipInConfig_IsSilentAndSafe()
        {
            AudioService service = MakeService(MakeConfig(null), _settings);

            Assert.IsFalse(_output.MusicAudible);
            Assert.IsNull(_output.MusicClip);
            Assert.DoesNotThrow(() => _settings.SetMusic(true));
            Assert.IsFalse(((IMusic)service).IsAudible);
        }

        [Test]
        public void Music_StopAndPlay_ChangeWhatIsWanted()
        {
            AudioService service = MakeService(MakeConfig(_musicClip), _settings);
            var music = (IMusic)service;

            music.Stop();
            Assert.IsFalse(_output.MusicAudible);

            music.Play();
            Assert.IsTrue(_output.MusicAudible);

            _settings.SetMusic(false);
            music.Play(); // wanted, but the player's switch still wins
            Assert.IsFalse(_output.MusicAudible);
        }

        [Test]
        public void Dispose_SilencesMusic_AndStopsListeningToSettings()
        {
            AudioService service = MakeService(MakeConfig(_musicClip), _settings);
            service.Dispose();
            Assert.IsFalse(_output.MusicAudible);

            int calls = _output.MusicCalls;
            _settings.SetMusic(false);
            _settings.SetMusic(true);
            Assert.AreEqual(calls, _output.MusicCalls);
        }

        // ---------------------------------------------------------------- haptics

        [Test]
        public void Haptics_FollowTheEffectsSwitch()
        {
            var device = new FakeDevice();
            var haptics = new HapticsService(MakeConfig(null), device, _settings, _clock);

            _settings.SetSfx(false);
            haptics.Tap();
            Assert.IsEmpty(device.Milliseconds);

            _settings.SetSfx(true);
            haptics.Tap();
            Assert.AreEqual(1, device.Milliseconds.Count);
        }

        [Test]
        public void Haptics_UseTheConfigLengthAndStrength()
        {
            var device = new FakeDevice();
            AudioConfig config = MakeConfig(null);
            new HapticsService(config, device, _settings, _clock).Tap();

            Assert.AreEqual(config.HapticMilliseconds, device.Milliseconds[0]);
            Assert.AreEqual(config.HapticAmplitude, device.Amplitudes[0]);
        }

        [Test]
        public void Haptics_KeepTheirInterval()
        {
            var device = new FakeDevice();
            AudioConfig config = MakeConfig(null);
            var haptics = new HapticsService(config, device, _settings, _clock);

            haptics.Tap();
            _clock.Now += config.HapticMinIntervalSeconds * 0.5f;
            haptics.Tap();
            Assert.AreEqual(1, device.Milliseconds.Count);

            _clock.Now += config.HapticMinIntervalSeconds;
            haptics.Tap();
            Assert.AreEqual(2, device.Milliseconds.Count);
        }

        [Test]
        public void Haptics_ZeroLength_NeverBuzzes()
        {
            var device = new FakeDevice();
            var haptics = new HapticsService(MakeConfig(null, hapticMs: 0), device, _settings, _clock);

            haptics.Tap();

            Assert.IsEmpty(device.Milliseconds);
        }

        [Test]
        public void NullDevice_DoesNothingAndDoesNotThrow()
        {
            Assert.DoesNotThrow(() => new NullHapticsDevice().Vibrate(10, 50));
        }
    }
}
