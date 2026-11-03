using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>Pure rule of a settings slider: the value snapped to its step and kept inside the range.</summary>
    public static class SliderSnap
    {
        public static float Snap(float value, float step, float min, float max)
        {
            float snapped = step > 0f ? Mathf.Round(value / step) * step : value;
            return Mathf.Clamp(snapped, min, max);
        }
    }

    /// <summary>
    /// One slider row of the settings window (a SettingsRow with a slider in its control slot): fills the caption, shows the
    /// saved value and reports changes in whole steps, so a drag saves only when the value really moves. Binds objects of the
    /// prefab; builds nothing.
    /// </summary>
    public sealed class SettingsSliderRowView : MonoBehaviour
    {
        [Tooltip("Row caption.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("The slider in the row's control slot.")]
        [SerializeField] private Slider slider;

        [Tooltip("The slider moves in steps of this size.")]
        [Min(0.001f)]
        [SerializeField] private float step = 0.05f;

        private Action<float> _changed;
        private float _last = float.NaN;
        private bool _listening;

        /// <summary>Sets the caption and range, shows <paramref name="value"/> and reports later changes (snapped) to <paramref name="changed"/>.</summary>
        public void Bind(string caption, float min, float max, float value, Action<float> changed)
        {
            if (label != null) label.text = caption;
            slider.minValue = min;
            slider.maxValue = max;
            _changed = changed;
            Listen();
            SetValue(value);
        }

        /// <summary>Follows a value that changed elsewhere, without reporting it.</summary>
        public void SetValue(float value)
        {
            _last = SliderSnap.Snap(value, step, slider.minValue, slider.maxValue);
            slider.SetValueWithoutNotify(value);
        }

        private void Awake() => Listen();

        private void OnDestroy()
        {
            if (slider != null && _listening) slider.onValueChanged.RemoveListener(OnSlider);
            _listening = false;
        }

        private void Listen()
        {
            if (_listening) return;
            _listening = true;
            slider.onValueChanged.AddListener(OnSlider);
        }

        private void OnSlider(float raw)
        {
            float snapped = SliderSnap.Snap(raw, step, slider.minValue, slider.maxValue);
            if (!float.IsNaN(_last) && Mathf.Approximately(snapped, _last)) return;
            _last = snapped;
            _changed?.Invoke(snapped);
        }
    }
}
