using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>
    /// What a worn slot holds: an item id and the colour of each of its colour channels. Colours are indexed by the item's
    /// own channel order (index 0 = its primary channel, the only colour old saves have; the catalog says which material
    /// each index paints). Empty colour = that channel keeps its material's own colour. Empty id = nothing worn.
    /// </summary>
    public readonly struct CosmeticSlot : IEquatable<CosmeticSlot>
    {
        /// <summary>Most colour channels an item can have (Color1_Toon .. Color3_Toon).</summary>
        public const int MaxChannels = 3;

        private readonly string _color0;
        private readonly string _color1;
        private readonly string _color2;

        public CosmeticSlot(string cosmeticId, string colorHex) : this(cosmeticId, colorHex, null, null) { }

        public CosmeticSlot(string cosmeticId, string colorHex, string colorHex2, string colorHex3)
        {
            CosmeticId = cosmeticId ?? string.Empty;
            _color0 = colorHex ?? string.Empty;
            _color1 = colorHex2 ?? string.Empty;
            _color2 = colorHex3 ?? string.Empty;
        }

        /// <summary>Empty = the slot is bare.</summary>
        public string CosmeticId { get; }

        /// <summary>"RRGGBB" of the primary channel (index 0). Empty = the item's own colours.</summary>
        public string ColorHex => _color0 ?? string.Empty;

        /// <summary>"RRGGBB" of a channel by index; empty for an unset channel or an index outside 0..MaxChannels-1.</summary>
        public string ColorAt(int channelIndex)
        {
            switch (channelIndex)
            {
                case 0: return _color0 ?? string.Empty;
                case 1: return _color1 ?? string.Empty;
                case 2: return _color2 ?? string.Empty;
                default: return string.Empty;
            }
        }

        /// <summary>A copy with one channel's colour changed. An index outside the channels returns the slot as it is.</summary>
        public CosmeticSlot WithColor(int channelIndex, string colorHex)
        {
            switch (channelIndex)
            {
                case 0: return new CosmeticSlot(CosmeticId, colorHex, _color1, _color2);
                case 1: return new CosmeticSlot(CosmeticId, _color0, colorHex, _color2);
                case 2: return new CosmeticSlot(CosmeticId, _color0, _color1, colorHex);
                default: return this;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(CosmeticId);

        public bool Equals(CosmeticSlot other) =>
            string.Equals(CosmeticId ?? string.Empty, other.CosmeticId ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(ColorAt(0), other.ColorAt(0), StringComparison.OrdinalIgnoreCase)
            && string.Equals(ColorAt(1), other.ColorAt(1), StringComparison.OrdinalIgnoreCase)
            && string.Equals(ColorAt(2), other.ColorAt(2), StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is CosmeticSlot other && Equals(other);
        public override int GetHashCode() => (CosmeticId ?? string.Empty).GetHashCode();
    }

    /// <summary>
    /// A complete outfit: one slot per category. Immutable; <c>With</c> / <c>Without</c> return a changed copy, so a
    /// preview and the saved outfit can never alias each other.
    /// </summary>
    public sealed class CosmeticOutfit : IEquatable<CosmeticOutfit>
    {
        public static readonly CosmeticCategory[] Categories = (CosmeticCategory[])Enum.GetValues(typeof(CosmeticCategory));

        private static readonly int SlotCount = MaxCategory() + 1;

        /// <summary>Nothing worn.</summary>
        public static readonly CosmeticOutfit Empty = new CosmeticOutfit(new CosmeticSlot[SlotCount]);

        private readonly CosmeticSlot[] _slots;

        private CosmeticOutfit(CosmeticSlot[] slots) => _slots = slots;

        public CosmeticSlot Get(CosmeticCategory category)
        {
            int index = (int)category;
            return index >= 0 && index < _slots.Length ? _slots[index] : default;
        }

        /// <summary>A copy wearing this item (primary-channel colour optional) in the category.</summary>
        public CosmeticOutfit With(CosmeticCategory category, string cosmeticId, string colorHex = "") =>
            WithSlot(category, new CosmeticSlot(cosmeticId, colorHex));

        /// <summary>A copy with this slot (item and every channel colour) in the category.</summary>
        public CosmeticOutfit WithSlot(CosmeticCategory category, CosmeticSlot slot)
        {
            var copy = (CosmeticSlot[])_slots.Clone();
            copy[(int)category] = slot;
            return new CosmeticOutfit(copy);
        }

        /// <summary>A copy with the category bare.</summary>
        public CosmeticOutfit Without(CosmeticCategory category) => With(category, string.Empty, string.Empty);

        /// <summary>A copy with only the primary-channel colour of the category's item changed.</summary>
        public CosmeticOutfit WithColor(CosmeticCategory category, string colorHex) => WithColor(category, 0, colorHex);

        /// <summary>A copy with only one channel's colour of the category's item changed (other channels keep theirs).</summary>
        public CosmeticOutfit WithColor(CosmeticCategory category, int channelIndex, string colorHex) =>
            WithSlot(category, Get(category).WithColor(channelIndex, colorHex));

        public bool Equals(CosmeticOutfit other)
        {
            if (other == null) return false;
            for (int i = 0; i < _slots.Length; i++)
                if (!_slots[i].Equals(other._slots[i])) return false;
            return true;
        }

        public override bool Equals(object obj) => Equals(obj as CosmeticOutfit);

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (CosmeticSlot slot in _slots) hash = hash * 31 + slot.GetHashCode();
            return hash;
        }

        private static int MaxCategory()
        {
            int max = 0;
            foreach (CosmeticCategory category in Categories) max = Math.Max(max, (int)category);
            return max;
        }
    }

    /// <summary>One catalog item as the editor screen needs it, with the player's ownership.</summary>
    public readonly struct CosmeticItemInfo
    {
        private readonly IReadOnlyList<string> _channelDefaults;

        public CosmeticItemInfo(string id, CosmeticCategory category, string name, bool isPremium, int price,
            CosmeticCurrency currency, bool isOwned, IReadOnlyList<string> channelDefaults = null)
        {
            _channelDefaults = channelDefaults;
            Id = id;
            Category = category;
            Name = name;
            IsPremium = isPremium;
            Price = price;
            Currency = currency;
            IsOwned = isOwned;
        }

        public string Id { get; }
        public CosmeticCategory Category { get; }

        /// <summary>Part name of the item (e.g. "H01"); a placeholder label until real names and icons exist.</summary>
        public string Name { get; }

        public bool IsPremium { get; }

        /// <summary>0 = not for sale.</summary>
        public int Price { get; }

        public CosmeticCurrency Currency { get; }
        public bool IsOwned { get; }

        /// <summary>Not owned: shown with a lock; premium items ask for a purchase.</summary>
        public bool IsLocked => !IsOwned;

        public bool IsForSale => Price > 0;

        /// <summary>
        /// One entry per colour channel of the item, primary first: the "RRGGBB" its material has by default (empty when the
        /// catalog does not know it). Never null; empty for an item whose channels are unknown (then it counts as one channel).
        /// </summary>
        public IReadOnlyList<string> ChannelDefaults => _channelDefaults ?? Array.Empty<string>();

        /// <summary>How many colours the player can set on this item (1 to <see cref="CosmeticSlot.MaxChannels"/>).</summary>
        public int ChannelCount => Math.Min(CosmeticSlot.MaxChannels, Math.Max(1, ChannelDefaults.Count));

        /// <summary>Default colour of a channel by index; empty when unknown or out of range.</summary>
        public string DefaultColorAt(int channelIndex) =>
            channelIndex >= 0 && channelIndex < ChannelDefaults.Count ? ChannelDefaults[channelIndex] ?? string.Empty : string.Empty;
    }

    public enum CosmeticPurchaseResult
    {
        /// <summary>Paid and now owned (saved).</summary>
        Purchased,
        /// <summary>Already owned; nothing was charged.</summary>
        AlreadyOwned,
        /// <summary>The wallet holds less than the price; nothing changed.</summary>
        NotEnough,
        /// <summary>The item has no price (it cannot be bought).</summary>
        NotForSale,
        /// <summary>No item with that id in the catalog.</summary>
        UnknownItem
    }

    public enum CosmeticGrantResult
    {
        /// <summary>Now owned (saved).</summary>
        Granted,
        /// <summary>Owned already (or the transaction id was applied before); nothing changed.</summary>
        AlreadyOwned,
        /// <summary>No item with that id in the catalog.</summary>
        UnknownItem
    }

    public enum CosmeticSaveResult
    {
        /// <summary>The outfit is stored in the preset (and written to disk).</summary>
        Saved,
        /// <summary>The outfit contains an item the player does not own; nothing was saved.</summary>
        LockedItem,
        /// <summary>The outfit names an item that is not in the catalog (or is in the wrong slot); nothing was saved.</summary>
        UnknownItem,
        /// <summary>No such preset.</summary>
        InvalidPreset,
        /// <summary>The save file is protected (newer or unreadable save); the change lives in memory only.</summary>
        NotPersisted
    }

    /// <summary>
    /// Racer presets ("Custom 1..N"), ownership, purchase and the saved outfits. All numbers (preset count, prices, default
    /// outfit, palette) come from the cosmetic catalog asset. Everything the costume editor screen needs is here.
    /// </summary>
    public interface ICosmeticService
    {
        /// <summary>Raised after a preset was selected or saved, or an item was bought.</summary>
        event Action Changed;

        int PresetCount { get; }

        /// <summary>"Custom 1" ... (the format is in the catalog).</summary>
        string GetPresetName(int index);

        /// <summary>The preset worn in menu and race.</summary>
        int SelectedPreset { get; }

        /// <summary>False for an index outside 0..PresetCount-1. Saved at once.</summary>
        bool SelectPreset(int index);

        /// <summary>The saved outfit of a preset; locked or unknown items are left out. Empty for a bad index.</summary>
        CosmeticOutfit GetOutfit(int presetIndex);

        /// <summary>Saved outfit of the selected preset.</summary>
        CosmeticOutfit SelectedOutfit { get; }

        /// <summary>The outfit every new preset starts with (from the catalog).</summary>
        CosmeticOutfit DefaultOutfit { get; }

        /// <summary>Stores the outfit in the preset. Rejects an outfit that holds a locked item.</summary>
        CosmeticSaveResult SaveOutfit(int presetIndex, CosmeticOutfit outfit);

        /// <summary>Items of one category, premium first (list order of the catalog).</summary>
        IReadOnlyList<CosmeticItemInfo> GetItems(CosmeticCategory category);

        bool TryGetItem(string cosmeticId, out CosmeticItemInfo item);

        /// <summary>Normal items (no price) are owned from the start; premium ones after a purchase. False for an unknown id.</summary>
        bool IsOwned(string cosmeticId);

        /// <summary>Known but not owned.</summary>
        bool IsLocked(string cosmeticId);

        /// <summary>Pays with the item's currency from the wallet; never charges twice for the same item.</summary>
        CosmeticPurchaseResult TryPurchase(string cosmeticId);

        /// <summary>
        /// Gives the item for free (rewards). Idempotent: an owned item or a repeated transaction id changes nothing.
        /// Saves and raises <c>Changed</c> when granted. The default body keeps older test doubles compiling.
        /// </summary>
        CosmeticGrantResult GrantOwned(string cosmeticId, string transactionId) => CosmeticGrantResult.UnknownItem;

        /// <summary>Colours (RRGGBB) the colour picker and Random use.</summary>
        IReadOnlyList<string> ColorPalette { get; }

        /// <summary>Owned items and palette colours only. Not saved; the caller previews it or saves it. Seeded by the caller.</summary>
        CosmeticOutfit CreateRandomOutfit(System.Random random);
    }

    /// <summary>
    /// The outfit the menu character wears right now. It follows the selected preset until the costume screen shows
    /// something else; <c>Reset</c> (Back) returns to the saved outfit. Nothing here is saved.
    /// </summary>
    public interface ICosmeticPreview
    {
        /// <summary>Raised when <c>Current</c> changed.</summary>
        event Action Changed;

        /// <summary>The previewed outfit, or the selected preset's saved outfit while nothing is previewed.</summary>
        CosmeticOutfit Current { get; }

        /// <summary>True while an outfit other than the saved one is being shown.</summary>
        bool IsPreviewing { get; }

        /// <summary>Wear this outfit on the menu character without saving.</summary>
        void Show(CosmeticOutfit outfit);

        /// <summary>Back to the selected preset's saved outfit.</summary>
        void Reset();
    }
}
