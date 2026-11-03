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
        public const string LeaderboardPath = Folder + "/LeaderboardConfig.asset";
        public const string AudioPath = Folder + "/AudioConfig.asset";
        public const string AdsDevPath = Folder + "/AdsDevConfig.asset";

        /// <summary>
        /// Events in display order (PDF §1.3, MD §12). Distance only where the name states it; Training, Race of the
        /// Day and Medley have none in the documents, so 0. Purple Gems: PDF §1.3 ("yalnizca 400m, 60mH, Medley").
        /// Ticket: only 100m, the slice assumption (OPEN #3: which events were paid in the original is unverified).
        /// Playable: 100m and 110mH, the races the runner mechanic has (OPEN #9: relay, Medley rules). Hurdle layouts are
        /// the athletics standard; only 110mH is checked against the reference videos and the bots' times.
        /// </summary>
        private static readonly EventSeed[] Events =
        {
            new EventSeed("training", "Antrenman Alanı", 100f, ticket: false, gems: false, playable: true),
            new EventSeed("race_of_the_day", "Race of the Day", 0f, ticket: false, gems: false, playable: false),
            new EventSeed("100m", "100m", 100f, ticket: true, gems: false, playable: true),
            new EventSeed("200m", "200m", 200f, ticket: false, gems: false, playable: false),
            new EventSeed("400m", "400m", 400f, ticket: false, gems: true, playable: false),
            new EventSeed("60mh", "60mH", 60f, ticket: false, gems: true, playable: false, hurdles: 5, firstHurdle: 13.72f, hurdleSpacing: 9.14f),
            new EventSeed("110mh", "110mH", 110f, ticket: false, gems: false, playable: true, hurdles: 10, firstHurdle: 13.72f, hurdleSpacing: 9.14f),
            new EventSeed("400mh", "400mH", 400f, ticket: false, gems: false, playable: false, hurdles: 10, firstHurdle: 45f, hurdleSpacing: 35f),
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
            LeaderboardConfigSeed.Seed(LeaderboardPath, overwrite);
            RaceStartConfigSeed.Seed(overwrite); // T-039: READY / SET timing + start motion
            EmoteCatalogSeed.Seed(overwrite); // T-040
            BotRaceConfigSeed.Seed(overwrite); // T-074: opponent bots (field, difficulty, press driver, names)
            AudioConfigSeed.Seed(AudioPath, overwrite); // T-052: sound cues (placeholder WAVs), music slot, haptic numbers
            SeedAdsDev(overwrite); // T-050: fake rewarded ad (dev only)
            ApplyEconomyV2();
            AssetDatabase.SaveAssets();
        }

        // ---- Economy v2 (T-084; docs/analiz/2026-11-03-referans-oyun-ekonomi.md section 4, user decision 03.11) ----
        private const int EntryTicketCashPrice = 100;
        private const int TimedTicketCashPrice = 1000;
        private const int RaceFinishCash = 10;
        private const int RaceDifficultyBonusCash = 10;
        private const int RaceOpponentBonusCash = 10;   // paid in full when every opponent is passed
        private const int RewardedAdCash = 200;
        private const int RewardedAdCashDailyLimit = 3;
        private const int HeadwearCashPrice = 1000;      // normal Headwear
        private const int OtherCosmeticCashPrice = 500;  // normal Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes
        private static readonly int[] PaidEmotePrices = { 2500, 2500, 5000, 7500 }; // non-free emotes in list order; the last repeats

        private static void WriteEconomyV2(SerializedObject economy)
        {
            Prop(economy, "entryTicketCashPrice").intValue = EntryTicketCashPrice;
            Prop(economy, "timedTicketCashPrice").intValue = TimedTicketCashPrice;
            Prop(economy, "raceFinishCash").intValue = RaceFinishCash;
            Prop(economy, "raceDifficultyBonusCash").intValue = RaceDifficultyBonusCash;
            Prop(economy, "raceOpponentBonusCash").intValue = RaceOpponentBonusCash;
            Prop(economy, "rewardedAdCash").intValue = RewardedAdCash;
            Prop(economy, "rewardedAdCashDailyLimit").intValue = RewardedAdCashDailyLimit;
        }

        /// <summary>
        /// Writes the v2 prices into the EXISTING economy, cosmetic and emote assets (a migration, not a first fill: it
        /// ignores the skip-existing rule on purpose and only touches the price fields, so it is safe to run again).
        /// Cosmetics: premium items keep their Purple Gem price; the default outfit (what every Custom 1..N preset wears,
        /// so the presets never show a locked item) stays free; every other normal item costs Cash, Headwear more.
        /// Emotes: free ones stay free; the paid ones get the price ladder in list order.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Build/Apply economy v2 prices")]
        public static void ApplyEconomyV2()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyPath);
            if (economy != null)
            {
                var so = new SerializedObject(economy);
                WriteEconomyV2(so);
                Apply(so);
            }
            else Warn(Source, $"'{EconomyPath}' is missing; economy v2 numbers not written.");

            var cosmetics = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CosmeticCatalogPath);
            if (cosmetics != null) ApplyCosmeticPrices(cosmetics);
            else Warn(Source, $"'{CosmeticCatalogPath}' is missing; cosmetic prices not written.");

            var emotes = AssetDatabase.LoadAssetAtPath<SpeedStars.Meta.Emotes.EmoteCatalog>(EmoteCatalogSeed.CatalogPath);
            if (emotes != null) ApplyEmotePrices(emotes);
            else Warn(Source, $"'{EmoteCatalogSeed.CatalogPath}' is missing; emote prices not written.");

            AssetDatabase.SaveAssets();
        }

        private static void ApplyCosmeticPrices(CosmeticCatalog catalog)
        {
            var free = new HashSet<string>();
            foreach (CosmeticOutfitEntry entry in catalog.DefaultOutfit) free.Add(entry.CosmeticId);

            int priced = 0;
            foreach (CosmeticCategory category in System.Enum.GetValues(typeof(CosmeticCategory)))
            {
                foreach (CosmeticDefinition item in catalog.GetByCategory(category))
                {
                    if (item.IsPremium) continue; // 50 Purple Gems stays as seeded
                    var so = new SerializedObject(item);
                    int price = free.Contains(item.Id) ? 0 : category == CosmeticCategory.Headwear ? HeadwearCashPrice : OtherCosmeticCashPrice;
                    Prop(so, "price").intValue = price;
                    Prop(so, "currency").enumValueIndex = (int)CosmeticCurrency.Cash;
                    Apply(so);
                    if (price > 0) priced++;
                }
            }
            Log(Source, $"Cosmetic prices written: {priced} normal items cost Cash; {free.Count} default-outfit items stay free.");
        }

        private static void ApplyEmotePrices(SpeedStars.Meta.Emotes.EmoteCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            SerializedProperty list = Prop(so, "emotes");
            int paid = 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty price = Rel(list.GetArrayElementAtIndex(i), "cashPrice");
                if (price.intValue <= 0) continue; // free stays free
                price.intValue = PaidEmotePrices[System.Math.Min(paid, PaidEmotePrices.Length - 1)];
                paid++;
            }
            Apply(so);
            Log(Source, $"Emote prices written: {paid} paid emotes.");
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
            // rewarded ad: 1 ad = 1 Entry Ticket (PDF 1.2; supervisor decision 30.10, the SDK is still OPEN #11).
            Prop(so, "startingCash").intValue = 0;
            Prop(so, "startingPurpleGems").intValue = 0;
            // Not in the PDF either; decided 28.10: a new player starts with a full inventory (= entryTicketCap), otherwise
            // the very first race waits for a regen.
            Prop(so, "startingEntryTickets").intValue = so.FindProperty("entryTicketCap").intValue;
            Prop(so, "startingTimedTickets").intValue = 0;
            WriteEconomyV2(so);
            Prop(so, "rewardedAdEnabled").boolValue = true;
            Prop(so, "rewardedAdEntryTickets").intValue = 1;
            Apply(so);
            Log(Source, $"Seeded {EconomyPath}.");
        }

        private static void SeedAdsDev(bool overwrite)
        {
            // The field defaults (about a second, Completed) are the seed; nothing is written over them.
            var asset = CreateOrReset<SpeedStars.Meta.Ads.AdsDevConfig>(AdsDevPath, overwrite);
            if (asset == null) { Skipped(Source, AdsDevPath); return; }
            Log(Source, $"Seeded {AdsDevPath}.");
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

        private const string DaySevenCosmeticId = "headwear.ph02"; // Headwear_PH02 in Data/Config/Cosmetics

        private static void SeedSevenDay(bool overwrite, CosmeticCatalog cosmetics)
        {
            var asset = CreateOrReset<SevenDayRewardConfig>(SevenDayRewardPath, overwrite);
            if (asset == null) { Skipped(Source, SevenDayRewardPath); return; }

            // Day 7 (PDF §2.4 left it open; user decision 2026-10-30): the Premium 02 headwear, looked up by its catalog id.
            CosmeticDefinition daySevenItem = null;
            if (cosmetics != null) cosmetics.TryGet(DaySevenCosmeticId, out daySevenItem);
            if (daySevenItem == null)
                Warn(Source, $"Cosmetic '{DaySevenCosmeticId}' is not in the cosmetic catalog; day 7 is left without an item (it gives nothing until one is set).");

            var so = new SerializedObject(asset);
            var days = Prop(so, "days");
            days.arraySize = SevenDayRewardConfig.DayCount;
            SetDay(days, 1, SevenDayRewardKind.Cash, 100, null);          // PDF §2.4: 1. Gun 100 Nakit
            SetDay(days, 2, SevenDayRewardKind.Cash, 200, null);          // PDF §2.4: 2. Gun 200 Nakit
            SetDay(days, 3, SevenDayRewardKind.TimedTicket, 1, null);     // PDF §2.4: 3. Gun 1 Sureli Bilet
            SetDay(days, 4, SevenDayRewardKind.Cash, 200, null);          // PDF §2.4: 4. Gun 200 Nakit
            SetDay(days, 5, SevenDayRewardKind.TimedTicket, 1, null);     // PDF §2.4: 5. Gun 1 Sureli Bilet
            SetDay(days, 6, SevenDayRewardKind.Cash, 300, null);          // PDF §2.4 has no day 6; design + user decision 2026-10-30: 300 Cash
            SetDay(days, 7, SevenDayRewardKind.Cosmetic, 0, daySevenItem); // Premium 02 headwear (user decision 2026-10-30)
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
                Prop(so, "hasOpponents").boolValue = seed.Id != PracticeEventId;
                Prop(so, "hurdleCount").intValue = seed.HurdleCount;
                Prop(so, "firstHurdleMeters").floatValue = seed.FirstHurdleMeters;
                Prop(so, "hurdleSpacingMeters").floatValue = seed.HurdleSpacingMeters;
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

        private const string PracticeEventId = "training"; // runs alone

        private readonly struct EventSeed
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly float DistanceMeters;
            public readonly bool RequiresTicket;
            public readonly bool AwardsPurpleGems;
            public readonly bool IsPlayable;
            public readonly int HurdleCount;
            public readonly float FirstHurdleMeters;
            public readonly float HurdleSpacingMeters;

            public EventSeed(string id, string displayName, float distanceMeters, bool ticket, bool gems, bool playable,
                int hurdles = 0, float firstHurdle = 0f, float hurdleSpacing = 0f)
            {
                Id = id;
                DisplayName = displayName;
                DistanceMeters = distanceMeters;
                RequiresTicket = ticket;
                AwardsPurpleGems = gems;
                IsPlayable = playable;
                HurdleCount = hurdles;
                FirstHurdleMeters = firstHurdle;
                HurdleSpacingMeters = hurdleSpacing;
            }
        }
    }
}
