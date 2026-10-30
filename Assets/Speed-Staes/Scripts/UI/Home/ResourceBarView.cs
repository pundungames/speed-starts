using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Resource bar (prefab ResourceBar, on the overlay canvas): Purple Gems, Entry Tickets with the regen countdown,
    /// Timed Tickets and Cash. Visible only while the Home screen is the top screen (other full screens have their own
    /// resource columns or none); popups do not hide it. Redraws when the wallet or tickets change; about once a second it calls
    /// <c>IEntryTickets.Refresh</c> (regen is lazy) and updates the countdown. Binds existing texts, creates nothing.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ResourceBarView : MonoBehaviour
    {
        [Tooltip("PurpleGem/Value.")]
        [SerializeField] private Text purpleGemText;

        [Tooltip("EntryTicket/Value: 'count/cap'.")]
        [SerializeField] private Text entryTicketText;

        [Tooltip("TicketTimer (whole chip): hidden when the tickets are full.")]
        [SerializeField] private GameObject entryTimerRoot;

        [Tooltip("TicketTimer/Value: time to the next ticket.")]
        [SerializeField] private Text entryTimerText;

        [Tooltip("TimedTicket/Value: unused Timed Tickets, or the time left while the window runs.")]
        [SerializeField] private Text timedTicketText;

        [Tooltip("Cash/Value.")]
        [SerializeField] private Text cashText;

        [Tooltip("EntryTicket (the whole box): where the daily gift ticket flies to and punches. Empty = the parent of the ticket value text.")]
        [SerializeField] private RectTransform entryTicketBox;

        [Tooltip("The Home screen (root of the stack): the bar is shown only while it is the top screen. Empty = always shown.")]
        [SerializeField] private UiScreen homeScreen;

        [Tooltip("Seconds between ticket refresh ticks.")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        private IWallet _wallet;
        private IEntryTickets _tickets;
        private IClock _clock;
        private ScreenNavigator _navigator;
        private CanvasGroup _group;
        private float _elapsed;

        /// <summary>The Entry Ticket box of the bar: the target of the daily gift ticket flight.</summary>
        public RectTransform EntryTicketBox => entryTicketBox != null ? entryTicketBox : entryTicketText.transform.parent as RectTransform;

        [Inject]
        public void ConstructResourceBar(IWallet wallet, IEntryTickets tickets, IClock clock)
        {
            _wallet = wallet;
            _tickets = tickets;
            _clock = clock;
        }

        /// <summary>Optional so a scene without a navigator keeps the bar always shown.</summary>
        [Inject]
        public void ConstructResourceBarNavigation([InjectOptional] ScreenNavigator navigator)
        {
            _navigator = navigator;
        }

        private void OnEnable()
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            if (_navigator != null) _navigator.CurrentScreenChanged += OnScreenChanged;
            ApplyVisibility();
            _wallet.Changed += Redraw;
            _tickets.Changed += Redraw;
            _elapsed = 0f;
            _tickets.Refresh();
            Redraw();
        }

        private void OnDisable()
        {
            if (_navigator != null) _navigator.CurrentScreenChanged -= OnScreenChanged;
            _wallet.Changed -= Redraw;
            _tickets.Changed -= Redraw;
        }

        private void OnScreenChanged(IUiView top) => ApplyVisibility();

        /// <summary>Shown before the first screen opens and while Home is on top; a CanvasGroup so the bar keeps ticking.</summary>
        private void ApplyVisibility()
        {
            IUiView top = _navigator?.CurrentScreen;
            bool visible = homeScreen == null || top == null || ReferenceEquals(top, homeScreen);
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
            _group.interactable = visible;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;

            _elapsed -= tickInterval;
            _tickets.Refresh(); // may raise Changed, which redraws
            RedrawCountdown();
            RedrawTimed();
        }

        private void Redraw()
        {
            purpleGemText.text = _wallet.PurpleGems.ToString();
            cashText.text = _wallet.Cash.ToString();
            RedrawTimed();
            entryTicketText.text = HomeTexts.TicketCount(_tickets.Count, _tickets.Cap);
            RedrawCountdown();
        }

        /// <summary>Stock normally; the countdown while a Timed Ticket window runs (the once-a-second tick keeps it moving).</summary>
        private void RedrawTimed()
        {
            DateTime? ends = _wallet.TimedAccessEndsUtc;
            timedTicketText.text = _wallet.TimedAccessActive && ends.HasValue
                ? CountdownFormatter.Format(ends.Value - _clock.UtcNow)
                : _wallet.TimedTickets.ToString();
        }

        private void RedrawCountdown()
        {
            DateTime? next = _tickets.NextTicketUtc;
            bool running = !_tickets.IsFull && next.HasValue;

            entryTimerRoot.SetActive(running);
            if (running) entryTimerText.text = CountdownFormatter.Format(next.Value - _clock.UtcNow);
        }
    }
}
