using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// The tap area of one action row of the settings window (a link or a reset): a transparent button over the whole
    /// row. While the row is not usable (a link with no address) it shows the row's disabled art and ignores taps.
    /// Binds objects of the prefab; builds nothing.
    /// </summary>
    public sealed class SettingsActionRowView : MonoBehaviour
    {
        [Tooltip("Transparent button over the whole row.")]
        [SerializeField] private Button button;

        [Tooltip("Row background (the SettingsRow root Image).")]
        [SerializeField] private Image background;

        [Tooltip("Row caption.")]
        [SerializeField] private Text label;

        [Tooltip("Row background while usable.")]
        [SerializeField] private Sprite enabledSprite;

        [Tooltip("Row background while not usable.")]
        [SerializeField] private Sprite disabledSprite;

        [Tooltip("Caption alpha while the row is not usable.")]
        [Range(0f, 1f)]
        [SerializeField] private float disabledTextAlpha = 0.45f;

        private Action _onTap;

        private void Awake() => button.onClick.AddListener(OnClicked);

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        /// <summary>Sets the caption, the tap action and whether the row can be tapped.</summary>
        public void Bind(string caption, Action onTap, bool usable)
        {
            label.text = caption;
            _onTap = onTap;
            SetUsable(usable);
        }

        public void SetUsable(bool usable)
        {
            button.interactable = usable;
            background.sprite = usable ? enabledSprite : disabledSprite;
            Color color = label.color;
            color.a = usable ? 1f : disabledTextAlpha;
            label.color = color;
        }

        private void OnClicked() => _onTap?.Invoke();
    }
}
