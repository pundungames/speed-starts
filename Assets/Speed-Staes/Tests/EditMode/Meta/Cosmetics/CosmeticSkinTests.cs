using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;
using SpeedStars.Meta.Persistence;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Cosmetics
{
    /// <summary>Skin colour in the outfit, its save/load, the preset count growing to nine, and the engine-free skin plan.</summary>
    public sealed class CosmeticSkinTests
    {
        private const int NineSlots = 9;
        private CosmeticTestKit kit;

        [SetUp]
        public void SetUp() => kit = new CosmeticTestKit(0);

        [TearDown]
        public void TearDown() => kit.Dispose();

        // --- outfit value -----------------------------------------------------------------------------------------

        [Test]
        public void Outfit_WithSkinColor_IsACopy_KeepsItems_AndEqualityCoversIt()
        {
            CosmeticOutfit worn = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatA);
            CosmeticOutfit tanned = worn.WithSkinColor("6B4226");

            Assert.AreEqual(string.Empty, worn.SkinColorHex);
            Assert.AreEqual("6B4226", tanned.SkinColorHex);
            Assert.AreEqual(CosmeticTestKit.HatA, tanned.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreNotEqual(worn, tanned);
            Assert.AreEqual(tanned, worn.WithSkinColor("6b4226"), "Hex case does not matter.");
            Assert.AreEqual("6B4226", tanned.With(CosmeticCategory.Tops, CosmeticTestKit.TopA).SkinColorHex, "Changing an item keeps the skin.");
            Assert.AreEqual(string.Empty, worn.WithSkinColor(null).SkinColorHex);
        }

        // --- save -------------------------------------------------------------------------------------------------

        [Test]
        public void Skin_IsSavedWithThePreset_AndComesBackAfterRestart()
        {
            kit.Service.SaveOutfit(1, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB).WithSkinColor("#a9693d"));

            kit.Open();

            CosmeticOutfit loaded = kit.Service.GetOutfit(1);
            Assert.AreEqual("A9693D", loaded.SkinColorHex);
            Assert.AreEqual(CosmeticTestKit.HatB, loaded.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(string.Empty, kit.Service.GetOutfit(0).SkinColorHex, "Other presets keep their own skin.");
        }

        [Test]
        public void Skin_InvalidText_IsStoredAsDefault()
        {
            kit.Service.SaveOutfit(0, CosmeticOutfit.Empty.WithSkinColor("not-a-colour"));
            kit.Open();

            Assert.AreEqual(string.Empty, kit.Service.GetOutfit(0).SkinColorHex);
        }

        [Test]
        public void OldSave_WithoutSkinField_OpensWithTheDefaultSkin()
        {
            kit.Store.Write(ProgressRepository.SaveKey,
                "{\"schemaVersion\":2,\"cosmeticPresets\":[{\"slots\":[" +
                "{\"category\":\"Headwear\",\"cosmeticId\":\"" + CosmeticTestKit.HatB + "\",\"colorHex\":\"FF8800\"}]}]}");

            kit.Open();

            CosmeticOutfit outfit = kit.Service.GetOutfit(0);
            Assert.AreEqual(string.Empty, outfit.SkinColorHex);
            Assert.AreEqual(CosmeticTestKit.HatB, outfit.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(CosmeticTestKit.DefaultSkin, OutfitPlan.Build(outfit, kit.Catalog).Skin.ColorHex);
        }

        [Test]
        public void ThreePresets_GrowToNine_WithoutLosingAnything()
        {
            kit.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB, "FF8800").WithSkinColor("8D5524"));
            kit.Service.SaveOutfit(2, CosmeticOutfit.Empty.With(CosmeticCategory.Tops, CosmeticTestKit.TopA).WithSkinColor("FFDFC4"));
            kit.Service.SelectPreset(2);

            ConfigFixture.SetInt(kit.Catalog, "presetCount", NineSlots);
            kit.Open();

            Assert.AreEqual(NineSlots, kit.Service.PresetCount);
            Assert.AreEqual(2, kit.Service.SelectedPreset);
            Assert.AreEqual(CosmeticTestKit.HatB, kit.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual("FF8800", kit.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).ColorHex);
            Assert.AreEqual("8D5524", kit.Service.GetOutfit(0).SkinColorHex);
            Assert.AreEqual("FFDFC4", kit.Service.GetOutfit(2).SkinColorHex);
            for (int i = CosmeticTestKit.Presets; i < NineSlots; i++)
                Assert.AreEqual(kit.Service.DefaultOutfit, kit.Service.GetOutfit(i), "New presets wear the default outfit and the default skin.");
            Assert.AreEqual("Custom 9", kit.Service.GetPresetName(NineSlots - 1));
        }

        // --- catalog + plan ---------------------------------------------------------------------------------------

        [Test]
        public void Catalog_ExposesSkinTonesAndDefault()
        {
            CollectionAssert.AreEqual(new[] { CosmeticTestKit.SkinToneA, CosmeticTestKit.SkinToneB }, kit.Catalog.SkinTones);
            Assert.AreEqual(CosmeticTestKit.DefaultSkin, kit.Catalog.DefaultSkinColor);
        }

        [Test]
        public void Plan_UsesTheOutfitSkin_ElseTheDefault_ElseNothing()
        {
            Assert.AreEqual("6B4226", OutfitPlan.Build(CosmeticOutfit.Empty.WithSkinColor("6b4226"), kit.Catalog).Skin.ColorHex);
            Assert.AreEqual(CosmeticTestKit.DefaultSkin, OutfitPlan.Build(CosmeticOutfit.Empty, kit.Catalog).Skin.ColorHex);
            Assert.AreEqual(CosmeticTestKit.DefaultSkin, OutfitPlan.Build(CosmeticOutfit.Empty.WithSkinColor("zzz"), kit.Catalog).Skin.ColorHex);

            ConfigFixture.SetString(kit.Catalog, "defaultSkinColor", "");
            SkinPlan none = OutfitPlan.Build(CosmeticOutfit.Empty, kit.Catalog).Skin;
            Assert.IsFalse(none.HasColor);
        }

        [Test]
        public void Plan_NamesTheSkinRendererAndMaterialSlot()
        {
            SkinPlan skin = OutfitPlan.Build(CosmeticOutfit.Empty, kit.Catalog).Skin;

            Assert.IsTrue(skin.MatchesRenderer(CosmeticTestKit.SkinRenderer));
            Assert.IsFalse(skin.MatchesRenderer("Tshirt"));
            Assert.IsTrue(skin.MatchesMaterial(CosmeticTestKit.SkinMaterial + " (Instance)"));
            Assert.IsFalse(skin.MatchesMaterial("Color1_Toon"));
            Assert.IsFalse(skin.MatchesMaterial(null));
        }
    }
}
