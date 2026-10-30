using SpeedStars.Meta.Leaderboard;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of Data/Config/LeaderboardConfig.asset (T-037). Every bot number is a PLACEHOLDER until the runner's real
    /// times are known: 500 bots per event, pace 0.105 / 0.16 / 0.26 s per metre (best / median / worst), results spread
    /// over 120 days, about 40 short international first names, the 16 settings flags with equal weight, 400 saved day
    /// bests, pages of at most 100 rows. An existing asset is the user's and is never touched unless overwrite is asked for.
    /// </summary>
    internal static class LeaderboardConfigSeed
    {
        private const string Source = nameof(LeaderboardConfigSeed);

        private static readonly string[] Names =
        {
            "Aiden", "Mia", "Leo", "Zoe", "Noah", "Emma", "Kai", "Lena", "Omar", "Sara",
            "Luca", "Nina", "Eren", "Ayse", "Hugo", "Clara", "Ivan", "Olga", "Yuki", "Haru",
            "Sven", "Freya", "Marco", "Giulia", "Jan", "Eva", "Piotr", "Kasia", "Finn", "Ida",
            "Theo", "Lou", "Nils", "Anna", "Jonas", "Maja", "Emre", "Deniz", "Max", "Lily",
        };

        // The 16 flag codes of the settings art (SettingsConfigSeed); bots draw their country from these.
        private static readonly string[] Countries =
        {
            "TR", "US", "GB", "DE", "FR", "IT", "NL", "BE", "PL", "UA", "JP", "SE", "NO", "DK", "FI", "CH",
        };

        public static LeaderboardConfig Seed(string path, bool overwrite)
        {
            var config = CreateOrReset<LeaderboardConfig>(path, overwrite);
            if (config == null) { Skipped(Source, path); return AssetDatabase.LoadAssetAtPath<LeaderboardConfig>(path); }

            var so = new SerializedObject(config);
            Prop(so, "botsPerEvent").intValue = 500;
            Prop(so, "bestSecondsPerMeter").floatValue = 0.105f;
            Prop(so, "medianSecondsPerMeter").floatValue = 0.16f;
            Prop(so, "worstSecondsPerMeter").floatValue = 0.26f;
            Prop(so, "botHistoryDays").intValue = 120;
            Prop(so, "maxDayBestEntries").intValue = 400;
            Prop(so, "maxPageSize").intValue = 100;

            var names = Prop(so, "botNames");
            names.arraySize = Names.Length;
            for (int i = 0; i < Names.Length; i++) names.GetArrayElementAtIndex(i).stringValue = Names[i];

            var countries = Prop(so, "botCountries");
            countries.arraySize = Countries.Length;
            for (int i = 0; i < Countries.Length; i++)
            {
                var entry = countries.GetArrayElementAtIndex(i);
                Rel(entry, "countryCode").stringValue = Countries[i];
                Rel(entry, "weight").floatValue = 1f;
            }
            Apply(so);
            Log(Source, $"Seeded {path} (placeholder bot numbers).");
            return config;
        }
    }
}
