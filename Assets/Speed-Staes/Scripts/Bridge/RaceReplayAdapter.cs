using System;
using SpeedStars.Core.Race;
using SpeedStars.Physical;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// IRaceReplay over Malik's RacePlayback / RaceRecorder. Both are added to the RaceController at its Start, after the scene's
    /// bindings, so they are looked up on first use. Played to the end, the replay is left here, so the runners are live again
    /// before the Results screen comes back. Runner names come from RaceBots (the player's and the bots' shown names).
    /// </summary>
    public sealed class RaceReplayAdapter : IRaceReplay
    {
        RacePlayback _playback;
        RaceBots _bots;
        bool _botsLooked;

        public event Action WatchEnded;

        public bool CanWatch
        {
            get
            {
                RacePlayback playback = Playback;
                return playback != null && playback.recorder != null && playback.recorder.HasRace;
            }
        }

        public bool IsWatching => Playback != null && _playback.IsActive;

        RacePlayback Playback
        {
            get
            {
                if (_playback == null) _playback = UnityEngine.Object.FindObjectOfType<RacePlayback>();
                return _playback;
            }
        }

        public bool Watch()
        {
            RacePlayback playback = Playback;
            if (playback == null) return false;
            playback.Ended -= OnEnded;
            playback.Stopped -= OnStopped;
            if (!playback.Play()) return false;
            playback.Ended += OnEnded;
            playback.Stopped += OnStopped;
            return true;
        }

        public void Leave()
        {
            if (Playback != null) _playback.Stop();
        }

        void OnEnded() => _playback.Stop();

        void OnStopped()
        {
            _playback.Ended -= OnEnded;
            _playback.Stopped -= OnStopped;
            WatchEnded?.Invoke();
        }

        public float Time => IsWatching ? _playback.Time : 0f;
        public float Duration => IsWatching ? _playback.Duration : 0f;
        public bool IsPlaying => IsWatching && _playback.IsPlaying;
        public void SetPlaying(bool playing) => Playback?.SetPlaying(playing);
        public void Seek(float seconds) => Playback?.Seek(seconds);
        public float Speed => Playback != null ? _playback.Speed : 1f;
        public void NextSpeed() => Playback?.NextSpeed();

        public int RunnerCount => IsWatching ? _playback.Recording.Tracks.Count : 0;
        public int FollowIndex => IsWatching ? _playback.FollowIndex : 0;
        public bool FollowedIsPlayer => IsWatching && _playback.Followed != null && _playback.Followed.IsPlayer;
        public void Follow(int step) => Playback?.Follow(step);

        public string FollowedName
        {
            get
            {
                RunnerTrack track = IsWatching ? _playback.Followed : null;
                if (track == null || !track.Body) return string.Empty;
                if (!_botsLooked)
                {
                    _bots = UnityEngine.Object.FindObjectOfType<RaceBots>();
                    _botsLooked = true;
                }
                string name = _bots ? _bots.NameOf(track.Body) : null;
                return string.IsNullOrEmpty(name) ? track.Body.gameObject.name : name;
            }
        }

        public float FollowedSpeed
        {
            get
            {
                RunnerTrack track = IsWatching ? _playback.Followed : null;
                if (track == null) return 0f;
                track.ValuesAt(_playback.Time, out float speed, out _, out _);
                return speed;
            }
        }

        public int ShotIndex => Playback != null ? _playback.ShotIndex : 0;
        public int ShotCount => Playback != null && _playback.replayCamera ? _playback.replayCamera.shots.Length : 0;
        public void NextShot(int step) => Playback?.NextShot(step);
    }
}
