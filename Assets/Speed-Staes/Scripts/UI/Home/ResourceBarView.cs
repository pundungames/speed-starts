using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Contracts;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Resource bar (prefab ResourceBar, on the overlay canvas): Purple Gems, Entry Tickets with the regen countdown,
    /// Timed Tickets and Cash. Redraws when the wallet or tickets change; about once a second it calls
    /// <c>IEntryTickets.Refresh</c> (regen is lazy) and updates the countdown. Binds existing texts, creates nothing.
    /// </summary>
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

        [Tooltip("TimedTicket/Value: unused Timed Tickets, or 'n ON' while the window runs.")]
        [SerializeField] private Text timedTicketText;

        [Tooltip("Cash/Value.")]
        [SerializeField] private Text cashText;

        [Tooltip("Seconds between ticket refresh ticks.")]
        [Min(0.1f)]
        [SerializeField] private float tickInterval = 1f;

        private IWallet _wallet;
        private IEntryTickets _tickets;
        private IClock _clock;
        private float _elapsed;

        [Inject]
        public void ConstructResourceBar(IWallet wallet, IEntryTickets tickets, IClock clock)
        {
            _wallet = wallet;
            _tickets = tickets;
            _clock = clock;
        }

        private void OnEnable()
        {
            _wallet.Changed += Redraw;
            _tickets.Changed += Redraw;
            _elapsed = 0f;
            _tickets.Refresh();
            Redraw();
        }

        private void OnDisable()
        {
            _wallet.Changed -= Redraw;
            _tickets.Changed -= Redraw;
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < tickInterval) return;

            _elapsed -= tickInterval;
            _tickets.Refresh(); // may raise Changed, which redraws
            RedrawCountdown();
        }

        private void Redraw()
        {
            purpleGemText.text = _wallet.PurpleGems.ToString();
            cashText.text = _wallet.Cash.ToString();
            timedTicketText.text = _wallet.TimedAccessActive ? $"{_wallet.TimedTickets} ON" : _wallet.TimedTickets.ToString();
            entryTicketText.text = HomeTexts.TicketCount(_tickets.Count, _tickets.Cap);
            RedrawCountdown();
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
