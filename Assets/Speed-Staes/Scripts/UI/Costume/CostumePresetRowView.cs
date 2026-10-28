using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>One row of the open preset list (prefab CostumePresetRow): normal or selected (red) background and the preset name.</summary>
    public sealed class CostumePresetRowView : MonoBehaviour
    {
        [Tooltip("Row root button (tap area).")]
        [SerializeField] private Button button;

        [Tooltip("Selected background layer (red): shown on the row of the chosen preset.")]
        [SerializeField] private GameObject selectedBackground;

        [Tooltip("Row/Label: preset name.")]
        [SerializeField] private Text label;

        private int _index;

        /// <summary>The player chose this row (preset index).</summary>
        public event Action<int> Chosen;

        private void Awake() => button.onClick.AddListener(OnClick);

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        public void Bind(int index, string presetName)
        {
            _index = index;
            label.text = presetName;
        }

        public void SetSelected(bool selected) => selectedBackground.SetActive(selected);

        private void OnClick() => Chosen?.Invoke(_index);
    }
}
