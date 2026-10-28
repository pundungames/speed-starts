using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>What a worn slot holds: an item id and the colour of its primary channel. Empty = nothing worn.</summary>
    public readonly struct CosmeticSlot : IEquatable<CosmeticSlot>
    {
        public CosmeticSlot(string cosmeticId, string colorHex)
        {
            CosmeticId = cosmeticId ?? string.Empty;
            ColorHex = colorHex ?? string.Empty;
        }

        /// <summary>Empty = the slot is bare.</summary>
        public string CosmeticId { get; }

        /// <summary>"RRGGBB". Empty = the item's own colours.</summary>
        public string ColorHex { get; }

        public bool IsEmpty => string.IsNullOrEmpty(CosmeticId);

        public bool Equals(CosmeticSlot other) =>
            string.Equals(CosmeticId ?? string.Empty, other.CosmeticId ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(ColorHex ?? string.Empty, other.ColorHex ?? string.Empty, StringComparison.OrdinalIgnoreCase);

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

        /// <summary>A copy wearing this item (colour optional) in the category.</summary>
        public CosmeticOutfit With(CosmeticCategory category, string cosmeticId, string colorHex = "")
        {
            var copy = (CosmeticSlot[])_slots.Clone();
            copy[(int)category] = new CosmeticSlot(cosmeticId, colorHex);
            return new CosmeticOutfit(copy);
        }

        /// <summary>A copy with the category bare.</summary>
        public CosmeticOutfit Without(CosmeticCategory category) => With(category, string.Empty, string.Empty);

        /// <summary>A copy with only the colour of the category's item changed.</summary>
        public CosmeticOutfit WithColor(CosmeticCategory category, string colorHex) =>
            With(category, Get(category).CosmeticId, colorHex);

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
        public CosmeticItemInfo(string id, CosmeticCategory category, string name, bool isPremium, int price,
            CosmeticCurrency currency, bool isOwned)
        {
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
