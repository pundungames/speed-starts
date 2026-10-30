namespace SpeedStars.UI.Costume
{
    /// <summary>What a tab does when it is tapped.</summary>
    public enum CostumeTabKind
    {
        /// <summary>Lists the items of one cosmetic category (config: tab to category).</summary>
        Items,
        /// <summary>Picks the runner's skin colour: ready-made tones plus the colour panel.</summary>
        Skin,
        /// <summary>Leaves for the emote screen; nothing is listed here.</summary>
        Emote
    }

    /// <summary>
    /// Fixed behaviour of each tab (pure, tested). It lives in code and not in the config asset so that an asset saved before
    /// the Body and Emote tabs existed still maps every tab to the right behaviour.
    /// </summary>
    public static class CostumeTabRules
    {
        public static CostumeTabKind KindOf(CostumeTab tab)
        {
            switch (tab)
            {
                case CostumeTab.Body: return CostumeTabKind.Skin;
                case CostumeTab.Emote: return CostumeTabKind.Emote;
                default: return CostumeTabKind.Items;
            }
        }
    }
}
