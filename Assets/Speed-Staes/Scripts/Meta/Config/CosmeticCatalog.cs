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
