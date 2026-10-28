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

        // ---- fake ----

        /// <summary>The cosmetic contract over a handful of test-chosen items; prices are arbitrary test inputs.</summary>
        private sealed class FakeCosmeticService : ICosmeticService
        {
            public const int PremiumPrice = 12;

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
                return new CosmeticItemInfo(entry.id, entry.category, entry.id, entry.premium, entry.price, CosmeticCurrency.PurpleGem, owned);
            }
        }
    }
}
