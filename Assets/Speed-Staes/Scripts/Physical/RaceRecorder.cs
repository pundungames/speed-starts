using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Records every registered runner of a race for Watch Replay (see <see cref="RaceRecording"/>). Starts when the race goes Running and
    /// samples after everything else has posed the bones (rig, pose driver's LateUpdate, emotes, feedback). The player's track ends at the
    /// finish: RaceController puts him back on the start line in that same frame, so the reset frame is never taken and the last frame
    /// before it is kept. The others run on and each track ends tailAfterFinish after its own crossing; the recording is done when every
    /// track has ended. A reset without a finish (Restart) throws the recording away. Records only: changes nothing in the race.
    /// </summary>
    [DefaultExecutionOrder(1500)]
    public sealed class RaceRecorder : MonoBehaviour
    {
        [Tooltip("The race to record (its runner is the player). Found in the scene if empty.")]
        public RaceController race;
        [Tooltip("Samples per second. The playback interpolates between them, so 30 is smooth even at 0.5x.")]
        public float sampleRate = 30f;
        [Tooltip("How long a runner is still recorded after crossing the line (s), so the replay can show the finish.")]
        public float tailAfterFinish = 1.5f;
        [Tooltip("Safety stop (s): the recording ends here even if a runner never finishes.")]
        public float maxDuration = 60f;

        /// <summary>The last finished recording; null before the first one.</summary>
        public RaceRecording Last { get; private set; }
        public bool IsRecording => _current != null;
        /// <summary>There is a race to replay: a finished recording, or the player has crossed and the others are still running.</summary>
        public bool HasRace => Last != null || (_current != null && _playerFinished);

        /// <summary>
        /// Closes the recording now if the player has finished (Watch Replay while the others still run): their tracks end where they are,
        /// which is past the player's finish, so following the player loses nothing.
        /// </summary>
        public void FinishNow()
        {
            if (_current != null && _playerFinished) Complete();
        }
        /// <summary>A recording finished and became Last.</summary>
        public event Action<RaceRecording> Recorded;

        readonly List<RunnerBody> _runners = new List<RunnerBody>();
        readonly List<Pending> _pending = new List<Pending>();
        RaceRecording _current;
        RaceController.State _lastState;
        float _lastRaceTime, _time, _nextSample;
        bool _playerFinished;

        /// <summary>Last frame of a track, taken every frame and appended only when a sample is due (or at the player's finish).</summary>
        sealed class Pending
        {
            public RunnerTrack Track;
            public bool Has, Appended;
            public float Time, Speed, Energy, Distance;
            public Vector3 RootPosition;
            public Quaternion RootRotation;
            public Vector3[] Positions;
            public Quaternion[] Rotations;
            public float PrevTime, PrevDistance;
            /// <summary>The bot's RunReset handler: put back on the line (a new round), it has nothing more to give to this race.</summary>
            public Action OnReset;
        }

        /// <summary>
        /// Adds a runner to the next recordings. The player comes from race.body; with nobody registered every live RunnerBody is taken.
        /// </summary>
        public void Register(RunnerBody body)
        {
            if (body && !_runners.Contains(body)) _runners.Add(body);
        }

        public void Unregister(RunnerBody body) => _runners.Remove(body);

        void Start()
        {
            if (!race) race = FindObjectOfType<RaceController>();
            if (race)
            {
                _lastState = race.Current;
                _lastRaceTime = race.LastRaceTime;
            }
        }

        void OnDisable() => Drop();

        void LateUpdate()
        {
            if (!race || !race.body) return;

            RaceController.State state = race.Current;
            if (state == RaceController.State.Running && _lastState != RaceController.State.Running) Begin();
            else if (state != RaceController.State.Running && _lastState == RaceController.State.Running && _current != null)
            {
                // Running → Ready: the player crossed (a new LastRaceTime) or the race was reset without a finish.
                if (!Mathf.Approximately(race.LastRaceTime, _lastRaceTime)) FinishPlayer();
                else Drop();
            }
            _lastState = state;
            _lastRaceTime = race.LastRaceTime;

            if (_current == null) return;
            // The race's own clock while the player runs, so replay times are the HUD's times; after his finish it runs on by the frame.
            _time = _playerFinished ? _time + Time.deltaTime : Mathf.Max(_time, race.RaceTime);
            bool due = _time >= _nextSample;
            if (due) _nextSample += 1f / Mathf.Max(1f, sampleRate);

            bool anyOpen = false;
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending p = _pending[i];
                if (p.Track.Ended) continue;
                if (p.Track.IsPlayer && _playerFinished) continue;
                Capture(p);
                if (due) Append(p);
                CheckFinish(p);
                if (!p.Track.Ended) anyOpen = true;
            }
            if (!anyOpen || _time >= maxDuration) Complete();
        }

        void Begin()
        {
            if (_current != null && _playerFinished) Complete(); // a new race before the last bots were done
            Drop();
            _current = new RaceRecording { RaceDistance = race.raceDistance };
            _time = 0f;
            _nextSample = 0f;
            _playerFinished = false;

            AddTrack(race.body, true);
            if (_runners.Count == 0)
            {
                // Nobody registered: every live runner on the field (the bots of the Race scene; a menu copy has its body turned off).
                foreach (RunnerBody body in FindObjectsOfType<RunnerBody>())
                    if (body != race.body && body.isActiveAndEnabled && body.Sim != null) AddTrack(body, false);
            }
            else
            {
                for (int i = 0; i < _runners.Count; i++)
                    if (_runners[i] && _runners[i] != race.body) AddTrack(_runners[i], false);
            }
        }

        void AddTrack(RunnerBody body, bool isPlayer)
        {
            var track = new RunnerTrack(body, isPlayer, RunnerTrack.FindBones(body.transform));
            _current.Tracks.Add(track);
            var p = new Pending
            {
                Track = track,
                Positions = new Vector3[track.BoneCount],
                Rotations = new Quaternion[track.BoneCount],
                PrevDistance = body.Sim != null ? body.Sim.Distance : 0f,
            };
            _pending.Add(p);
            if (isPlayer) return;
            p.OnReset = () => { if (!p.Track.Ended) EndTrack(p); };
            body.RunReset += p.OnReset;
        }

        void Capture(Pending p)
        {
            RunnerTrack track = p.Track;
            Transform root = track.Body.transform;
            RunnerSim sim = track.Body.Sim;
            if (p.Has)
            {
                p.PrevTime = p.Time;
                p.PrevDistance = p.Distance;
            }
            p.Time = _time;
            p.RootPosition = root.position;
            p.RootRotation = root.rotation;
            for (int b = 0; b < track.BoneCount; b++)
            {
                p.Positions[b] = track.Bones[b].localPosition;
                p.Rotations[b] = track.Bones[b].localRotation;
            }
            p.Speed = sim != null ? sim.Speed : 0f;
            p.Energy = sim != null ? sim.Energy : 0f;
            p.Distance = sim != null ? sim.Distance : 0f;
            p.Has = true;
            p.Appended = false;
        }

        static void Append(Pending p)
        {
            if (!p.Has || p.Appended) return;
            RunnerTrack track = p.Track;
            track.Times.Add(p.Time);
            track.RootPositions.Add(p.RootPosition);
            track.RootRotations.Add(p.RootRotation);
            track.BonePositions.AddRange(p.Positions);
            track.BoneRotations.AddRange(p.Rotations);
            track.Speeds.Add(p.Speed);
            track.Energies.Add(p.Energy);
            track.Distances.Add(p.Distance);
            p.Appended = true;
        }

        /// <summary>The others' crossing, interpolated inside the frame the same way the race times are; the track runs on for the tail.</summary>
        void CheckFinish(Pending p)
        {
            RunnerTrack track = p.Track;
            if (track.IsPlayer) return;
            float line = _current.RaceDistance;
            if (!track.Finished && p.Distance >= line && p.PrevDistance < line)
            {
                float span = p.Distance - p.PrevDistance;
                float fraction = span > 0f ? (line - p.PrevDistance) / span : 1f;
                track.FinishTime = Mathf.Lerp(p.PrevTime, p.Time, fraction);
            }
            if (track.Finished && _time >= track.FinishTime + tailAfterFinish) EndTrack(p);
        }

        /// <summary>
        /// The player crossed this frame and is already back on the start line: his last frame (kept from the previous LateUpdate) closes
        /// the track. The recording runs on the race clock, so the finish is the race's own time.
        /// </summary>
        void FinishPlayer()
        {
            _playerFinished = true;
            _time = Mathf.Max(_time, race.LastRaceTime);
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending p = _pending[i];
                if (!p.Track.IsPlayer) continue;
                Append(p);
                p.Track.FinishTime = race.LastRaceTime;
                p.Track.Ended = true;
            }
        }

        void EndTrack(Pending p)
        {
            Append(p);
            p.Track.Ended = true;
            Unhook(p);
        }

        static void Unhook(Pending p)
        {
            if (p.OnReset != null && p.Track.Body) p.Track.Body.RunReset -= p.OnReset;
            p.OnReset = null;
        }

        void Complete()
        {
            if (_current == null) return;
            for (int i = 0; i < _pending.Count; i++)
                if (!_pending[i].Track.Ended) EndTrack(_pending[i]);
            RaceRecording done = _current;
            Unhook();
            _current = null;
            Last = done;
            Recorded?.Invoke(done);
        }

        void Drop()
        {
            Unhook();
            _current = null;
        }

        void Unhook()
        {
            for (int i = 0; i < _pending.Count; i++) Unhook(_pending[i]);
            _pending.Clear();
        }
    }
}
