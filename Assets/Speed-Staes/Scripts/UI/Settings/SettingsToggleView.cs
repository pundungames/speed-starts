using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// On/off switch of a settings row (prefab part of the settings popup): the track swaps its sprite, the thumb slides
    /// by <c>thumbTravel</c> (measured by the builder from the two thumb sprites) and the ON / OFF word sits on the side
    /// the thumb left. Only binds and animates; the value lives in the settings service. Core DOTween only (the UI
    /// assembly cannot see the module shortcuts); unscaled time, linked to this object.
    /// </summary>
    public sealed class SettingsToggleView : MonoBehaviour
    {
        [Tooltip("The track's Button: a tap flips the switch.")]
        [SerializeField] private Button button;

        [Tooltip("Track image: shows the on or off sprite.")]
        [SerializeField] private Image track;

        [Tooltip("Thumb: rests at its designed (off) position and slides right by Thumb Travel when on.")]
        [SerializeField] private RectTransform thumb;

        [Tooltip("ON / OFF word on the track.")]
        [SerializeField] private Text stateLabel;

        [Tooltip("Track sprite while on.")]
        [SerializeField] private Sprite onTrack;

        [Tooltip("Track sprite while off.")]
        [SerializeField] private Sprite offTrack;

        [Tooltip("Canvas pixels the thumb moves between off and on (the builder measures it from the art).")]
        [SerializeField] private float thumbTravel;

        [Tooltip("Seconds the thumb takes to slide.")]
        [Min(0f)]
        [SerializeField] private float slideSeconds = 0.15f;

        [Tooltip("Easing of the slide.")]
        [SerializeField] private Ease slideEase = Ease.OutQuad;

        [Tooltip("Gap between the track end and the ON / OFF word, in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float labelInset = 22f;

        private Action<bool> _changed;
        private bool _on;
        private float _offX;
        private bool _captured;
        private Tween _slide;

        /// <summary>Shows the value without animation and reports later taps to <paramref name="changed"/> (the new value).</summary>
        public void Bind(bool on, Action<bool> changed)
        {
            Capture();
            _changed = changed;
            Apply(on, animate: false);
        }

        /// <summary>Follows a value that changed elsewhere.</summary>
        public void SetOn(bool on, bool animate)
        {
            Capture();
            if (_on == on && !animate) return;
            Apply(on, animate);
        }

        private void Awake()
        {
            Capture();
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
            KillSlide();
        }

        private void OnDisable() => KillSlide();

        private void OnClicked()
        {
            Apply(!_on, animate: true);
            _changed?.Invoke(_on);
        }

        private void Capture()
        {
            if (_captured) return;
            _captured = true;
            _offX = thumb.anchoredPosition.x; // the designer's resting place
        }

        private void Apply(bool on, bool animate)
        {
            _on = on;
            if (track != null) track.sprite = on ? onTrack : offTrack;
            if (stateLabel != null)
            {
                stateLabel.text = SettingsTexts.OnOff(on);
                stateLabel.alignment = on ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                RectTransform rt = stateLabel.rectTransform;
                rt.offsetMin = new Vector2(on ? labelInset : 0f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(on ? 0f : -labelInset, rt.offsetMax.y);
            }

            KillSlide();
            float target = _offX + (on ? thumbTravel : 0f);
            if (!animate || slideSeconds <= 0f || !isActiveAndEnabled)
            {
                SetThumbX(target);
                return;
            }

            _slide = DOTween.To(() => thumb.anchoredPosition.x, SetThumbX, target, slideSeconds)
                .SetEase(slideEase)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetThumbX(float x)
        {
            Vector2 p = thumb.anchoredPosition;
            p.x = x;
            thumb.anchoredPosition = p;
        }

        private void KillSlide()
        {
            if (_slide == null) return;
            Tween running = _slide;
            _slide = null;
            running.Kill();
        }
    }
}
