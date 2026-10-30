using System.Collections.Generic;
using System.Linq;
using SpeedStars.Meta.Config;
using SpeedStars.UI.Framework;
using UnityEditor;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// First fill of the config assets under Data/Config (contract §6). The numbers come from the PDF
    /// (docs/gdd/Speed_Stars_V1.0_Turkce.pdf §1.1-1.3, §2.4); each one carries its PDF item next to it. A value the
    /// PDF does not give is not invented: it stays 0 / off / empty (OPEN-2026-10-28). This is a seed only: an
    /// asset that exists is the user's and is never touched unless <c>overwrite</c> is asked for; at runtime the
    /// game reads the assets, not this file. A catalog and its entries are one unit: an existing catalog keeps
    /// its list and its entry assets as they are.
    /// </summary>
    public static class ConfigAssetSeeder
    {
        private const string Source = nameof(ConfigAssetSeeder);

        public const string Folder = "Assets/Speed-Staes/Data/Config";
        public const string EventsFolder = Folder + "/Events";
        public const string CosmeticsFolder = Folder + "/Cosmetics";

        public const string EconomyPath = Folder + "/EconomyConfig.asset";
        public const string GemRewardPath = Folder + "/GemRewardConfig.asset";
        public const string SevenDayRewardPath = Folder + "/SevenDayRewardConfig.asset";
        public const string RaceEventCatalogPath = Folder + "/RaceEventCatalog.asset";
        public const string CosmeticCatalogPath = Folder + "/CosmeticCatalog.asset";
        public const string UiMotionPath = Folder + "/UiMotionConfig.asset";
        public const string ThemeCatalogPath = Folder + "/ThemeCatalog.asset";
        public const string SettingsPath = Folder + "/SettingsConfig.asset";
        public const string AnalysisPath = Folder + "/AnalysisConfig.asset";

        /// <summary>
        /// Events in display order (PDF §1.3, MD §12). Distance only where the name states it; Training, Race of the
        /// Day and Medley have none in the documents, so 0. Purple Gems: PDF §1.3 ("yalnizca 400m, 60mH, Medley").
        /// Ticket: only 100m, the slice assumption (OPEN #3: which events were paid in the original is unverified).
        /// Playable: only 100m, the one race the runner mechanic has (OPEN #9: hurdles, relay, Medley rules).
        /// </summary>
        private static readonly EventSeed[] Events =
        {
            new EventSeed("training", "Antrenman Alanı", 0f, ticket: false, gems: false, playable: false),
            new EventSeed("race_of_the_day", "Race of the Day", 0f, ticket: false, gems: false, playable: false),
            new EventSeed("100m", "100m", 100f, ticket: true, gems: false, playable: true),
            new EventSeed("200m", "200m", 200f, ticket: false, gems: false, playable: false),
            new EventSeed("400m", "400m", 400f, ticket: false, gems: true, playable: false),
            new EventSeed("60mh", "60mH", 60f, ticket: false, gems: true, playable: false),
            new EventSeed("110mh", "110mH", 110f, ticket: false, gems: false, playable: false),
            new EventSeed("400mh", "400mH", 400f, ticket: false, gems: false, playable: false),
            new EventSeed("4x100m", "4×100m", 400f, ticket: false, gems: false, playable: false), // relay: 4 legs x 100 m, from the name
            new EventSeed("medley", "Medley", 0f, ticket: false, gems: true, playable: false),
        };

        public static void SeedAll(bool overwrite)
        {
            EnsureFolder(Folder);
            SeedEconomy(overwrite);
            SeedGemRewards(overwrite);
            CosmeticCatalog cosmetics = CosmeticCatalogSeed.Seed(CosmeticCatalogPath, CosmeticsFolder, overwrite);
            SeedSevenDay(overwrite, cosmetics);
            SeedRaceEvents(overwrite);
            SeedUiMotion(overwrite);
            EventSelectConfigSeed.Seed(overwrite);
            ThemeCatalogSeed.Seed(ThemeCatalogPath, overwrite);
            SettingsConfigSeed.Seed(SettingsPath, overwrite);
            AnalysisConfigSeed.Seed(AnalysisPath, overwrite);
            AssetDatabase.SaveAssets();
        }

        private static void SeedEconomy(bool overwrite)
        {
            var asset = CreateOrReset<EconomyConfig>(EconomyPath, overwrite);
            if (asset == null) { Skipped(Source, EconomyPath); return; }

            var so = new SerializedObject(asset);
            Prop(so, "entryTicketCap").intValue = 3;                // PDF §1, §1.2 Kurallar: "en fazla 3 adet tutulabilir"
            Prop(so, "entryTicketRegenMinutes").floatValue = 30f;   // PDF §1, §1.2 Edinme: "her 30 dakikada 1 adet yenilenir"
            Prop(so, "dailyGiftEntryTickets").intValue = 1;         // PDF §1.2 Edinme, §2.1: "her gun ilk giriste 1 adet"
            Prop(so, "timedAccessMinutes").floatValue = 60f;        // PDF §1, §1.1: "Sure Aktif ... 1 saat surer"

            // Not in the PDF -> not invented (contract §6): new saves start empty, ticket prices closed (OPEN #4),
            // no rewarded ad (OPEN #11: no ad SDK in the project).
            Prop(so, "startingCash").intValue = 0;
            Prop(so, "startingPurpleGems").intValue = 0;
            // Not in the PDF either; decided 28.10: a new player starts with a full inventory (= entryTicketCap), otherwise
            // the very first race waits for a regen.
            Prop(so, "startingEntryTickets").intValue = so.FindProperty("entryTicketCap").intValue;
            Prop(so, "startingTimedTickets").intValue = 0;
            Prop(so, "entryTicketCashPrice").intValue = 0;
            Prop(so, "timedTicketCashPrice").intValue = 0;
            Prop(so, "rewardedAdEnabled").boolValue = false;
            Prop(so, "rewardedAdEntryTickets").intValue = 0;
            Prop(so, "dayBoundary").intValue = (int)DayBoundaryPolicy.LocalMidnight; // OPEN #2 working assumption
            Apply(so);
            Log(Source, $"Seeded {EconomyPath}.");
        }

        private static void SeedGemRewards(bool overwrite)
        {
            var asset = CreateOrReset<GemRewardConfig>(GemRewardPath, overwrite);
            if (asset == null) { Skipped(Source, GemRewardPath); return; }

            var so = new SerializedObject(asset);
            var byRank = Prop(so, "purpleGemsByRank");
            byRank.arraySize = 3;                             // PDF §1.3 Kazanim: only 1st / 2nd / 3rd win; 4th and later get 0
            byRank.GetArrayElementAtIndex(0).intValue = 3;    // PDF §1.3: 1st place, 3 Purple Gems
            byRank.GetArrayElementAtIndex(1).intValue = 2;    // PDF §1.3: 2nd place, 2
            byRank.GetArrayElementAtIndex(2).intValue = 1;    // PDF §1.3: 3rd place, 1
            Apply(so);
            Log(Source, $"Seeded {GemRewardPath}.");
        }

        private static void SeedSevenDay(bool overwrite, CosmeticCatalog cosmetics)
        {
            var asset = CreateOrReset<SevenDayRewardConfig>(SevenDayRewardPath, overwrite);
            if (asset == null) { Skipped(Source, SevenDayRewardPath); return; }

            // Day 7 (PDF §2.4): "a premium cosmetic, for now a head cosmetic". Which one is open (OPEN #6); working
            // assumption: the first premium Headwear in the catalog's own list order.
            CosmeticDefinition daySevenItem = cosmetics != null
                ? cosmetics.GetByCategory(CosmeticCategory.Headwear).FirstOrDefault(c => c.IsPremium)
                : null;
            if (daySevenItem == null)
                Warn(Source, "No premium Headwear in the cosmetic catalog; day 7 is left without an item (it gives nothing until one is set).");

            var so = new SerializedObject(asset);
            var days = Prop(so, "days");
            days.arraySize = SevenDayRewardConfig.DayCount;
            SetDay(days, 1, SevenDayRewardKind.Cash, 100, null);          // PDF §2.4: 1. Gun 100 Nakit
            SetDay(days, 2, SevenDayRewardKind.Cash, 200, null);          // PDF §2.4: 2. Gun 200 Nakit
            SetDay(days, 3, SevenDayRewardKind.TimedTicket, 1, null);     // PDF §2.4: 3. Gun 1 Sureli Bilet
            SetDay(days, 4, SevenDayRewardKind.Cash, 200, null);          // PDF §2.4: 4. Gun 200 Nakit
            SetDay(days, 5, SevenDayRewardKind.TimedTicket, 1, null);     // PDF §2.4: 5. Gun 1 Sureli Bilet
            SetDay(days, 6, SevenDayRewardKind.None, 0, null);            // PDF §2.4 has no day 6: empty day (OPEN #1)
            SetDay(days, 7, SevenDayRewardKind.Cosmetic, 0, daySevenItem); // PDF §2.4: 7. Gun premium kafa kozmetigi (OPEN #6)
            Prop(so, "cyclePolicy").intValue = (int)SevenDayCyclePolicy.RestartFromDayOne; // OPEN #2 working assumption
            Apply(so);
            Log(Source, $"Seeded {SevenDayRewardPath} (day 7: {(daySevenItem != null ? daySevenItem.Id : "no item")}).");
        }

        private static void SetDay(SerializedProperty days, int dayNumber, SevenDayRewardKind kind, int amount, CosmeticDefinition cosmetic)
        {
            var day = days.GetArrayElementAtIndex(dayNumber - 1);
            Rel(day, "kind").intValue = (int)kind;
            Rel(day, "amount").intValue = amount;
            Rel(day, "cosmetic").objectReferenceValue = cosmetic;
        }

        private static void SeedRaceEvents(bool overwrite)
        {
            var catalog = CreateOrReset<RaceEventCatalog>(RaceEventCatalogPath, overwrite);
            if (catalog == null) { Skipped(Source, RaceEventCatalogPath); return; }

            EnsureFolder(EventsFolder);
            var definitions = new List<UnityEngine.Object>();
            foreach (var seed in Events)
            {
                string path = $"{EventsFolder}/{seed.Id}.asset";
                var definition = CreateOrReset<RaceEventDefinition>(path, overwrite);
                if (definition == null)
                {
                    // Kept as the user left it, still listed in the new catalog.
                    Skipped(Source, path);
                    definitions.Add(LoadRequired<RaceEventDefinition>(path, Source));
                    continue;
                }

                var so = new SerializedObject(definition);
                Prop(so, "eventId").stringValue = seed.Id;
                Prop(so, "displayName").stringValue = seed.DisplayName;
                Prop(so, "distanceMeters").floatValue = seed.DistanceMeters;
                Prop(so, "requiresTicket").boolValue = seed.RequiresTicket;
                Prop(so, "awardsPurpleGems").boolValue = seed.AwardsPurpleGems;
                Prop(so, "isPlayable").boolValue = seed.IsPlayable;
                Apply(so);
                definitions.Add(definition);
            }

            var catalogSo = new SerializedObject(catalog);
            SetObjectList(Prop(catalogSo, "events"), definitions.ToArray());
            Apply(catalogSo);
            Log(Source, $"Seeded {RaceEventCatalogPath} with {definitions.Count} events.");
        }

        private static void SeedUiMotion(bool overwrite)
        {
            // The field defaults are the kit's approved motion values (agent-kit/05 §1); nothing to write over them.
            var asset = CreateOrReset<UiMotionConfig>(UiMotionPath, overwrite);
            if (asset == null) { Skipped(Source, UiMotionPath); return; }
            Log(Source, $"Created {UiMotionPath} with the field defaults.");
        }

        private readonly struct EventSeed
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly float DistanceMeters;
            public readonly bool RequiresTicket;
            public readonly bool AwardsPurpleGems;
            public readonly bool IsPlayable;

            public EventSeed(string id, string displayName, float distanceMeters, bool ticket, bool gems, bool playable)
            {
                Id = id;
                DisplayName = displayName;
                DistanceMeters = distanceMeters;
                RequiresTicket = ticket;
                AwardsPurpleGems = gems;
                IsPlayable = playable;
            }
        }
    }
}
