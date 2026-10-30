using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>One country of the region list.</summary>
    public readonly struct LeaderboardCountry
    {
        public readonly string Code;
        public readonly string Label;

        public LeaderboardCountry(string code, string label)
        {
            Code = code ?? string.Empty;
            Label = label ?? string.Empty;
        }
    }

    /// <summary>Pure rules of the region list: shown labels and the accent / case insensitive search.</summary>
    public static class LeaderboardCountryList
    {
        /// <summary>Upper case without accents ("Türkiye" gives "TURKIYE"): the art has no accented capitals.</summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string decomposed = text.Replace('ı', 'i').Replace('İ', 'I').Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
            return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }

        /// <summary>Countries whose label (or code) matches the query; an empty query lists everything. List order is kept.</summary>
        public static List<LeaderboardCountry> Filter(IReadOnlyList<LeaderboardCountry> all, string query)
        {
            var result = new List<LeaderboardCountry>();
            if (all == null) return result;
            string needle = Normalize(query).Trim();
            foreach (LeaderboardCountry country in all)
            {
                if (needle.Length == 0
                    || Normalize(country.Label).Contains(needle)
                    || string.Equals(Normalize(country.Code), needle, StringComparison.Ordinal))
                    result.Add(country);
            }
            return result;
        }
    }
}
