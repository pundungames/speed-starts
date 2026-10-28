using SpeedStars.Meta.Settings;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of Data/Config/SettingsConfig.asset (T-031): music and sound effects on, English only (no translation
    /// system exists, the choice is just saved), the 16 flags of the settings art with TR as the start flag (as drawn in
    /// the approved design), name "Player", 1-16 characters. No CN flag: it is not in the art package yet; when it
    /// arrives it is one more flag entry in the asset. An existing asset is the user's and is never touched unless
    /// overwrite is asked for.
    /// </summary>
    internal static class SettingsConfigSeed
    {
        private const string Source = nameof(SettingsConfigSeed);

        // Names in English ASCII (kept for tooling; the UI shows only the pictures).
        private static readonly (string Code, string Name)[] Flags =
        {
            ("TR", "Turkiye"), ("US", "United States"), ("GB", "United Kingdom"), ("DE", "Germany"),
            ("FR", "France"), ("IT", "Italy"), ("NL", "Netherlands"), ("BE", "Belgium"),
            ("PL", "Poland"), ("UA", "Ukraine"), ("JP", "Japan"), ("SE", "Sweden"),
            ("NO", "Norway"), ("DK", "Denmark"), ("FI", "Finland"), ("CH", "Switzerland"),
        };

        public static SettingsConfig Seed(string path, bool overwrite)
        {
            var config = CreateOrReset<SettingsConfig>(path, overwrite);
            if (config == null) { Skipped(Source, path); return AssetDatabase.LoadAssetAtPath<SettingsConfig>(path); }

            UiSettingsPrefabs.EnsureArt(); // the flag sprites must be imported before they can be referenced

            var so = new SerializedObject(config);
            Prop(so, "defaultMusicOn").boolValue = true;
            Prop(so, "defaultSfxOn").boolValue = true;
            Prop(so, "defaultLanguageCode").stringValue = "en";
            Prop(so, "defaultFlagCode").stringValue = "TR";
            Prop(so, "defaultPlayerName").stringValue = "Player";
            Prop(so, "nameMinLength").intValue = 1;
            Prop(so, "nameMaxLength").intValue = 16;

            var languages = Prop(so, "languages");
            languages.arraySize = 1;
            Rel(languages.GetArrayElementAtIndex(0), "languageCode").stringValue = "en";
            Rel(languages.GetArrayElementAtIndex(0), "displayName").stringValue = "English";

            var flags = Prop(so, "flags");
            flags.arraySize = Flags.Length;
            for (int i = 0; i < Flags.Length; i++)
            {
                var entry = flags.GetArrayElementAtIndex(i);
                Rel(entry, "flagCode").stringValue = Flags[i].Code;
                Rel(entry, "displayName").stringValue = Flags[i].Name;
                Rel(entry, "sprite").objectReferenceValue = UiSettingsPrefabs.FlagSprite(Flags[i].Code);
            }
            Apply(so);
            Log(Source, $"Seeded {path} with {Flags.Length} flags and 1 language.");
            return config;
        }
    }
}
