using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Meta.Leaderboard
{
    /// <summary>A country the bots can come from, with its draw weight.</summary>
    [Serializable]
    public sealed class BotCountry
    {
        [Tooltip("Country code of the flag (TR, US...). Same codes as the settings flag list. PLACEHOLDER until real data exists.")]
        [SerializeField] private string countryCode;

        [Tooltip("Relative chance a bot comes from this country. PLACEHOLDER: uniform.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        public string Code => countryCode ?? string.Empty;
        public float Weight => Mathf.Max(0f, weight);
    }

    /// <summary>
    /// Numbers of the local leaderboard: the fake field of bot results (a stand-in for the future server) and the cap of the
    /// player's saved day bests. Bot values are PLACEHOLDERS until the runner's real times are known.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Leaderboard", fileName = "LeaderboardConfig")]
    public sealed class LeaderboardConfig : ScriptableObject
    {
        [Header("Bots (placeholder until the runner's real times are known)")]
        [Tooltip("Bots per race event. PLACEHOLDER until the runner's real times are known.")]
        [SerializeField, Min(0)] private int botsPerEvent = 500;

        [Tooltip("Pace of the fastest bots, seconds per metre. PLACEHOLDER until the runner's real times are known.")]
        [SerializeField, Min(0f)] private float bestSecondsPerMeter = 0.105f;

        [Tooltip("Pace of the median bot, seconds per metre. PLACEHOLDER until the runner's real times are known.")]
        [SerializeField, Min(0f)] private float medianSecondsPerMeter = 0.16f;

        [Tooltip("Pace of the slowest bots, seconds per metre. PLACEHOLDER until the runner's real times are known.")]
        [SerializeField, Min(0f)] private float worstSecondsPerMeter = 0.26f;

        [Tooltip("Bot results are spread over this many days back from now, so Today / This Week / This Month have different fields. PLACEHOLDER.")]
        [SerializeField, Min(1)] private int botHistoryDays = 120;

        [Tooltip("Bot first names (picked by seed). PLACEHOLDER.")]
        [SerializeField] private string[] botNames = new string[0];

        [Tooltip("Countries the bots come from, with weights. PLACEHOLDER: the settings flags, uniform.")]
        [SerializeField] private BotCountry[] botCountries = new BotCountry[0];

        [Header("Player records")]
        [Tooltip("Most day-best entries kept in the save (all events together). The oldest go first; an event's all-time best is never dropped.")]
        [SerializeField, Min(1)] private int maxDayBestEntries = 400;

        [Header("Paging")]
        [Tooltip("Most rows one page may hold, whatever the query asks for.")]
        [SerializeField, Min(1)] private int maxPageSize = 100;

        public int BotsPerEvent => Mathf.Max(0, botsPerEvent);
        public float BestSecondsPerMeter => Mathf.Max(0f, bestSecondsPerMeter);
        public float MedianSecondsPerMeter => Mathf.Max(BestSecondsPerMeter, medianSecondsPerMeter);
        public float WorstSecondsPerMeter => Mathf.Max(MedianSecondsPerMeter, worstSecondsPerMeter);
        public int BotHistoryDays => Mathf.Max(1, botHistoryDays);
        public int MaxDayBestEntries => Mathf.Max(1, maxDayBestEntries);
        public int MaxPageSize => Mathf.Max(1, maxPageSize);

        /// <summary>Non-empty names in list order.</summary>
        public IReadOnlyList<string> GetBotNames()
        {
            var list = new List<string>();
            if (botNames == null) return list;
            foreach (string name in botNames)
                if (!string.IsNullOrWhiteSpace(name)) list.Add(name.Trim());
            return list;
        }

        /// <summary>Countries with a code and a positive weight, in list order.</summary>
        public IReadOnlyList<BotCountry> GetBotCountries()
        {
            var list = new List<BotCountry>();
            if (botCountries == null) return list;
            foreach (BotCountry country in botCountries)
                if (country != null && !string.IsNullOrEmpty(country.Code) && country.Weight > 0f) list.Add(country);
            return list;
        }
    }
}
