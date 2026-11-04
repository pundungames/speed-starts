using SpeedStars.Core.Race;
using SpeedStars.Physical;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Keeps the player's runner past the line. RaceController puts its runner back on the start line in the finish frame (and only that
    /// branch gives it the time RaceRecorder needs to close the player's track), so Malik's code stays as it is and the finish is handed
    /// to it on a stand-in: an empty RunnerBody, never drawn, with the clock of the crossing. The controller then sets its time, resets
    /// the stand-in and goes Ready; the real runner never hears of it and runs on over the line. The stand-in stays in the controller
    /// (nothing to race, nothing to start) until <see cref="GiveBack"/>, right before the next race resets the real runner.
    /// Runs after RunnerBody has moved the sim (-100) and before RaceController (0); the session takes the result after it (10).
    /// When the mechanic gets a real finish event, this class goes away. Added by RaceSessionAdapter.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public sealed class RaceFinishGate : MonoBehaviour
    {
        RaceSessionAdapter _session;
        RaceController _race;
        RunnerBody _player;
        RunnerBody _standIn;
        float _prevDistance;
        double _prevTime;
        bool _forced;
        double _forcedSeconds;
        bool _crossed;
        double _crossedSeconds;
        bool _handingOver;
        float _length;
        Transform _line;

        /// <summary>The race length while the controller is finishing on the stand-in (its own reads 0 for that one frame).</summary>
        public bool IsHandingOver => _handingOver;
        public float HeldLength => _length;

        public void Bind(RaceSessionAdapter session, RaceController race, RunnerBody player)
        {
            _session = session;
            _race = race;
            _player = player;
        }

        /// <summary>Finishes with this time without the line (the dev key); taken in the next frame.</summary>
        public void Force(double seconds)
        {
            _forced = true;
            _forcedSeconds = seconds;
        }

        void Update()
        {
            if (!_race || !_player || _player.Sim == null) return;
            RunnerSim sim = _player.Sim;

            if (_forced)
            {
                _forced = false;
                Finish(_forcedSeconds);
            }
            else if (_race.body == _player && _race.Current == RaceController.State.Running && !_session.IsFailing
                     && _session.State != RaceSessionState.Finished && sim.Distance >= _race.raceDistance)
            {
                // RaceController's own interpolation; its RaceTime still is last frame's here, which is when _prevTime was.
                float span = sim.Distance - _prevDistance;
                double fraction = span > 0f ? (_race.raceDistance - _prevDistance) / span : 1.0;
                Finish(_race.RaceTime + fraction * (sim.Time - _prevTime));
            }

            _prevDistance = sim.Distance;
            _prevTime = sim.Time;
        }

        void Finish(double seconds)
        {
            _crossed = true;
            _crossedSeconds = seconds;
            RunnerBody standIn = StandIn();
            bool running = _race.Current == RaceController.State.Running;
            if (running)
            {
                // With no distance to compare the controller's time comes out as its runner's clock minus the start of the race.
                standIn.Sim.Reset();
                standIn.Sim.Advance(Mathf.Max(0f, (float)(_prevTime - _race.RaceTime + seconds)));
                _length = _race.raceDistance;
                _line = _race.finishLine;
                _race.raceDistance = 0f;
                _race.finishLine = null; // it would be placed from the stand-in
                _handingOver = true;
            }
            _race.body = standIn;
        }

        /// <summary>The session, after the controller's frame: true once with the time of the finish; the race has its length back.</summary>
        public bool TakeCrossing(out double seconds)
        {
            seconds = _crossedSeconds;
            if (!_crossed) return false;
            _crossed = false;
            if (_handingOver)
            {
                _handingOver = false;
                _race.raceDistance = _length;
                _race.finishLine = _line;
            }
            return true;
        }

        /// <summary>A new race: the controller gets its runner back.</summary>
        public void GiveBack()
        {
            _forced = false;
            _crossed = false;
            if (_race && _player) _race.body = _player;
        }

        RunnerBody StandIn()
        {
            if (_standIn) return _standIn;
            var go = new GameObject("Finish Stand-in");
            go.transform.SetParent(transform, false);
            _standIn = go.AddComponent<RunnerBody>();
            _standIn.enabled = false; // its Sim is all that is used; RaceRecorder takes only live bodies
            return _standIn;
        }
    }
}
