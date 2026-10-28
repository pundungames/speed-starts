using SpeedStars.Meta.Contracts;
using SpeedStars.Themes;
using UnityEngine;
using Zenject;

namespace SpeedStars.Bridge
{
    /// <summary>
    /// Shows the shown theme in Serhat's main menu showcase and follows changes (preview, use, cancel). While the theme
    /// select screen is browsing, the character and plinth are hidden: that screen shows only the world. Only
    /// translates: the service decides, <see cref="MainMenuThemeShowcase.SelectTheme"/> switches the world. Serhat's
    /// scripts are not touched.
    /// </summary>
    public sealed class MenuThemeView : MonoBehaviour
    {
        [Tooltip("The showcase on the scene root (Anamenu Themalari). Its theme list order must match the catalog's showcase indices.")]
        [SerializeField] private MainMenuThemeShowcase showcase;

        [Tooltip("Foreground Character Platform (character + plinth). Hidden while the theme select screen is open. Empty = never hidden.")]
        [SerializeField] private GameObject characterPlatform;

        private IThemeService _themes;
        private bool _subscribed;

        [Inject]
        public void Construct(IThemeService themes)
        {
            _themes = themes;
            Initialize();
        }

        private void Awake() => Initialize();

        private void OnEnable() => Initialize();

        /// <summary>Idempotent: subscribes once and applies the current state. Safe before injection (does nothing yet).</summary>
        public void Initialize()
        {
            if (_themes == null || showcase == null) return;
            if (!_subscribed)
            {
                _themes.ShownChanged += ApplyTheme;
                _themes.BrowsingChanged += ApplyBrowsing;
                _subscribed = true;
            }
            ApplyTheme();
            ApplyBrowsing();
        }

        private void OnDisable()
        {
            if (_themes != null && _subscribed)
            {
                _themes.ShownChanged -= ApplyTheme;
                _themes.BrowsingChanged -= ApplyBrowsing;
            }
            _subscribed = false;
        }

        private void ApplyTheme()
        {
            if (showcase != null) showcase.SelectTheme(_themes.ShownShowcaseIndex);
        }

        private void ApplyBrowsing()
        {
            if (characterPlatform != null) characterPlatform.SetActive(!_themes.IsBrowsing);
        }
    }
}
