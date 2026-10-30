using SpeedStars.UI.Race;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First creation of Data/Config/AnalysisConfig.asset (T-036). The field defaults of AnalysisConfig are the first
    /// values (sampling step, axis rounding, line colours from the art's layout.json), so there is nothing to write over
    /// them. An existing asset is the user's and is never touched unless overwrite is asked for; the existing asset is returned.
    /// </summary>
    internal static class AnalysisConfigSeed
    {
        private const string Source = nameof(AnalysisConfigSeed);

        public static AnalysisConfig Seed(string path, bool overwrite)
        {
            var config = CreateOrReset<AnalysisConfig>(path, overwrite);
            if (config == null)
            {
                Skipped(Source, path);
                return AssetDatabase.LoadAssetAtPath<AnalysisConfig>(path);
            }
            Log(Source, $"Created {path} with the field defaults.");
            return config;
        }
    }
}
