using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Rewards
{
    /// <summary>
    /// The daily gift popup (prefab DailyGiftPopup): DAILY GIFT, the red Entry Ticket, "+1", ENTRY TICKET, "Tickets: n / cap"
    /// and one button. Home opens it by itself when the gift is due. CLAIM asks <see cref="IDailyGift.Claim"/>; a granted
    /// ticket closes the popup and starts the ticket flight to the resource bar, a full inventory turns it into the
    /// TICKETS FULL view with OK (no reward, no flight, <see cref="IDailyGift.MarkSeenFull"/> on OK). There is no X: the
    /// button is the only way out. The button is dead while a claim runs. What each state shows is decided by
    /// <see cref="RewardPanelsModel"/>; this view binds existing objects and fills them. One geometry for every state: only
    /// the button art and the texts change.
    /// </summary>
    public sealed class DailyGiftPopupView : UiPopup
    {
        [Header("Texts")]
        [Tooltip("Window/Title: DAILY GIFT (on the red header).")]
        [SerializeField] private Text titleText;

        [Tooltip("Window/Quantity: '+1' next to the ticket; hidden in the full view.")]
        [SerializeField] private Text quantityText;

        [Tooltip("Window/State: ENTRY TICKET or TICKETS FULL.")]
        [SerializeField] private Text stateText;

        [Tooltip("Window/Capacity: 'Tickets: n / cap'.")]
        [SerializeField] private Text capacityText;

        [Header("Button")]
        [Tooltip("Window/Action: CLAIM, or OK in the full view.")]
        [SerializeField] private Button actionButton;

        [Tooltip("Window/Action/Art: the button picture that swaps between the CLAIM and OK art.")]
        [SerializeField] private Image actionArt;

        [Tooltip("Window/Action/Label: CLAIM or OK.")]
        [SerializeField] private Text actionLabel;

        [Tooltip("Button art of the CLAIM state.")]
        [SerializeField] private Sprite claimSprite;

        [Tooltip("Button art of the OK (tickets full) state.")]
        [SerializeField] private Sprite okSprite;

        [Tooltip("Pressed art of the CLAIM state.")]
        [SerializeField] private Sprite claimPressedSprite;

        [Tooltip("Label colour on the red CLAIM art.")]
        [SerializeField] private Color claimLabelColor = Color.white;

        [Tooltip("Label colour on the cream OK art.")]
        [SerializeField] private Color okLabelColor = new Color(0.04f, 0.12f, 0.36f, 1f);

        [Header("Ticket flight")]
        [Tooltip("RewardFlight widget (scene object on the overlay canvas): flies one ticket to the resource bar after a granted claim. Scene-bound.")]
        [SerializeField] private TicketFlightView flight;

        private IDailyGift _gift;
        private IEntryTickets _tickets;
        private ScreenNavigator _navigator;
        private UiMotionConfig _closeMotion;
        private DailyGiftLayout _layout;
        private bool _busy;

        [Inject]
        public void ConstructDailyGift(IDailyGift gift, IEntryTickets tickets, UiMotionConfig motion, [InjectOptional] ScreenNavigator navigator)
        {
            _gift = gift;
            _tickets = tickets;
            _closeMotion = motion;
            _navigator = navigator;
        }

        protected override void Awake()
        {
            base.Awake();
            actionButton.onClick.AddListener(OnAction);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_gift == null) return;

            _busy = false;
            Draw(RewardPanelsModel.Daily(_gift.State, _gift.Amount, _tickets.Count, _tickets.Cap));
        }

        protected override void OnDestroy()
        {
            if (actionButton != null) actionButton.onClick.RemoveListener(OnAction);
            base.OnDestroy();
        }

        // ---------------------------------------------------------------- drawing

        private void Draw(DailyGiftLayout layout)
        {
            _layout = layout;
            titleText.text = RewardTexts.DailyGiftTitle;
            quantityText.gameObject.SetActive(layout.ShowQuantity);
            quantityText.text = layout.Quantity;
            stateText.text = layout.State;
            capacityText.text = layout.Capacity;
            actionLabel.text = layout.Button;
            actionLabel.color = layout.ButtonIsOk ? okLabelColor : claimLabelColor;
            actionArt.sprite = layout.ButtonIsOk ? okSprite : claimSprite;

            // The pressed picture follows the state: the red one would flash over the cream OK button.
            SpriteState sprites = actionButton.spriteState;
            sprites.pressedSprite = layout.ButtonIsOk ? okSprite : claimPressedSprite;
            actionButton.spriteState = sprites;
            actionButton.interactable = layout.ButtonInteractable && !_busy;
        }

        // ---------------------------------------------------------------- taps

        private void OnAction()
        {
            if (_busy || _gift == null) return;
            _busy = true;
            actionButton.interactable = false;

            if (_layout.ButtonIsOk)
            {
                _gift.MarkSeenFull(); // the notice was seen: it does not come back this session (the day's right is not spent)
                Close();
                return;
            }

            switch (RewardPanelsModel.OutcomeOf(_gift.Claim()))
            {
                case DailyClaimOutcome.Fly:
                    Close();
                    if (flight != null) flight.Play(_closeMotion != null ? _closeMotion.PopupClose.Duration : 0f);
                    else Debug.LogWarning($"{name}: the flight widget is not bound; the ticket is granted without the flight.", this);
                    break;
                case DailyClaimOutcome.ShowFull:
                    _busy = false;
                    Draw(RewardPanelsModel.Daily(DailyGiftState.Full, _gift.Amount, _tickets.Count, _tickets.Cap));
                    break;
                default:
                    Close();
                    break;
            }
        }

        /// <summary>Through the navigator (it owns the popup layer); without one the popup simply hides.</summary>
        private void Close()
        {
            if (_navigator != null) _navigator.ClosePopup(this);
            else Hide();
        }
    }
}
