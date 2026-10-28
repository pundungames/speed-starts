using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// One wearable item. It names the prefab objects it turns on instead of referencing them, because the parts live
    /// inside the runner prefab (not ours to edit) and are switched on/off at runtime. Names are relative to
    /// <c>Accessories/&lt;Category&gt;/</c> so the category is stated once; some items are several objects
    /// (a glove set with separate left/right pieces).
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Cosmetic", fileName = "Cosmetic")]
    public sealed class CosmeticDefinition : ScriptableObject
    {
        [Tooltip("Stable id written to saves (owned and selected items). Never change it once players have saves.")]
        [SerializeField] private string cosmeticId;

        [Tooltip("Slot the item goes into; same name as the prefab's Accessories/<Category> object.")]
        [SerializeField] private CosmeticCategory category;

        [Tooltip("Object names under the runner's Accessories/<Category>/ that this item turns on (one or more pieces).")]
        [SerializeField] private string[] partNames = new string[0];

        [Tooltip("Premium items are pinned to the top of their category's list with a highlighted panel.")]
        [SerializeField] private bool isPremium;

        [Tooltip("Price in the currency below. 0 = no price: the item cannot be bought.")]
        [SerializeField, Min(0)] private int price;

        [Tooltip("Currency the price is paid in. Premium items use Purple Gems.")]
        [SerializeField] private CosmeticCurrency currency;

        [Tooltip("Position inside its group (premium or normal) in the category list; lower comes first.")]
        [SerializeField] private int sortOrder;

        public string Id => cosmeticId ?? string.Empty;
        public CosmeticCategory Category => category;
        public IReadOnlyList<string> PartNames => partNames ?? Array.Empty<string>();
        public bool IsPremium => isPremium;
        public int Price => price;
        public CosmeticCurrency Currency => currency;
        public int SortOrder => sortOrder;

        /// <summary>False when the item has no price (e.g. normal items until Cash prices are decided).</summary>
        public bool IsForSale => price > 0;
    }
}
