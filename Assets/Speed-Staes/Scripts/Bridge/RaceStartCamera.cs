using SpeedStars.Core.Race;
using SpeedStars.Runner;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Every race opens with the camera in front of the player's runner, on its right, so the player can pick out which one is theirs in
    /// a lane that changes each race; it holds there through READY and SET and swings round to the race camera on GO (config). The race camera keeps running underneath (its own snap,
    /// zoom and follow), and this only blends from the opening place to where that one put the camera this frame, so the end of the swing
    /// is exactly the race camera and there is nothing to hand back. Real time, so the tutorial's waiting does not stop the swing.
    /// Added by RaceSessionAdapter; its LateUpdate comes right after the race camera's.
    /// </summary>
    [DefaultExecutionOrder(90)]
    public sealed class RaceStartCamera : MonoBehaviour
    {
        RaceSessionAdapter _session;
        Camera _camera;
        SideFollowCamera _follow;
        float _raceFieldOfView;
        bool _active;
        bool _go;
        float _held;
        float _swing;

        void Awake()
        {
            _session = GetComponent<RaceSessionAdapter>();
            if (_session) _session.StartPhaseChanged += OnStartPhase;
        }

        void OnDestroy()
        {
            if (_session) _session.StartPhaseChanged -= OnStartPhase;
        }

        void OnDisable() => Stop();

        void OnStartPhase(RaceStartPhase phase)
        {
            if (phase == RaceStartPhase.Ready) Begin();
            else if (phase == RaceStartPhase.Go) _go = true;
            else if (phase == RaceStartPhase.Idle) Stop(); // the finish: its own camera takes over
        }

        void Begin()
        {
            Stop();
            _camera = Camera.main;
            _follow = _camera ? _camera.GetComponent<SideFollowCamera>() : null;
            if (!_follow || !_follow.target) return;
            _raceFieldOfView = _camera.fieldOfView;
            _follow.SnapToTarget(); // the lane has just changed: the race camera starts from there, not on its way there
            _active = true;
            _go = false;
            _held = 0f;
            _swing = 0f;
        }

        void Stop()
        {
            if (!_active) return;
            _active = false;
            if (_camera) _camera.fieldOfView = _raceFieldOfView;
        }

        void LateUpdate()
        {
            if (!_active || !_camera || !_follow || !_follow.target) return;
            RaceStartConfig config = _session.StartSettings;
            float seconds = Mathf.Min(Time.unscaledDeltaTime, config.MaxFrameSeconds);
            _held += seconds;
            if (_go || (!config.IntroHoldUntilGo && _held >= config.IntroHoldSeconds)) _swing += seconds / config.IntroSwingSeconds;
            if (_swing >= 1f)
            {
                Stop();
                return;
            }

            float t = Mathf.SmoothStep(0f, 1f, _swing);
            Transform runner = _follow.target;
            Transform cam = _camera.transform;
            Vector3 raceOffset = cam.position - runner.position;
            Vector3 opening = runner.right * config.IntroCameraOffset.x + Vector3.up * config.IntroCameraOffset.y + runner.forward * config.IntroCameraOffset.z;
            Vector3 look = runner.position + Vector3.up * (config.IntroLookHeight - config.IntroStanceLookDrop) - (runner.position + opening);

            cam.position = runner.position + Vector3.Slerp(opening, raceOffset, t); // round the runner, not through it
            cam.rotation = Quaternion.Slerp(Quaternion.LookRotation(look, Vector3.up), cam.rotation, t);
            _camera.fieldOfView = Mathf.Lerp(config.IntroFieldOfView, _raceFieldOfView, t);
        }
    }
}
