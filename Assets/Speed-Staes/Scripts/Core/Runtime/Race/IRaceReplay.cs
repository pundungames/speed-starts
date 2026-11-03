using System;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Watch Replay of the race just run: the Results screen asks for it and waits for it to end, the replay controls drive it while it plays.
    /// </summary>
    public interface IRaceReplay
    {
        /// <summary>The player has finished a race that was recorded.</summary>
        bool CanWatch { get; }

        bool IsWatching { get; }

        /// <summary>Plays the last race from the start, following the player. False if there is nothing to play.</summary>
        bool Watch();

        /// <summary>Leaves the replay early; the runners are live again.</summary>
        void Leave();

        /// <summary>The replay is over (played to the end or left).</summary>
        event Action WatchEnded;

        /// <summary>Replay clock (s from the race start) and where it ends (the followed runner's finish).</summary>
        float Time { get; }
        float Duration { get; }

        bool IsPlaying { get; }
        void SetPlaying(bool playing);
        void Seek(float seconds);

        /// <summary>Playback speed (1 = real time); NextSpeed steps through the offered ones.</summary>
        float Speed { get; }
        void NextSpeed();

        /// <summary>The runners of the replay; the followed one's name, speed (m/s) and whether it is the player.</summary>
        int RunnerCount { get; }
        int FollowIndex { get; }
        string FollowedName { get; }
        float FollowedSpeed { get; }
        bool FollowedIsPlayer { get; }
        /// <summary>Follows the next (+1) or previous (-1) runner.</summary>
        void Follow(int step);

        /// <summary>The camera shots: which is on, how many, and a step to the next (+1) or previous (-1).</summary>
        int ShotIndex { get; }
        int ShotCount { get; }
        void NextShot(int step);
    }
}
