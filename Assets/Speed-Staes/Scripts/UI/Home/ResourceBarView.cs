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
        [Tooltip("PurpleGem/Amount.")]
        [SerializeField] private Text gemAmount;

        [Tooltip("EntryTicket/Amount: 'count/cap'.")]
        [SerializeField] private Text ticketAmount;

        [Tooltip("EntryTicket/Countdown: time to the next ticket; hidden when the tickets are full.")]
        [SerializeField] private Text ticketCountdown;

        [Tooltip("TimedTicket/Amount: unused Timed Tickets.")]
        [SerializeField] private Text timedAmount;

        [Tooltip("Cash/Amount.")]
        [SerializeField] private Text cashAmount;

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
            gemAmount.text = _wallet.PurpleGems.ToString();
            cashAmount.text = _wallet.Cash.ToString();
            timedAmount.text = _wallet.TimedAccessActive ? $"{_wallet.TimedTickets} ON" : _wallet.TimedTickets.ToString();
            ticketAmount.text = HomeTexts.TicketCount(_tickets.Count, _tickets.Cap);
            RedrawCountdown();
        }

        private void RedrawCountdown()
        {
            DateTime? next = _tickets.NextTicketUtc;
            bool running = !_tickets.IsFull && next.HasValue;

            ticketCountdown.gameObject.SetActive(running);
            if (running) ticketCountdown.text = CountdownFormatter.Format(next.Value - _clock.UtcNow);
        }
    }
}
