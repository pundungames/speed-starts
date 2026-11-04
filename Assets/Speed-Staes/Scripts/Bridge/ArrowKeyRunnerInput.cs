using System;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using UnityEngine;
using Zenject;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Left arrow = left foot (leg 0), right arrow = right foot (leg 1) for the race scene's player (T-081). Malik's RunnerInput
    /// keeps N/M and touch; the arrows enter the same press queue through Inject, so an arrow press is as exact as an N/M press.
    /// A plain Zenject object (no scene component): it opens and closes its own InputActions. Only the player's body is fed;
    /// the bots never read this. The gate and the time conversion live in RunnerTapTranslator (tested).
    /// </summary>
    public sealed class ArrowKeyRunnerInput : IInitializable, IDisposable
    {
        const int LeftLeg = 0;
        const int RightLeg = 1;

        readonly RaceSessionAdapter _session;

#if ENABLE_INPUT_SYSTEM
        InputAction _left;
        InputAction _right;
#endif

        public ArrowKeyRunnerInput(RaceSessionAdapter session) => _session = session;

        public void Initialize()
        {
#if ENABLE_INPUT_SYSTEM
            if (_left != null) return; // idempotent
            _left = Create("ArrowLeftFoot", "<Keyboard>/leftArrow", LeftLeg);
            _right = Create("ArrowRightFoot", "<Keyboard>/rightArrow", RightLeg);
#endif
        }

        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            _left?.Dispose();
            _right?.Dispose();
            _left = null;
            _right = null;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        InputAction Create(string name, string binding, int leg)
        {
            var action = new InputAction(name, InputActionType.Button);
            action.AddBinding(binding);
            action.performed += ctx => Forward(leg, true, ctx.time);
            action.canceled += ctx => Forward(leg, false, ctx.time);
            action.Enable();
            return action;
        }

        /// <summary>Gate on the runner, then the Input System event time (realtime timeline) onto game time, into the runner's queue.</summary>
        void Forward(int leg, bool pressed, double eventRealtime)
        {
            if (_session == null) return;
            RunnerBody body = _session.PlayerBody;
            if (body == null || body.input == null) return;
            // During the countdown and after the finish a press goes in too: RunnerPressGate knows what it means then, as for N/M and touch.
            if (!RunnerTapTranslator.ShouldForward(pressed, body.acceptInput || _session.IsCountingDown || _session.State == RaceSessionState.Finished)) return;
            double gameTime = RunnerTapTranslator.ToGameTime(eventRealtime, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble);
            body.input.Inject(leg, pressed, gameTime);
        }
#endif
    }
}
