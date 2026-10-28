using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Race flow: Ready (standing at the line) → Running (clock starts with the first press). Crossing
    /// the finish puts the runner straight back at the start line, Ready again; the finished time is kept
    /// in LastRaceTime.
    /// </summary>
    public sealed class RaceController : MonoBehaviour
    {
        public enum State { Ready, Running }

        [Tooltip("The runner whose distance is raced.")]
        public RunnerBody body;
        [Tooltip("Race length (m): crossing it keeps the time and puts the runner back at the start line.")]
        public float raceDistance = 100f;
        [Tooltip("Optional: placed raceDistance ahead of the runner's start at play start.")]
        public Transform finishLine;

        public State Current { get; private set; }
        /// <summary>Race clock (s): from the first press.</summary>
        public float RaceTime { get; private set; }
        /// <summary>Time of the last finished race (s), 0 before the first finish.</summary>
        public float LastRaceTime { get; private set; }

        double _startTime;

        void Start()
        {
            if (!body) body = FindObjectOfType<RunnerBody>();
            if (finishLine && body)
            {
                Vector3 p = body.transform.position + body.transform.forward * raceDistance;
                finishLine.position = new Vector3(finishLine.position.x, finishLine.position.y, p.z);
            }
        }

        void Update()
        {
            if (!body || body.Sim == null) return;
            var sim = body.Sim;

            switch (Current)
            {
                case State.Ready:
                    if (sim.Presses > 0)
                    {
                        Current = State.Running;
                        _startTime = sim.LastPressTime;
                    }
                    break;
                case State.Running:
                    RaceTime = (float)(sim.Time - _startTime);
                    if (sim.Distance >= raceDistance)
                    {
                        LastRaceTime = RaceTime;
                        ResetRace();
                    }
                    break;
            }
        }

        public void ResetRace()
        {
            if (body) body.ResetRun();
            Current = State.Ready;
            RaceTime = 0f;
        }
    }
}
