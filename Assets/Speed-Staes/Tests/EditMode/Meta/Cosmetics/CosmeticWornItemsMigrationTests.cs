using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Persistence;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Cosmetics
{
    /// <summary>Economy v2: normal items that get a price keep belonging to players who already wear them.</summary>
    public sealed class CosmeticWornItemsMigrationTests
    {
        private const int NewPrice = 17; // arbitrary test input

        private CosmeticTestKit _k;

        [SetUp]
        public void SetUp() => _k = new CosmeticTestKit(0);

        [TearDown]
        public void TearDown() => _k.Dispose();

        private void PutPrice(string id)
        {
            Assert.IsTrue(_k.Catalog.TryGet(id, out CosmeticDefinition item));
            ConfigFixture.SetInt(item, "price", NewPrice);
        }

        private void Wear(string category, string id)
        {
            _k.Service.GetOutfit(0); // fills the presets
            _k.Progress.CosmeticPresets[0].Slots.Add(new SelectedCosmetic(category, id, ""));
        }

        [Test]
        public void WornItemThatNowHasAPrice_StaysOwned_AndWorn()
        {
            Wear("Headwear", CosmeticTestKit.HatA);
            PutPrice(CosmeticTestKit.HatA);

            Assert.IsTrue(_k.Service.IsOwned(CosmeticTestKit.HatA) == false, "before the migration runs the priced item reads as locked");
            Assert.AreEqual(CosmeticTestKit.HatA, _k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.IsTrue(_k.Service.IsOwned(CosmeticTestKit.HatA));
            Assert.Contains(CosmeticTestKit.HatA, _k.Progress.OwnedCosmeticIds);
        }

        [Test]
        public void UnwornPricedItem_StaysLocked()
        {
            Wear("Headwear", CosmeticTestKit.HatA);
            PutPrice(CosmeticTestKit.HatA);
            PutPrice(CosmeticTestKit.HatB);

            _k.Service.GetOutfit(0);

            Assert.IsTrue(_k.Service.IsLocked(CosmeticTestKit.HatB));
        }

        [Test]
        public void Migration_IsIdempotent_AndSavesOnlyWhenItAddedSomething()
        {
            Wear("Headwear", CosmeticTestKit.HatA);
            PutPrice(CosmeticTestKit.HatA);
            _k.Service.GetOutfit(0);
            int writes = _k.Store.Writes;

            _k.Service.GetOutfit(0);
            _k.Service.GetOutfit(0);

            Assert.AreEqual(writes, _k.Store.Writes);
            Assert.AreEqual(1, _k.Progress.OwnedCosmeticIds.FindAll(x => x == CosmeticTestKit.HatA).Count);
        }

        [Test]
        public void Ownership_SurvivesARestart()
        {
            Wear("Headwear", CosmeticTestKit.HatA);
            PutPrice(CosmeticTestKit.HatA);
            _k.Service.GetOutfit(0);

            _k.Open();

            Assert.IsTrue(_k.Service.IsOwned(CosmeticTestKit.HatA));
        }

        [Test]
        public void LockedPremiumInASavedPreset_IsNotGrantedByTheMigration()
        {
            Wear("Headwear", CosmeticTestKit.HatPremium);

            _k.Service.GetOutfit(0);

            Assert.IsTrue(_k.Service.IsLocked(CosmeticTestKit.HatPremium));
            Assert.IsFalse(_k.Progress.OwnedCosmeticIds.Contains(CosmeticTestKit.HatPremium));
        }
    }
}
