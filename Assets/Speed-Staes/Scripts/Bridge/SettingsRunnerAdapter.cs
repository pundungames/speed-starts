using System.Collections.Generic;
using SpeedStars.Meta.Contracts;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Carries two player settings onto the race runners without touching the mechanic's files: the hurdle sensitivity goes to the
    /// player's <c>RunnerInput.swipeUpDistance</c>, and "Particle effects" off switches the effect components off (step feedback
    /// on the player, premium sparkles and shoe trails on every runner). Applied when bound, when a setting changes and at the
    /// start of every round (the outfit and the feedback components may be new by then); never polled. A component this adapter
    /// switched off is the only one it switches back on, so a component that disabled itself (missing material) stays off.
    /// </summary>
    public sealed class SettingsRunnerAdapter : MonoBehaviour
    {
        IPlayerSettings _settings;
        RunnerBody _player;
        bool _subscribed;
        readonly List<Behaviour> _scratch = new List<Behaviour>();
        readonly HashSet<Behaviour> _switchedOff = new HashSet<Behaviour>();

        /// <summary>Idempotent. Both arguments may be null (nothing happens then).</summary>
        public void Bind(IPlayerSettings settings, RunnerBody player)
        {
            Unsubscribe();
            _settings = settings;
            _player = player;
            Subscribe();
            Apply();
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (_subscribed || _settings == null || !isActiveAndEnabled) return;
            _settings.Changed += Apply;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_settings != null) _settings.Changed -= Apply;
            _subscribed = false;
        }

        /// <summary>Puts the current settings onto the runners. Safe to call any time.</summary>
        public void Apply()
        {
            if (_settings == null) return;
            if (_player && _player.input) _player.input.swipeUpDistance = _settings.SwipeUpDistance;
            ApplyParticles(_settings.ParticleEffects);
        }

        void ApplyParticles(bool on)
        {
            _scratch.Clear();
            if (_player)
            {
                _scratch.AddRange(_player.GetComponentsInChildren<PerfectStepFeedback>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<EarlyStepFeedback>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<LateStepFeedback>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<MomentumWindFeedback>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<SweatFeedback>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<PremiumCosmeticSparkles>(true));
                _scratch.AddRange(_player.GetComponentsInChildren<PremiumShoeTrails>(true));
            }
            _scratch.AddRange(GetComponentsInChildren<PremiumCosmeticSparkles>(true)); // the bots live under the RaceBots object
            _scratch.AddRange(GetComponentsInChildren<PremiumShoeTrails>(true));

            foreach (Behaviour effect in _scratch)
            {
                if (!effect) continue;
                if (!on)
                {
                    if (effect.enabled) _switchedOff.Add(effect);
                    effect.enabled = false; // OnDisable of each component clears what it was showing
                    StopParticles(effect);
                }
                else if (_switchedOff.Remove(effect))
                {
                    effect.enabled = true;
                }
            }
            _switchedOff.RemoveWhere(effect => !effect);
        }

        /// <summary>The sparkle component can be refreshed (and started) while disabled; make sure nothing keeps emitting.</summary>
        static void StopParticles(Behaviour effect)
        {
            if (!(effect is PremiumCosmeticSparkles)) return;
            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                if (particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
