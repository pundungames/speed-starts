using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>The leaderboard art of one country flag: the same flag on the three canvases it is drawn on.</summary>
    [Serializable]
    public sealed class LeaderboardFlagEntry
    {
        [Tooltip("Country code (ISO 3166-1 alpha-2). Empty on the entry that is used for unknown countries.")]
        [SerializeField] private string code = string.Empty;

        [Tooltip("ranking_row_flag_XX: full table-row canvas with the flag already in its column.")]
        [SerializeField] private Sprite tableRow;

        [Tooltip("country_row_flag_XX: full region-list-row canvas with the flag already in place.")]
        [SerializeField] private Sprite listRow;

        [Tooltip("flag_XX: the flag alone (shown next to the region name in the filter field).")]
        [SerializeField] private Sprite icon;

        public string Code => code ?? string.Empty;
        public Sprite TableRow => tableRow;
        public Sprite ListRow => listRow;
        public Sprite Icon => icon;
    }

    /// <summary>Lookup of flag art by country code; a code without art gets the neutral "unknown" flag (never a random one).</summary>
    public sealed class LeaderboardFlagLibrary
    {
        private readonly Dictionary<string, LeaderboardFlagEntry> _byCode = new Dictionary<string, LeaderboardFlagEntry>(StringComparer.Ordinal);
        private readonly LeaderboardFlagEntry _unknown;

        public LeaderboardFlagLibrary(IEnumerable<LeaderboardFlagEntry> entries, LeaderboardFlagEntry unknown)
        {
            _unknown = unknown;
            if (entries == null) return;
            foreach (LeaderboardFlagEntry entry in entries)
                if (entry != null && !string.IsNullOrEmpty(entry.Code)) _byCode[entry.Code.ToUpperInvariant()] = entry;
        }

        /// <summary>The entry of a code, or the unknown flag's entry (may be null when the art is not assigned).</summary>
        public LeaderboardFlagEntry Get(string code)
        {
            string key = LeaderboardPresenter.NormalizeCountry(code);
            if (key != null && _byCode.TryGetValue(key, out LeaderboardFlagEntry entry)) return entry;
            return _unknown;
        }
    }
}
