using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;

namespace SpeedStars.Tests.Meta.Cosmetics
{
    public sealed class CosmeticPreviewTests
    {
        private CosmeticTestKit kit;
        private CosmeticPreview preview;
        private int changes;

        [SetUp]
        public void SetUp()
        {
            kit = new CosmeticTestKit(0);
            preview = new CosmeticPreview(kit.Service);
            changes = 0;
            preview.Changed += () => changes++;
        }

        [TearDown]
        public void TearDown() => kit.Dispose();

        [Test]
        public void Default_IsTheSelectedPresetsSavedOutfit()
        {
            Assert.IsFalse(preview.IsPreviewing);
            Assert.AreEqual(kit.Service.SelectedOutfit, preview.Current);

            kit.Service.SaveOutfit(2, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB));
            kit.Service.SelectPreset(2);

            Assert.AreEqual(CosmeticTestKit.HatB, preview.Current.Get(CosmeticCategory.Headwear).CosmeticId);
        }

        [Test]
        public void Show_ChangesCurrentWithoutSaving_ResetReturnsToSaved()
        {
            CosmeticOutfit saved = kit.Service.SelectedOutfit;
            CosmeticOutfit shown = saved.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB);

            preview.Show(shown);

            Assert.IsTrue(preview.IsPreviewing);
            Assert.AreEqual(shown, preview.Current);
            Assert.AreEqual(saved, kit.Service.SelectedOutfit, "Previewing must not save.");
            Assert.AreEqual(1, changes);

            preview.Reset();

            Assert.IsFalse(preview.IsPreviewing);
            Assert.AreEqual(saved, preview.Current);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void Show_SameOutfitTwice_RaisesOnce_ResetWithoutPreviewIsSilent()
        {
            preview.Reset();
            Assert.AreEqual(0, changes);

            var shown = kit.Service.SelectedOutfit.Without(CosmeticCategory.Tops);
            preview.Show(shown);
            preview.Show(shown);

            Assert.AreEqual(1, changes);
        }

        [Test]
        public void SelectingAnotherPreset_DropsThePreview()
        {
            preview.Show(CosmeticOutfit.Empty);

            kit.Service.SelectPreset(1);

            Assert.IsFalse(preview.IsPreviewing);
            Assert.AreEqual(kit.Service.GetOutfit(1), preview.Current);
        }

