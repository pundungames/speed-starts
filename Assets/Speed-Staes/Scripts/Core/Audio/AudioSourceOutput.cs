using System;
using UnityEngine;

namespace SpeedStars.Core.Audio
{
    /// <summary>
    /// The engine side of the sound system: one persistent GameObject (not UI, so scene changes and screen hides never cut it)
    /// with an effects source and a looping music source. Created at the first use; outside Play mode it only exists while used.
    /// Music is kept playing and muted instead of stopped, so switching it on again needs no restart and no seek.
    /// </summary>
    public sealed class AudioSourceOutput : IAudioOutput, IDisposable
    {
        private const string ObjectName = "AudioService";

        private GameObject _root;
        private AudioSource _sfx;
        private AudioSource _music;

        public void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null) return;
            EnsureObjects();
            _sfx.PlayOneShot(clip, volume);
        }

        public void SetMusic(AudioClip clip, float volume, bool audible)
        {
            if (clip == null && _music == null) return; // nothing to stop yet; do not create the object for silence
            EnsureObjects();

            if (_music.clip != clip)
            {
                _music.Stop();
                _music.clip = clip;
            }
            _music.volume = volume;
            _music.mute = !audible;
            if (clip != null && !_music.isPlaying) _music.Play();
        }

        public void Dispose()
        {
            if (_root != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_root);
                else UnityEngine.Object.DestroyImmediate(_root); // Destroy is not allowed outside Play mode
            }
            _root = null;
            _sfx = null;
            _music = null;
        }

        private void EnsureObjects()
        {
            if (_root != null) return;

            _root = new GameObject(ObjectName);
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(_root);
            _sfx = AddSource(_root, loop: false);
            _music = AddSource(_root, loop: true);
        }

        private static AudioSource AddSource(GameObject root, bool loop)
        {
            AudioSource source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // plain 2D: UI sounds have no place in the world
            return source;
        }
    }
}
