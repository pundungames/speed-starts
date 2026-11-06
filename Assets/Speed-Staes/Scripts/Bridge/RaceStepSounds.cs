using System;
using SpeedStars.Core.Audio;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The sound of the player's own steps (T-120): left foot = StepLeft, right foot = StepRight, one per press. It listens to
    /// <see cref="RunnerBody.Tapped"/> of the player's body only, and only while the step is a running one: GO has come, the
    /// line is not crossed and no false-start fall is playing (<see cref="RaceSfxRules.StepAudible"/>). So the bots, the replay,
    /// the countdown, the fall and the cool-down steps after the line are silent. A plain Zenject object (no scene component),
    /// like <see cref="ArrowKeyRunnerInput"/>. The player's body is not there at the first moment and can change between races,
    /// so every tick it checks which body it should listen to and moves its subscription (released again in Dispose).
    /// </summary>
    public sealed class RaceStepSounds : ITickable, IDisposable
    {
        readonly RaceSessionAdapter _session;
        readonly ISfx _sfx;
        RunnerBody _body;

        public RaceStepSounds(RaceSessionAdapter session, [InjectOptional] ISfx sfx)
        {
            _session = session;
            _sfx = sfx;
        }

        public void Tick()
        {
            RunnerBody now = _session ? _session.PlayerBody : null;
            if (now == _body) return;
            Unbind();
            _body = now;
            if (_body) _body.Tapped += OnTapped;
        }

        public void Dispose() => Unbind();

        void Unbind()
        {
            if (_body) _body.Tapped -= OnTapped;
            _body = null;
        }

        void OnTapped(int leg, bool pressed, double simTime)
        {
            if (_sfx == null || !_session) return;
            // The first press of the race arrives while the state is still Ready (the session turns Running after it), so the
            // gate is "not counting down, not finished" rather than "Running".
            bool raceOpen = !_session.IsCountingDown && _session.State != RaceSessionState.Finished;
            if (RaceSfxRules.StepAudible(pressed, raceOpen, _session.IsFailing)) _sfx.Play(RaceSfxRules.StepFor(leg));
        }
    }
}
