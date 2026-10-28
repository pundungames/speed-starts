using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// "Every tappable thing says it was pressed" (agent-kit/05 §5): a punch scale on pointer DOWN (not on release),
    /// sized from the designed scale, numbers from UiMotionConfig. A non-interactable button does not punch; it only
    /// reports Denied to the hook. Sound and haptics are not here: the optional IButtonFeedbackHook gets the
    /// same-frame call. Put it on the Button's own GameObject (pointer events stop at the first object that handles
    /// them, so a parent would never hear the press).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler
    {
        [Tooltip("Button whose interactable state decides punch or denied. Empty: the Selectable on this object; none at all = always punches.")]
        [SerializeField] private Selectable button;

        [Tooltip("What punches. Empty: this object. Point it at a child to punch the art but keep the hit area still.")]
        [SerializeField] private Transform punchTarget;

        private UiMotionConfig _motion;
        private IButtonFeedbackHook _hook;
        private Tween _punch;
        private Vector3 _baseScale;
        private bool _initialized;
        private bool _warnedNoMotion;

        [Inject]
        public void Construct(UiMotionConfig motion, [InjectOptional] IButtonFeedbackHook hook)
        {
            _motion = motion;
            _hook = hook;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return; // same rule as Button's click
            Initialize();

            if (button != null && !button.IsInteractable())
            {
                _hook?.Denied(this);
                return;
            }

            Punch();
            _hook?.Pressed(this);
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnDisable()
        {
            KillPunch();
            if (_initialized) punchTarget.localScale = _baseScale;
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (button == null) button = GetComponent<Selectable>();
            if (punchTarget == null) punchTarget = transform;
            _baseScale = punchTarget.localScale; // the designer's scale; the punch always starts from it
        }

        private void Punch()
        {
            if (_motion == null)
            {
                if (!_warnedNoMotion)
                {
                    _warnedNoMotion = true;
                    Debug.LogWarning($"{name}: UiMotionConfig was not injected; the button does not punch.", this);
                }
                return;
            }

            // A new press restarts the punch from the designed scale; stacking punches would drift the size.
            KillPunch();
            punchTarget.localScale = _baseScale;
            Tween punch = punchTarget
                .DOPunchScale(_baseScale * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            punch.OnKill(() => { if (_punch == punch) _punch = null; }); // a late kill of an old punch must not drop the new one
            _punch = punch;
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
