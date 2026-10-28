using DG.Tweening;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Themes
{
    /// <summary>
    /// Theme select screen (opened by Home's environment button). The world behind it is the theme being browsed; the
    /// arrows preview the neighbouring theme (no Cash, no save), the action button buys / uses it. Leaving the screen by
    /// any way drops an unapplied preview. All logic is <see cref="ThemeSelectPresenter"/>; this view binds prefab objects,
    /// draws the presenter's snapshot and listens to the service and the wallet. Back is UiPanel's close button.
    /// </summary>
    public sealed class ThemeSelectScreenView : UiScreen
    {
        [Header("Shown theme")]
        [Tooltip("ThemeName/Label: the shown theme's display name.")]
        [SerializeField] private Text nameText;

        [Tooltip("ThemePrevious: previews the previous theme. Not interactable at the first theme.")]
        [SerializeField] private Button previousButton;

        [Tooltip("ThemeNext: previews the next theme. Not interactable at the last theme.")]
        [SerializeField] private Button nextButton;

        [Header("Action button")]
        [Tooltip("ThemeAction root Button (buys or uses the shown theme).")]
        [SerializeField] private Button actionButton;

        [Tooltip("ThemeAction's own background Image: shows the sprite of the current state.")]
        [SerializeField] private Image actionBackground;

        [Tooltip("ThemeAction/CurrencyIcon: shown next to a price only.")]
        [SerializeField] private GameObject currencyIcon;

        [Tooltip("ThemeAction/PriceLabel: the price, right of the currency icon.")]
        [SerializeField] private Text priceText;

        [Tooltip("ThemeAction/StateLabel: 'USE' / 'IN USE', centered.")]
        [SerializeField] private Text stateText;

        [Tooltip("Text of the USE state.")]
        [SerializeField] private string useLabel = "USE";

        [Tooltip("Text of the IN USE state.")]
        [SerializeField] private string inUseLabel = "IN USE";

        [Tooltip("Text color on cream buttons (button_states.json: #061A41).")]
        [SerializeField] private Color darkTextColor = new Color(0.0235f, 0.102f, 0.2549f, 1f);

        [Tooltip("Text color on the IN USE button (button_states.json: #FFFCEF).")]
        [SerializeField] private Color lightTextColor = new Color(1f, 0.9882f, 0.9373f, 1f);

        [Header("Action button art (one background at a time)")]
        [Tooltip("theme_action_background: buy state.")]
        [SerializeField] private Sprite purchaseSprite;

        [Tooltip("theme_use_background: USE state.")]
        [SerializeField] private Sprite useSprite;

        [Tooltip("theme_use_pressed_background: shown while USE is held down.")]
        [SerializeField] private Sprite usePressedSprite;

        [Tooltip("theme_in_use_background: the active theme.")]
        [SerializeField] private Sprite inUseSprite;

        [Tooltip("theme_disabled_background: not enough Cash / not for sale.")]
        [SerializeField] private Sprite disabledSprite;

        [Header("Resources (left column)")]
        [Tooltip("PurpleGem/Value.")]
        [SerializeField] private Text purpleGemText;

        [Tooltip("EntryTicket/Value: 'count/cap'.")]
        [SerializeField] private Text entryTicketText;

        [Tooltip("TimedTicket/Value.")]
        [SerializeField] private Text timedTicketText;

        [Tooltip("Cash/Value.")]
        [SerializeField] private Text cashText;

        private ThemeSelectPresenter _presenter;
        private IThemeService _themes;
        private IWallet _wallet;
        private IEntryTickets _tickets;
        private UiMotionConfig _motion;
        private Tween _stamp;
        private Vector3 _stateBaseScale;
        private bool _baseKnown;
        private bool _subscribed;

        [Inject]
        public void ConstructThemeSelect(IThemeService themes, IWallet wallet, IEntryTickets tickets, UiMotionConfig motion)
        {
            _themes = themes;
            _wallet = wallet;
            _tickets = tickets;
            _motion = motion;
            _presenter = new ThemeSelectPresenter(themes);
        }

        protected override void Awake()
        {
            base.Awake();
            previousButton.onClick.AddListener(OnPrevious);
            nextButton.onClick.AddListener(OnNext);
            actionButton.onClick.AddListener(OnAction);
            _stateBaseScale = stateText.transform.localScale; // the designer's scale; the stamp always returns to it
            _baseKnown = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_presenter == null) return;

            _presenter.Open();
            Subscribe();
            Redraw();
        }

        protected override void OnDisable()
        {
            Unsubscribe();
            KillStamp();
            _presenter?.Close();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(OnPrevious);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
            if (actionButton != null) actionButton.onClick.RemoveListener(OnAction);
            base.OnDestroy();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            _themes.ShownChanged += Redraw;
            _themes.ActiveChanged += Redraw;
            _themes.OwnershipChanged += Redraw;
            _wallet.Changed += Redraw;
            _tickets.Changed += Redraw;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _themes.ShownChanged -= Redraw;
            _themes.ActiveChanged -= Redraw;
            _themes.OwnershipChanged -= Redraw;
            _wallet.Changed -= Redraw;
            _tickets.Changed -= Redraw;
        }

        private void OnPrevious() => _presenter.Step(-1);

        private void OnNext() => _presenter.Step(1);

        private void OnAction()
        {
            ThemeActionOutcome outcome = _presenter.PressAction();
            if (outcome == ThemeActionOutcome.Purchased) PlayStamp();
        }

        private void Redraw()
        {
            ThemeSelectSnapshot snapshot = _presenter.Snapshot();
            ThemeActionLook look = snapshot.Action;

            nameText.text = snapshot.Name;
            previousButton.interactable = snapshot.CanPrevious;
            nextButton.interactable = snapshot.CanNext;

            Sprite background = SpriteFor(look.Background);
            actionBackground.sprite = background;
            actionButton.interactable = look.Interactable;
            actionButton.spriteState = new SpriteState
            {
                pressedSprite = look.Background == ThemeActionBackground.Use ? usePressedSprite : background,
                highlightedSprite = background,
                selectedSprite = background,
                disabledSprite = background
            };

            Color tone = look.Tone == ThemeActionTone.Light ? lightTextColor : darkTextColor;
            bool price = look.Label == ThemeActionLabel.Price;
            currencyIcon.SetActive(look.ShowsCurrencyIcon);
            priceText.gameObject.SetActive(price);
            stateText.gameObject.SetActive(!price);
            priceText.color = tone;
            stateText.color = tone;
            priceText.text = snapshot.Price > 0 ? snapshot.Price.ToString() : string.Empty;
            stateText.text = look.Label == ThemeActionLabel.InUse ? inUseLabel : useLabel;

            purpleGemText.text = _wallet.PurpleGems.ToString();
            cashText.text = _wallet.Cash.ToString();
            timedTicketText.text = _wallet.TimedTickets.ToString();
            entryTicketText.text = HomeTexts.TicketCount(_tickets.Count, _tickets.Cap);
        }

        private Sprite SpriteFor(ThemeActionBackground background)
        {
            switch (background)
            {
                case ThemeActionBackground.Purchase: return purchaseSprite;
                case ThemeActionBackground.Use: return useSprite;
                case ThemeActionBackground.InUse: return inUseSprite;
                default: return disabledSprite;
            }
        }

        /// <summary>
        /// A purchase went through: the button now says USE, and that label punches (the press itself already punched the
        /// button through ButtonFeedback, so the two never fight over one scale). Numbers from UiMotionConfig.
        /// </summary>
        private void PlayStamp()
        {
            if (_motion == null || !_baseKnown) return;

            KillStamp();
            Transform label = stateText.transform;
            label.localScale = _stateBaseScale;
            _stamp = label
                .DOPunchScale(_stateBaseScale * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillStamp()
        {
            if (_stamp != null)
            {
                _stamp.Kill();
                _stamp = null;
            }
            if (_baseKnown && stateText != null) stateText.transform.localScale = _stateBaseScale;
        }
    }
}
