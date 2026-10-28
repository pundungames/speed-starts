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
        [Tooltip("All the physics numbers: speed, stride, push efficiency, hold, lean, head, energy.")]
        public RunnerTuning tuning = new RunnerTuning();
        [Tooltip("Presses are ignored while false (e.g. after the finish).")]
        public bool acceptInput = true;

        public RunnerSim Sim { get; private set; }

        /// <summary>A press (true) or release (false) the sim took: leg, sim time. For the visual step cycle.</summary>
        public event System.Action<int, bool, double> Tapped;
        /// <summary>The run was reset (sim time is back at 0).</summary>
        public event System.Action RunReset;

        Vector3 _startPos, _forward;
        double _origin;

        void Awake()
        {
            if (!input) input = GetComponent<RunnerInput>();
            _startPos = transform.position;
            _forward = transform.forward;
            Sim = new RunnerSim(tuning);
            _origin = Time.timeAsDouble;
        }

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
            if (now > Sim.Time) Sim.Advance((float)(now - Sim.Time));
            transform.position = _startPos + _forward * Sim.Distance;
        }

        /// <summary>Back to the start line, standing still, full energy.</summary>
        public void ResetRun()
        {
            Sim.Reset();
            _origin = Time.timeAsDouble;
            if (input) input.Clear();
            transform.position = _startPos;
            acceptInput = true;
            RunReset?.Invoke();
        }
    }
}
