using System;
using System.Globalization;

namespace SpeedStars.Meta.Rewards
{
    /// <summary>The reward day: a UTC calendar day (same as the leaderboard), stored as "yyyy-MM-dd".</summary>
    internal static class RewardDay
    {
        public static string Key(DateTime utcNow) =>
            utcNow.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
