using UnityEngine;

namespace SpeedStars.Core.Audio
{
    /// <summary>The short sounds the game plays. The WAV file of each one carries its name (Audio/Sfx/&lt;name&gt;.wav).</summary>
    public enum SfxId
    {
        /// <summary>A button went down.</summary>
        Tap,
        /// <summary>A button that cannot be used went down.</summary>
        Denied,
        /// <summary>A daily gift was taken.</summary>
        Claim,
        /// <summary>Something was bought.</summary>
        Purchase,
        CountdownReady,
        CountdownSet,
        CountdownGo,
        /// <summary>A bigger reward (the 7-day claim).</summary>
        Reward
    }

    /// <summary>Plays the short sound effects. Silent while the player's sound switch is off.</summary>
    public interface ISfx
    {
        void Play(SfxId id);
    }

    /// <summary>The one looping music source. Silent while the player's music switch is off or the config has no clip.</summary>
    public interface IMusic
    {
        /// <summary>True while the music is actually heard (wanted, switch on, a clip in the config).</summary>
        bool IsAudible { get; }

        /// <summary>Wants the music (the default after creation); it sounds only while the switch is on.</summary>
        void Play();

        /// <summary>Does not want the music (e.g. for a quiet screen); the switch has no say then.</summary>
        void Stop();
    }

    /// <summary>A very short buzz. Follows the sound-effects switch (there is no haptics switch).</summary>
    public interface IHaptics
    {
        void Tap();
    }

    /// <summary>The engine side of <see cref="AudioService"/>: kept apart so the rules are tested without a sound card.</summary>
    public interface IAudioOutput
    {
        void PlayOneShot(AudioClip clip, float volume);

        /// <summary>Sets what the looping music source holds and whether it is heard. A null clip stops it.</summary>
        void SetMusic(AudioClip clip, float volume, bool audible);
    }

    /// <summary>The device side of <see cref="HapticsService"/>.</summary>
    public interface IHapticsDevice
    {
        void Vibrate(int milliseconds, int amplitude);
    }

    /// <summary>Frame and real time, so same-frame and repeat rules can be tested.</summary>
    public interface IAudioClock
    {
        int Frame { get; }

        /// <summary>Seconds on a clock that ignores the time scale (a paused game still has sounds).</summary>
        float Now { get; }
    }

    internal sealed class UnityAudioClock : IAudioClock
    {
        public int Frame => Time.frameCount;
        public float Now => Time.unscaledTime;
    }
}
