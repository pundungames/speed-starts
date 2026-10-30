using System;
using DG.Tweening;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// One ready-made skin tone of the Body tab (prefab CostumeSkinSwatch, created by the screen under the item grid's
    /// content): a flat colour over a normal or a selected (red) card frame, with a tick while it is the worn skin. It is a
    /// Button: the screen hears the tap through <see cref="Tapped"/> with the colour text. Binds existing objects only.
    /// </summary>
    public sealed class CostumeSkinSwatchView : MonoBehaviour
    {
        [Tooltip("Swatch root button (tap area).")]
        [SerializeField] private Button button;

        [Tooltip("Normal frame layer: shown while the tone is not the worn skin.")]
        [SerializeField] private GameObject normalBackground;

        [Tooltip("Selected frame layer (red): shown while the tone is the worn skin.")]
        [SerializeField] private GameObject selectedBackground;

        [Tooltip("Flat colour of the tone, inside the frame.")]
        [SerializeField] private Image colorImage;

        [Tooltip("Tick: shown while the tone is the worn skin.")]
        [SerializeField] private GameObject check;

        private Tween _punch;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseKnown;

        /// <summary>The "RRGGBB" colour this swatch shows.</summary>
        public string ColorHex { get; private set; } = string.Empty;

        /// <summary>The player tapped the swatch (colour "RRGGBB").</summary>
        public event Action<string> Tapped;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
            RememberScale();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            KillPunch();
        }

        private void OnDisable()
        {
            KillPunch();
            if (_baseKnown) transform.localScale = _baseScale;
        }

        public void Bind(string hex, Color color)
        {
            ColorHex = hex ?? string.Empty;
            colorImage.color = color;
        }

        public void SetSelected(bool selected)
        {
            normalBackground.SetActive(!selected);
            selectedBackground.SetActive(selected);
            check.SetActive(selected);
        }

        /// <summary>A short scale punch on a tap: numbers from the motion config; no motion config = no punch.</summary>
        public void Punch(UiMotionConfig motion)
        {
            if (motion == null || !isActiveAndEnabled) return;
            RememberScale();
            KillPunch();
            transform.localScale = _baseScale;
            Tween punch = transform
                .DOPunchScale(_baseScale * motion.ButtonPunchScale, motion.ButtonPunchDuration, motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() => { if (_punch == punch) _punch = null; });
            _punch = punch;
        }

        private void OnClick() => Tapped?.Invoke(ColorHex);

        private void RememberScale()
        {
            if (_baseKnown) return;
            _baseKnown = true;
            _baseScale = transform.localScale;
        }

        private void KillPunch()
        {
            if (_punch == null) return;
            Tween running = _punch;
            _punch = null;
            running.Kill();
        }
    }
}
