namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Cosmetic slots. The names are a contract with the runner prefab: each one is exactly the name of a child of
    /// <c>Accessories/</c> (reports/kozmetik-hazirlik-2026-10-28.md), and the applier finds the slot by this name.
    /// Serialized as int: append new values, never reorder or renumber.
    /// </summary>
    public enum CosmeticCategory
    {
        Headwear = 0,
        Glasses = 1,
        Tops = 2,
        Gloves = 3,
        Shorts = 4,
        Leggings = 5,
        Socks = 6,
        Shoes = 7
    }

    /// <summary>What a cosmetic is paid with. Premium items use Purple Gems (PDF §2.3). Serialized as int: append only.</summary>
    public enum CosmeticCurrency
    {
        Cash = 0,
        PurpleGem = 1
    }
}
