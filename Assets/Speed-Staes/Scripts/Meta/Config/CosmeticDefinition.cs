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

        [Tooltip("First colour channel (the material named ColorN_Toon). Only read while 'Channels' below is empty (assets made before items had several channels).")]
        [SerializeField] private CosmeticColorChannel primaryChannel;

        [Tooltip("Every colour channel the player can recolour, in order: the first is the primary one (old saves' single colour). 1-3 entries, no repeats. Seeded from the prefab's materials.")]
        [SerializeField] private CosmeticColorChannel[] channels = new CosmeticColorChannel[0];

        [Tooltip("Default colour (RRGGBB) of each channel above, same order: the material's own colour, shown in the colour boxes until the player picks one. Seeded from Materials/RunnerToon.")]
        [SerializeField] private string[] channelDefaultColors = new string[0];

        public string Id => cosmeticId ?? string.Empty;
        public CosmeticCategory Category => category;
        public IReadOnlyList<string> PartNames => partNames ?? Array.Empty<string>();
        public bool IsPremium => isPremium;
        public int Price => price;
        public CosmeticCurrency Currency => currency;
        public int SortOrder => sortOrder;
        public CosmeticColorChannel PrimaryChannel => Channels[0];

        /// <summary>The colour channels in order, primary first; at least one, at most <c>CosmeticSlot.MaxChannels</c> (extras ignored).</summary>
        public IReadOnlyList<CosmeticColorChannel> Channels
        {
            get
            {
                if (channels == null || channels.Length == 0) return new[] { primaryChannel };
                return channels.Length <= MaxChannels ? (IReadOnlyList<CosmeticColorChannel>)channels : new ArraySegment<CosmeticColorChannel>(channels, 0, MaxChannels);
            }
        }

        /// <summary>Default colour (RRGGBB as written in the asset) of a channel by index; empty when the asset has none.</summary>
        public string DefaultColorAt(int channelIndex) =>
            channelDefaultColors != null && channelIndex >= 0 && channelIndex < channelDefaultColors.Length
                ? channelDefaultColors[channelIndex] ?? string.Empty
                : string.Empty;

        private const int MaxChannels = 3;

        /// <summary>False when the item has no price (e.g. normal items until Cash prices are decided).</summary>
        public bool IsForSale => price > 0;
    }
}
