using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Scene side of the runner's physics: feeds RunnerInput presses into RunnerSim at their own
    /// timestamps (the sim is advanced exactly to each press, not to the frame) and moves the root
    /// along its forward axis by the sim's distance. Knows nothing about bones or IK.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RunnerBody : MonoBehaviour
    {
        [Tooltip("Where the presses come from (keyboard N/M, touch: left/right half of the screen).")]
        public RunnerInput input;
        [Tooltip("All the physics numbers: speed, stride, push efficiency, hold, lean, head (energy is in Energy Tuning).")]
        public RunnerTuning tuning = new RunnerTuning();
        [Tooltip("The energy numbers (Physical/EnergyTuning asset): capacity, best rhythm, use, recovery. Every runner and bot reads the same asset. Empty = the code defaults.")]
        public EnergyTuning energyTuning;
        [Tooltip("Presses are ignored while false (e.g. after the finish).")]
        public bool acceptInput = true;
        [Tooltip("Jumps are ignored while false (a race without hurdles).")]
        public bool acceptJump = true;
        [Tooltip("The root goes along at the sim's speed smoothed over this long (s), pulled back to the sim's distance: every press is a push " +
                 "and the drag takes it back, so the sim's speed swings 7-16% each step and the root (and the camera on it) jolted with every " +
                 "press (17.11). Only the picture: the race, the finish and the speed read the sim. 0 = the sim's distance as it is.")]
        public float shownSpeedSmoothing = 0.2f;

        public RunnerSim Sim { get; private set; }

        /// <summary>A press (true) or release (false) the sim took: leg, sim time. For the visual step cycle.</summary>
        public event System.Action<int, bool, double> Tapped;
        /// <summary>The run was reset (sim time is back at 0).</summary>
        public event System.Action RunReset;

        const float ShownPull = 5f;        // the shown distance closes on the sim's at this rate (1/s), so it never drifts off (~0.13 m behind at the start's pull)
        const float ShownMaxOff = 0.3f;    // ...and is never more than this (m) from it

        Vector3 _startPos, _forward;
        double _origin;
        bool _jumpAsked;
        float _shownDistance, _shownSpeed;   // where the root is drawn along the run, and how fast it goes there

        void Awake()
        {
            if (!input) input = GetComponent<RunnerInput>();
            _startPos = transform.position;
            _forward = transform.forward;
            Sim = new RunnerSim(tuning, energyTuning ? energyTuning.model : null);
            _origin = Time.timeAsDouble;
            if (input) input.JumpPressed += OnJumpPressed;
        }

        /// <summary>Only the scene's explicitly selected player receives gameplay feedback.</summary>
        public void EnablePlayerFeedback()
        {
            if (!GetComponent<PerfectStepFeedback>()) gameObject.AddComponent<PerfectStepFeedback>();
            if (!GetComponent<EarlyStepFeedback>()) gameObject.AddComponent<EarlyStepFeedback>();
            if (!GetComponent<LateStepFeedback>()) gameObject.AddComponent<LateStepFeedback>();
            if (!GetComponent<MomentumWindFeedback>()) gameObject.AddComponent<MomentumWindFeedback>();
            if (!GetComponent<SweatFeedback>()) gameObject.AddComponent<SweatFeedback>();
        }

        void OnDestroy()
        {
            if (input) input.JumpPressed -= OnJumpPressed;
        }

        void OnJumpPressed() => _jumpAsked = true;

        void Update()
        {
            double now = Time.timeAsDouble - _origin;
            if (input)
            {
                while (input.TryDequeue(out var tap))
                {
                    // Bring the sim up to the press itself; a press older than the sim lands now.
                    double at = System.Math.Min(tap.Time - _origin, now);
                    if (at > Sim.Time) Sim.Advance((float)(at - Sim.Time));
                    // Releases always go through, so a foot never stays stuck down.
                    if (!acceptInput && tap.Pressed) continue;
                    bool wasDown = Sim.Legs[tap.Leg].Pressed;
                    if (tap.Pressed) Sim.Press(tap.Leg);
                    else Sim.Release(tap.Leg);
                    if (wasDown != Sim.Legs[tap.Leg].Pressed) Tapped?.Invoke(tap.Leg, tap.Pressed, Sim.Time);
                }
            }
            if (_jumpAsked && acceptInput && acceptJump) Sim.Jump();
            _jumpAsked = false;
            if (now > Sim.Time) Sim.Advance((float)(now - Sim.Time));
            transform.position = _startPos + _forward * ShownDistance(Time.deltaTime);
        }

        /// <summary>The sim's distance through a smoothed speed (shownSpeedSmoothing), held within ShownMaxOff of it.</summary>
        float ShownDistance(float dt)
        {
            if (shownSpeedSmoothing <= 0f || dt <= 0f)
            {
                _shownSpeed = Sim.Speed;
                return _shownDistance = Sim.Distance;
            }
            _shownSpeed += (Sim.Speed - _shownSpeed) * (1f - Mathf.Exp(-dt / shownSpeedSmoothing));
            _shownDistance += (_shownSpeed + ShownPull * (Sim.Distance - _shownDistance)) * dt;
            _shownDistance = Mathf.Clamp(_shownDistance, Sim.Distance - ShownMaxOff, Sim.Distance + ShownMaxOff);
            return _shownDistance;
        }

        /// <summary>Back to the start line, standing still, full energy.</summary>
        public void ResetRun()
        {
            Sim.Reset();
            _origin = Time.timeAsDouble;
            _shownDistance = _shownSpeed = 0f;
            if (input) input.Clear();
            transform.position = _startPos;
            acceptInput = true;
            RunReset?.Invoke();
        }
    }
}
