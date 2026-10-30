using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Cosmetics
{
    public sealed class CosmeticServiceTests
    {
        private CosmeticTestKit kit;

        [TearDown]
        public void TearDown() => kit?.Dispose();

        private CosmeticTestKit Kit(int gems)
        {
            kit = new CosmeticTestKit(gems);
            return kit;
        }

        // --- ownership -------------------------------------------------------------------------------------------

        [Test]
        public void NormalItem_IsOwned_PremiumIsLockedUntilBought()
        {
            var k = Kit(0);

            Assert.IsTrue(k.Service.IsOwned(CosmeticTestKit.HatA));
            Assert.IsFalse(k.Service.IsLocked(CosmeticTestKit.HatA));
            Assert.IsTrue(k.Service.IsLocked(CosmeticTestKit.HatPremium));
            Assert.IsFalse(k.Service.IsOwned(CosmeticTestKit.HatPremium));
        }

        [Test]
        public void UnknownId_IsNeitherOwnedNorLocked()
        {
            var k = Kit(0);

            Assert.IsFalse(k.Service.IsOwned("nothing"));
            Assert.IsFalse(k.Service.IsLocked("nothing"));
            Assert.IsFalse(k.Service.TryGetItem("nothing", out _));
        }

        // --- purchase --------------------------------------------------------------------------------------------

        [Test]
        public void TryPurchase_ChargesTheCatalogPriceOnce()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice * 3);
            int before = k.Wallet.PurpleGems;

            var first = k.Service.TryPurchase(CosmeticTestKit.HatPremium);
            int afterFirst = k.Wallet.PurpleGems;
            var second = k.Service.TryPurchase(CosmeticTestKit.HatPremium);

            Assert.AreEqual(CosmeticPurchaseResult.Purchased, first);
            Assert.AreEqual(CosmeticTestKit.PremiumPrice, before - afterFirst);
            Assert.AreEqual(CosmeticPurchaseResult.AlreadyOwned, second);
            Assert.AreEqual(afterFirst, k.Wallet.PurpleGems, "A second purchase of the same item must not charge.");
            Assert.IsTrue(k.Service.IsOwned(CosmeticTestKit.HatPremium));
            Assert.IsFalse(k.Service.IsLocked(CosmeticTestKit.HatPremium));
        }

        [Test]
        public void TryPurchase_NotEnoughGems_ChangesNothing()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice - 1);
            int writes = k.Store.Writes;

            var result = k.Service.TryPurchase(CosmeticTestKit.HatPremium);

            Assert.AreEqual(CosmeticPurchaseResult.NotEnough, result);
            Assert.AreEqual(CosmeticTestKit.PremiumPrice - 1, k.Wallet.PurpleGems);
            Assert.IsTrue(k.Service.IsLocked(CosmeticTestKit.HatPremium));
            Assert.AreEqual(writes, k.Store.Writes);
        }

        [Test]
        public void TryPurchase_ExactPrice_LeavesZeroGems()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice);

            Assert.AreEqual(CosmeticPurchaseResult.Purchased, k.Service.TryPurchase(CosmeticTestKit.HatPremium));
            Assert.AreEqual(0, k.Wallet.PurpleGems);
        }

        [Test]
        public void TryPurchase_OtherResults()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice * 3);

            Assert.AreEqual(CosmeticPurchaseResult.AlreadyOwned, k.Service.TryPurchase(CosmeticTestKit.HatA));
            Assert.AreEqual(CosmeticPurchaseResult.NotForSale, k.Service.TryPurchase(CosmeticTestKit.HatNoPrice));
            Assert.AreEqual(CosmeticPurchaseResult.UnknownItem, k.Service.TryPurchase("nothing"));
            Assert.AreEqual(CosmeticTestKit.PremiumPrice * 3, k.Wallet.PurpleGems);
        }

        [Test]
        public void TryPurchase_PersistsOwnershipAndBalance()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice * 2);
            k.Service.TryPurchase(CosmeticTestKit.HatPremium);

            k.Open(); // app restart over the same store

            Assert.IsTrue(k.Service.IsOwned(CosmeticTestKit.HatPremium));
            Assert.AreEqual(CosmeticTestKit.PremiumPrice, k.Wallet.PurpleGems);
        }

        [Test]
        public void TryPurchase_NotifiesWalletAndService()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice);
            int walletEvents = 0, serviceEvents = 0;
            k.Wallet.Changed += () => walletEvents++;
            k.Service.Changed += () => serviceEvents++;

            k.Service.TryPurchase(CosmeticTestKit.HatPremium);

            Assert.AreEqual(1, walletEvents);
            Assert.AreEqual(1, serviceEvents);
        }

        // --- items -----------------------------------------------------------------------------------------------

        [Test]
        public void GetItems_PremiumFirst_WithOwnership()
        {
            var k = Kit(0);

            IReadOnlyList<CosmeticItemInfo> hats = k.Service.GetItems(CosmeticCategory.Headwear);

            Assert.IsTrue(hats[0].IsPremium);
            Assert.IsTrue(hats[0].IsLocked);
            Assert.AreEqual(CosmeticTestKit.HatPremium, hats[0].Id);
            Assert.IsTrue(hats[hats.Count - 1].IsOwned);
            Assert.IsFalse(hats[hats.Count - 1].IsPremium);
        }

        // --- saving outfits --------------------------------------------------------------------------------------

        [Test]
        public void SaveOutfit_WithLockedItem_IsRejectedAndChangesNothing()
        {
            var k = Kit(0);
            CosmeticOutfit before = k.Service.GetOutfit(0);
            int writes = k.Store.Writes;

            var result = k.Service.SaveOutfit(0, before.With(CosmeticCategory.Headwear, CosmeticTestKit.HatPremium));

            Assert.AreEqual(CosmeticSaveResult.LockedItem, result);
            Assert.AreEqual(before, k.Service.GetOutfit(0));
            Assert.AreEqual(writes, k.Store.Writes);
        }

        [Test]
        public void SaveOutfit_UnknownOrMisSlottedItem_IsRejected()
        {
            var k = Kit(0);

            Assert.AreEqual(CosmeticSaveResult.UnknownItem,
                k.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, "nothing")));
            Assert.AreEqual(CosmeticSaveResult.UnknownItem,
                k.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Shoes, CosmeticTestKit.HatA)));
        }

        [Test]
        public void SaveOutfit_InvalidPreset_IsRejected()
        {
            var k = Kit(0);

            Assert.AreEqual(CosmeticSaveResult.InvalidPreset, k.Service.SaveOutfit(-1, CosmeticOutfit.Empty));
            Assert.AreEqual(CosmeticSaveResult.InvalidPreset, k.Service.SaveOutfit(k.Service.PresetCount, CosmeticOutfit.Empty));
        }

        [Test]
        public void SaveOutfit_BoughtItem_IsSavedAndSurvivesRestart()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice);
            k.Service.TryPurchase(CosmeticTestKit.HatPremium);
            var outfit = CosmeticOutfit.Empty
                .With(CosmeticCategory.Headwear, CosmeticTestKit.HatPremium, "#ff8800")
                .With(CosmeticCategory.Tops, CosmeticTestKit.TopA);

            Assert.AreEqual(CosmeticSaveResult.Saved, k.Service.SaveOutfit(1, outfit));
            k.Open();

            CosmeticOutfit loaded = k.Service.GetOutfit(1);
            Assert.AreEqual(CosmeticTestKit.HatPremium, loaded.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual("FF8800", loaded.Get(CosmeticCategory.Headwear).ColorHex, "Colour is stored normalized.");
            Assert.AreEqual(CosmeticTestKit.TopA, loaded.Get(CosmeticCategory.Tops).CosmeticId);
            Assert.IsTrue(loaded.Get(CosmeticCategory.Shoes).IsEmpty);
        }

        [Test]
        public void SaveOutfit_BareSlot_IsWornBare()
        {
            var k = Kit(0);

            k.Service.SaveOutfit(0, k.Service.GetOutfit(0).Without(CosmeticCategory.Headwear));

            Assert.IsTrue(k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).IsEmpty);
        }

        [Test]
        public void SaveOutfit_ProtectedSave_ReportsNotPersisted()
        {
            var k = Kit(0);
            k.Store.Write(ProgressRepository.SaveKey, "{\"schemaVersion\":" + (ProgressRepository.CurrentSchemaVersion + 1) + "}");
            k.Open();

            var result = k.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatA));

            Assert.AreEqual(CosmeticSaveResult.NotPersisted, result);
        }

        [Test]
        public void GetOutfit_LeavesOutLockedAndUnknownItemsFoundInTheSave()
        {
            var k = Kit(0);
            var slots = k.Progress.CosmeticPresets;
            k.Service.GetOutfit(0); // fills the presets
            slots[0].Slots.Clear();
            slots[0].Slots.Add(new SelectedCosmetic("Headwear", CosmeticTestKit.HatPremium, ""));   // locked
            slots[0].Slots.Add(new SelectedCosmetic("Tops", "removed-from-catalog", ""));           // unknown
            slots[0].Slots.Add(new SelectedCosmetic("Shoes", CosmeticTestKit.HatA, ""));            // wrong slot
            slots[0].Slots.Add(new SelectedCosmetic("NotACategory", CosmeticTestKit.TopA, ""));     // bad category

            Assert.AreEqual(CosmeticOutfit.Empty, k.Service.GetOutfit(0));
        }

        // --- presets ---------------------------------------------------------------------------------------------

        [Test]
        public void Presets_CountAndNamesFollowTheCatalog()
        {
            var k = Kit(0);

            Assert.AreEqual(CosmeticTestKit.Presets, k.Service.PresetCount);
            for (int i = 0; i < k.Service.PresetCount; i++)
                Assert.AreEqual("Custom " + (i + 1), k.Service.GetPresetName(i));
        }

        [Test]
        public void NewPlayer_EveryPresetStartsWithTheDefaultOutfit()
        {
            var k = Kit(0);

            Assert.AreEqual(0, k.Service.SelectedPreset);
            for (int i = 0; i < k.Service.PresetCount; i++)
                Assert.AreEqual(k.Service.DefaultOutfit, k.Service.GetOutfit(i));
            Assert.AreEqual(CosmeticTestKit.HatA, k.Service.DefaultOutfit.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual("AA0000", k.Service.DefaultOutfit.Get(CosmeticCategory.Headwear).ColorHex);
        }

        [Test]
        public void SelectPreset_IsSavedAndRestored_BadIndexIsRefused()
        {
            var k = Kit(0);

            Assert.IsTrue(k.Service.SelectPreset(2));
            Assert.IsFalse(k.Service.SelectPreset(k.Service.PresetCount));
            Assert.IsFalse(k.Service.SelectPreset(-1));
            Assert.AreEqual(2, k.Service.SelectedPreset);

            k.Open();
            Assert.AreEqual(2, k.Service.SelectedPreset);
        }

        [Test]
        public void SaveOutfit_ChangesOnlyThatPreset()
        {
            var k = Kit(0);
            CosmeticOutfit defaults = k.Service.DefaultOutfit;

            k.Service.SaveOutfit(1, defaults.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB));

            Assert.AreEqual(CosmeticTestKit.HatB, k.Service.GetOutfit(1).Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(defaults, k.Service.GetOutfit(0));
            Assert.AreEqual(defaults, k.Service.GetOutfit(2));
        }

        [Test]
        public void SelectedOutfit_FollowsTheSelectedPreset()
        {
            var k = Kit(0);
            k.Service.SaveOutfit(2, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB));

            k.Service.SelectPreset(2);

            Assert.AreEqual(CosmeticTestKit.HatB, k.Service.SelectedOutfit.Get(CosmeticCategory.Headwear).CosmeticId);
        }

        [Test]
        public void RaisingThePresetCount_AddsDefaultPresets_KeepsTheOldOnes()
        {
            var k = Kit(0);
            k.Service.SaveOutfit(0, CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, CosmeticTestKit.HatB));

            ConfigFixture.SetInt(k.Catalog, "presetCount", CosmeticTestKit.Presets + 2);

            Assert.AreEqual(CosmeticTestKit.Presets + 2, k.Service.PresetCount);
            Assert.AreEqual(CosmeticTestKit.HatB, k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(k.Service.DefaultOutfit, k.Service.GetOutfit(CosmeticTestKit.Presets + 1));
        }

        [Test]
        public void LoweringThePresetCount_KeepsSelectionValid()
        {
            var k = Kit(0);
            k.Service.SelectPreset(CosmeticTestKit.Presets - 1);

            ConfigFixture.SetInt(k.Catalog, "presetCount", 1);

            Assert.AreEqual(0, k.Service.SelectedPreset);
        }

        // --- migration -------------------------------------------------------------------------------------------

        [Test]
        public void Migration_OldSingleOutfitBecomesPresetZero_OthersGetDefaults()
        {
            var k = Kit(0);
            k.Store.Write(ProgressRepository.SaveKey,
                "{\"schemaVersion\":1,\"selectedCosmetics\":[" +
                "{\"category\":\"Headwear\",\"cosmeticId\":\"" + CosmeticTestKit.HatB + "\",\"colorHex\":\"FF8800\"}]}");

            k.Open();

            CosmeticSlot hat = k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear);
            Assert.AreEqual(CosmeticTestKit.HatB, hat.CosmeticId);
            Assert.AreEqual("FF8800", hat.ColorHex);
            Assert.AreEqual(k.Service.DefaultOutfit, k.Service.GetOutfit(1));
            CollectionAssert.IsEmpty(k.Progress.SelectedCosmetics, "The old list is emptied so it is never migrated twice.");
        }

        [Test]
        public void Migration_OldSaveWithoutSelection_FillsDefaults_KeepsBalances()
        {
            var k = Kit(0);
            k.Store.Write(ProgressRepository.SaveKey, "{\"schemaVersion\":1,\"purpleGems\":7,\"ownedCosmeticIds\":[\"" + CosmeticTestKit.HatPremium + "\"]}");

            k.Open();

            Assert.AreEqual(7, k.Wallet.PurpleGems);
            Assert.IsTrue(k.Service.IsOwned(CosmeticTestKit.HatPremium));
            Assert.AreEqual(k.Service.DefaultOutfit, k.Service.GetOutfit(0));
        }

        [Test]
        public void Migration_RunsOnce_SavedAsTheCurrentSchema()
        {
            var k = Kit(0);
            k.Store.Write(ProgressRepository.SaveKey,
                "{\"schemaVersion\":1,\"selectedCosmetics\":[" +
                "{\"category\":\"Headwear\",\"cosmeticId\":\"" + CosmeticTestKit.HatB + "\",\"colorHex\":\"\"}]}");
            k.Open();
            k.Repository.Save();

            k.Open();

            Assert.AreEqual(ProgressRepository.CurrentSchemaVersion, k.Progress.SchemaVersion);
            Assert.AreEqual(CosmeticTestKit.HatB, k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(1, CountPresetsWith(k, CosmeticTestKit.HatB), "Not copied into more presets (defaults wear another hat).");
        }

        private static int CountPresetsWith(CosmeticTestKit k, string id)
        {
            int count = 0;
            foreach (CosmeticPreset preset in k.Progress.CosmeticPresets)
                foreach (SelectedCosmetic slot in preset.Slots)
                    if (slot.CosmeticId == id) { count++; break; }
            return count;
        }

        // --- random ----------------------------------------------------------------------------------------------

        [Test]
        public void Random_PicksOnlyOwnedItems_AndPaletteColours()
        {
            var k = Kit(0);
            var palette = new HashSet<string>(k.Service.ColorPalette);

            for (int seed = 0; seed < 100; seed++)
            {
                CosmeticOutfit outfit = k.Service.CreateRandomOutfit(new Random(seed));
                foreach (CosmeticCategory category in CosmeticOutfit.Categories)
                {
                    CosmeticSlot slot = outfit.Get(category);
                    if (slot.IsEmpty) continue;
                    Assert.IsTrue(k.Service.IsOwned(slot.CosmeticId), slot.CosmeticId + " is not owned.");
                    TryGetChannels(k, slot.CosmeticId, out int channels);
                    for (int channel = 0; channel < channels; channel++)
                        Assert.IsTrue(palette.Contains(slot.ColorAt(channel)), slot.ColorAt(channel) + " is not a palette colour (channel " + channel + ").");
                }
                Assert.AreEqual(CosmeticSaveResult.Saved, k.Service.SaveOutfit(0, outfit), "A random outfit must be savable.");
            }
        }

        private static void TryGetChannels(CosmeticTestKit k, string id, out int channels)
        {
            channels = k.Service.TryGetItem(id, out CosmeticItemInfo info) ? info.ChannelCount : 1;
        }

        [Test]
        public void Random_GivesEveryChannelOfTheItemAColour()
        {
            var k = Kit(0);
            bool sawThree = false;

            for (int seed = 0; seed < 50; seed++)
            {
                CosmeticOutfit outfit = k.Service.CreateRandomOutfit(new Random(seed));
                CosmeticSlot socks = outfit.Get(CosmeticCategory.Socks);
                CosmeticSlot shorts = outfit.Get(CosmeticCategory.Shorts);
                Assert.AreEqual(CosmeticTestKit.SocksThree, socks.CosmeticId);
                for (int channel = 0; channel < 3; channel++) Assert.IsNotEmpty(socks.ColorAt(channel));
                for (int channel = 0; channel < 2; channel++) Assert.IsNotEmpty(shorts.ColorAt(channel));
                Assert.IsEmpty(shorts.ColorAt(2), "A two-channel item has no third colour.");
                sawThree |= socks.ColorAt(2).Length > 0;
            }
            Assert.IsTrue(sawThree);
        }

        // --- colour channels -------------------------------------------------------------------------------------

        [Test]
        public void Channels_ComeFromTheCatalog_PrimaryFirst_WithDefaultColours()
        {
            var k = Kit(0);

            Assert.IsTrue(k.Service.TryGetItem(CosmeticTestKit.SocksThree, out CosmeticItemInfo three));
            Assert.AreEqual(3, three.ChannelCount);
            CollectionAssert.AreEqual(CosmeticTestKit.SocksThreeDefaults, three.ChannelDefaults);

            Assert.IsTrue(k.Service.TryGetItem(CosmeticTestKit.ShortsTwo, out CosmeticItemInfo two));
            Assert.AreEqual(2, two.ChannelCount);

            Assert.IsTrue(k.Service.TryGetItem(CosmeticTestKit.HatA, out CosmeticItemInfo one));
            Assert.AreEqual(1, one.ChannelCount, "An asset without a channel list counts its primary channel only.");
            Assert.AreEqual(string.Empty, one.DefaultColorAt(0), "No default is known, and none is invented.");
        }

        [Test]
        public void ChannelColours_SaveAndSurviveRestart_PerChannel()
        {
            var k = Kit(0);
            var slot = new CosmeticSlot(CosmeticTestKit.SocksThree, "#aa0000", "00bb00", "0000cc");

            Assert.AreEqual(CosmeticSaveResult.Saved, k.Service.SaveOutfit(1, CosmeticOutfit.Empty.WithSlot(CosmeticCategory.Socks, slot)));
            k.Open();

            CosmeticSlot loaded = k.Service.GetOutfit(1).Get(CosmeticCategory.Socks);
            Assert.AreEqual("AA0000", loaded.ColorAt(0));
            Assert.AreEqual("00BB00", loaded.ColorAt(1));
            Assert.AreEqual("0000CC", loaded.ColorAt(2));
        }

        [Test]
        public void ChannelColours_UnsetChannelStaysUnset_WhenAnotherIsSaved()
        {
            var k = Kit(0);
            var outfit = CosmeticOutfit.Empty.With(CosmeticCategory.Shorts, CosmeticTestKit.ShortsTwo).WithColor(CosmeticCategory.Shorts, 1, "123456");

            k.Service.SaveOutfit(0, outfit);
            k.Open();

            CosmeticSlot loaded = k.Service.GetOutfit(0).Get(CosmeticCategory.Shorts);
            Assert.AreEqual(string.Empty, loaded.ColorAt(0), "The first channel was never set: it keeps its material's colour.");
            Assert.AreEqual("123456", loaded.ColorAt(1));
        }

        [Test]
        public void ChannelColours_BadColourText_IsDroppedOnSave()
        {
            var k = Kit(0);
            var slot = new CosmeticSlot(CosmeticTestKit.SocksThree, "AA0000", "nonsense", "0000CC");

            k.Service.SaveOutfit(0, CosmeticOutfit.Empty.WithSlot(CosmeticCategory.Socks, slot));
            k.Open();

            CosmeticSlot loaded = k.Service.GetOutfit(0).Get(CosmeticCategory.Socks);
            Assert.AreEqual("AA0000", loaded.ColorAt(0));
            Assert.AreEqual(string.Empty, loaded.ColorAt(1));
            Assert.AreEqual("0000CC", loaded.ColorAt(2));
        }

        [Test]
        public void SavedColours_AreJsonWithTheOldKeyForThePrimaryChannel()
        {
            // The primary colour keeps its old key, so a build that only knows one colour still reads it.
            var k = Kit(0);
            k.Service.SaveOutfit(0, CosmeticOutfit.Empty.WithSlot(CosmeticCategory.Socks,
                new CosmeticSlot(CosmeticTestKit.SocksThree, "AA0000", "00BB00", "0000CC")));

            Assert.IsTrue(k.Store.TryRead(ProgressRepository.SaveKey, out string json));
            StringAssert.Contains("\"colorHex\":\"AA0000\"", json);
        }

        [Test]
        public void Migration_OldSingleColour_BecomesThePrimaryChannelsColour()
        {
            var k = Kit(0);
            // A save from before items had several channels: one colorHex per slot, no further colours. HatA's primary is Color2.
            k.Store.Write(ProgressRepository.SaveKey,
                "{\"schemaVersion\":2,\"cosmeticPresets\":[{\"slots\":[" +
                "{\"category\":\"Headwear\",\"cosmeticId\":\"" + CosmeticTestKit.HatA + "\",\"colorHex\":\"FF8800\"}," +
                "{\"category\":\"Socks\",\"cosmeticId\":\"" + CosmeticTestKit.SocksThree + "\",\"colorHex\":\"00FF00\"}]}]}");

            k.Open();

            CosmeticSlot hat = k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear);
            Assert.AreEqual("FF8800", hat.ColorAt(0));
            Assert.AreEqual(string.Empty, hat.ColorAt(1));
            CosmeticSlot socks = k.Service.GetOutfit(0).Get(CosmeticCategory.Socks);
            Assert.AreEqual("00FF00", socks.ColorAt(0));
            Assert.AreEqual(string.Empty, socks.ColorAt(1));
            Assert.AreEqual(string.Empty, socks.ColorAt(2));

            // And the plan paints that colour on the item's primary material only.
            var plan = SpeedStars.Meta.Cosmetics.OutfitPlan.Build(k.Service.GetOutfit(0), k.Catalog);
            Assert.IsTrue(plan.Get(CosmeticCategory.Headwear).TryGetColor(CosmeticColorChannel.Color2.MaterialName(), out string onPrimary));
            Assert.AreEqual("FF8800", onPrimary);
            Assert.AreEqual(1, plan.Get(CosmeticCategory.Headwear).Colors.Count);
            Assert.AreEqual(1, plan.Get(CosmeticCategory.Socks).Colors.Count);
            Assert.IsTrue(plan.Get(CosmeticCategory.Socks).TryGetColor(CosmeticColorChannel.Color2.MaterialName(), out _));
        }

        [Test]
        public void Migration_OldSingleColour_ResavedKeepsItsColour()
        {
            var k = Kit(0);
            k.Store.Write(ProgressRepository.SaveKey,
                "{\"schemaVersion\":2,\"cosmeticPresets\":[{\"slots\":[" +
                "{\"category\":\"Headwear\",\"cosmeticId\":\"" + CosmeticTestKit.HatA + "\",\"colorHex\":\"FF8800\"}]}]}");
            k.Open();

            k.Service.SaveOutfit(0, k.Service.GetOutfit(0).WithColor(CosmeticCategory.Headwear, 0, "FF8800"));
            k.Open();

            Assert.AreEqual("FF8800", k.Service.GetOutfit(0).Get(CosmeticCategory.Headwear).ColorAt(0));
        }

        [Test]
        public void Random_PaletteDropsInvalidEntries()
        {
            var k = Kit(0);

            CollectionAssert.AreEqual(new[] { "FF0000", "00FF00" }, k.Service.ColorPalette);
        }

        [Test]
        public void Random_SameSeedSameOutfit_BoughtItemCanBePicked()
        {
            var k = Kit(CosmeticTestKit.PremiumPrice);
            Assert.AreEqual(k.Service.CreateRandomOutfit(new Random(5)), k.Service.CreateRandomOutfit(new Random(5)));

            k.Service.TryPurchase(CosmeticTestKit.HatPremium);
            bool picked = false;
            for (int seed = 0; seed < 200 && !picked; seed++)
                picked = k.Service.CreateRandomOutfit(new Random(seed)).Get(CosmeticCategory.Headwear).CosmeticId == CosmeticTestKit.HatPremium;

            Assert.IsTrue(picked, "Once owned, a premium item is part of the random pool.");
        }

        [Test]
        public void Random_NeedsARandomSource()
        {
            var k = Kit(0);

            Assert.Throws<ArgumentNullException>(() => k.Service.CreateRandomOutfit(null));
        }
    }
}
