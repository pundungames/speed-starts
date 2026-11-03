using System;
using System.Collections.Generic;
using SpeedStars.UI.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// One pick-from-a-short-list row of the settings window (a SettingsRow with a field in its control slot): each tap on the
    /// field moves to the next option and wraps around at the end. Fills the caption and the option names; builds nothing.
    /// </summary>
    public sealed class SettingsChoiceRowView : MonoBehaviour
    {
        [Tooltip("Row caption.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("The field button in the row's control slot.")]
        [SerializeField] private Button button;

        [Tooltip("Name of the current option, shown in the field.")]
        [SerializeField] private TMP_Text valueText;

        private IReadOnlyList<string> _options = Array.Empty<string>();
        private int _index;
        private Action<int> _changed;
        private bool _listening;

        public int Index => _index;

        /// <summary>Sets the caption and the options, shows option <paramref name="index"/> and reports later taps (the new index).</summary>
        public void Bind(string caption, IReadOnlyList<string> options, int index, Action<int> changed)
        {
            if (label != null) label.text = caption;
            _options = options ?? Array.Empty<string>();
            _changed = changed;
            if (!_listening)
            {
                _listening = true;
                button.onClick.AddListener(OnClicked);
            }
            SetIndex(index);
            button.interactable = _options.Count > 0;
        }

        /// <summary>Follows a value that changed elsewhere.</summary>
        public void SetIndex(int index)
        {
            _index = _options.Count == 0 ? 0 : Mathf.Clamp(index, 0, _options.Count - 1);
            valueText.text = _options.Count == 0 ? string.Empty : _options[_index];
        }

        private void OnDestroy()
        {
            if (button != null && _listening) button.onClick.RemoveListener(OnClicked);
            _listening = false;
        }

        private void OnClicked()
        {
            if (_options.Count == 0) return;
            SetIndex(PresetStepper.Step(_index, _options.Count, +1));
            _changed?.Invoke(_index);
        }
    }
}
