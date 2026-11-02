using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Race flow: Ready (standing at the line) → Running: the clock starts at GO (StartClock), or with the first
    /// press where nothing calls GO (the test scene). Crossing the finish puts the runner straight back at the start
    /// line, Ready again; the finished time, interpolated inside the frame, is kept in LastRaceTime.
    /// </summary>
    public sealed class RaceController : MonoBehaviour
    {
        public enum State { Ready, Running }

        [Tooltip("The runner whose distance is raced.")]
        public RunnerBody body;
        [Tooltip("Race length (m): crossing it keeps the time and puts the runner back at the start line.")]
        public float raceDistance = 100f;
        [Tooltip("Optional: placed raceDistance ahead of the runner's start at play start and at every race reset.")]
        public Transform finishLine;

        public State Current { get; private set; }
        /// <summary>Race clock (s): from GO, the reaction included (from the first press without a GO).</summary>
        public float RaceTime { get; private set; }
        /// <summary>Time of the last finished race (s), 0 before the first finish.</summary>
        public float LastRaceTime { get; private set; }

        double _startTime, _prevTime;
        float _prevDistance;

        void Start()
        {
            if (!body) body = FindObjectOfType<RunnerBody>();
            PlaceFinishLine();
        }

        /// <summary>The runner stands on the start line here; the length may have changed with the event.</summary>
        void PlaceFinishLine()
        {
            if (!finishLine || !body) return;
            Vector3 p = body.transform.position + body.transform.forward * raceDistance;
            finishLine.position = new Vector3(finishLine.position.x, finishLine.position.y, p.z);
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
                        Begin(sim, sim.LastPressTime);
                    }
                    break;
                case State.Running:
                    RaceTime = (float)(sim.Time - _startTime);
                    if (sim.Distance >= raceDistance)
                    {
                        float span = sim.Distance - _prevDistance;
                        double fraction = span > 0f ? (raceDistance - _prevDistance) / span : 1.0;
                        LastRaceTime = (float)(_prevTime + fraction * (sim.Time - _prevTime) - _startTime);
                        ResetRace();
                    }
                    else
                    {
                        _prevDistance = sim.Distance;
                        _prevTime = sim.Time;
                    }
                    break;
            }
        }

        /// <summary>GO: the clock starts now for everyone, whenever the first press comes.</summary>
        public void StartClock()
        {
            if (!body || body.Sim == null || Current != State.Ready) return;
            Begin(body.Sim, body.Sim.Time);
        }

        void Begin(RunnerSim sim, double start)
        {
            Current = State.Running;
            _startTime = start;
            _prevDistance = sim.Distance;
            _prevTime = sim.Time;
        }

        public void ResetRace()
        {
            if (body) body.ResetRun();
            PlaceFinishLine();
            Current = State.Ready;
            RaceTime = 0f;
        }
    }
}
