using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Costume;

namespace SpeedStars.Tests.UI.Costume
{
    public sealed class CostumeEditorModelTests
    {
        private const string HatA = "hat-a";
        private const string HatB = "hat-b";
        private const string HatPremium = "hat-premium";
        private const string TopA = "top-a";
        private const string Red = "FF0000";
        private const string Blue = "0000FF";
        private const string Green = "00FF00";

        private FakeCosmeticService _service;
        private CostumeEditorModel _model;

        [SetUp]
        public void SetUp()
        {
            _service = new FakeCosmeticService();
            _model = new CostumeEditorModel(_service);
            _model.SelectCategory(CosmeticCategory.Headwear);
            _model.Begin();
        }

        // ---- session start ----

        [Test]
        public void BeginShowsTheSavedOutfitOfTheSelectedPreset()
        {
            _service.Saved[1] = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatB);
            _service.SelectPreset(1);

            _model.Begin();

            Assert.AreEqual(1, _model.PresetIndex);
            Assert.AreEqual(HatB, _model.Outfit.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.IsFalse(_model.IsDirty);
        }

        // ---- trying items on ----

        [Test]
        public void TappingAnItemWearsItAndMakesTheOutfitDirty()
        {
            ItemTapOutcome outcome = _model.TapItem(HatB);

            Assert.AreEqual(ItemTapOutcome.Worn, outcome);
            Assert.AreEqual(HatB, _model.SelectedSlot.CosmeticId);
            Assert.IsTrue(_model.IsDirty);
        }

        [Test]
        public void TappingTheWornItemTakesItOff()
        {
            _model.TapItem(HatB);

            ItemTapOutcome outcome = _model.TapItem(HatB);

            Assert.AreEqual(ItemTapOutcome.Removed, outcome);
            Assert.IsTrue(_model.SelectedSlot.IsEmpty);
            Assert.IsFalse(_model.CanColor, "nothing worn, nothing to recolour");
        }

        [Test]
        public void AnItemOfAnotherCategoryOrAnUnknownIdChangesNothing()
        {
            CosmeticOutfit before = _model.Outfit;

            Assert.AreEqual(ItemTapOutcome.None, _model.TapItem(TopA));
            Assert.AreEqual(ItemTapOutcome.None, _model.TapItem("no-such-item"));

            Assert.AreEqual(before, _model.Outfit);
            Assert.IsFalse(_model.IsDirty);
        }

        [Test]
        public void ANewItemKeepsTheColourOfTheCategory()
        {
            _model.TapItem(HatA);
            _model.SetColor(Red);

            _model.TapItem(HatB);

            Assert.AreEqual(HatB, _model.SelectedSlot.CosmeticId);
            Assert.AreEqual(Red, _model.SelectedSlot.ColorHex);
        }

        [Test]
        public void ColourChangesTheWornItemOnly()
        {
            _model.TapItem(HatA);

            Assert.IsTrue(_model.SetColor(Blue));

            Assert.AreEqual(Blue, _model.Outfit.Get(CosmeticCategory.Headwear).ColorHex);
            Assert.IsTrue(_model.IsDirty);
        }

        [Test]
        public void ABareSlotCannotBeRecoloured()
        {
            Assert.IsFalse(_model.SetColor(Blue));
            Assert.IsFalse(_model.IsDirty);
        }

        // ---- colour channels ----

        [Test]
        public void TheBoxesFollowTheWornItemsChannels()
        {
            Assert.AreEqual(0, _model.ChannelCount, "a bare slot has no boxes");

            _model.TapItem(HatA);
            Assert.AreEqual(FakeCosmeticService.HatAChannels, _model.ChannelCount);

            _model.TapItem(HatA); // off
            _model.TapItem(HatB);
            Assert.AreEqual(1, _model.ChannelCount);
        }

        [Test]
        public void AnItemWithUnknownChannelsHasOneBox()
        {
            _model.TapItem(HatPremium);

            Assert.AreEqual(1, _model.ChannelCount);
        }

        [Test]
        public void AFreshItemShowsTheMaterialDefaultsNotAPlaceholder()
        {
            _model.TapItem(HatA);
            CosmeticOutfit worn = _model.Outfit; // wearing the item is the edit; reading its colours must add none

            for (int i = 0; i < _model.ChannelCount; i++)
            {
                Assert.AreEqual(FakeCosmeticService.HatADefaults[i], _model.ChannelHex(i));
                Assert.IsNotEmpty(_model.ChannelHex(i));
            }
            Assert.AreEqual(worn, _model.Outfit, "showing defaults is not an edit");
            Assert.AreEqual(string.Empty, _model.SelectedSlot.ColorAt(0), "no colour was written");
        }

