using System;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>One worn item of the default outfit: the slot, the item and its colour.</summary>
    [Serializable]
    public sealed class CosmeticOutfitEntry
    {
        [Tooltip("Slot this entry fills.")]
        [SerializeField] private CosmeticCategory category;

        [Tooltip("Id of a cosmetic of that category. It must be owned from the start (normal, not premium).")]
        [SerializeField] private string cosmeticId = string.Empty;

        [Tooltip("Colour of the item's primary channel as RRGGBB. Empty = the item's own colours.")]
        [SerializeField] private string colorHex = string.Empty;

        public CosmeticOutfitEntry() { }

        public CosmeticOutfitEntry(CosmeticCategory category, string cosmeticId, string colorHex)
        {
            this.category = category;
            this.cosmeticId = cosmeticId ?? string.Empty;
            this.colorHex = colorHex ?? string.Empty;
        }

        public CosmeticCategory Category => category;
        public string CosmeticId => cosmeticId ?? string.Empty;
        public string ColorHex => colorHex ?? string.Empty;
    }
}
