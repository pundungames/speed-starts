using System;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// The picture between the line and the Results: slow motion over the line, the runner easing off (<see cref="RunnerCooldown"/>) while
    /// the camera swings from the side round to the front, the chosen emote (or the body's own reaction) when it has slowed down, then the
    /// Results, with the runner and the camera staying where they are behind them and the emote coming again. The timing is <see cref="RaceFinishTimeline"/> (pure,
    /// tested); this class acts on it. It follows whoever the race camera follows, so a relay ends on the team's last runner (no emote: it
    /// is not the player's). A replay borrows the camera in between. A new race or leaving the scene gives everything back.
    /// Added by RaceSceneInstaller next to the session; its LateUpdate comes after everything that poses the runner.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class RaceFinishSequence : MonoBehaviour, IRaceFinishSequence
    {
        RaceSessionAdapter _session;
        RaceEmotes _emotes;
        RaceFinishConfig _config;
        RaceFinishTimeline _timeline;
        Action _onResultsDue;

        Camera _camera;
        SideFollowCamera _follow;
        RacePlayback _playback;
        bool _followWasOn;
        float _savedFieldOfView;
        Vector3 _startOffset;
        Quaternion _startRotation;
        float _savedTimeScale = 1f;

        Transform _runner;
        RunnerBody _body;
        RunnerCooldown _cooldown;
        int _rank;
        bool _isPlayer;
        float _emoteSeconds;
        float _emoteAgain;

        [Inject]
        void Construct([InjectOptional] RaceEmotes emotes) => _emotes = emotes;

        void Awake()
        {
            _session = GetComponent<RaceSessionAdapter>();
            if (!_session) return;
            _session.Restarting += Stop;
            _session.FinishTapped += Skip;
        }

        void OnDestroy()
        {
            if (!_session) return;
            _session.Restarting -= Stop;
            _session.FinishTapped -= Skip;
        }

        void OnDisable() => Stop();

        public void Play(RaceOutcome outcome, Action onResultsDue)
        {
            Stop();
            if (!_session) return;
            _config = _session.FinishConfig;
            _onResultsDue = onResultsDue;

            _camera = Camera.main;
            _follow = _camera ? _camera.GetComponent<SideFollowCamera>() : null;
            _playback = _session.Playback;
            RunnerBody player = _session.PlayerBody;
            _runner = _follow && _follow.target ? _follow.target : player ? player.transform : null;
            _body = _runner ? _runner.GetComponent<RunnerBody>() : null;
            _isPlayer = _body && _body == player;
            if (!_body)
            {
                OpenResults();
                return;
            }

            _rank = outcome.Rank;
            _cooldown = RunnerCooldown.Begin(_body, _config);
            TakeCamera();
            _savedTimeScale = Time.timeScale;
            _emoteSeconds = 0f;
            _emoteAgain = float.PositiveInfinity;
            _timeline = new RaceFinishTimeline(_config);
            _timeline.Begin();
            Time.timeScale = _timeline.TimeScale;
        }

        void TakeCamera()
        {
            if (!_camera) return;
            Transform cam = _camera.transform;
            _startOffset = cam.position - _runner.position;
            _startOffset = new Vector3(Vector3.Dot(_startOffset, _runner.right), _startOffset.y, Vector3.Dot(_startOffset, _runner.forward));
            _startRotation = cam.rotation;
            _savedFieldOfView = _camera.fieldOfView;
            if (!_follow) return;
            _followWasOn = _follow.enabled;
            _follow.enabled = false;
        }

        /// <summary>A tap on the screen during the sequence.</summary>
        void Skip() => _timeline?.Skip();

        /// <summary>Gives back the game speed and the camera; the runner's own reset gives back the stance. The Results callback is dropped.</summary>
        void Stop()
        {
            _onResultsDue = null;
            if (_timeline == null) return;
            _timeline = null;
            Time.timeScale = _savedTimeScale;
            if (_camera) _camera.fieldOfView = _savedFieldOfView;
            if (_follow) _follow.enabled = _followWasOn;
        }

        void Update()
        {
            if (_timeline == null) return;
            float seconds = Mathf.Min(Time.unscaledDeltaTime, _config.MaxFrameSeconds);
            bool wasHolding = _timeline.Phase == RaceFinishPhase.Holding;
            _timeline.Tick(seconds, _cooldown ? _cooldown.Speed : 0f);

            if (!wasHolding || Time.timeScale != _savedTimeScale)
                Time.timeScale = _timeline.Phase == RaceFinishPhase.Holding ? _savedTimeScale : _timeline.TimeScale * _savedTimeScale;

            if (_timeline.TakeEmoteDue())
            {
                _emoteSeconds = PlayEmote();
                _timeline.EmoteStarted(_emoteSeconds);
                _emoteAgain = _emoteSeconds + _config.EmoteRepeatGapSeconds;
            }
            else if (_emoteSeconds > 0f && !Replaying)
            {
                _emoteAgain -= seconds;
                if (_emoteAgain <= 0f)
                {
                    PlayEmote();
                    _emoteAgain = _emoteSeconds + _config.EmoteRepeatGapSeconds;
                }
            }

            if (_timeline.TakeResultsDue()) OpenResults();
        }

        /// <summary>The player's emote, or the body's own reaction when none plays. Seconds it lasts.</summary>
        float PlayEmote()
        {
            float seconds = _isPlayer && _emotes ? _emotes.PlayFinishEmote() : 0f;
            if (seconds > 0f || !_cooldown) return seconds;
            _cooldown.React(_rank == 1 ? RunnerFinishReaction.Cheer : RunnerFinishReaction.Winded);
            return _config.ReactionSeconds;
        }

        void OpenResults()
        {
            Action due = _onResultsDue;
            _onResultsDue = null;
            due?.Invoke();
        }

        bool Replaying => _playback && _playback.IsActive;

        void LateUpdate()
        {
            if (_timeline == null || !_camera || !_runner || Replaying) return;

            float t = _timeline.CameraProgress;
            Vector3 place = Vector3.Slerp(_startOffset, _config.CameraOffset, t);
            Vector3 position = _runner.position + _runner.right * place.x + Vector3.up * place.y + _runner.forward * place.z;
            Vector3 look = _runner.position + Vector3.up * _config.CameraLookHeight - position;

            Transform cam = _camera.transform;
            cam.position = position;
            if (look.sqrMagnitude > 1e-6f) cam.rotation = Quaternion.Slerp(_startRotation, Quaternion.LookRotation(look, Vector3.up), t);
            _camera.fieldOfView = Mathf.Lerp(_savedFieldOfView, _config.CameraFieldOfView, t);
        }
    }
}