        [Test]
        public void ThePickerEditsOnlyTheSelectedChannel()
        {
            _model.TapItem(HatA);
            Assert.AreEqual(0, _model.ChannelIndex);

            Assert.IsTrue(_model.SelectChannel(1));
            Assert.IsTrue(_model.SetColor(Green));

            CosmeticSlot slot = _model.SelectedSlot;
            Assert.AreEqual(Green, slot.ColorAt(1));
            Assert.AreEqual(string.Empty, slot.ColorAt(0), "the first channel was not touched");
            Assert.AreEqual(Green, _model.ChannelHex(1));
            Assert.AreEqual(FakeCosmeticService.HatADefaults[0], _model.ChannelHex(0), "an unset channel still shows its default");
            Assert.IsTrue(_model.IsDirty);
        }

        [Test]
        public void EachChannelKeepsItsOwnColour()
        {
            _model.TapItem(HatA);
            _model.SetColor(Red);
            _model.SelectChannel(1);
            _model.SetColor(Blue);
            _model.SelectChannel(0);

            Assert.AreEqual(Red, _model.ChannelHex(0));
            Assert.AreEqual(Blue, _model.ChannelHex(1));
            _model.SetColor(Green);
            Assert.AreEqual(Green, _model.ChannelHex(0));
            Assert.AreEqual(Blue, _model.ChannelHex(1));
        }

        [Test]
        public void EveryBoxShowsItsChannelsColourWhicheverBoxIsSelected()
        {
            _model.TapItem(HatA);
            _model.SetColor(Red);
            _model.SelectChannel(1);

            Assert.AreEqual(Red, _model.ChannelHex(0), "the edited colour stays on its box after switching");
            Assert.AreEqual(FakeCosmeticService.HatADefaults[1], _model.ChannelHex(1), "an untouched channel shows its default");

            _service.RandomOutfit = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatA, Blue);
            _model.Randomize(new Random(1));
            Assert.AreEqual(Blue, _model.ChannelHex(0));
            Assert.AreEqual(FakeCosmeticService.HatADefaults[1], _model.ChannelHex(1));
        }

        [Test]
        public void AChannelTheItemDoesNotHaveCannotBeSelected()
        {
            _model.TapItem(HatB);

            Assert.IsFalse(_model.SelectChannel(1));
            Assert.IsFalse(_model.SelectChannel(-1));
            Assert.AreEqual(0, _model.ChannelIndex);
        }

        [Test]
        public void ChangingItemOrTabGoesBackToTheFirstChannel()
        {
            _model.TapItem(HatA);
            _model.SelectChannel(1);

            _model.TapItem(HatB);
            Assert.AreEqual(0, _model.ChannelIndex);

            _model.TapItem(HatA);
            _model.SelectChannel(1);
            _model.SelectCategory(CosmeticCategory.Tops);
            Assert.AreEqual(0, _model.ChannelIndex);
        }

        [Test]
        public void ANewItemKeepsTheColoursOfEveryChannel()
        {
            _model.TapItem(HatA);
            _model.SetColor(Red);
            _model.SelectChannel(1);
            _model.SetColor(Blue);

            _model.TapItem(HatPremium);

            Assert.AreEqual(Red, _model.SelectedSlot.ColorAt(0));
            Assert.AreEqual(Blue, _model.SelectedSlot.ColorAt(1));
        }

        [Test]
        public void SavingKeepsTheColourOfEveryChannel()
        {
            _model.TapItem(HatA);
            _model.SetColor(Red);
            _model.SelectChannel(1);
            _model.SetColor(Blue);

            Assert.AreEqual(CosmeticSaveResult.Saved, _model.Save());

            CosmeticSlot saved = _service.Saved[_model.PresetIndex].Get(CosmeticCategory.Headwear);
            Assert.AreEqual(Red, saved.ColorAt(0));
            Assert.AreEqual(Blue, saved.ColorAt(1));
        }

        [Test]
        public void ARandomOutfitOrRevertGoesBackToTheFirstChannel()
        {
            _model.TapItem(HatA);
            _model.SelectChannel(1);
            _service.RandomOutfit = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatA, Blue);

