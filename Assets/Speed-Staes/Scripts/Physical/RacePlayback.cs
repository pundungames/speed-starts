using System;
using System.Collections.Generic;
using SpeedStars.Runner;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Watch Replay: plays a <see cref="RaceRecording"/> back on the same runners. While it plays, everything that would move them is off
    /// (RunnerInput, RunnerBody, RunnerPoseDriver, the rig, emotes) and every track writes its recorded root and bones; leaving puts every
    /// component back as it was, and the runners return to where their own physics has them. The race itself is never touched.
    /// The race camera is off meanwhile and <see cref="ReplayCamera"/> frames the followed runner with the chosen shot.
    /// It ends at the followed runner's last sample (the player's is his finish); the UI listens to Ended.
    /// Debug keys until the UI has its buttons: P start/leave, O play/pause, [ ] back/forward, K speed, , . previous/next runner, C shot.
    /// Runs its LateUpdate before the other cameras' (order 0): with the driver and the rig off nothing else writes the bones.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class RacePlayback : MonoBehaviour
    {
        [Tooltip("Where the recordings come from. Found on this object or in the scene if empty.")]
        public RaceRecorder recorder;
        [Tooltip("The replay's shots. Found on this object if empty.")]
        public ReplayCamera replayCamera;
        [Tooltip("The race camera: turned off while a replay plays (its Camera is lent to the replay shots). Found in the scene if empty.")]
        public SideFollowCamera followCamera;
        [Tooltip("Playback speeds the speed key steps through (1 = real time).")]
        public float[] speeds = { 1f, 2f, 0.5f };
        [Tooltip("How far the back/forward keys jump (s).")]
        public float seekStep = 2f;
        [Tooltip("Debug keys (P, O, [ ], K, comma, period, C) work while true.")]
        public bool debugKeys = true;

        public bool IsActive => _recording != null;
        public bool IsPlaying { get; private set; }
        /// <summary>Replay clock (s from the race start).</summary>
        public float Time { get; private set; }
        /// <summary>Where this replay ends (s): the followed runner's last sample, at least his finish (the player's track stops a frame before it).</summary>
        public float Duration
        {
            get
            {
                RunnerTrack followed = Followed;
                if (followed == null) return 0f;
                return followed.Finished ? Mathf.Max(followed.Duration, followed.FinishTime) : followed.Duration;
            }
        }
        public float Speed => speeds.Length > 0 ? speeds[_speedIndex] : 1f;
        public int FollowIndex { get; private set; }
        public RunnerTrack Followed => _recording != null && FollowIndex < _recording.Tracks.Count ? _recording.Tracks[FollowIndex] : null;
        public RaceRecording Recording => _recording;
        public int ShotIndex => replayCamera ? replayCamera.Index : 0;

        /// <summary>A replay started (playing from 0).</summary>
        public event Action Started;
        /// <summary>The replay clock reached the followed runner's end.</summary>
        public event Action Ended;
        /// <summary>The replay was left; the runners are live again.</summary>
        public event Action Stopped;
        /// <summary>Another runner is followed.</summary>
        public event Action<int> FollowChanged;
        /// <summary>Another shot is on.</summary>
        public event Action<int> ShotChanged;

        readonly List<(Behaviour behaviour, bool enabled)> _frozen = new List<(Behaviour, bool)>();
        RaceRecording _recording;
        int _speedIndex;
        bool _cameraWasEnabled;

        void Awake()
        {
            if (!recorder) recorder = GetComponent<RaceRecorder>();
            if (!recorder) recorder = FindObjectOfType<RaceRecorder>();
            if (!replayCamera) replayCamera = GetComponent<ReplayCamera>();
            if (!followCamera) followCamera = FindObjectOfType<SideFollowCamera>();
        }

        void OnDisable() => Stop();

        /// <summary>Plays the recorder's last race from the start, following the player. False if there is nothing to play.</summary>
        public bool Play()
        {
            if (!recorder) return false;
            recorder.FinishNow();
            return Play(recorder.Last);
        }

        public bool Play(RaceRecording recording)
        {
            if (recording == null || recording.Tracks.Count == 0) return false;
            Stop();
            _recording = recording;
            if (recorder) recorder.enabled = false;
            for (int i = 0; i < recording.Tracks.Count; i++) Freeze(recording.Tracks[i]);
            FollowIndex = Mathf.Max(0, recording.Tracks.FindIndex(t => t.IsPlayer));
            _speedIndex = 0;
            Time = 0f;
            IsPlaying = true;
            Apply();
            if (followCamera)
            {
                _cameraWasEnabled = followCamera.enabled;
                followCamera.enabled = false;
            }
            if (replayCamera)
            {
                replayCamera.Take(followCamera ? followCamera.GetComponent<Camera>() : Camera.main);
                replayCamera.ResetShot();
                FrameCamera(0f);
            }
            Started?.Invoke();
            return true;
        }

        /// <summary>Leaves the replay: components back as they were, the race camera back on its own runner.</summary>
        public void Stop()
        {
            if (_recording == null) return;
            for (int i = _frozen.Count - 1; i >= 0; i--)
                if (_frozen[i].behaviour) _frozen[i].behaviour.enabled = _frozen[i].enabled;
            _frozen.Clear();
            _recording = null;
            IsPlaying = false;
            if (recorder) recorder.enabled = true;
            if (replayCamera) replayCamera.Release();
            if (followCamera)
            {
                followCamera.enabled = _cameraWasEnabled;
                followCamera.SnapToTarget();
            }
            Stopped?.Invoke();
        }

        public void SetPlaying(bool playing)
        {
            if (_recording == null) return;
            if (playing && Time >= Duration) Time = 0f; // play at the end starts over
            IsPlaying = playing;
        }

        public void Seek(float time)
        {
            if (_recording == null) return;
            Time = Mathf.Clamp(time, 0f, Duration);
            Apply();
            if (replayCamera) replayCamera.Snap();
        }

        public void NextSpeed()
        {
            if (speeds.Length > 0) _speedIndex = (_speedIndex + 1) % speeds.Length;
        }

        /// <summary>Follows the next (+1) or previous (-1) runner; the clock goes on.</summary>
        public void Follow(int step)
        {
            if (_recording == null || _recording.Tracks.Count == 0) return;
            int n = _recording.Tracks.Count;
            FollowIndex = ((FollowIndex + step) % n + n) % n;
            if (Time > Duration) Time = Duration;
            if (replayCamera) replayCamera.Snap();
            FollowChanged?.Invoke(FollowIndex);
        }

        /// <summary>Next (+1) or previous (-1) shot.</summary>
        public void NextShot(int step = 1)
        {
            if (!replayCamera) return;
            replayCamera.Step(step);
            ShotChanged?.Invoke(replayCamera.Index);
        }

        /// <summary>Turns off everything that moves this runner; remembers how each was, for Stop.</summary>
        void Freeze(RunnerTrack track)
        {
            if (!track.Body) return;
            GameObject go = track.Body.gameObject;
            Off(track.Body);
            Off(go.GetComponent<RunnerInput>());
            Off(go.GetComponentInChildren<RunnerPoseDriver>());
            Off(go.GetComponentInChildren<RunnerEmote>());
            Off(go.GetComponentInChildren<RigBuilder>());
        }

        void Off(Behaviour b)
        {
            if (!b) return;
            _frozen.Add((b, b.enabled));
            b.enabled = false;
        }

        void Apply()
        {
            for (int i = 0; i < _recording.Tracks.Count; i++) _recording.Tracks[i].Apply(Time);
        }

        void FrameCamera(float deltaTime)
        {
            RunnerTrack followed = Followed;
            if (!replayCamera || followed == null || !followed.Body) return;
            followed.ValuesAt(Time, out _, out _, out float distance);
            replayCamera.Frame(followed.Body.transform, distance, deltaTime);
        }

        void Update()
        {
            if (!debugKeys) return;
            if (DebugKeys.Down(KeyCode.P))
            {
                if (IsActive) Stop();
                else Play();
            }
            if (!IsActive) return;
            if (DebugKeys.Down(KeyCode.O)) SetPlaying(!IsPlaying);
            if (DebugKeys.Down(KeyCode.LeftBracket)) Seek(Time - seekStep);
            if (DebugKeys.Down(KeyCode.RightBracket)) Seek(Time + seekStep);
            if (DebugKeys.Down(KeyCode.K)) NextSpeed();
            if (DebugKeys.Down(KeyCode.Comma)) Follow(-1);
            if (DebugKeys.Down(KeyCode.Period)) Follow(1);
            if (DebugKeys.Down(KeyCode.C)) NextShot();
        }

        void LateUpdate()
        {
            if (_recording == null) return;
            float dt = UnityEngine.Time.deltaTime;
            if (IsPlaying)
            {
                Time += dt * Speed;
                if (Time >= Duration)
                {
                    Time = Duration;
                    IsPlaying = false;
                    Ended?.Invoke();
                    if (_recording == null) return; // left on Ended
                }
            }
            Apply();
            FrameCamera(dt);
        }
    }
}
