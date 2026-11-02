using SpeedStars.Core.Race.Bots;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First creation of Data/Config/BotRaceConfig.asset (T-074). The field defaults of BotRaceConfig are the first values (field size,
    /// difficulty, driver limits, the placeholder calibration table, the made-up name pool), so nothing is written over them. An existing
    /// asset is the user's and is never touched unless overwrite is asked for; the existing asset is returned.
    /// </summary>
    internal static class BotRaceConfigSeed
    {
        private const string Source = nameof(BotRaceConfigSeed);

        public const string Path = ConfigAssetSeeder.Folder + "/BotRaceConfig.asset";

        public static BotRaceConfig Seed(bool overwrite)
        {
            EnsureFolder(ConfigAssetSeeder.Folder);
            var config = CreateOrReset<BotRaceConfig>(Path, overwrite);
            if (config == null)
            {
                Skipped(Source, Path);
                return AssetDatabase.LoadAssetAtPath<BotRaceConfig>(Path);
            }
            Log(Source, $"Created {Path} with the field defaults.");
            return config;
        }
    }
}
