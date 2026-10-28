using System;
using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Entry gate modal (prefab EntryGateModal, a UiPopup): event name, Entry Ticket count, "Use Entry Ticket", Timed
    /// Ticket status (with "Use Timed Ticket" and the running window's countdown) and the ways to get tickets (Cash; the
    /// rewarded ad stays closed until an ad SDK exists; both closed while the inventory is full). Confirm goes through <see cref="HomeEntryFlow"/>; a refused
    /// entry shakes the window and writes a warning into the Remaining line (the prefab has no separate warning text).
    /// While Timed access runs the ticket button is hidden: nothing is spent then (PDF 1.1).
    /// </summary>
    public sealed class EntryGateModalView : UiPopup
    {
        [Tooltip("Window: shaken when the entry is refused.")]
        [SerializeField] private RectTransform window;

        [Tooltip("Window/EventName.")]
        [SerializeField] private Text eventNameText;

        [Tooltip("Window/UseEntryTicket: confirm button; hidden while Timed access is active, not interactable with no ticket.")]
        [SerializeField] private Button useTicketButton;

        [Tooltip("Window/UseEntryTicket/Label.")]
        [SerializeField] private Text useTicketLabel;

        [Tooltip("Window/UseEntryTicket/Remaining: tickets left, or the refusal warning.")]
        [SerializeField] private Text remainingText;

        [Tooltip("Window/TimedTicketStatus/Label: 'Timed Ticket' or 'Timed Ticket active'.")]
        [SerializeField] private Text timedLabel;

        [Tooltip("Window/TimedTicketStatus/Value: Timed Ticket stock.")]
        [SerializeField] private Text timedValue;

        [Tooltip("Window/UseTimedTicket: starts the Timed Ticket window; shown only with a ticket in stock and no window running.")]
        [SerializeField] private Button useTimedButton;

        [Tooltip("Window/UseTimedTicket/Label.")]
        [SerializeField] private Text useTimedLabel;

        [Tooltip("Window/TimedTicketStatus: punched when a Timed Ticket window starts.")]
        [SerializeField] private RectTransform timedStatus;

        [Tooltip("Window/GetTickets/Caption: 'Get tickets', or 'Entry Tickets Full' while the inventory is full.")]
        [SerializeField] private Text offersCaption;

        [Tooltip("Window/GetTickets/BuyWithCash: not interactable while full, without a Cash price or without enough Cash.")]
        [SerializeField] private Button buyWithCashButton;

        [Tooltip("Window/GetTickets/BuyWithCash/Price: the Cash price (empty when there is none).")]
        [SerializeField] private Text cashPriceText;

        [Tooltip("Window/GetTickets/WatchAd: stays not interactable (no rewarded-ad SDK).")]
        [SerializeField] private Button watchAdButton;

        [Header("Countdown")]
        [Tooltip("Seconds between countdown redraws while a Timed Ticket window runs.")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        [Header("Refusal shake")]
        [Tooltip("Seconds the window shakes after a refused entry.")]
        [Min(0f)]
        [SerializeField] private float shakeDuration = 0.35f;

        [Tooltip("Sideways shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float shakeStrength = 24f;

        [Tooltip("How many times the shake swings.")]
        [Min(1)]
        [SerializeField] private int shakeVibrato = 18;

        private IEntryTickets _tickets;
        private IWallet _wallet;
        private HomeEntryFlow _entryFlow;
        private ScreenNavigator _navigator;
        private RaceEventDefinition _event;
        private Tween _shake;
        private Tween _punch;
        private Vector3 _timedBaseScale = Vector3.one;
        private float _elapsed;
        private ITimedAccess _timedAccess;
        private IEntryTicketShop _shop;
        private EconomyConfig _economy;
        private IClock _clock;
        private UiMotionConfig _punchMotion;

        [Inject]
        public void ConstructEntryGate(IEntryTickets tickets, IWallet wallet, HomeEntryFlow entryFlow, ScreenNavigator navigator)
        {
            _tickets = tickets;
            _wallet = wallet;
            _entryFlow = entryFlow;
            _navigator = navigator;
        }

        [Inject]
        public void ConstructEntryGateEconomy(ITimedAccess timedAccess, IEntryTicketShop shop, EconomyConfig economy, IClock clock, UiMotionConfig motion)
        {
            _timedAccess = timedAccess;
            _shop = shop;
            _economy = economy;
            _clock = clock;
            _punchMotion = motion;
        }

        /// <summary>Opens the modal for <paramref name="definition"/> through the navigator's popup layer.</summary>
        public void Open(RaceEventDefinition definition)
        {
            _event = definition;
            Refresh();
            _navigator.OpenPopup(this);
        }

        protected override void Awake()
        {
            base.Awake();
            useTicketButton.onClick.AddListener(OnUseTicket);
            useTimedButton.onClick.AddListener(OnUseTimed);
            buyWithCashButton.onClick.AddListener(OnBuyWithCash);
            watchAdButton.interactable = false; // no rewarded-ad SDK in the project
            useTicketLabel.text = HomeTexts.UseEntryTicket;
            useTimedLabel.text = HomeTexts.UseTimedTicket;
            _timedBaseScale = timedStatus.localScale; // the designer's scale; the punch always returns to it
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _tickets.Changed += Refresh;
            _wallet.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            _tickets.Changed -= Refresh;
            _wallet.Changed -= Refresh;
            KillShake();
            KillPunch();
            base.OnDisable();
        }

        private void Update()
        {
            if (_event == null || !_wallet.TimedAccessActive) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;
            _elapsed -= tickInterval;
            Refresh(); // the countdown; also flips the screen when the window runs out
        }

        protected override void OnDestroy()
        {
            if (useTicketButton != null) useTicketButton.onClick.RemoveListener(OnUseTicket);
            if (useTimedButton != null) useTimedButton.onClick.RemoveListener(OnUseTimed);
            if (buyWithCashButton != null) buyWithCashButton.onClick.RemoveListener(OnBuyWithCash);
            base.OnDestroy();
        }

        private void Refresh()
        {
            if (_event == null) return;

            bool timedActive = _wallet.TimedAccessActive;
            TimeSpan remaining = timedActive && _wallet.TimedAccessEndsUtc.HasValue ? _wallet.TimedAccessEndsUtc.Value - _clock.UtcNow : TimeSpan.Zero;
            int cashPrice = _economy != null && _economy.TryGetEntryTicketCashPrice(out int price) ? price : 0;
            var state = new EntryGateState(_tickets.Count, _tickets.Cap, timedActive, _wallet.TimedTickets, remaining, cashPrice, _wallet.Cash, adAvailable: false);
            EntryGateModel model = EntryGateModel.Build(state);

            eventNameText.text = _event.DisplayName;
            useTimedButton.gameObject.SetActive(model.ShowUseTimedButton);
            offersCaption.text = model.OffersCaption;
            buyWithCashButton.interactable = model.BuyWithCashInteractable;
            cashPriceText.text = model.CashPriceText;
            watchAdButton.interactable = model.WatchAdInteractable;
            useTicketButton.gameObject.SetActive(model.ShowUseTicketButton);
            useTicketButton.interactable = model.UseTicketInteractable;
            remainingText.text = model.RemainingText;
            timedLabel.text = model.TimedStatusText;
            timedValue.text = model.TimedValueText;
        }

        private void OnUseTicket()
        {
            if (_event == null) return;

            EntryOutcome outcome = _entryFlow.TryEnter(_event.Id);
            if (outcome == EntryOutcome.StartRace)
            {
                _navigator.ClosePopup(this);
                return;
            }

            remainingText.text = HomeEntryRules.WarningFor(outcome);
            Shake();
        }

        private void OnUseTimed()
        {
            TimedAccessResult result = _timedAccess.TryActivate();
            if (result == TimedAccessResult.Activated)
            {
                Refresh();
                PlayPunch();
                return;
            }
            if (result != TimedAccessResult.AlreadyActive) Shake();
        }

        private void OnBuyWithCash()
        {
            TicketPurchaseResult result = _shop.TryBuyWithCash(Guid.NewGuid().ToString("N")); // one id per tap
            if (result != TicketPurchaseResult.Bought && result != TicketPurchaseResult.AlreadyProcessed) Shake();
            Refresh();
        }

        /// <summary>The Timed Ticket box punches when its window starts (numbers from UiMotionConfig).</summary>
        private void PlayPunch()
        {
            if (_punchMotion == null) return;

            KillPunch();
            timedStatus.localScale = _timedBaseScale;
            _punch = timedStatus
                .DOPunchScale(_timedBaseScale * _punchMotion.ButtonPunchScale, _punchMotion.ButtonPunchDuration, _punchMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillPunch()
        {
            if (_punch != null)
            {
                _punch.Kill();
                _punch = null;
            }
            if (timedStatus != null) timedStatus.localScale = _timedBaseScale;
        }

        private void Shake()
        {
            KillShake();
            _shake = window.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), shakeVibrato, 0f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillShake()
        {
            if (_shake == null) return;
            Tween running = _shake;
            _shake = null;
            running.Kill(true); // complete: the window ends on its designed position
        }
    }
}
