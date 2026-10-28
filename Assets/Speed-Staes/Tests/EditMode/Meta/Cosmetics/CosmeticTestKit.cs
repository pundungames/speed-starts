using SpeedStars.Meta.Config;
using SpeedStars.Meta.Cosmetics;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Cosmetics
{
    /// <summary>
    /// A small catalog with test-chosen ids and prices, a repository on a memory store and the real services over them.
    /// The numbers are arbitrary test inputs; tests assert relations (price charged, gems before/after), never shipped values.
    /// </summary>
    internal sealed class CosmeticTestKit
    {
        public const int Presets = 3;
        public const int PremiumPrice = 12;

        public const string HatA = "hat-a";
        public const string HatB = "hat-b";
        public const string HatPremium = "hat-premium";
        public const string HatNoPrice = "hat-premium-noprice";
        public const string TopA = "top-a";
        public const string GlovesPair = "gloves-pair";

        public readonly ConfigFixture Fixture = new ConfigFixture();
        public readonly FakeClock Clock = new FakeClock();
        public readonly MemoryStore Store = new MemoryStore();
        public readonly CosmeticCatalog Catalog;
        public ProgressRepository Repository;
        public WalletService Wallet;
        public EconomyLedger Ledger;
        public CosmeticService Service;

        public CosmeticTestKit(int startingGems)
        {
            Catalog = BuildCatalog();
            Open(startingGems);
        }

        /// <summary>(Re)opens the save: a new repository, wallet and service over the same store, as after an app restart.</summary>
        public void Open(int startingGems = 0)
        {
            Repository = new ProgressRepository(Store, () => new StartingBalances(0, startingGems, 0, 0));
            Repository.Load();
            Wallet = new WalletService(Repository, Clock);
            Ledger = new EconomyLedger(Repository, Wallet, Fixture.Create<EconomyConfig>());
            Service = new CosmeticService(Repository, Ledger, Catalog);
        }

        public PlayerProgress Progress => Repository.Current;

        public CosmeticDefinition Item(string id, CosmeticCategory category, bool premium, int price,
            CosmeticCurrency currency, int sortOrder, CosmeticColorChannel channel, params string[] parts)
        {
            var item = Fixture.Create<CosmeticDefinition>();
            ConfigFixture.SetString(item, "cosmeticId", id);
            ConfigFixture.SetInt(item, "category", (int)category);
            ConfigFixture.SetBool(item, "isPremium", premium);
            ConfigFixture.SetInt(item, "price", price);
            ConfigFixture.SetInt(item, "currency", (int)currency);
            ConfigFixture.SetInt(item, "sortOrder", sortOrder);
            ConfigFixture.SetInt(item, "primaryChannel", (int)channel);
            ConfigFixture.SetStrings(item, "partNames", parts.Length > 0 ? parts : new[] { id.ToUpperInvariant() });
            return item;
        }

        private CosmeticCatalog BuildCatalog()
        {
            var catalog = Fixture.Create<CosmeticCatalog>();
            ConfigFixture.SetObjects(catalog, "cosmetics",
                Item(HatA, CosmeticCategory.Headwear, false, 0, CosmeticCurrency.Cash, 1, CosmeticColorChannel.Color2),
                Item(HatB, CosmeticCategory.Headwear, false, 0, CosmeticCurrency.Cash, 2, CosmeticColorChannel.Color1),
                Item(HatPremium, CosmeticCategory.Headwear, true, PremiumPrice, CosmeticCurrency.PurpleGem, 1, CosmeticColorChannel.Color3),
                Item(HatNoPrice, CosmeticCategory.Headwear, true, 0, CosmeticCurrency.PurpleGem, 2, CosmeticColorChannel.Color1),
                Item(TopA, CosmeticCategory.Tops, false, 0, CosmeticCurrency.Cash, 1, CosmeticColorChannel.Color1),
                Item(GlovesPair, CosmeticCategory.Gloves, false, 0, CosmeticCurrency.Cash, 1, CosmeticColorChannel.Color1,
                    "GL", "GL-left", "GL-right"));
            ConfigFixture.SetInt(catalog, "presetCount", Presets);
            ConfigFixture.SetStrings(catalog, "colorPalette", "FF0000", "00ff00", "not-a-colour");
            ConfigFixture.SetStrings(catalog, "legacyOutfitObjects", "OldShirt", "OldShoes");
            ConfigFixture.Edit(catalog, so =>
            {
                var list = ConfigFixture.Find(so, "defaultOutfit");
                list.arraySize = 2;
                Fill(list.GetArrayElementAtIndex(0), CosmeticCategory.Headwear, HatA, "AA0000");
                Fill(list.GetArrayElementAtIndex(1), CosmeticCategory.Tops, TopA, "");
            });
            return catalog;
        }

        private static void Fill(UnityEditor.SerializedProperty entry, CosmeticCategory category, string id, string hex)
        {
            entry.FindPropertyRelative("category").intValue = (int)category;
            entry.FindPropertyRelative("cosmeticId").stringValue = id;
            entry.FindPropertyRelative("colorHex").stringValue = hex;
        }

        public void Dispose() => Fixture.DestroyAll();
    }
}
