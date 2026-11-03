using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// The clock of one playing emote: start, advance, release, stop. Pure C#; the menu character ticks it every frame and reads
    /// the clip time and the weight. A looping emote (the menu preview) runs until <see cref="Release"/> fades it out; a
    /// one-shot ends by itself with its fade-out. Playing a new emote while one runs keeps the current weight as the start of
    /// the fade-in, so the pose never snaps.
    /// </summary>
    public sealed class EmotePlayback
    {
        private float _length;
        private float _fadeIn;
        private float _fadeOut;
        private float _carry;
        private bool _releasing;
        private float _releaseTime;
        private float _releaseFrom;

        public bool IsPlaying { get; private set; }
        public bool Loop { get; private set; }

        /// <summary>Seconds since the emote started (keeps counting while it loops).</summary>
        public float Time { get; private set; }

        /// <summary>0..1 share of the emote on top of the menu stance (0 when nothing plays).</summary>
        public float Weight { get; private set; }

        /// <summary>Where in the clip the pose is now: wraps while looping, holds the end otherwise.</summary>
        public float ClipTime => _length <= 0f ? 0f : Loop ? Time % _length : Mathf.Min(Time, _length);

        /// <summary>Starts an emote of <paramref name="length"/> seconds; a length of 0 or less stops instead.</summary>
        public void Play(float length, float fadeIn, float fadeOut, bool loop)
        {
            if (length <= 0f)
            {
                Stop();
                return;
            }
            _carry = Weight;
            _length = length;
            _fadeIn = Mathf.Max(0f, fadeIn);
            _fadeOut = Mathf.Max(0f, fadeOut);
            Loop = loop;
            _releasing = false;
            Time = 0f;
            IsPlaying = true;
            Weight = Envelope();
        }

        /// <summary>Starts to fade out from the current weight; the emote ends when the fade is done. No effect when nothing plays.</summary>
        public void Release()
        {
            if (!IsPlaying || _releasing) return;
            if (_fadeOut <= 0f)
            {
                Stop();
                return;
            }
            _releasing = true;
            _releaseTime = 0f;
            _releaseFrom = Weight;
        }

        /// <summary>Ends at once; the caller restores the stance.</summary>
        public void Stop()
        {
            IsPlaying = false;
            Weight = 0f;
            Time = 0f;
            _carry = 0f;
            _releasing = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying) return;
            Time += deltaTime;
            if (_releasing) _releaseTime += deltaTime;
            if (!Loop && Time >= _length)
            {
                Stop();
                return;
            }
            if (_releasing && _releaseTime >= _fadeOut)
            {
                Stop();
                return;
            }
            Weight = Envelope();
        }

        private float Envelope()
        {
            if (_releasing) return _releaseFrom * (1f - EmoteSampler.Smooth(_releaseTime / _fadeOut));
            if (Loop) return _fadeIn <= 0f ? 1f : Mathf.Lerp(Mathf.Clamp01(_carry), 1f, EmoteSampler.Smooth(Time / _fadeIn));
            return EmoteSampler.Weight(Time, _length, _fadeIn, _fadeOut, _carry);
        }
    }
}
