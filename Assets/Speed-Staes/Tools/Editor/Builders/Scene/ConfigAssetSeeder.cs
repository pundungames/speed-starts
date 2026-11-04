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
        public const string RaceOfTheDayPath = Folder + "/RaceOfTheDayConfig.asset";

        /// <summary>
        /// Events in display order (PDF §1.3, MD §12). Distance only where the name states it; Training, Race of the
        /// Day and Medley have none in the documents, so 0. Purple Gems: PDF §1.3 ("yalnizca 400m, 60mH, Medley").
        /// Ticket: only 100m, the slice assumption (OPEN #3: which events were paid in the original is unverified).
        /// Playable: 100m and 110mH, the races the runner mechanic has (OPEN #9: relay, Medley rules). Hurdle layouts are
        /// the athletics standard; only 110mH is checked against the reference videos and the bots' times.
        /// </summary>
        private static readonly EventSeed[] Events =
        {
            new EventSeed("training", "Practice", 100f, ticket: false, gems: false, playable: true),
            new EventSeed("race_of_the_day", "Race of the Day", 0f, ticket: false, gems: false, playable: true), // T-087: open, free; length and outfit come from RaceOfTheDayConfig
            new EventSeed("100m", "100m", 100f, ticket: false, gems: false, playable: true),
            new EventSeed("200m", "200m", 200f, ticket: true, gems: false, playable: true),
            new EventSeed("400m", "400m", 400f, ticket: true, gems: true, playable: true),
            new EventSeed("60mh", "60mH", 60f, ticket: true, gems: true, playable: true, hurdles: 5, firstHurdle: 13.72f, hurdleSpacing: 9.14f),
            new EventSeed("110mh", "110mH", 110f, ticket: true, gems: false, playable: true, hurdles: 10, firstHurdle: 13.72f, hurdleSpacing: 9.14f),
            new EventSeed("400mh", "400mH", 400f, ticket: true, gems: false, playable: true, hurdles: 10, firstHurdle: 45f, hurdleSpacing: 35f),
            new EventSeed("4x100m", "4×100m", 400f, ticket: true, gems: false, playable: false), // relay: 4 legs x 100 m, from the name
            new EventSeed("medley", "Medley", 0f, ticket: true, gems: true, playable: false),
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
            RaceFinishConfigSeed.Seed(overwrite); // the picture between the line and the Results
            EmoteCatalogSeed.Seed(overwrite); // T-040
            BotRaceConfigSeed.Seed(overwrite); // T-074: opponent bots (field, difficulty, press driver, names)
            AudioConfigSeed.Seed(AudioPath, overwrite); // T-052: sound cues (placeholder WAVs), music slot, haptic numbers
            SeedAdsDev(overwrite); // T-050: fake rewarded ad (dev only)
            SeedRaceOfTheDay(overwrite); // T-087: the day's distance list and racer looks
            ApplyEconomyV2();
            AssetDatabase.SaveAssets();
        }

        // ---- Race of the Day (T-087) ----
        // Distances: reference measurement 03.11 (60 m on 30.10, 150 m on 03.11) widened to a list; flat races only, user can edit the asset.
        private static readonly float[] RaceOfTheDayDistances = { 60f, 100f, 150f, 200f, 300f };

        private readonly struct LookSeed
        {
            public readonly string Label;
            public readonly string Skin;
            public readonly (CosmeticCategory category, string id, string hex)[] Items;

            public LookSeed(string label, string skin, params (CosmeticCategory, string, string)[] items)
            {
                Label = label;
                Skin = skin;
                Items = items;
            }
        }

        // Ready-made racers (catalog ids from Data/Config/Cosmetics; a colour is the primary channel, "" = the item's own colours).
        private static readonly LookSeed[] RaceOfTheDayLooks =
        {
            new LookSeed("Blue Sprinter", "E0AC69",
                (CosmeticCategory.Headwear, "headwear.h02", "1E88E5"), (CosmeticCategory.Tops, "tops.t01", "1E88E5"),
                (CosmeticCategory.Shorts, "shorts.s01", "F5F5F5"), (CosmeticCategory.Leggings, "leggings.l01", "F5F5F5"),
                (CosmeticCategory.Shoes, "shoes.sh01", "")),
            new LookSeed("Red Flash", "8D5524",
                (CosmeticCategory.Glasses, "glasses.g01", ""), (CosmeticCategory.Tops, "tops.t02", "E53935"),
                (CosmeticCategory.Shorts, "shorts.s02", "212121"), (CosmeticCategory.Leggings, "leggings.l02", ""),
                (CosmeticCategory.Shoes, "shoes.sh02", "")),
            new LookSeed("Green Machine", "FFDFC4",
                (CosmeticCategory.Headwear, "headwear.h03", "43A047"), (CosmeticCategory.Tops, "tops.t03", "43A047"),
                (CosmeticCategory.Gloves, "gloves.gl01", "212121"), (CosmeticCategory.Shorts, "shorts.s03", "F5F5F5"),
                (CosmeticCategory.Shoes, "shoes.sh03", "")),
            new LookSeed("Golden Dash", "C68642",
                (CosmeticCategory.Glasses, "glasses.g02", ""), (CosmeticCategory.Tops, "tops.t04", "FDD835"),
                (CosmeticCategory.Shorts, "shorts.s04", "212121"), (CosmeticCategory.Leggings, "leggings.l03", ""),
                (CosmeticCategory.Socks, "socks.so01", "F5F5F5"), (CosmeticCategory.Shoes, "shoes.sh04", "")),
            new LookSeed("Purple Rain", "F1C27D",
                (CosmeticCategory.Headwear, "headwear.h04", "8E24AA"), (CosmeticCategory.Tops, "tops.t05", "8E24AA"),
                (CosmeticCategory.Gloves, "gloves.gl02", ""), (CosmeticCategory.Shorts, "shorts.s05", "F5F5F5"),
                (CosmeticCategory.Shoes, "shoes.sh05", "")),
            new LookSeed("Night Runner", "6B4226",
                (CosmeticCategory.Glasses, "glasses.g03", ""), (CosmeticCategory.Tops, "tops.t06", "212121"),
                (CosmeticCategory.Shorts, "shorts.s06", "212121"), (CosmeticCategory.Leggings, "leggings.l04", "FB8C00"),
                (CosmeticCategory.Shoes, "shoes.sh06", "")),
            new LookSeed("Orange Blaze", "A9693D",
                (CosmeticCategory.Headwear, "headwear.h05", "FB8C00"), (CosmeticCategory.Tops, "tops.t07", "FB8C00"),
                (CosmeticCategory.Shorts, "shorts.s07", "F5F5F5"), (CosmeticCategory.Socks, "socks.so02", ""),
                (CosmeticCategory.Shoes, "shoes.sh07", "")),
        };

        /// <summary>
        /// T-087, for an existing project: creates the Race of the Day config when it is missing (an existing one is never touched) and
        /// opens the event (isPlayable written from the seed table, like <c>ApplyEconomyV2</c> does for every event). Safe to run again.
        /// Parameterless, so a verification copy can run it with <c>-executeMethod</c>.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Build/Race of the Day - seed config + open the event")]
        public static void ApplyRaceOfTheDay()
        {
            EnsureFolder(Folder);
            SeedRaceOfTheDay(false);
            ApplyEventAccess();
            AssetDatabase.SaveAssets();
        }

        private static void SeedRaceOfTheDay(bool overwrite)
        {
            var asset = CreateOrReset<SpeedStars.Meta.DailyRace.RaceOfTheDayConfig>(RaceOfTheDayPath, overwrite);
            if (asset == null) { Skipped(Source, RaceOfTheDayPath); return; }

            var so = new SerializedObject(asset);
            Prop(so, "eventId").stringValue = "race_of_the_day";
            SerializedProperty distances = Prop(so, "distancesMeters");
            distances.arraySize = RaceOfTheDayDistances.Length;
            for (int i = 0; i < RaceOfTheDayDistances.Length; i++) distances.GetArrayElementAtIndex(i).floatValue = RaceOfTheDayDistances[i];

            SerializedProperty looks = Prop(so, "looks");
            looks.arraySize = RaceOfTheDayLooks.Length;
            for (int i = 0; i < RaceOfTheDayLooks.Length; i++)
            {
                SerializedProperty look = looks.GetArrayElementAtIndex(i);
                Rel(look, "label").stringValue = RaceOfTheDayLooks[i].Label;
                Rel(look, "skinColorHex").stringValue = RaceOfTheDayLooks[i].Skin;
                SerializedProperty items = Rel(look, "items");
                items.arraySize = RaceOfTheDayLooks[i].Items.Length;
                for (int j = 0; j < RaceOfTheDayLooks[i].Items.Length; j++)
                {
                    var item = RaceOfTheDayLooks[i].Items[j];
                    SerializedProperty entry = items.GetArrayElementAtIndex(j);
                    Rel(entry, "category").intValue = (int)item.category;
                    Rel(entry, "cosmeticId").stringValue = item.id;
                    Rel(entry, "colorHex").stringValue = item.hex;
                }
            }
            Apply(so);
            Log(Source, $"Seeded {RaceOfTheDayPath} ({RaceOfTheDayDistances.Length} distances, {RaceOfTheDayLooks.Length} looks).");
        }

        // ---- Economy v2 (T-084; docs/analiz/2026-11-03-referans-oyun-ekonomi.md section 4, user decision 03.11) ----
        private const int EntryTicketCashPrice = 0;        // the reference sells no single entry; Entry Tickets are not sold for Cash
        private const int TimedTicketCashPrice = 300;     // reference game: "Spend 300 -> unlock for 1 hour"
        private const int RaceFinishCash = 10;
        private const int RaceDifficultyBonusCash = 10;
        private const int RaceOpponentBonusCash = 10;   // paid in full when every opponent is passed
        private const int RewardedAdCash = 200;
        private const int RewardedAdCashDailyLimit = 3;
        private const int HeadwearCashPrice = 1000;      // normal Headwear
        private const int OtherCosmeticCashPrice = 500;  // normal Glasses, Tops, Gloves, Shorts, Leggings, Socks, Shoes
        // Emotes (T-089): the paid ones in list order, EmotesPerPriceTier of each price (reference tiers: 3 x 2500, 3 x 5000, 3 x 7500); more repeat the last.
        private static readonly int[] EmotePriceTiers = { 2500, 5000, 7500 };
        private const int EmotesPerPriceTier = 3;

        /// <summary>Cash price of the n-th paid emote (0-based, list order).</summary>
        public static int EmotePrice(int paidIndex) =>
            EmotePriceTiers[System.Math.Min(System.Math.Max(0, paidIndex) / EmotesPerPriceTier, EmotePriceTiers.Length - 1)];

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

            ApplyEventAccess();

            var cosmetics = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CosmeticCatalogPath);
            if (cosmetics != null) ApplyCosmeticPrices(cosmetics);
            else Warn(Source, $"'{CosmeticCatalogPath}' is missing; cosmetic prices not written.");

            var emotes = AssetDatabase.LoadAssetAtPath<SpeedStars.Meta.Emotes.EmoteCatalog>(EmoteCatalogSeed.CatalogPath);
            if (emotes != null) ApplyEmotePrices(emotes);
            else Warn(Source, $"'{EmoteCatalogSeed.CatalogPath}' is missing; emote prices not written.");

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// T-106: writes the Practice event's display name from the seed table into the EXISTING asset (seeding skips existing
        /// assets). Touches that one field of that one asset and saves only when the name differs, so a second run changes nothing.
        /// </summary>
        [MenuItem("Tools/Speed Stars/Build/Apply theme prices")]
        public static void ApplyThemePrices() => ThemeCatalogSeed.ApplyPrices(ThemeCatalogPath);

        [MenuItem("Tools/Speed Stars/Build/Apply Practice event name")]
        public static void ApplyPracticeEventName()
        {
            string path = $"{EventsFolder}/{PracticeEventId}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<RaceEventDefinition>(path);
            if (definition == null) { Warn(Source, $"'{path}' is missing; Practice name not written."); return; }

            string name = null;
            foreach (var seed in Events)
                if (seed.Id == PracticeEventId) name = seed.DisplayName;
            if (name == null) { Warn(Source, $"No '{PracticeEventId}' row in the seed table; Practice name not written."); return; }

            var so = new SerializedObject(definition);
            SerializedProperty property = Prop(so, "displayName");
            if (property.stringValue == name) return;
            property.stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        /// <summary>
        /// Event access as in the reference game: 100m, Race of the Day and Practice are free and unlimited; the other events
        /// need a ticket (Timed Ticket). The flag per id comes from the seed table <c>Events</c>; an event of the catalog
        /// that is not in the table is left as it is. requiresTicket and isPlayable are written from the table (200m, 400m, 60mH, 400mH and Race of the Day are open; 4x100m, Medley stay closed).
        /// </summary>
        private static void ApplyEventAccess()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RaceEventCatalog>(RaceEventCatalogPath);
            if (catalog == null) { Warn(Source, $"'{RaceEventCatalogPath}' is missing; event access not written."); return; }

            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                int index = System.Array.FindIndex(Events, e => e.Id == definition.Id.Value);
                if (index < 0) continue;
                var so = new SerializedObject(definition);
                Prop(so, "requiresTicket").boolValue = Events[index].RequiresTicket;
                Prop(so, "isPlayable").boolValue = Events[index].IsPlayable;
                Apply(so);
            }
            Log(Source, "Event access written (requiresTicket per the seed table).");
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
                price.intValue = EmotePrice(paid);
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
