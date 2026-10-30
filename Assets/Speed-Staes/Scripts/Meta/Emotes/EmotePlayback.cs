using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// The clock of one playing emote: start, advance, stop. Pure C#; the menu character ticks it every frame and reads the
    /// rotation and weight per bone. Playing a new emote while one runs keeps the current weight as the start of the fade-in,
    /// so the pose never snaps.
    /// </summary>
    public sealed class EmotePlayback
    {
        private EmoteDefinition _emote;
        private float _carry;

        public bool IsPlaying { get; private set; }
        public float Time { get; private set; }

        /// <summary>0..1 share of the emote on top of the menu stance (0 when nothing plays).</summary>
        public float Weight { get; private set; }

        public EmoteDefinition Current => IsPlaying ? _emote : null;

        public void Play(EmoteDefinition emote)
        {
            if (emote == null || emote.Duration <= 0f)
            {
                Stop();
                return;
            }
            _carry = Weight;
            _emote = emote;
            Time = 0f;
            IsPlaying = true;
            Weight = EmoteSampler.Weight(0f, emote.Duration, emote.BlendIn, emote.BlendOut, _carry);
        }

        /// <summary>Ends at once; the caller restores the stance.</summary>
        public void Stop()
        {
            IsPlaying = false;
            Weight = 0f;
            Time = 0f;
            _carry = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying) return;
            Time += deltaTime;
            if (Time >= _emote.Duration)
            {
                Stop();
                return;
            }
            Weight = EmoteSampler.Weight(Time, _emote.Duration, _emote.BlendIn, _emote.BlendOut, _carry);
        }

        /// <summary>Full-strength rotation of a bone now (euler degrees, runner space); the caller scales by <see cref="Weight"/>.</summary>
        public Vector3 Euler(EmoteBone bone) => IsPlaying ? EmoteSampler.Evaluate(_emote.Keys, bone, Time) : Vector3.zero;
    }
}
