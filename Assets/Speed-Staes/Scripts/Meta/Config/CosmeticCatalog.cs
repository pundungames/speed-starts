using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Every cosmetic. The list order of one category is decided here, once: premium items first (pinned to the top,
    /// PDF §2.3), then by SortOrder; equal SortOrder keeps catalog order so the result never shuffles.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Cosmetic Catalog", fileName = "CosmeticCatalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        [Tooltip("All cosmetics, any category. Ids must be unique; empty slots are ignored.")]
        [SerializeField] private CosmeticDefinition[] cosmetics = new CosmeticDefinition[0];

        [Tooltip("How many racer presets (Custom 1..N) the player has. Not in the PDF; raising it adds default-outfit presets to existing saves.")]
        [SerializeField, Min(1)] private int presetCount = 9;

        [Tooltip("Preset name; {0} is the preset number starting at 1.")]
        [SerializeField] private string presetNameFormat = "Custom {0}";

        [Tooltip("What every new preset wears: one entry per worn category. Items must be owned from the start.")]
        [SerializeField] private CosmeticOutfitEntry[] defaultOutfit = new CosmeticOutfitEntry[0];

        [Tooltip("Colours (RRGGBB) the Random button and the colour picker draw from.")]
        [SerializeField] private string[] colorPalette = new string[0];

        [Tooltip("Names of the old base-outfit objects at the runner root (not under Accessories) that are hidden at runtime, since the wardrobe replaces them.")]
        [SerializeField] private string[] legacyOutfitObjects = { "Tshirt", "Short", "Leggings", "Shoes", "Glove", "Helmet" };

        [Tooltip("Skin colour (RRGGBB) of the runner as it is now: the colour of the skin material in the runner prefab. Used when an outfit has no skin colour.")]
        [SerializeField] private string defaultSkinColor = "462B27";

        [Tooltip("Ready-made skin tones (RRGGBB) the costume screen offers, light to dark.")]
        [SerializeField] private string[] skinTones = { "FFDFC4", "F1C27D", "E0AC69", "C68642", "A9693D", "8D5524", "6B4226", "462B27" };

        [Tooltip("Name of the object under the runner root that carries the skin mesh (not under Accessories).")]
        [SerializeField] private string skinRendererName = "LowPoly Male";

        [Tooltip("Name of the skin material on that renderer (the slot is found by name; instances add ' (Instance)').")]
        [SerializeField] private string skinMaterialName = "ColorSkin_Toon";

        public int PresetCount => Mathf.Max(1, presetCount);
        public string PresetNameFormat => string.IsNullOrEmpty(presetNameFormat) ? "Custom {0}" : presetNameFormat;
        public IReadOnlyList<CosmeticOutfitEntry> DefaultOutfit => defaultOutfit ?? Array.Empty<CosmeticOutfitEntry>();
        /// <summary>"RRGGBB" of the skin an outfit without a skin colour gets.</summary>
        public string DefaultSkinColor => defaultSkinColor ?? string.Empty;

        /// <summary>Ready-made skin tones as written in the asset (hex text, not normalized).</summary>
        public IReadOnlyList<string> SkinTones => skinTones ?? Array.Empty<string>();

        public string SkinRendererName => skinRendererName ?? string.Empty;
        public string SkinMaterialName => skinMaterialName ?? string.Empty;
        public IReadOnlyList<string> ColorPalette => colorPalette ?? Array.Empty<string>();
        public IReadOnlyList<string> LegacyOutfitObjects => legacyOutfitObjects ?? Array.Empty<string>();

        /// <summary>The first cosmetic with this id (ordinal); false for an empty or unknown id.</summary>
        public bool TryGet(string cosmeticId, out CosmeticDefinition definition)
        {
            if (!string.IsNullOrEmpty(cosmeticId) && cosmetics != null)
            {
                foreach (var candidate in cosmetics)
                {
                    if (candidate != null && string.Equals(candidate.Id, cosmeticId, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }
            definition = null;
            return false;
        }

        /// <summary>One category in list order: premium first, then SortOrder ascending, ties in catalog order. A new list each call.</summary>
        public IReadOnlyList<CosmeticDefinition> GetByCategory(CosmeticCategory category)
        {
            if (cosmetics == null) return new List<CosmeticDefinition>();
            // LINQ ordering is stable: equal keys keep catalog order.
            return cosmetics
                .Where(c => c != null && c.Category == category)
                .OrderByDescending(c => c.IsPremium)
                .ThenBy(c => c.SortOrder)
                .ToList();
        }

        /// <summary>Ids used by more than one cosmetic (only the first of each can be looked up).</summary>
        public IReadOnlyList<string> FindDuplicateIds() => ConfigIdCheck.FindDuplicates(Ids());

        private IEnumerable<string> Ids()
        {
            if (cosmetics == null) yield break;
            foreach (var definition in cosmetics)
                if (definition != null) yield return definition.Id;
        }

        private void OnValidate() => ConfigIdCheck.Warn(this, "cosmetic", Ids());
    }
}
