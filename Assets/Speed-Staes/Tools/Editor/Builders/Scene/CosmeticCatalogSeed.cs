using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SpeedStars.Meta.Config;
using UnityEditor;
using UnityEngine;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of the cosmetic catalog from the runner prefab's wardrobe (reports/kozmetik-hazirlik-2026-10-28.md).
    /// The prefab is only read, never instantiated or saved: it is Malik's file. Each category slot
    /// <c>Accessories/&lt;Category&gt;/</c> holds 7 normal and 2 premium sets; a set may be several objects (GL03 +
    /// GL03SMLEL + GL03SMLER), so one cosmetic lists all of its parts and the runtime never parses names again.
    /// Prices: premium = Purple Gems (PDF §1.3, §2.3); normal items have no price in the documents (OPEN #6), so 0 =
    /// not for sale.
    /// </summary>
    internal static class CosmeticCatalogSeed
    {
        private const string Source = nameof(ConfigAssetSeeder) + "/Cosmetics";

        public const string CharacterPrefabPath = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const string AccessoriesName = "Accessories";

        // Set numbers of the wardrobe naming (Serhat's AccessoryPhotoStudio): 1-7 normal, 8-9 premium.
        private const int LastNormalSet = 7;
        private const int LastPremiumSet = 9;

        /// <summary>The catalog to read day 7 from: the seeded one, or the existing one when it is left as it is.</summary>
        public static CosmeticCatalog Seed(string catalogPath, string folder, bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(catalogPath);
            if (existing != null && !overwrite)
            {
                Skipped(Source, catalogPath);
                return existing;
            }

            // Read the wardrobe before writing anything, so a missing prefab cannot leave an empty catalog behind.
            var prefab = LoadRequired<GameObject>(CharacterPrefabPath, "the runner prefab (Malik)");
            Transform accessories = prefab.transform.Find(AccessoriesName);
            if (accessories == null)
                throw new InvalidOperationException($"'{CharacterPrefabPath}' has no '{AccessoriesName}' child; the cosmetic slots cannot be read.");

            var catalog = CreateOrReset<CosmeticCatalog>(catalogPath, overwrite);
            EnsureFolder(folder);
            var items = new List<UnityEngine.Object>();
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                string slot = category.ToString(); // enum names are the slot object names (CosmeticTypes.cs)
                Transform slotRoot = accessories.Find(slot);
                if (slotRoot == null)
                {
                    Warn(Source, $"'{AccessoriesName}/{slot}' is missing in the runner prefab; no {slot} cosmetics seeded.");
                    continue;
                }

                SortedDictionary<int, List<string>> sets = GroupBySet(slot, slotRoot);
                for (int set = 1; set <= LastPremiumSet; set++)
                {
                    if (!sets.TryGetValue(set, out List<string> parts))
                    {
                        Warn(Source, $"{slot}: no object for set {set}; that cosmetic is not seeded.");
                        continue;
                    }
                    items.Add(SeedItem(folder, category, slot, set, parts, overwrite));
                }
            }

            var so = new SerializedObject(catalog);
            SetObjectList(Prop(so, "cosmetics"), items.ToArray());
            Apply(so);
            Log(Source, $"Seeded {catalogPath} with {items.Count} cosmetics.");
            return catalog;
        }

        private static SortedDictionary<int, List<string>> GroupBySet(string slot, Transform slotRoot)
        {
            var sets = new SortedDictionary<int, List<string>>();
            var unknown = new List<string>();
            foreach (Transform child in slotRoot)
            {
                int set = SetFor(slot, child.name);
                if (set == 0) { unknown.Add(child.name); continue; }
                if (!sets.TryGetValue(set, out List<string> parts)) sets[set] = parts = new List<string>();
                parts.Add(child.name);
            }
            if (unknown.Count > 0)
                Warn(Source, $"{slot}: objects without a set number were skipped: {string.Join(", ", unknown)}.");
            foreach (var parts in sets.Values) parts.Sort(StringComparer.Ordinal);
            return sets;
        }

        private static CosmeticDefinition SeedItem(string folder, CosmeticCategory category, string slot, int set, List<string> parts, bool overwrite)
        {
            string main = MainPart(parts);
            string path = $"{folder}/{slot}_{main}.asset";
            var definition = CreateOrReset<CosmeticDefinition>(path, overwrite);
            if (definition == null)
            {
                Skipped(Source, path);
                return LoadRequired<CosmeticDefinition>(path, Source);
            }

            bool premium = set > LastNormalSet;
            var so = new SerializedObject(definition);
            Prop(so, "cosmeticId").stringValue = $"{slot.ToLowerInvariant()}.{main.ToLowerInvariant()}";
            Prop(so, "category").intValue = (int)category;
            var partNames = Prop(so, "partNames");
            partNames.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++) partNames.GetArrayElementAtIndex(i).stringValue = parts[i];
            Prop(so, "isPremium").boolValue = premium;                          // PDF §2.3: two premium items per category
            Prop(so, "price").intValue = premium ? 50 : 0;                      // PDF §1.3 Harcama: "Fiyat 50 adet"; normal: not in the PDF (OPEN #6)
            Prop(so, "currency").intValue = (int)(premium ? CosmeticCurrency.PurpleGem : CosmeticCurrency.Cash); // PDF §2.3: premium paid with Purple Gems
            Prop(so, "sortOrder").intValue = premium ? set - LastNormalSet : set; // place inside its group, from the set number
            Apply(so);
            return definition;
        }

        /// <summary>The item's own name among its parts: the shortest (GL03 of GL03 / GL03SMLEL / GL03SMLER), ties by name.</summary>
        private static string MainPart(List<string> parts)
        {
            string main = parts[0];
            foreach (string part in parts)
                if (part.Length < main.Length) main = part;
            return main;
        }

        /// <summary>
        /// Set number of a wardrobe object, same rule as SpeedStars.Runner.AccessoryPhotoStudio.SetFor (Serhat): the
        /// number in the name is the set, a leading P moves it to the premium sets 8-9, GLP01 is the first premium
        /// glove (unlike PGLP02), extra pieces (GL03SMLEL, PS02SMLE) carry their item's number; 0 = not a set.
        /// Copied rather than called so a change in Serhat's runtime code cannot break this editor assembly; it is
        /// used once, here, and the catalog stores the explicit part lists.
        /// </summary>
        private static int SetFor(string slot, string itemName)
        {
            if (slot == nameof(CosmeticCategory.Gloves) && itemName == "GLP01") return LastNormalSet + 1;
            Match match = Regex.Match(itemName, @"\d+");
            if (!match.Success || !int.TryParse(match.Value, out int number)) return 0;
            bool premium = itemName.StartsWith("P", StringComparison.OrdinalIgnoreCase);
            if (premium) return number >= 1 && number <= LastPremiumSet - LastNormalSet ? LastNormalSet + number : 0;
            return number >= 1 && number <= LastNormalSet ? number : 0;
        }
    }
}
