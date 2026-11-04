using System;
using UnityEngine;

namespace SpeedStars.Core.Race
{
    /// <summary>Where the first race's tutorial is. One step = one sentence and one thing to look at.</summary>
    public enum RaceTutorialStep
    {
        None,
        /// <summary>The countdown: a tap now is a false start.</summary>
        WaitForGo,
        /// <summary>The game waits for a tap on this side.</summary>
        TapLeft,
        TapRight,
        /// <summary>The step just tapped is being taken, at normal speed, up to the next prompt.</summary>
        Stride,
        /// <summary>On its own now, with a hint.</summary>
        KeepGoing,
        /// <summary>The last hint: the rhythm counts, not the speed of the fingers.</summary>
        Rhythm,
        Done
    }

    /// <summary>
    /// The tutorial of the first race as a state machine, without any Unity object: wait for GO, then the first steps one by one (the game
    /// nearly stops on each and only the asked side counts, and the prompt comes as the foot lands, so the taps are on the beat), then two
    /// hints while running. It is the numbers and the order only; the Bridge component sets the game speed and the view shows the step.
    /// </summary>
    public sealed class RaceTutorialFlow
    {
        private const int LeftLeg = 0;

        private readonly RaceTutorialConfig _config;
        private int _promptsDone;
        private int _presses;
        private float _left;
        private float _stepSeconds;

        public RaceTutorialFlow(RaceTutorialConfig config) => _config = config;

        public RaceTutorialStep Step { get; private set; }

        /// <summary>Which prompted step this is (0 = the first): the first of each side is the one that explains.</summary>
        public int PromptIndex => _promptsDone;

        public event Action<RaceTutorialStep> StepChanged;

        /// <summary>The player has tapped through the prompts: leaving now, the lesson still counts as seen.</summary>
        public bool KeysLearned => Step == RaceTutorialStep.KeepGoing || Step == RaceTutorialStep.Rhythm || Step == RaceTutorialStep.Done;

        public bool IsActive => Step != RaceTutorialStep.None && Step != RaceTutorialStep.Done;

        /// <summary>The game speed wanted now: normal, except while a prompt waits.</summary>
        public float TimeScale
        {
            get
            {
                if (Step != RaceTutorialStep.TapLeft && Step != RaceTutorialStep.TapRight) return 1f;
                float blend = _stepSeconds / Mathf.Max(0.0001f, _config.HoldBlendSeconds);
                return Mathf.Lerp(1f, _config.HoldTimeScale, Mathf.SmoothStep(0f, 1f, blend));
            }
        }

        /// <summary>The countdown began (a new race, or again after a false start): from the top.</summary>
        public void Begin()
        {
            _promptsDone = 0;
            _presses = 0;
            Enter(RaceTutorialStep.WaitForGo);
        }

        public void OnGo()
        {
            if (Step == RaceTutorialStep.WaitForGo) Prompt();
        }

        /// <summary>While a prompt waits only its side is a step; a wrong side does nothing at all.</summary>
        public bool AcceptsPress(int leg)
        {
            if (Step == RaceTutorialStep.TapLeft) return leg == LeftLeg;
            if (Step == RaceTutorialStep.TapRight) return leg != LeftLeg;
            return true;
        }

        public void OnPress(int leg)
        {
            if (Step == RaceTutorialStep.TapLeft || Step == RaceTutorialStep.TapRight)
            {
                if (!AcceptsPress(leg)) return;
                _promptsDone++;
                if (_promptsDone >= _config.PromptedSteps)
                {
                    _presses = 0;
                    _left = _config.KeepGoingMaxSeconds;
                    Enter(RaceTutorialStep.KeepGoing);
                    return;
                }
                _left = _config.StrideSeconds;
                Enter(RaceTutorialStep.Stride);
                return;
            }
            if (Step == RaceTutorialStep.KeepGoing && ++_presses >= _config.KeepGoingPresses) StartRhythm();
        }

        public void Tick(float gameSeconds, float realSeconds)
        {
            _stepSeconds += realSeconds;
            switch (Step)
            {
                case RaceTutorialStep.Stride:
                    _left -= gameSeconds;
                    if (_left <= 0f) Prompt();
                    break;
                case RaceTutorialStep.KeepGoing:
                    _left -= realSeconds;
                    if (_left <= 0f) StartRhythm();
                    break;
                case RaceTutorialStep.Rhythm:
                    _left -= realSeconds;
                    if (_left <= 0f) Enter(RaceTutorialStep.Done);
                    break;
            }
        }

        /// <summary>The race is over or left: nothing more to show or wait for.</summary>
        public void Stop() => Enter(RaceTutorialStep.None);

        private void Prompt() => Enter(_promptsDone % 2 == 0 ? RaceTutorialStep.TapLeft : RaceTutorialStep.TapRight);

        private void StartRhythm()
        {
            _left = _config.RhythmSeconds;
            Enter(RaceTutorialStep.Rhythm);
        }

        private void Enter(RaceTutorialStep step)
        {
            _stepSeconds = 0f;
            Step = step;
            StepChanged?.Invoke(step);
        }
    }
}
