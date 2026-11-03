using System.Collections.Generic;
using SpeedStars.Meta.Emotes;
using SpeedStars.UI.Emotes;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Fill of the emote assets: Data/Config/EmoteCatalog.asset (v2, T-089: "None" plus Malik's nine baked animations) and
    /// Data/Config/EmoteScreenConfig.asset (feedback numbers: field defaults). Each entry names a <c>RunnerEmoteData</c> asset of
    /// Animations/Baked; the display names are our own, short and generic (the files carry song and move names). The nine come in
    /// order of length, short and plain first, long and showy last, because price follows the order: the first three cost the
    /// first tier, the next three the second, the last three the third (<see cref="ConfigAssetSeeder.EmotePrice"/>). "None" is
    /// free and is what a new player (and an old save naming a procedural emote of v1) has. An existing asset belongs to the
    /// user and is only rewritten when <c>overwrite</c> is asked for; <see cref="ApplyEmoteV2"/> is that migration.
    /// </summary>
    public static class EmoteCatalogSeed
    {
        private const string Source = nameof(EmoteCatalogSeed);

        public const string CatalogPath = ConfigAssetSeeder.Folder + "/EmoteCatalog.asset";
        public const string ScreenConfigPath = ConfigAssetSeeder.Folder + "/EmoteScreenConfig.asset";

        public const string NoneId = "none";

        private readonly struct EmoteSeed
        {
            public readonly string Id, Name, Clip;

            public EmoteSeed(string id, string name, string clip)
            {
                Id = id;
                Name = name;
                Clip = clip;
            }
        }

        // Clip = name of the RunnerEmoteData asset. Lengths at 30 fps, from the frame counts: about 1.6, 4.5, 4.9, 5.4, 7.1, 8.5,
        // 10.2, 16.8 and 21.7 seconds.
        private static readonly EmoteSeed[] Seeds =
        {
            new EmoteSeed(NoneId, "None", string.Empty),
            new EmoteSeed("hip_wave", "Hip Wave", "Wave Hip Hop Dance"),
            new EmoteSeed("taunt", "Taunt", "Taunt"),
            new EmoteSeed("silly_dance", "Silly Dance", "Silly Dancing"),
            new EmoteSeed("surprise", "Surprise", "Surprised"),
            new EmoteSeed("hip_hop", "Hip Hop", "Hip Hop Dancing"),
            new EmoteSeed("air_guitar", "Air Guitar", "Guitar Playing"),
            new EmoteSeed("shuffle", "Shuffle", "Shuffling"),
            new EmoteSeed("horse_trot", "Horse Trot", "Gangnam Style"),
            new EmoteSeed("cart_dance", "Cart Dance", "Shopping Cart Dance"),
        };

        /// <summary>The ids in list order (for tests and tools).</summary>
        public static IReadOnlyList<string> Ids()
        {
            var ids = new List<string>(Seeds.Length);
            foreach (EmoteSeed seed in Seeds) ids.Add(seed.Id);
            return ids;
        }

        /// <summary>
        /// Rewrites the existing emote catalog with the v2 set (None + nine baked animations, the price tiers). Parameterless so
        /// the supervisor can run it with -executeMethod; safe to run again.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Build/Apply emote v2 (None + 9 baked animations)")]
        public static void ApplyEmoteV2()
        {
            EnsureFolder(ConfigAssetSeeder.Folder);
            Seed(overwrite: true);
            AssetDatabase.SaveAssets();
        }

        public static EmoteCatalog Seed(bool overwrite)
        {
            var catalog = CreateOrReset<EmoteCatalog>(CatalogPath, overwrite);
            if (catalog == null) { Skipped(Source, CatalogPath); return AssetDatabase.LoadAssetAtPath<EmoteCatalog>(CatalogPath); }

            var so = new SerializedObject(catalog);
            SerializedProperty list = Prop(so, "emotes");
            list.arraySize = Seeds.Length;
            int paid = 0;
            for (int i = 0; i < Seeds.Length; i++)
            {
                EmoteSeed seed = Seeds[i];
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                Rel(entry, "emoteId").stringValue = seed.Id;
                Rel(entry, "displayName").stringValue = seed.Name;
                Rel(entry, "clipName").stringValue = seed.Clip;
                bool isNone = seed.Clip.Length == 0;
                Rel(entry, "cashPrice").intValue = isNone ? 0 : ConfigAssetSeeder.EmotePrice(paid++);
            }
            Prop(so, "defaultEmoteId").stringValue = NoneId;
            Apply(so);
            Log(Source, $"Seeded {CatalogPath}: None + {paid} baked emotes.");
            return catalog;
        }

        /// <summary>The screen's feedback numbers are the field defaults; the asset only has to exist.</summary>
        public static EmoteScreenConfig SeedScreenConfig(bool overwrite)
        {
            var asset = CreateOrReset<EmoteScreenConfig>(ScreenConfigPath, overwrite);
            if (asset == null) { Skipped(Source, ScreenConfigPath); return AssetDatabase.LoadAssetAtPath<EmoteScreenConfig>(ScreenConfigPath); }
            Log(Source, $"Created {ScreenConfigPath} with the field defaults.");
            return asset;
        }
    }
}
