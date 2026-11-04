using NUnit.Framework;
using SpeedStars.UI.Costume;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.UI.Builders
{
    /// <summary>
    /// T-104: the wardrobe price is built into the prefabs, not made in code. The item card has a Price strip (text + Cash and
    /// gem icons) and the screen's Purchase button has a Cash icon next to the gem one; every new view field is bound to a node
    /// inside its own prefab. Checks structure and binding only, never geometry (the user hand-edits both prefabs). Red until
    /// <c>UiCostumePrefabs.PatchCostumePricePrefabs</c> has run once.
    /// </summary>
    public sealed class CostumePricePrefabTests
    {
        private const string CardPath = "Assets/Speed-Staes/UI/Rows/CostumeItemCard.prefab";
        private const string ScreenPath = "Assets/Speed-Staes/UI/Screens/CosmeticsScreen.prefab";

        [Test]
        public void EmoteCard_PriceCarriesTheCashIcon_SoItShowsAndHidesWithThePrice()
        {
            GameObject root = Load("Assets/Speed-Staes/UI/Rows/EmoteCard.prefab");
            Transform icon = root.transform.Find("Price/CashIcon");
            Assert.IsNotNull(icon, "EmoteCard has no Price/CashIcon");
            var image = new SerializedObject(icon.GetComponent("Image"));
            Assert.IsNotNull(image.FindProperty("m_Sprite").objectReferenceValue, "the Cash icon has no sprite");
        }

        [Test]
        public void ItemCard_HasThePriceStrip_AndEveryPriceFieldIsBoundInsideThePrefab()
        {
            GameObject root = Load(CardPath);
            Assert.IsNotNull(root.transform.Find("Price"), "CostumeItemCard has no Price node");
            var so = new SerializedObject(root.GetComponent<CostumeItemCardView>());
            foreach (string field in new[] { "priceGroup", "priceText", "priceCashIcon", "priceGemIcon" })
                AssertBoundInside(so, field, root);
        }

        [Test]
        public void ItemCard_PriceStripStartsHidden_SoAnOwnedCardNeverFlashesAPrice()
        {
            GameObject root = Load(CardPath);
            Transform price = root.transform.Find("Price");
            Assert.IsNotNull(price);
            Assert.IsFalse(price.gameObject.activeSelf, "the Price group starts hidden; the view shows it for a locked priced item");
        }

        [Test]
        public void PurchaseButton_HasBothCurrencyIcons_BoundOnTheScreenView()
        {
            GameObject root = Load(ScreenPath);
            Transform purchase = root.transform.Find("Purchase");
            Assert.IsNotNull(purchase, "CosmeticsScreen has no Purchase button");
            Assert.IsNotNull(purchase.Find("CashIcon"), "the Purchase button has no Cash icon");
            var so = new SerializedObject(root.GetComponent<CostumeEditorScreenView>());
            AssertBoundInside(so, "purchaseCashIcon", root);
            AssertBoundInside(so, "purchaseGemIcon", root);
            Assert.AreNotSame(so.FindProperty("purchaseCashIcon").objectReferenceValue, so.FindProperty("purchaseGemIcon").objectReferenceValue,
                "the two currency icons are two different objects");
        }

        private static GameObject Load(string path)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(root, $"prefab missing: {path}");
            return root;
        }

        private static void AssertBoundInside(SerializedObject so, string field, GameObject root)
        {
            SerializedProperty property = so.FindProperty(field);
            Assert.IsNotNull(property, $"{so.targetObject.GetType().Name} has no field '{field}'");
            Object target = property.objectReferenceValue;
            Assert.IsNotNull(target, $"{so.targetObject.GetType().Name}.{field} is empty");
            Transform transform = target is Component component ? component.transform : ((GameObject)target).transform;
            Assert.IsTrue(transform.IsChildOf(root.transform), $"{field} points outside the prefab");
        }
    }
}
