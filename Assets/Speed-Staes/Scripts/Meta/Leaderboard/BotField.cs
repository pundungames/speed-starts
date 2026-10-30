using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Leaderboard
{
    /// <summary>One generated bot result. The age is seconds before "now", so the date follows the clock while time, name and country never change.</summary>
    internal readonly struct BotResult
    {
        public readonly string EntryId;
        public readonly string Name;
        public readonly string CountryCode;
        public readonly long ElapsedMilliseconds;
        public readonly long AgeSeconds;

        public BotResult(string entryId, string name, string countryCode, long elapsedMilliseconds, long ageSeconds)
        {
            EntryId = entryId;
            Name = name;
            CountryCode = countryCode;
            ElapsedMilliseconds = elapsedMilliseconds;
            AgeSeconds = ageSeconds;
        }
    }

    /// <summary>
    /// The fake field the local leaderboard ranks the player against. Deterministic: a bot is a pure function of the
    /// event id, its number and the config, so every launch shows the same table. Uses its own hash and generator
    /// (not <c>string.GetHashCode</c> / <c>System.Random</c>) so the result never depends on the runtime.
    /// </summary>
    internal static class BotField
    {
        public static List<BotResult> Generate(LeaderboardConfig config, RaceEventId eventId, float distanceMeters)
        {
            var result = new List<BotResult>();
            if (config == null || distanceMeters <= 0f) return result; // events without a distance have no pace to scale

            IReadOnlyList<string> names = config.GetBotNames();
            IReadOnlyList<BotCountry> countries = config.GetBotCountries();
            float totalWeight = 0f;
            foreach (BotCountry country in countries) totalWeight += country.Weight;

            long historySeconds = config.BotHistoryDays * 86400L;
            ulong eventHash = Fnv1a(eventId.Value);

            for (int i = 0; i < config.BotsPerEvent; i++)
            {
                ulong state = eventHash ^ ((ulong)(i + 1) * 0x9E3779B97F4A7C15UL);
                double paceU = Next01(ref state);
                double nameU = Next01(ref state);
                double countryU = Next01(ref state);
                double ageU = Next01(ref state);

                double pace = Pace(config, paceU);
                long ms = (long)Math.Round(pace * distanceMeters * 1000d);
                if (ms <= 0L) ms = 1L;

                string name = names.Count > 0 ? names[(int)(nameU * names.Count) % names.Count] : "Runner " + (i + 1);
                result.Add(new BotResult(
                    "bot:" + eventId.Value + ":" + i,
                    name,
                    PickCountry(countries, totalWeight, countryU),
                    ms,
                    (long)(ageU * historySeconds)));
            }
            return result;
        }

        /// <summary>Piecewise pace by quantile: best at 0, median at 0.5, worst at 1 (skewed when the median is not midway).</summary>
        private static double Pace(LeaderboardConfig c, double u) =>
            u < 0.5d
                ? c.BestSecondsPerMeter + (c.MedianSecondsPerMeter - c.BestSecondsPerMeter) * (u * 2d)
                : c.MedianSecondsPerMeter + (c.WorstSecondsPerMeter - c.MedianSecondsPerMeter) * ((u - 0.5d) * 2d);

        private static string PickCountry(IReadOnlyList<BotCountry> countries, float totalWeight, double u)
        {
            if (countries.Count == 0 || totalWeight <= 0f) return string.Empty;
            double target = u * totalWeight;
            double run = 0d;
            for (int i = 0; i < countries.Count; i++)
            {
                run += countries[i].Weight;
                if (target < run) return countries[i].Code;
            }
            return countries[countries.Count - 1].Code;
        }

        private static ulong Fnv1a(string text)
        {
            ulong hash = 14695981039346656037UL;
            if (text == null) return hash;
            foreach (char ch in text)
            {
                hash ^= ch;
                hash *= 1099511628211UL;
            }
            return hash;
        }

        /// <summary>SplitMix64 step mapped to [0, 1).</summary>
        private static double Next01(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0d / 9007199254740992.0d);
        }
    }
}
