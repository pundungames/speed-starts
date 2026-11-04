using System.Collections;
using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Costume;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;
using static SpeedStars.PlayTests.PlaySmokeSupport;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// T-104: in the wardrobe a locked, priced card shows its price with the icon of its currency, a real tap on it opens the
    /// purchase button with that currency's icon and the catalog price, and buying a normal item takes Cash and unlocks it.
    /// Items, prices and currencies are read from the live catalog (nothing is hard-coded); the save is restored byte for byte.
    /// </summary>
    public sealed class CostumePriceSmokeTests
    {
        private SaveFolderSnapshot _saveFiles;

        private static DiContainer Container => ProjectContext.Instance.Container;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _saveFiles = SaveFolderSnapshot.Take();
            yield return LoadScene(HomeScene);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _saveFiles.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedNormalItem_ShowsCashPrice_AndARealTapBuysItWithCash()
        {
            ICosmeticService cosmetics = Container.Resolve<ICosmeticService>();
            IWallet wallet = Container.Resolve<IWallet>();
            PlayerProgress progress = Container.Resolve<ProgressRepository>().Current;

            CostumeEditorScreenView screen = null;
            yield return OpenWardrobe(s => screen = s);
            if (!TryFindLocked(screen, CosmeticCurrency.Cash, out CostumeTab tab, out CosmeticItemInfo item))
                Assert.Ignore("The catalog has no locked Cash item.");
            Assert.IsFalse(item.IsPremium, "a Cash item is a normal one");

            progress.Cash = item.Price - 1;
            CostumeItemCardView card = null;
            yield return ShowCard(screen, tab, item, c => card = c);
            AssertCardPrice(card, item, cashIcon: true);

            yield return ScrollIntoView(card);
            yield return Tap(RequireField<Button>(card, "button"), "locked item card");
            Button purchase = RequireField<Button>(screen, "purchaseButton");
            yield return WaitUntil(() => purchase.gameObject.activeSelf, "the purchase button to open on a locked normal item");
            Assert.IsTrue(RequireField<GameObject>(screen, "purchaseCashIcon").activeSelf, "a Cash price wears the Cash icon");
            Assert.IsFalse(RequireField<GameObject>(screen, "purchaseGemIcon").activeSelf, "a Cash price never wears the gem");
            Assert.AreEqual(item.Price.ToString(), RequireField<TMP_Text>(screen, "purchasePriceText").text);

            // Too little Cash: the refusal changes nothing.
            yield return Tap(purchase, "purchase with too little Cash");
            yield return null;
            Assert.AreEqual(item.Price - 1, wallet.Cash, "a refused purchase charges nothing");
            Assert.IsTrue(cosmetics.IsLocked(item.Id), "a refused purchase unlocks nothing");

            // Enough Cash: charged once, unlocked, price strip gone, Save available.
            progress.Cash = item.Price + 7;
            yield return Tap(purchase, "purchase with enough Cash");
            yield return WaitUntil(() => !cosmetics.IsLocked(item.Id), "the item to unlock");
            Assert.AreEqual(7, wallet.Cash, "the catalog price was charged once, in Cash");
            yield return null;
            Assert.IsFalse(RequireField<GameObject>(card, "priceGroup").activeSelf, "an owned card shows no price");
            Assert.IsFalse(purchase.gameObject.activeSelf, "an owned item is no longer offered");
            Assert.IsTrue(RequireField<Button>(screen, "saveButton").interactable, "Save is open once nothing worn is locked");
        }

        [UnityTest]
        public IEnumerator LockedPremiumItem_ShowsGemPrice_OnCardAndOnThePurchaseButton()
        {
            CostumeEditorScreenView screen = null;
            yield return OpenWardrobe(s => screen = s);
            if (!TryFindLocked(screen, CosmeticCurrency.PurpleGem, out CostumeTab tab, out CosmeticItemInfo item))
                Assert.Ignore("The catalog has no locked Purple Gem item.");

            CostumeItemCardView card = null;
            yield return ShowCard(screen, tab, item, c => card = c);
            AssertCardPrice(card, item, cashIcon: false);

            yield return ScrollIntoView(card);
            yield return Tap(RequireField<Button>(card, "button"), "locked premium card");
            Button purchase = RequireField<Button>(screen, "purchaseButton");
            yield return WaitUntil(() => purchase.gameObject.activeSelf, "the purchase button to open on a locked premium item");
            Assert.IsTrue(RequireField<GameObject>(screen, "purchaseGemIcon").activeSelf);
            Assert.IsFalse(RequireField<GameObject>(screen, "purchaseCashIcon").activeSelf);
            Assert.AreEqual(item.Price.ToString(), RequireField<TMP_Text>(screen, "purchasePriceText").text);
        }

        [UnityTest]
        public IEnumerator OwnedAndFreeCards_ShowNoPrice()
        {
            ICosmeticService cosmetics = Container.Resolve<ICosmeticService>();
            CostumeEditorScreenView screen = null;
            yield return OpenWardrobe(s => screen = s);
            if (!TryFindOwnedFree(screen, cosmetics, out CostumeTab tab, out CosmeticItemInfo item))
                Assert.Ignore("The catalog has no owned free item in an item tab.");

            CostumeItemCardView card = null;
            yield return ShowCard(screen, tab, item, c => card = c);
            Assert.IsFalse(RequireField<GameObject>(card, "priceGroup").activeSelf, "an owned free card shows no price");
        }

        // ---------------------------------------------------------------- helpers

        private static IEnumerator OpenWardrobe(System.Action<CostumeEditorScreenView> found)
        {
            HomeScreenView home = Require<HomeScreenView>();
            UiScreen screen = RequireField<UiScreen>(home, "cosmeticsScreen");
            yield return Tap(RequireField<Button>(home, "editRacerButton"), "editRacerButton");
            yield return WaitOpen(screen, "cosmeticsScreen");
            var wardrobe = screen as CostumeEditorScreenView;
            Assert.IsNotNull(wardrobe, "the cosmetics screen is a CostumeEditorScreenView");
            found(wardrobe);
        }

        private static IEnumerator ShowCard(CostumeEditorScreenView screen, CostumeTab tab, CosmeticItemInfo item, System.Action<CostumeItemCardView> found)
        {
            yield return Tap(RequireField<Button>(TabOf(screen, tab), "button"), tab + " tab");
            Transform content = RequireField<Transform>(screen, "itemsContent");
            CostumeItemCardView card = null;
            yield return WaitUntil(() =>
            {
                foreach (CostumeItemCardView candidate in content.GetComponentsInChildren<CostumeItemCardView>(false))
                    if (candidate.CosmeticId == item.Id) { card = candidate; return true; }
                return false;
            }, "the card of " + item.Id);
            yield return null; // RefreshAll runs after the grid is bound
            found(card);
        }

        private static void AssertCardPrice(CostumeItemCardView card, CosmeticItemInfo item, bool cashIcon)
        {
            Assert.IsTrue(RequireField<GameObject>(card, "priceGroup").activeSelf, "a locked priced card shows its price");
            Assert.AreEqual(item.Price.ToString(), RequireField<TMP_Text>(card, "priceText").text);
            Assert.AreEqual(cashIcon, RequireField<GameObject>(card, "priceCashIcon").activeSelf, "Cash icon");
            Assert.AreEqual(!cashIcon, RequireField<GameObject>(card, "priceGemIcon").activeSelf, "gem icon");
        }

        private static bool TryFindLocked(CostumeEditorScreenView screen, CosmeticCurrency currency, out CostumeTab tab, out CosmeticItemInfo found)
        {
            ICosmeticService cosmetics = Container.Resolve<ICosmeticService>();
            var config = RequireField<CostumeEditorConfig>(screen, "config");
            foreach (CostumeTabView view in RequireField<CostumeTabView[]>(screen, "tabs"))
            {
                if (view == null || CostumeTabRules.KindOf(view.Tab) != CostumeTabKind.Items) continue;
                if (!config.TryGetTab(view.Tab, out CostumeTabEntry entry) || !entry.HasContent) continue;
                foreach (CosmeticItemInfo item in cosmetics.GetItems(entry.Category))
                {
                    if (!item.IsLocked || !item.IsForSale || item.Currency != currency) continue;
                    tab = view.Tab;
                    found = item;
                    return true;
                }
            }
            tab = default;
            found = default;
            return false;
        }

        private static bool TryFindOwnedFree(CostumeEditorScreenView screen, ICosmeticService cosmetics, out CostumeTab tab, out CosmeticItemInfo found)
        {
            var config = RequireField<CostumeEditorConfig>(screen, "config");
            foreach (CostumeTabView view in RequireField<CostumeTabView[]>(screen, "tabs"))
            {
                if (view == null || CostumeTabRules.KindOf(view.Tab) != CostumeTabKind.Items) continue;
                if (!config.TryGetTab(view.Tab, out CostumeTabEntry entry) || !entry.HasContent) continue;
                foreach (CosmeticItemInfo item in cosmetics.GetItems(entry.Category))
                {
                    if (item.IsLocked) continue;
                    tab = view.Tab;
                    found = item;
                    return true;
                }
            }
            tab = default;
            found = default;
            return false;
        }

        private static CostumeTabView TabOf(UiScreen screen, CostumeTab tab)
        {
            foreach (CostumeTabView view in RequireField<CostumeTabView[]>(screen, "tabs"))
                if (view != null && view.Tab == tab) return view;
            Assert.Fail($"The costume screen has no '{tab}' tab view bound.");
            return null;
        }
    }
}
