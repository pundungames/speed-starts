using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>One switch row of the settings window (a SettingsRow with the ON/OFF switch in its control slot). Binds objects of the prefab; builds nothing.</summary>
    public sealed class SettingsToggleRowView : MonoBehaviour
    {
        [Tooltip("Row caption.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("The switch in the row's control slot.")]
        [SerializeField] private SettingsToggleView toggle;

        /// <summary>Sets the caption, shows the value and reports later taps (the new value).</summary>
        public void Bind(string caption, bool on, Action<bool> changed)
        {
            if (label != null) label.text = caption;
            toggle.Bind(on, changed);
        }

        /// <summary>Follows a value that changed elsewhere.</summary>
        public void SetOn(bool on) => toggle.SetOn(on, animate: false);
    }
}
