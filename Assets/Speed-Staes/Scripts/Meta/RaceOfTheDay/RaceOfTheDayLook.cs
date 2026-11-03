using System;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.DailyRace
{
    /// <summary>One ready-made racer of the pool: the items worn (by catalog id, with colour) and the skin tone.</summary>
    [Serializable]
    public sealed class RaceOfTheDayLook
    {
        [Tooltip("Name of the look (debugging, tests).")]
        [SerializeField] private string label = string.Empty;

        [Tooltip("Items worn, one per category (catalog ids; the player does not have to own them). A category left out is bare.")]
        [SerializeField] private CosmeticOutfitEntry[] items = new CosmeticOutfitEntry[0];

        [Tooltip("Skin tone as RRGGBB. Empty = the catalog's default skin.")]
        [SerializeField] private string skinColorHex = string.Empty;

        public RaceOfTheDayLook() { }

        public RaceOfTheDayLook(string label, CosmeticOutfitEntry[] items, string skinColorHex)
        {
            this.label = label ?? string.Empty;
            this.items = items ?? new CosmeticOutfitEntry[0];
            this.skinColorHex = skinColorHex ?? string.Empty;
        }

        public string Label => label ?? string.Empty;
        public string SkinColorHex => skinColorHex ?? string.Empty;

        /// <summary>The look as an outfit (a later entry of the same category replaces an earlier one).</summary>
        public CosmeticOutfit ToOutfit()
        {
            CosmeticOutfit outfit = CosmeticOutfit.Empty;
            if (items != null)
                foreach (CosmeticOutfitEntry entry in items)
                    if (entry != null && !string.IsNullOrEmpty(entry.CosmeticId))
                        outfit = outfit.With(entry.Category, entry.CosmeticId, entry.ColorHex);
            return string.IsNullOrEmpty(skinColorHex) ? outfit : outfit.WithSkinColor(skinColorHex);
        }
    }
}
