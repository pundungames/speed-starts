using System;
using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Entry gate modal (prefab EntryGateModal, a UiPopup, dressed with the daily reward art, T-050): RACE ENTRY, event name,
    /// "Tickets 2/3" with the next-ticket countdown, Timed Ticket status (with "Use Timed Ticket" and the running window's
    /// countdown), "Use Ticket" and the ways to get tickets (Cash and the rewarded ad; both closed while the inventory is
    /// full). Confirm goes through <see cref="HomeEntryFlow"/>; a refused entry shakes the window and writes a warning into
    /// the Remaining line (the prefab has no separate warning text). While Timed access runs the ticket button is hidden:
    /// nothing is spent then (PDF 1.1). WATCH AD runs <see cref="EntryGateAdFlow"/>: the button shows a spinner while the
    /// ad runs; a granted ticket punches the ticket icon, anything else shakes the window. The ad rules live in the flow;
    /// this view only shows them.
    /// </summary>
    public sealed class EntryGateModalView : UiPopup
    {
        [Tooltip("Window: shaken when the entry is refused.")]
        [SerializeField] private RectTransform window;

        [Tooltip("Window/Title: RACE ENTRY on the header.")]
        [SerializeField] private Text titleText;

        [Tooltip("Window/EventName.")]
        [SerializeField] private Text eventNameText;

        [Tooltip("Window/TicketCount: 'Tickets 2/3' next to the ticket icon.")]
        [SerializeField] private Text ticketCountText;

        [Tooltip("Window/TicketIcon: the red Entry Ticket; punched when a ticket arrives from an ad.")]
        [SerializeField] private RectTransform ticketIcon;

        [Tooltip("Window/Refill: 'Next ticket in mm:ss'; hidden when the inventory is full.")]
        [SerializeField] private Text refillText;

        [Tooltip("Window/UseEntryTicket: confirm button; hidden while Timed access is active, not interactable with no ticket.")]
        [SerializeField] private Button useTicketButton;

        [Tooltip("Window/UseEntryTicket/Label.")]
        [SerializeField] private Text useTicketLabel;

        [Tooltip("Window/Remaining: tickets left, or the refusal warning.")]
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

        [Tooltip("Window/GetTickets/Caption: 'GET TICKETS', or 'TICKETS FULL' while the inventory is full.")]
        [SerializeField] private Text offersCaption;

        [Tooltip("Window/GetTickets/BuyWithCash: not interactable while full, without a Cash price or without enough Cash.")]
        [SerializeField] private Button buyWithCashButton;

        [Tooltip("Window/GetTickets/BuyWithCash/Label: BUY WITH CASH.")]
        [SerializeField] private Text buyWithCashLabel;

        [Tooltip("Window/GetTickets/BuyWithCash/Price: the Cash price (empty when there is none).")]
        [SerializeField] private Text cashPriceText;

        [Tooltip("Window/GetTickets/WatchAd: not interactable while the ad offer is closed (full inventory, no ad service, ad off in the config) or an ad runs.")]
        [SerializeField] private Button watchAdButton;

        [Tooltip("Window/GetTickets/WatchAd/Label: WATCH AD; hidden while the spinner shows.")]
        [SerializeField] private Text watchAdLabel;

        [Tooltip("Window/GetTickets/WatchAd/Spinner: turns while an ad runs; inactive otherwise.")]
        [SerializeField] private RectTransform watchAdSpinner;

        [Header("Countdown")]
        [Tooltip("Seconds between countdown redraws (Timed window, next ticket).")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        [Header("Ad loading")]
        [Tooltip("Seconds one full turn of the loading spinner takes.")]
        [Min(0.1f)]
        [SerializeField] private float spinnerTurnSeconds = 1f;

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
        private Tween _ticketPunch;
        private Tween _spin;
        private Vector3 _timedBaseScale = Vector3.one;
        private Vector3 _ticketBaseScale = Vector3.one;
        private float _elapsed;
        private ITimedAccess _timedAccess;
        private IEntryTicketShop _shop;
        private EconomyConfig _economy;
        private IClock _clock;
        private UiMotionConfig _punchMotion;
        private IRewardedAds _ads;
        private IEntryTicketSource _ticketSource;
        private ProgressRepository _progress;
        private EntryGateAdFlow _adFlow;

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

        [Inject]
        public void ConstructEntryGateAds(IEntryTicketSource ticketSource, ProgressRepository progress, [InjectOptional] IRewardedAds ads)
        {
            _ticketSource = ticketSource;
            _progress = progress;
            _ads = ads; // null in a build without an ad service: WATCH AD stays closed
        }

        /// <summary>Opens the modal for <paramref name="definition"/> through the navigator's popup layer.</summary>
        public void Open(RaceEventDefinition definition)
        {
            _event = definition;
            Refresh();
            _navigator.OpenPopup(this);
        }

        /// <summary>Built on first use (after injection); the flow outlives the window being closed, so a late ad result still pays out.</summary>
        private EntryGateAdFlow AdFlow => _adFlow ?? (_adFlow = new EntryGateAdFlow(_ads, _ticketSource, _economy, SaveProgress));

        protected override void Awake()
        {
            base.Awake();
            useTicketButton.onClick.AddListener(OnUseTicket);
            useTimedButton.onClick.AddListener(OnUseTimed);
            buyWithCashButton.onClick.AddListener(OnBuyWithCash);
            watchAdButton.onClick.AddListener(OnWatchAd);
            titleText.text = HomeTexts.EntryGateTitle;
            useTicketLabel.text = HomeTexts.UseEntryTicket;
            useTimedLabel.text = HomeTexts.UseTimedTicket;
            buyWithCashLabel.text = HomeTexts.BuyWithCash;
            watchAdLabel.text = HomeTexts.WatchAd;
            _timedBaseScale = timedStatus.localScale; // the designer's scale; the punch always returns to it
            _ticketBaseScale = ticketIcon.localScale;
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
            KillSpin(); // an ad that is still running keeps going; Refresh restores the spinner on the next open
            base.OnDisable();
        }

        private void Update()
        {
            if (_event == null) return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;
            _elapsed -= tickInterval;
            Refresh(); // the countdowns (Timed window, next ticket); also flips the screen when the window runs out
        }

        protected override void OnDestroy()
        {
            if (useTicketButton != null) useTicketButton.onClick.RemoveListener(OnUseTicket);
            if (useTimedButton != null) useTimedButton.onClick.RemoveListener(OnUseTimed);
            if (buyWithCashButton != null) buyWithCashButton.onClick.RemoveListener(OnBuyWithCash);
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
            base.OnDestroy();
        }

        private void Refresh()
        {
            if (_event == null) return;

            bool timedActive = _wallet.TimedAccessActive;
            TimeSpan remaining = timedActive && _wallet.TimedAccessEndsUtc.HasValue ? _wallet.TimedAccessEndsUtc.Value - _clock.UtcNow : TimeSpan.Zero;
            int cashPrice = _economy != null && _economy.TryGetEntryTicketCashPrice(out int price) ? price : 0;
            EntryGateAdFlow ad = AdFlow;
            var state = new EntryGateState(_tickets.Count, _tickets.Cap, timedActive, _wallet.TimedTickets, remaining, cashPrice, _wallet.Cash, adAvailable: ad.IsOffered);
            EntryGateModel model = EntryGateModel.Build(state);

            eventNameText.text = _event.DisplayName;
            ticketCountText.text = HomeTexts.TicketsHeld(model.TicketCountText);
            DateTime? nextTicket = _tickets.NextTicketUtc;
            refillText.gameObject.SetActive(nextTicket.HasValue);
            if (nextTicket.HasValue) refillText.text = HomeTexts.NextTicketIn(CountdownFormatter.Format(nextTicket.Value - _clock.UtcNow));
            useTimedButton.gameObject.SetActive(model.ShowUseTimedButton);
            offersCaption.text = model.OffersCaption;
            buyWithCashButton.interactable = model.BuyWithCashInteractable;
            cashPriceText.text = model.CashPriceText;
            watchAdButton.interactable = model.WatchAdInteractable && !ad.IsBusy;
            watchAdLabel.gameObject.SetActive(!ad.IsBusy);
            SetSpinner(ad.IsBusy);
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

        private void OnWatchAd()
        {
            EntryGateAdFlow ad = AdFlow;
            if (!ad.IsAvailable) return; // closed offer, or an ad already runs (double tap)

            if (ad.TryStart(OnAdFinished)) Refresh(); // the spinner (a result that came at once has already refreshed through OnAdFinished)
        }

        /// <summary>
        /// The ad ended, maybe after the window was closed or the scene left. The flow has already paid out by now; this
        /// only shows it, and only while the window is up.
        /// </summary>
        private void OnAdFinished(AdWatchOutcome outcome)
        {
            if (this == null || !isActiveAndEnabled) return;

            Refresh();
            if (outcome == AdWatchOutcome.Granted) PlayTicketPunch();
            else Shake();
        }

        /// <summary>The ad ticket is written to the save right away (the grant itself does not save).</summary>
        private void SaveProgress()
        {
            _progress.Save();
        }

        private void SetSpinner(bool running)
        {
            watchAdSpinner.gameObject.SetActive(running);
            if (!running)
            {
                KillSpin();
                return;
            }
            if (_spin != null) return;

            watchAdSpinner.localRotation = Quaternion.identity;
            _spin = watchAdSpinner
                .DOLocalRotate(new Vector3(0f, 0f, -360f), spinnerTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillSpin()
        {
            if (_spin == null) return;
            _spin.Kill();
            _spin = null;
        }

        /// <summary>The ticket icon punches when an ad ticket arrives (numbers from UiMotionConfig).</summary>
        private void PlayTicketPunch()
        {
            if (_punchMotion == null) return;

            if (_ticketPunch != null) _ticketPunch.Kill();
            ticketIcon.localScale = _ticketBaseScale;
            _ticketPunch = ticketIcon
                .DOPunchScale(_ticketBaseScale * _punchMotion.ButtonPunchScale, _punchMotion.ButtonPunchDuration, _punchMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
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
            if (_ticketPunch != null)
            {
                _ticketPunch.Kill();
                _ticketPunch = null;
            }
            if (ticketIcon != null) ticketIcon.localScale = _ticketBaseScale;
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
