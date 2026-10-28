using DG.Tweening;
using SpeedStars.Core.Race;
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
    /// Ticket status and the (still disabled) ways to get tickets. Confirm goes through <see cref="HomeEntryFlow"/>; a refused
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

        [Tooltip("Window/GetTickets/BuyWithCash: stays not interactable (no price yet).")]
        [SerializeField] private Button buyWithCashButton;

        [Tooltip("Window/GetTickets/WatchAd: stays not interactable (no price yet).")]
        [SerializeField] private Button watchAdButton;

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

        [Inject]
        public void ConstructEntryGate(IEntryTickets tickets, IWallet wallet, HomeEntryFlow entryFlow, ScreenNavigator navigator)
        {
            _tickets = tickets;
            _wallet = wallet;
            _entryFlow = entryFlow;
            _navigator = navigator;
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
            buyWithCashButton.interactable = false;
            watchAdButton.interactable = false;
            useTicketLabel.text = HomeTexts.UseEntryTicket;
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
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (useTicketButton != null) useTicketButton.onClick.RemoveListener(OnUseTicket);
            base.OnDestroy();
        }

        private void Refresh()
        {
            if (_event == null) return;

            EntryGateModel model = EntryGateModel.Build(_tickets.Count, _tickets.Cap, _wallet.TimedAccessActive);
            eventNameText.text = _event.DisplayName;
            useTicketButton.gameObject.SetActive(model.ShowUseTicketButton);
            useTicketButton.interactable = model.UseTicketInteractable;
            remainingText.text = model.RemainingText;
            timedLabel.text = model.TimedStatusText;
            timedValue.text = _wallet.TimedTickets.ToString();
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
