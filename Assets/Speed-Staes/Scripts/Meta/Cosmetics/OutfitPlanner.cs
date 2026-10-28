using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Cosmetics
{
    /// <summary>What one category slot of the runner must look like.</summary>
    public readonly struct SlotPlan
    {
        public SlotPlan(CosmeticCategory category, IReadOnlyList<string> activeParts, string colorMaterial, string colorHex)
        {
            Category = category;
            ActiveParts = activeParts;
            ColorMaterial = colorMaterial;
            ColorHex = colorHex;
        }

        public CosmeticCategory Category { get; }

        /// <summary>Object names under <c>Accessories/&lt;Category&gt;/</c> to switch on; every other object there goes off.</summary>
        public IReadOnlyList<string> ActiveParts { get; }

        /// <summary>Material (e.g. Color1_Toon) that carries the primary colour; null when there is no colour to write.</summary>
        public string ColorMaterial { get; }

        /// <summary>"RRGGBB" to write into the material's colour; empty = leave the item's own colours.</summary>
        public string ColorHex { get; }

        public bool HasColor => !string.IsNullOrEmpty(ColorMaterial) && !string.IsNullOrEmpty(ColorHex);

        public bool IsActive(string partName)
        {
            if (ActiveParts == null) return false;
            for (int i = 0; i < ActiveParts.Count; i++)
                if (string.Equals(ActiveParts[i], partName, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    /// <summary>
    /// The engine-free half of the applier: from an outfit and the catalog, which parts of each slot are on and which
    /// channel gets which colour. The MonoBehaviour only carries this out on the runner's objects.
    /// </summary>
    public sealed class OutfitPlan
    {
        private readonly SlotPlan[] _slots;

        private OutfitPlan(SlotPlan[] slots, IReadOnlyList<string> legacyObjects)
        {
            _slots = slots;
            LegacyObjects = legacyObjects;
        }

        /// <summary>One plan per category, in enum order.</summary>
        public IReadOnlyList<SlotPlan> Slots => _slots;

        /// <summary>Old base-outfit objects at the runner root that must be hidden.</summary>
        public IReadOnlyList<string> LegacyObjects { get; }

        public SlotPlan Get(CosmeticCategory category)
        {
            foreach (SlotPlan slot in _slots)
                if (slot.Category == category) return slot;
            return default;
        }

        /// <summary>
        /// Unknown ids and items of another category are worn as bare (the applier must never switch on a part it cannot
        /// name). An empty or invalid colour leaves the item's own colours.
        /// </summary>
        public static OutfitPlan Build(CosmeticOutfit outfit, CosmeticCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            outfit = outfit ?? CosmeticOutfit.Empty;

            var slots = new List<SlotPlan>();
            foreach (CosmeticCategory category in CosmeticOutfit.Categories)
            {
                CosmeticSlot worn = outfit.Get(category);
                if (!worn.IsEmpty && catalog.TryGet(worn.CosmeticId, out CosmeticDefinition item) && item.Category == category)
                {
                    string color = CosmeticColor.Normalize(worn.ColorHex);
                    slots.Add(new SlotPlan(category, item.PartNames, color.Length > 0 ? item.PrimaryChannel.MaterialName() : null, color));
                }
                else
                {
                    slots.Add(new SlotPlan(category, Array.Empty<string>(), null, string.Empty));
                }
            }
            return new OutfitPlan(slots.ToArray(), catalog.LegacyOutfitObjects);
        }
    }
}
