using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
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

        // Catalog-level seed values (they live in the asset afterwards; edit them there). Preset count is not in the PDF.
        private const int SeedPresetCount = 9;
        private const string SeedPresetNameFormat = "Custom {0}";
        // Skin: ColorSkin_Toon's _Color in the runner prefab (0.2736, 0.1695, 0.1510) is 462B27; the tones run light to dark.
        private const string SeedDefaultSkinColor = "462B27";
        private const string SeedSkinRendererName = "LowPoly Male";
        private const string SeedSkinMaterialName = "ColorSkin_Toon";
        private static readonly string[] SeedSkinTones = { "FFDFC4", "F1C27D", "E0AC69", "C68642", "A9693D", "8D5524", "6B4226", "462B27" };
        private static readonly string[] SeedLegacyObjects = { "Tshirt", "Short", "Leggings", "Shoes", "Glove", "Helmet" };
        private static readonly string[] SeedPalette = { "E53935", "FB8C00", "FDD835", "43A047", "1E88E5", "8E24AA", "F5F5F5", "212121" };

        // Default outfit as in the Home design (UI/Art/Home/preview.jpg): red cap, dark glasses, light tank top, red
        // shorts, black leggings, shoes; no gloves or socks. Normal items only, so it is owned from the first launch.
        private static readonly (CosmeticCategory category, string id, string hex)[] SeedDefaultOutfit =
        {
            (CosmeticCategory.Headwear, "headwear.h01", "D01818"),
            (CosmeticCategory.Glasses, "glasses.g01", ""),
            (CosmeticCategory.Tops, "tops.t01", "E8E4DA"),
            (CosmeticCategory.Shorts, "shorts.s01", "CC1010"),
            (CosmeticCategory.Leggings, "leggings.l01", "1A1A1A"),
            (CosmeticCategory.Shoes, "shoes.sh01", ""),
        };

        /// <summary>The catalog to read day 7 from: the seeded one, or the existing one when it is left as it is.</summary>
        public static CosmeticCatalog Seed(string catalogPath, string folder, bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(catalogPath);
            if (existing != null && !overwrite)
            {
                Skipped(Source, catalogPath);
                BackfillChannels(existing); // items seeded before they had several channels get theirs; nothing else is touched
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
                    items.Add(SeedItem(folder, category, slot, set, parts, slotRoot, overwrite));
                }
            }

            var so = new SerializedObject(catalog);
            SetObjectList(Prop(so, "cosmetics"), items.ToArray());
            SeedCatalogSettings(so, items);
            Apply(so);
            Log(Source, $"Seeded {catalogPath} with {items.Count} cosmetics.");
            return catalog;
        }

        private static void SeedCatalogSettings(SerializedObject so, List<UnityEngine.Object> items)
        {
            Prop(so, "presetCount").intValue = SeedPresetCount;
            Prop(so, "presetNameFormat").stringValue = SeedPresetNameFormat;
            SetStrings(Prop(so, "colorPalette"), SeedPalette);
            SetStrings(Prop(so, "legacyOutfitObjects"), SeedLegacyObjects);
            Prop(so, "defaultSkinColor").stringValue = SeedDefaultSkinColor;
            Prop(so, "skinRendererName").stringValue = SeedSkinRendererName;
            Prop(so, "skinMaterialName").stringValue = SeedSkinMaterialName;
            SetStrings(Prop(so, "skinTones"), SeedSkinTones);

            var ids = new HashSet<string>();
            foreach (UnityEngine.Object item in items)
                if (item is CosmeticDefinition definition) ids.Add(definition.Id);

            var entries = new List<(CosmeticCategory category, string id, string hex)>();
            foreach (var entry in SeedDefaultOutfit)
            {
                if (ids.Contains(entry.id)) entries.Add(entry);
                else Warn(Source, $"Default outfit item '{entry.id}' is not in the catalog; skipped.");
            }
            SerializedProperty list = Prop(so, "defaultOutfit");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                Rel(element, "category").intValue = (int)entries[i].category;
                Rel(element, "cosmeticId").stringValue = entries[i].id;
                Rel(element, "colorHex").stringValue = entries[i].hex;
            }
        }

        private static void SetStrings(SerializedProperty list, string[] values)
        {
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        /// <summary>
        /// The colour channels of an item in order of first appearance, walking the parts and their material slots (material
        /// names Color1_Toon / Color2_Toon / Color3_Toon): the first is the primary channel, each is listed once, with the
        /// "RRGGBB" its material has (the shared toon material's own colour, which the item shows until the player picks one).
        /// Items without a toon colour material get Color1 and no default colour.
        /// </summary>
        private static List<(CosmeticColorChannel channel, string hex)> ReadChannels(Transform slotRoot, IReadOnlyList<string> parts, string itemName)
        {
            var found = new List<(CosmeticColorChannel channel, string hex)>();
            foreach (string part in parts)
            {
                Transform child = slotRoot.Find(part);
                if (child == null) continue;
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null) continue;
                        foreach (CosmeticColorChannel channel in Enum.GetValues(typeof(CosmeticColorChannel)))
                        {
                            if (!material.name.StartsWith(channel.MaterialName(), StringComparison.Ordinal)) continue;
                            if (found.Exists(f => f.channel == channel)) break;
                            string hex = material.HasProperty("_Color") ? ColorUtility.ToHtmlStringRGB(material.GetColor("_Color")) : string.Empty;
                            found.Add((channel, hex));
                            break;
                        }
                    }
            }
            if (found.Count > CosmeticSlot.MaxChannels) found.RemoveRange(CosmeticSlot.MaxChannels, found.Count - CosmeticSlot.MaxChannels);
            if (found.Count == 0)
            {
                Warn(Source, $"{itemName}: no Color1-3_Toon material found; primary channel left at {CosmeticColorChannel.Color1}.");
                found.Add((CosmeticColorChannel.Color1, string.Empty));
            }
            return found;
        }

        /// <summary>Writes primaryChannel, channels and channelDefaultColors of one definition from the prefab's materials.</summary>
        private static void WriteChannels(SerializedObject so, Transform slotRoot, IReadOnlyList<string> parts, string itemName)
        {
            var found = ReadChannels(slotRoot, parts, itemName);
            Prop(so, "primaryChannel").intValue = (int)found[0].channel;
            SerializedProperty channels = Prop(so, "channels");
            SerializedProperty defaults = Prop(so, "channelDefaultColors");
            channels.arraySize = found.Count;
            defaults.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                channels.GetArrayElementAtIndex(i).intValue = (int)found[i].channel;
                defaults.GetArrayElementAtIndex(i).stringValue = found[i].hex;
            }
        }

        /// <summary>
        /// Fills the channel lists of the existing items of a catalog that has none yet (assets seeded before items had
        /// several channels). Items that already list channels are left as they are; the prefab is only read.
        /// </summary>
        private static void BackfillChannels(CosmeticCatalog catalog)
        {
            var prefab = LoadRequired<GameObject>(CharacterPrefabPath, "the runner prefab (Malik)");
            Transform accessories = prefab.transform.Find(AccessoriesName);
            if (accessories == null) return;

            var catalogSo = new SerializedObject(catalog);
            SerializedProperty list = Prop(catalogSo, "cosmetics");
            int filled = 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                var definition = list.GetArrayElementAtIndex(i).objectReferenceValue as CosmeticDefinition;
                if (definition == null) continue;
                var so = new SerializedObject(definition);
                if (Prop(so, "channels").arraySize > 0) continue;
                Transform slotRoot = accessories.Find(definition.Category.ToString());
                if (slotRoot == null) continue;
                WriteChannels(so, slotRoot, definition.PartNames, definition.Id);
                Apply(so);
                filled++;
            }
            if (filled > 0) Log(Source, $"Filled the colour channels of {filled} cosmetics.");
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

        private static CosmeticDefinition SeedItem(string folder, CosmeticCategory category, string slot, int set, List<string> parts, Transform slotRoot, bool overwrite)
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
            WriteChannels(so, slotRoot, parts, main);
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
