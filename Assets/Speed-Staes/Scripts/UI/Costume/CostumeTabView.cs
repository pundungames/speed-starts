using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// One tab of the wardrobe panel: a button with a normal and a selected (red) background, its icon, and for Body a
    /// second icon that is white while selected. Binds existing objects only; which category the tab lists comes from the
    /// config asset (by <see cref="Tab"/>).
    /// </summary>
    public sealed class CostumeTabView : MonoBehaviour
    {
        [Tooltip("Which tab this is (matches the config asset's tab entries).")]
        [SerializeField] private CostumeTab tab;

        [Tooltip("The tab's button (its own background is the normal look and the disabled tint).")]
        [SerializeField] private Button button;

        [Tooltip("Selected background layer (red).")]
        [SerializeField] private GameObject selectedBackground;

        [Tooltip("Icon shown while not selected (and always, when there is no selected icon).")]
        [SerializeField] private GameObject icon;

        [Tooltip("Optional icon shown instead of the normal one while selected (Body: white). Empty = the same icon in both states.")]
        [SerializeField] private GameObject selectedIcon;

        [Tooltip("Tab name text (Face, Head, ...). Optional.")]
        [SerializeField] private Text label;

        [Tooltip("Label colour on the normal (cream) background.")]
        [SerializeField] private Color normalLabelColor = new Color(0.04f, 0.12f, 0.36f, 1f);

        [Tooltip("Label colour on the selected (red) background.")]
        [SerializeField] private Color selectedLabelColor = Color.white;

        public CostumeTab Tab => tab;

        /// <summary>The player tapped the tab.</summary>
        public event Action<CostumeTab> Tapped;

        private void Awake() => button.onClick.AddListener(OnClick);

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        /// <summary>False = nothing to show yet: the tab greys out and cannot be tapped.</summary>
        public void SetAvailable(bool available) => button.interactable = available;

        public void SetSelected(bool selected)
        {
            selectedBackground.SetActive(selected);
            if (label != null) label.color = selected ? selectedLabelColor : normalLabelColor;
            if (selectedIcon == null) return;
            selectedIcon.SetActive(selected);
            icon.SetActive(!selected);
        }

        private void OnClick() => Tapped?.Invoke(tab);
    }
}
