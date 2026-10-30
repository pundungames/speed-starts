using System.Collections.Generic;
using SpeedStars.Meta.Emotes;
using SpeedStars.UI.Emotes;
using UnityEditor;
using UnityEngine;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of the emote assets (T-040): Data/Config/EmoteCatalog.asset with our own six procedural emotes, and
    /// Data/Config/EmoteScreenConfig.asset (feedback numbers: field defaults). The original game's emote list is not known,
    /// so these are a starting set; the poses are authored by eye on the runner rig and are meant to be tuned in the
    /// Inspector. Rotations are runner-space degrees on top of the menu stance (x right, y up, z forward): a right-side limb
    /// goes outward / up with positive z, a left-side limb with negative z; negative x tips a bone forward or leans back.
    /// Prices are PLACEHOLDERS (the PDF has no emotes): the first two are free, the rest cost Cash. An existing asset
    /// belongs to the user and is never touched unless <c>overwrite</c> is asked for.
    /// </summary>
    internal static class EmoteCatalogSeed
    {
        private const string Source = nameof(EmoteCatalogSeed);

        public const string CatalogPath = ConfigAssetSeeder.Folder + "/EmoteCatalog.asset";
        public const string ScreenConfigPath = ConfigAssetSeeder.Folder + "/EmoteScreenConfig.asset";

        private static readonly EmoteBone SR = EmoteBone.ShoulderR, UR = EmoteBone.UpperArmR, FR = EmoteBone.ForearmR, HR = EmoteBone.HandR;
        private static readonly EmoteBone SL = EmoteBone.ShoulderL, UL = EmoteBone.UpperArmL, FL = EmoteBone.ForearmL, HL = EmoteBone.HandL;
        private static readonly EmoteBone Spine = EmoteBone.Spine, Chest = EmoteBone.Chest, Head = EmoteBone.Head;

        private sealed class EmoteSeed
        {
            public string Id, Name;
            public int Price;
            public float Duration, BlendIn, BlendOut;
            public readonly List<(float t, EmoteBone bone, Vector3 e, EmoteEase ease)> Keys = new List<(float, EmoteBone, Vector3, EmoteEase)>();

            public EmoteSeed Key(float t, EmoteBone bone, float x, float y, float z, EmoteEase ease = EmoteEase.Smooth)
            {
                Keys.Add((t, bone, new Vector3(x, y, z), ease));
                return this;
            }
        }

        private static List<EmoteSeed> Seeds()
        {
            return new List<EmoteSeed>
            {
                // Wave: right arm up, forearm swings from side to side.
                new EmoteSeed { Id = "wave", Name = "Wave", Price = 0, Duration = 2.4f, BlendIn = 0.25f, BlendOut = 0.4f }
                    .Key(0f, UR, 0, 0, 0).Key(0.5f, UR, 0, 0, 135).Key(2.0f, UR, 0, 0, 135).Key(2.4f, UR, 0, 0, 0)
                    .Key(0f, FR, 0, 0, 0).Key(0.5f, FR, 0, 0, 15)
                    .Key(0.8f, FR, 0, 0, 45, EmoteEase.Smooth).Key(1.1f, FR, 0, 0, -10).Key(1.4f, FR, 0, 0, 45)
                    .Key(1.7f, FR, 0, 0, -10).Key(2.0f, FR, 0, 0, 15).Key(2.4f, FR, 0, 0, 0)
                    .Key(0f, Head, 0, 0, 0).Key(0.6f, Head, 0, -12, 4).Key(2.0f, Head, 0, -12, 4).Key(2.4f, Head, 0, 0, 0)
                    .Key(0f, SR, 0, 0, 0).Key(0.5f, SR, 0, 0, 8),

                // Hands Up: both arms overhead, a small pump.
                new EmoteSeed { Id = "hands_up", Name = "Hands Up", Price = 0, Duration = 2.2f, BlendIn = 0.2f, BlendOut = 0.4f }
                    .Key(0f, UR, 0, 0, 0).Key(0.45f, UR, 0, 0, 165).Key(0.9f, UR, 0, 0, 150).Key(1.3f, UR, 0, 0, 170)
                    .Key(1.7f, UR, 0, 0, 150).Key(2.2f, UR, 0, 0, 0)
                    .Key(0f, UL, 0, 0, 0).Key(0.45f, UL, 0, 0, -165).Key(0.9f, UL, 0, 0, -150).Key(1.3f, UL, 0, 0, -170)
                    .Key(1.7f, UL, 0, 0, -150).Key(2.2f, UL, 0, 0, 0)
                    .Key(0f, SR, 0, 0, 0).Key(0.45f, SR, 0, 0, 10).Key(0f, SL, 0, 0, 0).Key(0.45f, SL, 0, 0, -10)
                    .Key(0f, Head, 0, 0, 0).Key(0.5f, Head, -16, 0, 0).Key(1.8f, Head, -16, 0, 0).Key(2.2f, Head, 0, 0, 0)
                    .Key(0f, Chest, 0, 0, 0).Key(0.5f, Chest, -8, 0, 0).Key(1.8f, Chest, -8, 0, 0).Key(2.2f, Chest, 0, 0, 0),

                // Flex: both arms out, forearms up, the muscles pulse.
                new EmoteSeed { Id = "flex", Name = "Flex", Price = 200, Duration = 2.4f, BlendIn = 0.3f, BlendOut = 0.4f }
                    .Key(0f, UR, 0, 0, 0).Key(0.5f, UR, 0, 0, 85).Key(2.0f, UR, 0, 0, 85).Key(2.4f, UR, 0, 0, 0)
                    .Key(0f, UL, 0, 0, 0).Key(0.5f, UL, 0, 0, -85).Key(2.0f, UL, 0, 0, -85).Key(2.4f, UL, 0, 0, 0)
                    .Key(0f, FR, 0, 0, 0).Key(0.6f, FR, 0, 0, 110).Key(0.95f, FR, 0, 0, 125).Key(1.3f, FR, 0, 0, 105)
                    .Key(1.65f, FR, 0, 0, 125).Key(2.0f, FR, 0, 0, 110).Key(2.4f, FR, 0, 0, 0)
                    .Key(0f, FL, 0, 0, 0).Key(0.6f, FL, 0, 0, -110).Key(0.95f, FL, 0, 0, -125).Key(1.3f, FL, 0, 0, -105)
                    .Key(1.65f, FL, 0, 0, -125).Key(2.0f, FL, 0, 0, -110).Key(2.4f, FL, 0, 0, 0)
                    .Key(0f, Chest, 0, 0, 0).Key(0.6f, Chest, 6, 0, 0).Key(2.0f, Chest, 6, 0, 0).Key(2.4f, Chest, 0, 0, 0)
                    .Key(0f, Head, 0, 0, 0).Key(0.6f, Head, 4, 0, 0).Key(2.0f, Head, 4, 0, 0).Key(2.4f, Head, 0, 0, 0),

                // Number One: right arm straight up and pumping, left hand on the hip.
                new EmoteSeed { Id = "number_one", Name = "Number One", Price = 300, Duration = 2.2f, BlendIn = 0.25f, BlendOut = 0.4f }
                    .Key(0f, UR, 0, 0, 0).Key(0.45f, UR, 0, 0, 165).Key(0.8f, UR, 0, 0, 150).Key(1.15f, UR, 0, 0, 168)
                    .Key(1.5f, UR, 0, 0, 150).Key(1.85f, UR, 0, 0, 165).Key(2.2f, UR, 0, 0, 0)
                    .Key(0f, UL, 0, 0, 0).Key(0.45f, UL, 0, 0, -28).Key(1.85f, UL, 0, 0, -28).Key(2.2f, UL, 0, 0, 0)
                    .Key(0f, FL, 0, 0, 0).Key(0.45f, FL, 0, 0, 85).Key(1.85f, FL, 0, 0, 85).Key(2.2f, FL, 0, 0, 0)
                    .Key(0f, Head, 0, 0, 0).Key(0.5f, Head, -10, 10, 0).Key(1.85f, Head, -10, 10, 0).Key(2.2f, Head, 0, 0, 0)
                    .Key(0f, Spine, 0, 0, 0).Key(0.5f, Spine, 0, 8, 0).Key(1.85f, Spine, 0, 8, 0).Key(2.2f, Spine, 0, 0, 0),

                // Shrug: shoulders up, forearms out with open hands, head tilts.
                new EmoteSeed { Id = "shrug", Name = "Shrug", Price = 400, Duration = 1.9f, BlendIn = 0.25f, BlendOut = 0.4f }
                    .Key(0f, SR, 0, 0, 0).Key(0.45f, SR, 0, 0, 14).Key(1.4f, SR, 0, 0, 14).Key(1.9f, SR, 0, 0, 0)
                    .Key(0f, SL, 0, 0, 0).Key(0.45f, SL, 0, 0, -14).Key(1.4f, SL, 0, 0, -14).Key(1.9f, SL, 0, 0, 0)
                    .Key(0f, UR, 0, 0, 0).Key(0.45f, UR, 0, 0, 28).Key(1.4f, UR, 0, 0, 28).Key(1.9f, UR, 0, 0, 0)
                    .Key(0f, UL, 0, 0, 0).Key(0.45f, UL, 0, 0, -28).Key(1.4f, UL, 0, 0, -28).Key(1.9f, UL, 0, 0, 0)
                    .Key(0f, FR, 0, 0, 0).Key(0.45f, FR, -35, 0, 50).Key(1.4f, FR, -35, 0, 50).Key(1.9f, FR, 0, 0, 0)
                    .Key(0f, FL, 0, 0, 0).Key(0.45f, FL, -35, 0, -50).Key(1.4f, FL, -35, 0, -50).Key(1.9f, FL, 0, 0, 0)
                    .Key(0f, Head, 0, 0, 0).Key(0.45f, Head, 0, 0, 12).Key(1.4f, Head, 0, 0, 12).Key(1.9f, Head, 0, 0, 0),

                // Salute: right hand to the brow, chin up, held.
                new EmoteSeed { Id = "salute", Name = "Salute", Price = 500, Duration = 2.0f, BlendIn = 0.3f, BlendOut = 0.4f }
                    .Key(0f, UR, 0, 0, 0).Key(0.5f, UR, -25, 0, 100).Key(1.6f, UR, -25, 0, 100).Key(2.0f, UR, 0, 0, 0)
                    .Key(0f, FR, 0, 0, 0).Key(0.5f, FR, 0, 0, 125).Key(1.6f, FR, 0, 0, 125).Key(2.0f, FR, 0, 0, 0)
                    .Key(0f, SR, 0, 0, 0).Key(0.5f, SR, 0, 0, 6)
                    .Key(0f, Head, 0, 0, 0).Key(0.5f, Head, -8, 0, 0).Key(1.6f, Head, -8, 0, 0).Key(2.0f, Head, 0, 0, 0)
                    .Key(0f, Chest, 0, 0, 0).Key(0.5f, Chest, -4, 0, 0).Key(1.6f, Chest, -4, 0, 0).Key(2.0f, Chest, 0, 0, 0),
            };
        }

        public static EmoteCatalog Seed(bool overwrite)
        {
            var catalog = CreateOrReset<EmoteCatalog>(CatalogPath, overwrite);
            if (catalog == null) { Skipped(Source, CatalogPath); return AssetDatabase.LoadAssetAtPath<EmoteCatalog>(CatalogPath); }

            List<EmoteSeed> seeds = Seeds();
            var so = new SerializedObject(catalog);
            SerializedProperty list = Prop(so, "emotes");
            list.arraySize = seeds.Count;
            for (int i = 0; i < seeds.Count; i++)
            {
                EmoteSeed seed = seeds[i];
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                Rel(entry, "emoteId").stringValue = seed.Id;
                Rel(entry, "displayName").stringValue = seed.Name;
                Rel(entry, "cashPrice").intValue = seed.Price; // placeholder: the PDF has no emotes
                Rel(entry, "duration").floatValue = seed.Duration;
                Rel(entry, "blendIn").floatValue = seed.BlendIn;
                Rel(entry, "blendOut").floatValue = seed.BlendOut;

                SerializedProperty keys = Rel(entry, "keys");
                keys.arraySize = seed.Keys.Count;
                for (int k = 0; k < seed.Keys.Count; k++)
                {
                    SerializedProperty key = keys.GetArrayElementAtIndex(k);
                    Rel(key, "time").floatValue = seed.Keys[k].t;
                    Rel(key, "bone").enumValueIndex = (int)seed.Keys[k].bone;
                    Rel(key, "euler").vector3Value = seed.Keys[k].e;
                    Rel(key, "ease").enumValueIndex = (int)seed.Keys[k].ease;
                }
            }
            Prop(so, "defaultEmoteId").stringValue = seeds[0].Id;
            Apply(so);
            Log(Source, $"Seeded {CatalogPath} with {seeds.Count} emotes (placeholder prices).");
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
