using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Shared open/close recipe of UiScreen and UiPopup (agent-kit/05 §2-§3). The panel is a prefab object in the scene;
    /// this only animates its own CanvasGroup and RectTransform, it never builds UI.
    /// - The open plays from OnEnable, so a panel switched on in the Inspector animates the same as one opened by code.
    /// - Input is off while opening and turns on when the open finishes; closing turns it off at once, then the
    ///   object deactivates.
    /// - A running animation is killed before the next one starts (open/close spam cannot leave it half-scaled);
    ///   Show while showing and Hide while hidden do nothing.
    /// - Alpha goes through DOTween.To: the CanvasGroup.DOFade shortcut lives in a DOTween module that the SpeedStars.UI
    ///   assembly cannot see (contract §3). Unscaled time, linked to the GameObject.
    /// Without an injected UiMotionConfig the panel still works, only without animation (warned once).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public abstract class UiPanel : MonoBehaviour, IUiView
    {
        private UiMotionConfig _motion;
        private CanvasGroup _group;
        private Vector3 _baseScale;
        private bool _initialized;
        private bool _closing;
        private bool _warnedNoMotion;
        private Sequence _sequence;
        private ScreenNavigator _panelNavigator;

        [Tooltip("Optional Back / Close button of this panel: closes the popup, or goes back from the screen. Leave empty on views that own their close button.")]
        [SerializeField] private Button closeButton;

        [Inject]
        public void Construct(UiMotionConfig motion)
        {
            _motion = motion;
        }

        /// <summary>Optional so a test scene without a navigator still works; the close button then does nothing.</summary>
        [Inject]
        public void ConstructPanelNavigation([InjectOptional] ScreenNavigator navigator)
        {
            _panelNavigator = navigator;
        }

        /// <summary>True for popups: the close button closes this one popup instead of going back a screen.</summary>
        protected virtual bool IsPopupPanel => false;

        /// <summary>True from Show (open animation included) until Hide.</summary>
        public bool IsVisible => gameObject.activeSelf && !_closing;

        protected abstract UiTransition OpenMotion(UiMotionConfig motion);
        protected abstract UiTransition CloseMotion(UiMotionConfig motion);

        public void Show()
        {
            Initialize();
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true); // OnEnable plays the open
                return;
            }
            if (!_closing) return;

            // Re-opened halfway through its close: turn around from where it is, no jump back to the start values.
            _closing = false;
            if (isActiveAndEnabled) PlayOpen(fromCurrent: true);
        }

        public void Hide()
        {
            Initialize();
            if (!gameObject.activeSelf || _closing) return;

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false); // nothing on screen to animate
                return;
            }
            _closing = true;
            PlayClose();
        }

        protected virtual void Awake()
        {
            Initialize();
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            if (_panelNavigator == null) return;
            PanelCloseRule.Close(_panelNavigator, this, IsPopupPanel);
        }

        protected virtual void OnEnable()
        {
            Initialize();
            _closing = false;
            PlayOpen(fromCurrent: false);
        }

        protected virtual void OnDisable()
        {
            KillSequence();
            _closing = false;
        }

        protected virtual void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
            KillSequence();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _group = GetComponent<CanvasGroup>();
            _baseScale = transform.localScale; // the designer's scale, captured before any animation touches it
        }

        private void PlayOpen(bool fromCurrent)
        {
            KillSequence();
            _group.blocksRaycasts = true;
            _group.interactable = false;

            if (!HasMotion())
            {
                _group.alpha = 1f;
                transform.localScale = _baseScale;
                _group.interactable = true;
                return;
            }

            UiTransition spec = OpenMotion(_motion);
            if (!fromCurrent)
            {
                _group.alpha = 0f;
                transform.localScale = _baseScale * spec.Scale;
            }

            Sequence seq = DOTween.Sequence();
            seq.Insert(0f, transform.DOScale(_baseScale, spec.Duration).SetEase(spec.Ease));
            seq.Insert(0f, DOTween.To(() => _group.alpha, a => _group.alpha = a, 1f, spec.Duration).SetEase(spec.Ease));
            seq.SetUpdate(true);
            seq.SetLink(gameObject);
            seq.OnComplete(OnOpenComplete);
            _sequence = seq;
        }

        private void OnOpenComplete()
        {
            _sequence = null;
            _group.interactable = true;
        }

        private void PlayClose()
        {
            KillSequence();
            _group.interactable = false;
            _group.blocksRaycasts = false; // a leaving panel must not eat taps meant for what is under it

            if (!HasMotion())
            {
                FinishClose();
                return;
            }

            UiTransition spec = CloseMotion(_motion);
            Sequence seq = DOTween.Sequence();
            seq.Insert(0f, transform.DOScale(_baseScale * spec.Scale, spec.Duration).SetEase(spec.Ease));
            seq.Insert(0f, DOTween.To(() => _group.alpha, a => _group.alpha = a, 0f, spec.Duration).SetEase(spec.Ease));
            seq.SetUpdate(true);
            seq.SetLink(gameObject);
            seq.OnComplete(FinishClose);
            _sequence = seq;
        }

        private void FinishClose()
        {
            _sequence = null;
            _closing = false;
            gameObject.SetActive(false);
            transform.localScale = _baseScale; // leave the designed scale behind, not the end of the shrink
        }

        private void KillSequence()
        {
            if (_sequence == null) return;
            Sequence running = _sequence;
            _sequence = null;
            running.Kill();
        }

        private bool HasMotion()
        {
            if (_motion != null) return true;
            if (!_warnedNoMotion)
            {
                _warnedNoMotion = true;
                Debug.LogWarning($"{name}: UiMotionConfig was not injected; the panel opens and closes without animation.", this);
            }
            return false;
        }
    }
}