            _model.Randomize(new Random(1));
            Assert.AreEqual(0, _model.ChannelIndex);

            _model.SelectChannel(1);
            _model.Revert();
            Assert.AreEqual(0, _model.ChannelIndex);
        }

        // ---- locked items and purchase ----

        [Test]
        public void ALockedItemCanBeWornButNotSaved()
        {
            _model.TapItem(HatPremium);

            Assert.IsTrue(_model.HasLockedItem);
            Assert.IsFalse(_model.CanSave);
            Assert.AreEqual(CosmeticSaveResult.LockedItem, _model.Save());
            Assert.AreEqual(0, _service.SaveCalls, "a refused save never reaches the service");
            Assert.IsTrue(_model.IsDirty);
        }

        [Test]
        public void OnlyALockedPremiumItemOffersAPurchase()
        {
            _model.TapItem(HatA);
            Assert.IsFalse(_model.TryGetBuyableItem(out _), "a normal item is not for sale here");

            _model.TapItem(HatA); // off
            _model.TapItem(HatPremium);

            Assert.IsTrue(_model.TryGetBuyableItem(out CosmeticItemInfo item));
            Assert.AreEqual(HatPremium, item.Id);
        }

        [Test]
        public void PurchaseWithTooFewGemsChangesNothing()
        {
            _service.Gems = FakeCosmeticService.PremiumPrice - 1;
            _model.TapItem(HatPremium);

            CosmeticPurchaseResult result = _model.Purchase();

            Assert.AreEqual(CosmeticPurchaseResult.NotEnough, result);
            Assert.IsTrue(_model.HasLockedItem);
            Assert.AreEqual(FakeCosmeticService.PremiumPrice - 1, _service.Gems);
        }

        [Test]
        public void ABoughtItemUnlocksAndCanBeSaved()
        {
            _service.Gems = FakeCosmeticService.PremiumPrice + 3;
            _model.TapItem(HatPremium);

            CosmeticPurchaseResult result = _model.Purchase();

            Assert.AreEqual(CosmeticPurchaseResult.Purchased, result);
            Assert.AreEqual(3, _service.Gems, "the price was charged once");
            Assert.IsFalse(_model.HasLockedItem);
            Assert.IsTrue(_model.CanSave);
            Assert.IsFalse(_model.TryGetBuyableItem(out _), "an owned item is no longer offered");
        }

        [Test]
        public void PurchaseWithNothingBuyableDoesNothing()
        {
            Assert.AreEqual(CosmeticPurchaseResult.UnknownItem, _model.Purchase());
            Assert.AreEqual(0, _service.PurchaseCalls);
        }

        // ---- saving ----

        [Test]
        public void SaveWritesTheOutfitToThePresetAndClearsDirty()
        {
            _model.TapItem(HatB);
            _model.SetColor(Red);

            CosmeticSaveResult result = _model.Save();

            Assert.AreEqual(CosmeticSaveResult.Saved, result);
            Assert.AreEqual(_model.Outfit, _service.Saved[_model.PresetIndex]);
            Assert.IsFalse(_model.IsDirty);
        }

        [Test]
        public void RevertDropsUnsavedChanges()
        {
            CosmeticOutfit saved = _model.Outfit;
            _model.TapItem(HatB);

            _model.Revert();

            Assert.AreEqual(saved, _model.Outfit);
            Assert.IsFalse(_model.IsDirty);
        }

        [Test]
        public void ARefusedSaveKeepsTheOutfitDirty()
        {
            _model.TapItem(HatB);
            _service.SaveResult = CosmeticSaveResult.InvalidPreset;

            Assert.AreEqual(CosmeticSaveResult.InvalidPreset, _model.Save());

            Assert.IsTrue(_model.IsDirty);
        }

        // ---- presets and random ----

        [Test]
        public void ChoosingAnotherPresetShowsItsSavedOutfitAndDropsTheTryOn()
        {
            _service.Saved[2] = CosmeticOutfit.Empty.With(CosmeticCategory.Tops, TopA);
            _model.TapItem(HatB);

            bool chosen = _model.SelectPreset(2);

            Assert.IsTrue(chosen);
            Assert.AreEqual(2, _model.PresetIndex);
            Assert.AreEqual(2, _service.SelectedPreset);
            Assert.AreEqual(TopA, _model.Outfit.Get(CosmeticCategory.Tops).CosmeticId);
            Assert.IsTrue(_model.Outfit.Get(CosmeticCategory.Headwear).IsEmpty);
            Assert.IsFalse(_model.IsDirty);
        }

        [Test]
        public void ABadPresetIndexChangesNothing()
        {
            _model.TapItem(HatB);

            Assert.IsFalse(_model.SelectPreset(_service.PresetCount));

            Assert.AreEqual(0, _model.PresetIndex);
            Assert.AreEqual(HatB, _model.SelectedSlot.CosmeticId);
        }

        [Test]
        public void RandomWearsTheServicesRandomOutfitWithoutSavingIt()
        {
            _service.RandomOutfit = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatB, Blue).With(CosmeticCategory.Tops, TopA);
            CosmeticOutfit savedBefore = _service.Saved[0];

            _model.Randomize(new Random(1));

            Assert.AreEqual(_service.RandomOutfit, _model.Outfit);
            Assert.IsTrue(_model.IsDirty);
            Assert.AreEqual(savedBefore, _service.Saved[0], "Random is a preview until Save");
            Assert.AreEqual(1, _service.RandomCalls);
        }

        [Test]
        public void ARandomOutfitWithOwnedItemsCanBeSaved()
        {
            _service.RandomOutfit = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatB, Blue);
            _model.Randomize(new Random(1));

            Assert.IsTrue(_model.CanSave);
            Assert.AreEqual(CosmeticSaveResult.Saved, _model.Save());
        }

        // ---- shake ----

        [Test]
        public void ShakeStartsAndEndsAtRest()
        {
            Assert.AreEqual(0f, ShakeCurve.Offset(0f, 10f, 4));
            Assert.AreEqual(0f, ShakeCurve.Offset(1f, 10f, 4));
            Assert.AreEqual(0f, ShakeCurve.Offset(1.5f, 10f, 4));
        }

        [Test]
        public void ShakeSwingsBothWaysAndDiesDown()
        {
            const float strength = 10f;
            float[] samples = Enumerable.Range(1, 99).Select(i => ShakeCurve.Offset(i / 100f, strength, 4)).ToArray();

            Assert.IsTrue(samples.Any(x => x > 0f));
            Assert.IsTrue(samples.Any(x => x < 0f));
            for (int i = 0; i < samples.Length; i++)
                Assert.LessOrEqual(Math.Abs(samples[i]), strength * (1f - (i + 1) / 100f) + 0.0001f);
        }

        [Test]
        public void ShakeScalesWithStrength()
        {
            float small = ShakeCurve.Offset(0.1f, 5f, 4);
            float big = ShakeCurve.Offset(0.1f, 10f, 4);

            Assert.AreEqual(small * 2f, big, 0.0001f);
        }

        // ---- skin (Body tab) ----

        private const string DefaultSkin = "462B27";
        private const string LightSkin = "FFDFC4";

        private CostumeEditorModel NewSkinModel()
        {
            var model = new CostumeEditorModel(_service, DefaultSkin);
            model.Begin();
            model.SelectSkin();
            return model;
        }

        [Test]
        public void TabRulesGiveEachTabItsBehaviour()
        {
            Assert.AreEqual(CostumeTabKind.Items, CostumeTabRules.KindOf(CostumeTab.Top), "Top is the old Body tab: the Tops list");
            Assert.AreEqual(CostumeTabKind.Skin, CostumeTabRules.KindOf(CostumeTab.Body));
            Assert.AreEqual(CostumeTabKind.Emote, CostumeTabRules.KindOf(CostumeTab.Emote));
            foreach (CostumeTab tab in Enum.GetValues(typeof(CostumeTab)))
            {
                if (tab == CostumeTab.Body || tab == CostumeTab.Emote) continue;
                Assert.AreEqual(CostumeTabKind.Items, CostumeTabRules.KindOf(tab), tab.ToString());
            }
        }

        [Test]
        public void TabNumbersStayCompatibleWithSavedAssets()
        {
            // Config assets and prefabs store the numbers: the old Body tab (2) is Top now, the new one takes a free number.
            Assert.AreEqual(2, (int)CostumeTab.Top);
            Assert.AreEqual(8, (int)CostumeTab.Emote);
            Assert.AreEqual(9, (int)CostumeTab.Body);
        }

        [Test]
        public void SkinModeColoursTheSkinEvenWithNothingWorn()
        {
            CostumeEditorModel model = NewSkinModel();

            Assert.IsTrue(model.SkinMode);
            Assert.IsTrue(model.CanColor);
            Assert.AreEqual(1, model.ChannelCount);
            Assert.AreEqual(DefaultSkin, model.ChannelHex(0), "no skin saved: the box shows the catalog default");
        }

        [Test]
        public void ASkinColourGoesToTheOutfitAndMakesItDirty()
        {
            CostumeEditorModel model = NewSkinModel();

            Assert.IsTrue(model.SetSkinColor(LightSkin.ToLowerInvariant()));

            Assert.AreEqual(LightSkin, model.Outfit.SkinColorHex);
            Assert.AreEqual(LightSkin, model.SkinHex);
            Assert.IsTrue(model.IsSkinColor("#" + LightSkin));
            Assert.IsFalse(model.IsSkinColor(DefaultSkin));
            Assert.IsTrue(model.IsDirty);
        }

        [Test]
        public void TheColourPanelInSkinModeChangesTheSkinNotTheItem()
        {
            _model.TapItem(HatA);
            _model.SelectSkin();

            Assert.IsTrue(_model.SetColor(Blue));

            Assert.AreEqual(Blue, _model.Outfit.SkinColorHex);
            Assert.AreNotEqual(Blue, _model.Outfit.Get(CosmeticCategory.Headwear).ColorHex);
        }

        [Test]
        public void NotAColourIsRefused()
        {
            CostumeEditorModel model = NewSkinModel();

            Assert.IsFalse(model.SetSkinColor("zzz"));
            Assert.IsFalse(model.SetSkinColor(string.Empty));

            Assert.IsFalse(model.IsDirty);
        }

        [Test]
        public void SavingKeepsTheSkinWithTheOutfit()
        {
            CostumeEditorModel model = NewSkinModel();
            model.SetSkinColor(LightSkin);

            Assert.AreEqual(CosmeticSaveResult.Saved, model.Save());

            Assert.AreEqual(LightSkin, _service.Saved[0].SkinColorHex);
            Assert.IsFalse(model.IsDirty);
        }

        [Test]
        public void RevertDropsAnUnsavedSkin()
        {
            CostumeEditorModel model = NewSkinModel();
            model.SetSkinColor(LightSkin);

            model.Revert();

            Assert.AreEqual(DefaultSkin, model.SkinHex);
            Assert.IsFalse(model.IsDirty);
        }

        [Test]
        public void ClothesAndRandomKeepTheSkin()
        {
            CostumeEditorModel model = NewSkinModel();
            model.SetSkinColor(LightSkin);
            model.SelectCategory(CosmeticCategory.Headwear);

            model.TapItem(HatA);
            Assert.AreEqual(LightSkin, model.Outfit.SkinColorHex, "wearing an item keeps the skin");

            _service.RandomOutfit = CosmeticOutfit.Empty.With(CosmeticCategory.Headwear, HatB);
            model.Randomize(new Random(1));
            Assert.AreEqual(HatB, model.Outfit.Get(CosmeticCategory.Headwear).CosmeticId);
            Assert.AreEqual(LightSkin, model.Outfit.SkinColorHex, "Random is clothes only");
        }

        [Test]
        public void PickingACategoryLeavesSkinMode()
        {
            CostumeEditorModel model = NewSkinModel();

            model.SelectCategory(CosmeticCategory.Headwear);

            Assert.IsFalse(model.SkinMode);
            Assert.IsFalse(model.CanColor, "bare slot again");
        }

        [Test]
        public void SkinModeOffersNoPurchase()
        {
            _model.TapItem(HatPremium);
            Assert.IsTrue(_model.TryGetBuyableItem(out _), "the premium hat is buyable while its tab is open");

            _model.SelectSkin();

            Assert.IsFalse(_model.TryGetBuyableItem(out _));
        }

        // ---- fake ----

        /// <summary>The cosmetic contract over a handful of test-chosen items; prices are arbitrary test inputs.</summary>
        private sealed class FakeCosmeticService : ICosmeticService
        {
            public const int PremiumPrice = 12;

            // HatA has two colour channels with these default colours; every other item is left without channel information.
            public const int HatAChannels = 2;
            public static readonly string[] HatADefaults = { "A0A0A0", "B0B0B0" };

            private readonly List<(string id, CosmeticCategory category, bool premium, int price)> _catalog =
                new List<(string, CosmeticCategory, bool, int)>
                {
                    (HatPremium, CosmeticCategory.Headwear, true, PremiumPrice),
                    (HatA, CosmeticCategory.Headwear, false, 0),
                    (HatB, CosmeticCategory.Headwear, false, 0),
                    (TopA, CosmeticCategory.Tops, false, 0),
                };

            private readonly HashSet<string> _bought = new HashSet<string>();
            private int _selected;

            /// <summary>Saved outfit per preset; every preset starts bare.</summary>
            public readonly CosmeticOutfit[] Saved = { CosmeticOutfit.Empty, CosmeticOutfit.Empty, CosmeticOutfit.Empty };
            public int Gems;
            public int SaveCalls;
            public int PurchaseCalls;
            public int RandomCalls;
            public CosmeticSaveResult? SaveResult;
            public CosmeticOutfit RandomOutfit = CosmeticOutfit.Empty;

            public event Action Changed;

            public int PresetCount => Saved.Length;
            public string GetPresetName(int index) => "Custom " + (index + 1);
            public int SelectedPreset => _selected;

            public bool SelectPreset(int index)
            {
                if (index < 0 || index >= PresetCount) return false;
                _selected = index;
                Changed?.Invoke();
                return true;
            }

            public CosmeticOutfit GetOutfit(int presetIndex) =>
                presetIndex >= 0 && presetIndex < PresetCount ? Saved[presetIndex] : CosmeticOutfit.Empty;

            public CosmeticOutfit SelectedOutfit => GetOutfit(_selected);
            public CosmeticOutfit DefaultOutfit => CosmeticOutfit.Empty;

            public CosmeticSaveResult SaveOutfit(int presetIndex, CosmeticOutfit outfit)
            {
                SaveCalls++;
                if (SaveResult.HasValue) return SaveResult.Value;
                if (presetIndex < 0 || presetIndex >= PresetCount) return CosmeticSaveResult.InvalidPreset;
                foreach (CosmeticCategory category in CosmeticOutfit.Categories)
                {
                    CosmeticSlot slot = outfit.Get(category);
                    if (!slot.IsEmpty && IsLocked(slot.CosmeticId)) return CosmeticSaveResult.LockedItem;
                }
                Saved[presetIndex] = outfit;
                Changed?.Invoke();
                return CosmeticSaveResult.Saved;
            }

            public IReadOnlyList<CosmeticItemInfo> GetItems(CosmeticCategory category)
            {
                var items = new List<CosmeticItemInfo>();
                foreach (var entry in _catalog)
                    if (entry.category == category) items.Add(Info(entry));
                return items;
            }

            public bool TryGetItem(string cosmeticId, out CosmeticItemInfo item)
            {
                foreach (var entry in _catalog)
                {
                    if (entry.id != cosmeticId) continue;
                    item = Info(entry);
                    return true;
                }
                item = default;
                return false;
            }

            public bool IsOwned(string cosmeticId) => TryGetItem(cosmeticId, out CosmeticItemInfo item) && item.IsOwned;

            public bool IsLocked(string cosmeticId) => TryGetItem(cosmeticId, out CosmeticItemInfo item) && !item.IsOwned;

            public CosmeticPurchaseResult TryPurchase(string cosmeticId)
            {
                PurchaseCalls++;
                if (!TryGetItem(cosmeticId, out CosmeticItemInfo item)) return CosmeticPurchaseResult.UnknownItem;
                if (item.IsOwned) return CosmeticPurchaseResult.AlreadyOwned;
                if (!item.IsForSale) return CosmeticPurchaseResult.NotForSale;
                if (Gems < item.Price) return CosmeticPurchaseResult.NotEnough;
                Gems -= item.Price;
                _bought.Add(cosmeticId);
                Changed?.Invoke();
                return CosmeticPurchaseResult.Purchased;
            }

            public IReadOnlyList<string> ColorPalette => new[] { Red, Blue };

            public CosmeticOutfit CreateRandomOutfit(Random random)
            {
                RandomCalls++;
                return RandomOutfit;
            }

            private CosmeticItemInfo Info((string id, CosmeticCategory category, bool premium, int price) entry)
            {
                bool owned = !entry.premium || _bought.Contains(entry.id);
                return new CosmeticItemInfo(entry.id, entry.category, entry.id, entry.premium, entry.price, CosmeticCurrency.PurpleGem, owned,
                    entry.id == HatA ? HatADefaults : entry.id == HatB ? new[] { "C0C0C0" } : null);
            }
        }
    }
}
