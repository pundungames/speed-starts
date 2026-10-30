using System;
using SpeedStars.Meta.Config;
using UnityEngine;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// The tabs of the wardrobe panel. The numbers are stored in the config asset and the prefab, so they never change:
    /// 2 was "Body" (the Tops list) and is now <see cref="Top"/>; the new skin tab "Body" takes the next free number (9).
    /// Their order on screen is the order of the tab views in the prefab, not this order.
    /// </summary>
    public enum CostumeTab
    {
        Face = 0,
        Head = 1,
        Top = 2,
        Shorts = 3,
        Leggings = 4,
        Hands = 5,
        Shoes = 6,
        Socks = 7,
        Emote = 8,
        Body = 9
    }

    /// <summary>What one tab shows: the cosmetic category behind it, or nothing (the tab is switched off).</summary>
    [Serializable]
    public sealed class CostumeTabEntry
    {
        [Tooltip("Which tab this is.")]
        [SerializeField] private CostumeTab tab;

        [Tooltip("Cosmetic category the tab lists (Face = Glasses, Head = Headwear, Top = Tops, Hands = Gloves). Ignored by the Body (skin) and Emote tabs, which list no category.")]
        [SerializeField] private CosmeticCategory category;

        [Tooltip("Off = the tab has nothing to show and cannot be tapped.")]
        [SerializeField] private bool hasContent = true;

        public CostumeTab Tab => tab;
        public CosmeticCategory Category => category;
        public bool HasContent => hasContent;
    }

    /// <summary>The icon picture of one cosmetic (a UI-side mapping: the cosmetic contract carries no sprite).</summary>
    [Serializable]
    public sealed class CostumeItemIcon
    {
        [Tooltip("Cosmetic id of the catalog.")]
        [SerializeField] private string cosmeticId = string.Empty;

        [Tooltip("The wardrobe picture (first part of the item).")]
        [SerializeField] private Sprite icon;

        public string CosmeticId => cosmeticId ?? string.Empty;
        public Sprite Icon => icon;
    }

    /// <summary>
    /// Content and tuning of the costume editor screen: tab to category mapping, item pictures, the display-only stat
    /// values, texts and the feedback numbers. Seeded by CostumeConfigSeed; the screen reads this asset (the view holds no
    /// numbers or sentences).
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/UI/Costume Editor Config", fileName = "CostumeEditorConfig")]
    public sealed class CostumeEditorConfig : ScriptableObject
    {
        [Header("Tabs")]
        [Tooltip("One entry per tab. A tab without an entry, or with 'has content' off, is not tappable.")]
        [SerializeField] private CostumeTabEntry[] tabs = new CostumeTabEntry[0];

        [Header("Item pictures")]
        [Tooltip("Cosmetic id to picture. Filled from the catalog by the builder; an item without one shows an empty card picture.")]
        [SerializeField] private CostumeItemIcon[] itemIcons = new CostumeItemIcon[0];

        [Tooltip("Tint of the card background of a premium item (placeholder: the art has no premium card).")]
        [SerializeField] private Color premiumCardTint = new Color(1f, 0.85f, 0.4f, 1f);

        [Header("Stats panel (display only)")]
        [Tooltip("Power bar fill, 0..1. Placeholder: the design documents give no values.")]
        [Range(0f, 1f)]
        [SerializeField] private float powerFill = 0.6f;

        [Tooltip("Stamina bar fill, 0..1. Placeholder: the design documents give no values.")]
        [Range(0f, 1f)]
        [SerializeField] private float staminaFill = 0.8f;

        [Tooltip("Text in the Height field. Placeholder.")]
        [SerializeField] private string heightText = string.Empty;

        [Header("Texts")]
        [Tooltip("Timed Ticket box while its window runs; {0} is the number of unused Timed Tickets.")]
        [SerializeField] private string timedActiveFormat = "{0} ON";

        [Header("Colour picker")]
        [Tooltip("Colour (RRGGBB) the picker shows for an item that wears its own colours.")]
        [SerializeField] private string emptyColorHex = "FFFFFF";

        [Header("Denied feedback (not enough gems, locked save)")]
        [Tooltip("Length of the shake (s).")]
        [Min(0f)]
        [SerializeField] private float deniedShakeDuration = 0.25f;

        [Tooltip("Shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float deniedShakeStrength = 14f;

        [Tooltip("How many times the shake swings to each side.")]
        [Min(1)]
        [SerializeField] private int deniedShakeCycles = 4;

        [Tooltip("The flash colour of a denied button.")]
        [SerializeField] private Color deniedFlashColor = new Color(0.9f, 0.16f, 0.16f, 1f);

        [Tooltip("Time the flash takes to fade back (s).")]
        [Min(0f)]
        [SerializeField] private float deniedFlashDuration = 0.35f;

        public CostumeTabEntry[] Tabs => tabs ?? new CostumeTabEntry[0];
        public Color PremiumCardTint => premiumCardTint;
        public float PowerFill => powerFill;
        public float StaminaFill => staminaFill;
        public string HeightText => heightText ?? string.Empty;
        public string TimedActiveFormat => string.IsNullOrEmpty(timedActiveFormat) ? "{0}" : timedActiveFormat;
        public string EmptyColorHex => emptyColorHex ?? string.Empty;
        public float DeniedShakeDuration => deniedShakeDuration;
        public float DeniedShakeStrength => deniedShakeStrength;
        public int DeniedShakeCycles => deniedShakeCycles;
        public Color DeniedFlashColor => deniedFlashColor;
        public float DeniedFlashDuration => deniedFlashDuration;

        public bool TryGetTab(CostumeTab tab, out CostumeTabEntry entry)
        {
            foreach (CostumeTabEntry candidate in Tabs)
            {
                if (candidate != null && candidate.Tab == tab)
                {
                    entry = candidate;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        /// <summary>Null when the item has no picture.</summary>
        public Sprite IconOf(string cosmeticId)
        {
            if (itemIcons == null || string.IsNullOrEmpty(cosmeticId)) return null;
            foreach (CostumeItemIcon candidate in itemIcons)
                if (candidate != null && string.Equals(candidate.CosmeticId, cosmeticId, StringComparison.Ordinal)) return candidate.Icon;
            return null;
        }
    }
}
