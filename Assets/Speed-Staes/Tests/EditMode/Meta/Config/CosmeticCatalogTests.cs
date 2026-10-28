using NUnit.Framework;
using SpeedStars.Meta.Config;

namespace SpeedStars.Tests.Meta.Config
{
    public sealed class CosmeticCatalogTests
    {
        private ConfigFixture fixture;

        [SetUp]
        public void SetUp() => fixture = new ConfigFixture();

        [TearDown]
        public void TearDown() => fixture.DestroyAll();

        private CosmeticDefinition Item(string id, CosmeticCategory category, bool premium, int sortOrder)
        {
            var item = fixture.Create<CosmeticDefinition>();
            ConfigFixture.SetString(item, "cosmeticId", id);
            ConfigFixture.SetInt(item, "category", (int)category);
            ConfigFixture.SetBool(item, "isPremium", premium);
            ConfigFixture.SetInt(item, "sortOrder", sortOrder);
            return item;
        }

        private CosmeticCatalog Catalog(params CosmeticDefinition[] items)
        {
            var catalog = fixture.Create<CosmeticCatalog>();
            ConfigFixture.SetObjects(catalog, "cosmetics", items);
            return catalog;
        }

        [Test]
        public void GetByCategory_PremiumFirstThenSortOrder()
        {
            var normalLate = Item("normal-late", CosmeticCategory.Headwear, false, 2);
            var premiumLate = Item("premium-late", CosmeticCategory.Headwear, true, 9);
            var normalEarly = Item("normal-early", CosmeticCategory.Headwear, false, 1);
            var premiumEarly = Item("premium-early", CosmeticCategory.Headwear, true, 5);
            var catalog = Catalog(normalLate, premiumLate, normalEarly, premiumEarly);

            CollectionAssert.AreEqual(
                new[] { premiumEarly, premiumLate, normalEarly, normalLate },
                catalog.GetByCategory(CosmeticCategory.Headwear));
        }

        [Test]
        public void GetByCategory_PremiumStaysOnTopEvenWithAHigherSortOrder()
        {
            var normal = Item("normal", CosmeticCategory.Shoes, false, -100);
            var premium = Item("premium", CosmeticCategory.Shoes, true, 100);
            var catalog = Catalog(normal, premium);

            var list = catalog.GetByCategory(CosmeticCategory.Shoes);

            Assert.AreSame(premium, list[0]);
            Assert.AreSame(normal, list[1]);
        }

        [Test]
        public void GetByCategory_EqualSortOrderKeepsCatalogOrder()
        {
            var first = Item("first", CosmeticCategory.Socks, false, 3);
            var second = Item("second", CosmeticCategory.Socks, false, 3);
            var third = Item("third", CosmeticCategory.Socks, false, 3);
            var catalog = Catalog(first, second, third);

            CollectionAssert.AreEqual(new[] { first, second, third }, catalog.GetByCategory(CosmeticCategory.Socks));
        }

        [Test]
        public void GetByCategory_OnlyThatCategoryAndNoEmptySlots()
        {
            var hat = Item("hat", CosmeticCategory.Headwear, false, 0);
            var shoe = Item("shoe", CosmeticCategory.Shoes, true, 0);
            var catalog = Catalog(hat, null, shoe);

            CollectionAssert.AreEqual(new[] { shoe }, catalog.GetByCategory(CosmeticCategory.Shoes));
            CollectionAssert.IsEmpty(catalog.GetByCategory(CosmeticCategory.Gloves));
        }

        [Test]
        public void TryGet_FindsById()
        {
            var hat = Item("hat", CosmeticCategory.Headwear, false, 0);
            var shoe = Item("shoe", CosmeticCategory.Shoes, false, 0);
            var catalog = Catalog(hat, shoe);

            Assert.IsTrue(catalog.TryGet("shoe", out var found));
            Assert.AreSame(shoe, found);
            Assert.IsFalse(catalog.TryGet("missing", out var missing));
            Assert.IsNull(missing);
            Assert.IsFalse(catalog.TryGet(null, out _));
        }

        [Test]
        public void DuplicateId_IsReportedAndLookupReturnsTheFirst()
        {
            var first = Item("same", CosmeticCategory.Headwear, false, 0);
            var second = Item("same", CosmeticCategory.Tops, false, 0);
            var catalog = Catalog(first, second, Item("other", CosmeticCategory.Tops, false, 0));

            CollectionAssert.AreEqual(new[] { "same" }, catalog.FindDuplicateIds());
            Assert.IsTrue(catalog.TryGet("same", out var found));
            Assert.AreSame(first, found);
        }

        [Test]
        public void Definition_ExposesPartsPriceAndCurrency()
        {
            var item = Item("gloves", CosmeticCategory.Gloves, true, 0);
            ConfigFixture.SetStrings(item, "partNames", "piece", "piece-left", "piece-right");
            ConfigFixture.SetInt(item, "price", 7);
            ConfigFixture.SetInt(item, "currency", (int)CosmeticCurrency.PurpleGem);

            CollectionAssert.AreEqual(new[] { "piece", "piece-left", "piece-right" }, item.PartNames);
            Assert.AreEqual(7, item.Price);
            Assert.AreEqual(CosmeticCurrency.PurpleGem, item.Currency);
            Assert.IsTrue(item.IsForSale);
        }

        [Test]
        public void Definition_WithoutPrice_IsNotForSale()
        {
            var item = Item("plain", CosmeticCategory.Tops, false, 0);

            Assert.IsFalse(item.IsForSale);
            CollectionAssert.IsEmpty(item.PartNames);
        }

        [Test]
        public void CategoryNames_MatchThePrefabSlots()
        {
            // The applier finds Accessories/<Category> by enum name, so the names are a contract with the prefab.
            CollectionAssert.AreEqual(
                new[] { "Headwear", "Glasses", "Tops", "Gloves", "Shorts", "Leggings", "Socks", "Shoes" },
                System.Enum.GetNames(typeof(CosmeticCategory)));
        }
    }
}
