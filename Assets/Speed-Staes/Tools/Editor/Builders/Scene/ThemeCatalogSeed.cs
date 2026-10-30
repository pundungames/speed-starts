using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Themes;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of Data/Config/ThemeCatalog.asset: the five themes of Serhat's main menu showcase, in the order of
    /// its theme list (Anamenu Themalari: Standard City, Egypt, Rome / Byzantium, Pirate Sea, Sky Islands). Prices are
    /// NOT decided yet, so every price is 0 = free (owned and usable in every build; a price above 0 makes the theme
    /// a purchase). An existing asset is
    /// the user's and is never touched unless overwrite is asked for.
    /// </summary>
    internal static class ThemeCatalogSeed
    {
        private const string Source = nameof(ThemeCatalogSeed);

        // Premium theme (user, 30.10): priced in Purple Gems, same family as cosmetics. The price is still open (0).
        private const string PremiumId = "sky_islands";

        // Showcase index = position in MainMenuThemeShowcase.themeDisplays (its titles array has the same order).
        private static readonly (string Id, string Name)[] Seeds =
        {
            ("standard_city", "Standard City"),
            ("egypt", "Egypt"),
            ("rome_byzantium", "Rome / Byzantium"),
            ("pirate_sea", "Pirate Sea"),
            ("sky_islands", "Sky Islands"),
        };

        public static ThemeCatalog Seed(string path, bool overwrite)
        {
            var catalog = CreateOrReset<ThemeCatalog>(path, overwrite);
            if (catalog == null) { Skipped(Source, path); return AssetDatabase.LoadAssetAtPath<ThemeCatalog>(path); }

            var so = new SerializedObject(catalog);
            var list = Prop(so, "themes");
            list.arraySize = Seeds.Length;
            for (int i = 0; i < Seeds.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                Rel(entry, "themeId").stringValue = Seeds[i].Id;
                Rel(entry, "displayName").stringValue = Seeds[i].Name;
                Rel(entry, "showcaseIndex").intValue = i;
                Rel(entry, "cashPrice").intValue = 0; // price not decided: 0 = free for now
                bool premium = Seeds[i].Id == PremiumId;
                Rel(entry, "premium").boolValue = premium;
                Rel(entry, "priceCurrency").enumValueIndex = (int)(premium ? ThemePriceCurrency.PurpleGem : ThemePriceCurrency.Cash);
            }
            Prop(so, "defaultThemeId").stringValue = Seeds[0].Id;
            Apply(so);
            Log(Source, $"Seeded {path} with {Seeds.Length} themes (all prices 0 = free until a price is set).");
            return catalog;
        }
    }
}