        [Test]
        public void SavingTheSelectedPreset_WithoutPreview_RaisesChanged_WithPreview_StaysQuiet()
        {
            kit.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB));
            Assert.AreEqual(1, changes);

            preview.Show(CosmeticOutfit.Empty);
            int afterShow = changes;
            kit.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatA));

            Assert.AreEqual(afterShow, changes);
            Assert.AreEqual(CosmeticOutfit.Empty, preview.Current);
        }
    }

    public sealed class OutfitPlanTests
    {
        private CosmeticTestKit kit;

        [SetUp]
        public void SetUp() => kit = new CosmeticTestKit(0);

        [TearDown]
        public void TearDown() => kit.Dispose();

        [Test]
        public void WornItem_TurnsOnAllItsParts_OthersOfTheSlotAreNotListed()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty.With(CosmeticCategory.Gloves, CosmeticTestKit.GlovesPair), kit.Catalog);

            SlotPlan gloves = plan.Get(CosmeticCategory.Gloves);
            Assert.IsTrue(gloves.IsActive("GL"));
            Assert.IsTrue(gloves.IsActive("GL-left"));
            Assert.IsTrue(gloves.IsActive("GL-right"));
            Assert.IsFalse(gloves.IsActive("HAT-A"));
        }

        [Test]
        public void BareSlot_HasNoActiveParts()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty, kit.Catalog);

            foreach (SlotPlan slot in plan.Slots)
                Assert.AreEqual(0, slot.ActiveParts.Count, slot.Category + " should be bare.");
            Assert.AreEqual(CosmeticOutfit.Categories.Length, plan.Slots.Count);
        }

        [Test]
        public void Colour_GoesToTheItemsPrimaryChannelMaterial()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty
                .With(CosmeticCategory.Headwear, CosmeticTestKit.HatA, "abcdef")
                .With(CosmeticCategory.Tops, CosmeticTestKit.TopA, "112233"), kit.Catalog);

            SlotPlan hat = plan.Get(CosmeticCategory.Headwear);
            SlotPlan top = plan.Get(CosmeticCategory.Tops);
            Assert.IsTrue(hat.HasColor);
            Assert.AreEqual("ABCDEF", hat.ColorHex);
            Assert.AreEqual(CosmeticColorChannel.Color2.MaterialName(), hat.ColorMaterial, "HatA's primary channel is Color2.");
            Assert.AreEqual(CosmeticColorChannel.Color1.MaterialName(), top.ColorMaterial);
            Assert.AreNotEqual(hat.ColorMaterial, top.ColorMaterial);
        }

        [Test]
        public void NoColour_OrBadColour_LeavesTheItemsOwnColours()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty
                .With(CosmeticCategory.Headwear, CosmeticTestKit.HatA, "")
                .With(CosmeticCategory.Tops, CosmeticTestKit.TopA, "zzz"), kit.Catalog);

            Assert.IsFalse(plan.Get(CosmeticCategory.Headwear).HasColor);
            Assert.IsFalse(plan.Get(CosmeticCategory.Tops).HasColor);
            Assert.IsTrue(plan.Get(CosmeticCategory.Headwear).IsActive("HAT-A"), "The item itself is still worn.");
        }

        [Test]
        public void UnknownOrMisSlottedItem_IsWornBare()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty
                .With(CosmeticCategory.Shoes, CosmeticTestKit.HatA)
                .With(CosmeticCategory.Socks, "nothing"), kit.Catalog);

            Assert.AreEqual(0, plan.Get(CosmeticCategory.Shoes).ActiveParts.Count);
            Assert.AreEqual(0, plan.Get(CosmeticCategory.Socks).ActiveParts.Count);
        }

        [Test]
        public void LegacyObjects_ComeFromTheCatalog()
        {
            var plan = OutfitPlan.Build(CosmeticOutfit.Empty, kit.Catalog);

            CollectionAssert.AreEqual(new[] { "OldShirt", "OldShoes" }, plan.LegacyObjects);
        }
    }

    public sealed class CosmeticValueTests
    {
        [Test]
        public void Outfit_IsImmutable_WithReturnsACopy()
        {
            var empty = CosmeticOutfit.Empty;
            var hat = empty.With(CosmeticCategory.Headwear, "a", "ff0000");

            Assert.IsTrue(empty.Get(CosmeticCategory.Headwear).IsEmpty);
            Assert.AreEqual("a", hat.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(empty, hat.Without(CosmeticCategory.Headwear));
            Assert.AreEqual("00FF00", hat.WithColor(CosmeticCategory.Headwear, "00ff00").Get(CosmeticCategory.Headwear).ColorHex.ToUpperInvariant());
        }

        [Test]
        public void Outfit_EqualityIgnoresColourCase()
        {
            var a = CosmeticOutfit.Empty.With(CosmeticCategory.Tops, "t", "aabbcc");
            var b = CosmeticOutfit.Empty.With(CosmeticCategory.Tops, "t", "AABBCC");

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a, CosmeticOutfit.Empty);
        }

        [Test]
        public void ColorNormalize_AcceptsHashAndCase_RejectsGarbage()
        {
            Assert.AreEqual("FF8800", CosmeticColor.Normalize("#ff8800"));
            Assert.AreEqual("FF8800", CosmeticColor.Normalize(" FF8800 "));
            Assert.AreEqual(string.Empty, CosmeticColor.Normalize("FF88"));
            Assert.AreEqual(string.Empty, CosmeticColor.Normalize("GG8800"));
            Assert.AreEqual(string.Empty, CosmeticColor.Normalize(null));
        }

        [Test]
        public void ChannelMaterialNames_FollowTheToonMaterials()
        {
            foreach (CosmeticColorChannel channel in System.Enum.GetValues(typeof(CosmeticColorChannel)))
                StringAssert.EndsWith("_Toon", channel.MaterialName());
        }
    }
}
