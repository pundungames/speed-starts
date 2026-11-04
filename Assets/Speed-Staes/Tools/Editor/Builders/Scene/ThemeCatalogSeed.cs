using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Themes;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of Data/Config/ThemeCatalog.asset: the five themes of Serhat's main menu showcase, in the order of
    /// its theme list (Anamenu Themalari: Standard City, Egypt, Rome / Byzantium, Pirate Sea, Sky Islands). Prices are the
    /// user's (04.11): 0 = free. An existing asset is the user's and is never touched unless overwrite is asked for, or
    /// <see cref="ApplyPrices"/> is run for the prices only.
    /// </summary>
    internal static class ThemeCatalogSeed
    {
        private const string Source = nameof(ThemeCatalogSeed);

        // Premium theme (user, 30.10): priced in Purple Gems, same family as cosmetics.
        private const string PremiumId = "sky_islands";

        // Showcase index = position in MainMenuThemeShowcase.themeDisplays (its titles array has the same order).
        private static readonly (string Id, string Name, int Price)[] Seeds =
        {
            ("standard_city", "Standard City", 0),
            ("egypt", "Egypt", 2500),
            ("rome_byzantium", "Rome / Byzantium", 5000),
            ("pirate_sea", "Pirate Sea", 7500),
            ("sky_islands", "Sky Islands", 50),
        };

        /// <summary>
        /// Writes the seed table's prices into the EXISTING catalog (the seeder skips an existing asset, so a new price would never
        /// reach it). Only the price fields change; a second run writes nothing.
        /// </summary>
        public static void ApplyPrices(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalog>(path);
            if (catalog == null) { Warn(Source, $"'{path}' is missing; theme prices not written."); return; }

            var so = new SerializedObject(catalog);
            var list = Prop(so, "themes");
            bool changed = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                string id = Rel(entry, "themeId").stringValue;
                foreach (var seed in Seeds)
                {
                    if (seed.Id != id) continue;
                    var price = Rel(entry, "cashPrice");
                    if (price.intValue == seed.Price) continue;
                    price.intValue = seed.Price;
                    changed = true;
                }
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }

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
                Rel(entry, "cashPrice").intValue = Seeds[i].Price;
                bool premium = Seeds[i].Id == PremiumId;
                Rel(entry, "premium").boolValue = premium;
                Rel(entry, "priceCurrency").enumValueIndex = (int)(premium ? ThemePriceCurrency.PurpleGem : ThemePriceCurrency.Cash);
            }
            Prop(so, "defaultThemeId").stringValue = Seeds[0].Id;
            Apply(so);
            Log(Source, $"Seeded {path} with {Seeds.Length} themes.");
            return catalog;
        }
    }
}
