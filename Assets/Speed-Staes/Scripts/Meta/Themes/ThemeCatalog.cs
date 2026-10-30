using System;
using System.Collections.Generic;
using System.Linq;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.Themes
{
    /// <summary>One world theme: a stable save id, a name, its place in the showcase scene and its price.</summary>
    [Serializable]
    public sealed class ThemeEntry
    {
        [Tooltip("Stable id written to saves (selected and owned themes). Never change it once players have saves.")]
        [SerializeField] private string themeId;

        [Tooltip("Name shown to the player (English).")]
        [SerializeField] private string displayName;

        [Tooltip("Index of this theme in the main menu showcase's theme list (Anamenu Themalari scene). Also the cycling order.")]
        [SerializeField, Min(0)] private int showcaseIndex;

        // Serialized name stays "cashPrice" so saved assets keep their prices; it is the amount in priceCurrency.
        [Tooltip("Price amount in the price currency below. 0 = no price: the theme cannot be bought yet.")]
        [SerializeField, Min(0)] private int cashPrice;

        [Tooltip("Currency the price is paid in. Premium themes use PurpleGem.")]
        [SerializeField] private ThemePriceCurrency priceCurrency = ThemePriceCurrency.Cash;

        [Tooltip("Premium theme: its name and action button wear the gold frame (red while in use).")]
        [SerializeField] private bool premium;

        public string Id => themeId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int ShowcaseIndex => showcaseIndex;
        public int Price => cashPrice;
        public ThemePriceCurrency PriceCurrency => priceCurrency;
        public bool IsPremium => premium;
    }

    /// <summary>
    /// Every world theme and which one a new player starts with. Code never lists themes itself: adding a theme means
    /// adding an entry here (and the matching display in the showcase scene).
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Theme Catalog", fileName = "ThemeCatalog")]
    public sealed class ThemeCatalog : ScriptableObject
    {
        [Tooltip("Themes. Ids must be unique; the cycling order is the showcase index.")]
        [SerializeField] private ThemeEntry[] themes = new ThemeEntry[0];

        [Tooltip("Id of the theme every player owns and starts with. Empty or unknown = the first theme in showcase order.")]
        [SerializeField] private string defaultThemeId;

        /// <summary>Themes in showcase order (ties keep list order), empty slots skipped. A new list each call.</summary>
        public IReadOnlyList<ThemeEntry> GetThemes()
        {
            if (themes == null) return new List<ThemeEntry>();
            return themes.Where(t => t != null).OrderBy(t => t.ShowcaseIndex).ToList();
        }

        /// <summary>The first theme with this id; false for an empty or unknown id.</summary>
        public bool TryGet(string id, out ThemeEntry entry)
        {
            if (!string.IsNullOrEmpty(id) && themes != null)
            {
                foreach (ThemeEntry candidate in themes)
                {
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        entry = candidate;
                        return true;
                    }
                }
            }
            entry = null;
            return false;
        }

        /// <summary>The start theme; null only when the catalog is empty.</summary>
        public ThemeEntry DefaultTheme
        {
            get
            {
                if (TryGet(defaultThemeId, out ThemeEntry entry)) return entry;
                IReadOnlyList<ThemeEntry> all = GetThemes();
                return all.Count > 0 ? all[0] : null;
            }
        }

        public IReadOnlyList<string> FindDuplicateIds() => ConfigIdCheck.FindDuplicates(Ids());

        private IEnumerable<string> Ids()
        {
            if (themes == null) yield break;
            foreach (ThemeEntry entry in themes)
                if (entry != null) yield return entry.Id;
        }

        private void OnValidate() => ConfigIdCheck.Warn(this, "theme", Ids());
    }
}
