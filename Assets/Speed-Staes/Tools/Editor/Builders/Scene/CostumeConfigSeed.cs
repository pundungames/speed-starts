using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Home;
using UnityEditor;
using UnityEngine;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of the costume editor config asset (T-026): which cosmetic category sits behind each of the nine tabs, and
    /// the picture of every catalog item. The picture of an item is the PNG named like the FIRST part of the item
    /// (<c>Art/Costume/Items/&lt;Category&gt;/&lt;PartName&gt;.png</c>, Serhat's wardrobe renders); it is read here once, so the
    /// game never parses part names. An existing asset belongs to the user: it is not touched except that items missing
    /// from its picture list are appended (a catalog that grew), unless <c>overwrite</c> is asked.
    /// </summary>
    public static class CostumeConfigSeed
    {
        private const string Source = nameof(CostumeConfigSeed);

        public const string Path = ConfigAssetSeeder.Folder + "/CostumeEditorConfig.asset";

        /// <summary>Tab -> category and whether it has content. Body (skin) and Emote list no category (that slot is unused); the
        /// screen gives them their behaviour in code (<see cref="CostumeTabRules"/>), so an older asset without these entries works too.</summary>
        private static readonly (CostumeTab tab, CosmeticCategory category, bool hasContent)[] SeedTabs =
        {
            (CostumeTab.Face, CosmeticCategory.Glasses, true),
            (CostumeTab.Head, CosmeticCategory.Headwear, true),
            (CostumeTab.Top, CosmeticCategory.Tops, true),
            (CostumeTab.Shorts, CosmeticCategory.Shorts, true),
            (CostumeTab.Leggings, CosmeticCategory.Leggings, true),
            (CostumeTab.Hands, CosmeticCategory.Gloves, true),
            (CostumeTab.Shoes, CosmeticCategory.Shoes, true),
            (CostumeTab.Socks, CosmeticCategory.Socks, true),
            (CostumeTab.Emote, CosmeticCategory.Headwear, true),
            (CostumeTab.Body, CosmeticCategory.Tops, true),
        };

        /// <returns>The asset (new, reset or the existing one with its picture list completed).</returns>
        public static CostumeEditorConfig Seed(bool overwrite)
        {
            CostumeEditorConfig asset = CreateOrReset<CostumeEditorConfig>(Path, overwrite);
            bool fresh = asset != null;
            if (!fresh)
            {
                asset = LoadRequired<CostumeEditorConfig>(Path, Source);
                Skipped(Source, Path);
            }

            var so = new SerializedObject(asset);
            if (fresh)
            {
                SerializedProperty tabs = Prop(so, "tabs");
                tabs.arraySize = SeedTabs.Length;
                for (int i = 0; i < SeedTabs.Length; i++)
                {
                    SerializedProperty entry = tabs.GetArrayElementAtIndex(i);
                    Rel(entry, "tab").intValue = (int)SeedTabs[i].tab;
                    Rel(entry, "category").intValue = (int)SeedTabs[i].category;
                    Rel(entry, "hasContent").boolValue = SeedTabs[i].hasContent;
                }
                Prop(so, "heightText").stringValue = HomeTexts.RacerHeight; // placeholder: the same text as the Home card
            }

            int added = FillItemIcons(Prop(so, "itemIcons"));
            Apply(so);
            Log(Source, $"{(fresh ? "Seeded" : "Completed")} {Path}: {added} item picture(s) added.");
            return asset;
        }

        private static int FillItemIcons(SerializedProperty list)
        {
            var catalog = LoadRequired<CosmeticCatalog>(ConfigAssetSeeder.CosmeticCatalogPath, nameof(ConfigAssetSeeder));
            var known = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < list.arraySize; i++)
                known[Rel(list.GetArrayElementAtIndex(i), "cosmeticId").stringValue] = i;

            int added = 0;
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                foreach (CosmeticDefinition definition in catalog.GetByCategory(category))
                {
                    if (definition.PartNames.Count == 0)
                    {
                        Warn(Source, $"{definition.Id}: no part names; it gets no picture.");
                        continue;
                    }

                    string spritePath = $"{UiCostumePrefabs.ItemsFolder}/{category}/{definition.PartNames[0]}.png";
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite == null)
                    {
                        Warn(Source, $"{definition.Id}: picture '{spritePath}' not found or not imported as a Sprite; the card shows no picture.");
                        continue;
                    }

                    if (known.TryGetValue(definition.Id, out int index))
                    {
                        SerializedProperty icon = Rel(list.GetArrayElementAtIndex(index), "icon");
                        if (icon.objectReferenceValue == null) icon.objectReferenceValue = sprite;
                        continue;
                    }

                    list.arraySize++;
                    SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
                    Rel(element, "cosmeticId").stringValue = definition.Id;
                    Rel(element, "icon").objectReferenceValue = sprite;
                    known[definition.Id] = list.arraySize - 1;
                    added++;
                }
            }
            return added;
        }
    }
}
