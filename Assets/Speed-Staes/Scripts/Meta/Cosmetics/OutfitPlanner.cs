using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Cosmetics
{
    /// <summary>One colour to write: which shared toon material (e.g. Color2_Toon) and the "RRGGBB" for it.</summary>
    public readonly struct ChannelColorPlan
    {
        public ChannelColorPlan(string materialName, string colorHex)
        {
            MaterialName = materialName;
            ColorHex = colorHex;
        }

        public string MaterialName { get; }
        public string ColorHex { get; }
    }

    /// <summary>
    /// The skin to paint: the renderer that carries the body mesh, the skin material on it and the colour. An empty
    /// <c>ColorHex</c> leaves the material's own colour (no default known).
    /// </summary>
    public readonly struct SkinPlan
    {
        public SkinPlan(string rendererName, string materialName, string colorHex)
        {
            RendererName = rendererName ?? string.Empty;
            MaterialName = materialName ?? string.Empty;
            ColorHex = colorHex ?? string.Empty;
        }

        public string RendererName { get; }
        public string MaterialName { get; }

        /// <summary>"RRGGBB"; outfit skin, else the catalog default, else empty.</summary>
        public string ColorHex { get; }

        public bool HasColor => ColorHex.Length > 0;

        /// <summary>True when a renderer object of this name has the skin (the material is matched by <see cref="MatchesMaterial"/>).</summary>
        public bool MatchesRenderer(string objectName) =>
            RendererName.Length > 0 && string.Equals(objectName, RendererName, StringComparison.Ordinal);

        /// <summary>Instances are named "ColorSkin_Toon (Instance)", so the name only has to start with the skin material's name.</summary>
        public bool MatchesMaterial(string materialName) =>
            MaterialName.Length > 0 && !string.IsNullOrEmpty(materialName) && materialName.StartsWith(MaterialName, StringComparison.Ordinal);
    }

    /// <summary>What one category slot of the runner must look like.</summary>
    public readonly struct SlotPlan
    {
        public SlotPlan(CosmeticCategory category, IReadOnlyList<string> activeParts, IReadOnlyList<ChannelColorPlan> colors)
        {
            Category = category;
            ActiveParts = activeParts;
            Colors = colors;
        }

        public CosmeticCategory Category { get; }

        /// <summary>Object names under <c>Accessories/&lt;Category&gt;/</c> to switch on; every other object there goes off.</summary>
        public IReadOnlyList<string> ActiveParts { get; }

        /// <summary>One entry per colour channel that has a colour to write (a channel without one keeps its material's own colour).</summary>
        public IReadOnlyList<ChannelColorPlan> Colors { get; }

        public bool HasColor => Colors != null && Colors.Count > 0;

        public bool IsActive(string partName)
        {
            if (ActiveParts == null) return false;
            for (int i = 0; i < ActiveParts.Count; i++)
                if (string.Equals(ActiveParts[i], partName, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// The colour for a renderer's material (instances are named "Color2_Toon (Instance)", so the name only has to start
        /// with the channel material's name); false when no channel of this slot paints that material.
        /// </summary>
        public bool TryGetColor(string materialName, out string colorHex)
        {
            if (Colors != null && !string.IsNullOrEmpty(materialName))
            {
                for (int i = 0; i < Colors.Count; i++)
                {
                    if (!materialName.StartsWith(Colors[i].MaterialName, StringComparison.Ordinal)) continue;
                    colorHex = Colors[i].ColorHex;
                    return true;
                }
            }
            colorHex = string.Empty;
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

        private OutfitPlan(SlotPlan[] slots, IReadOnlyList<string> legacyObjects, SkinPlan skin)
        {
            _slots = slots;
            LegacyObjects = legacyObjects;
            Skin = skin;
        }

        /// <summary>Skin colour for the runner's body mesh.</summary>
        public SkinPlan Skin { get; }

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
        /// name). An empty or invalid colour leaves that channel's own colour.
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
                    // Colour index i belongs to the item's i-th channel; a channel without a (valid) colour is left alone.
                    var colors = new List<ChannelColorPlan>();
                    IReadOnlyList<CosmeticColorChannel> channels = item.Channels;
                    for (int i = 0; i < channels.Count && i < CosmeticSlot.MaxChannels; i++)
                    {
                        string color = CosmeticColor.Normalize(worn.ColorAt(i));
                        if (color.Length > 0) colors.Add(new ChannelColorPlan(channels[i].MaterialName(), color));
                    }
                    slots.Add(new SlotPlan(category, item.PartNames, colors));
                }
                else
                {
                    slots.Add(new SlotPlan(category, Array.Empty<string>(), Array.Empty<ChannelColorPlan>()));
                }
            }
            // The outfit's own skin wins; an empty or invalid one falls back to the catalog's default skin.
            string skin = CosmeticColor.Normalize(outfit.SkinColorHex);
            if (skin.Length == 0) skin = CosmeticColor.Normalize(catalog.DefaultSkinColor);
            return new OutfitPlan(slots.ToArray(), catalog.LegacyOutfitObjects,
                new SkinPlan(catalog.SkinRendererName, catalog.SkinMaterialName, skin));
        }
    }
}
