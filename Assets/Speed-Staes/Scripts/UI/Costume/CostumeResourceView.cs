using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// The four resource boxes down the left of the costume editor: Purple Gems, Entry Tickets ("count/cap"), Timed
    /// Tickets and Cash. Same numbers as the Home resource bar (it redraws when the wallet or tickets change); the ticket
    /// regen countdown is not shown here. Binds existing texts, creates nothing.
    /// </summary>
    public sealed class CostumeResourceView : MonoBehaviour
    {
        [Tooltip("PurpleGem/Value.")]
        [SerializeField] private Text purpleGemText;

        [Tooltip("EntryTicket/Value: 'count/cap'.")]
        [SerializeField] private Text entryTicketText;

        [Tooltip("TimedTicket/Value: unused Timed Tickets, or the 'active' text while the window runs.")]
        [SerializeField] private Text timedTicketText;

        [Tooltip("Cash/Value.")]
        [SerializeField] private Text cashText;

        [Tooltip("Costume editor config asset (the text of the active Timed Ticket window).")]
        [SerializeField] private CostumeEditorConfig config;

        private IWallet _wallet;
        private IEntryTickets _tickets;

        [Inject]
        public void ConstructCostumeResources(IWallet wallet, IEntryTickets tickets)
        {
            _wallet = wallet;
            _tickets = tickets;
        }

        private void OnEnable()
        {
            _wallet.Changed += Redraw;
            _tickets.Changed += Redraw;
            _tickets.Refresh(); // regen is lazy: bring the count up to date once when the screen opens
            Redraw();
        }

        private void OnDisable()
        {
            _wallet.Changed -= Redraw;
            _tickets.Changed -= Redraw;
        }

        private void Redraw()
        {
            purpleGemText.text = _wallet.PurpleGems.ToString();
            cashText.text = _wallet.Cash.ToString();
            entryTicketText.text = HomeTexts.TicketCount(_tickets.Count, _tickets.Cap);
            timedTicketText.text = _wallet.TimedAccessActive && config != null
                ? string.Format(config.TimedActiveFormat, _wallet.TimedTickets)
                : _wallet.TimedTickets.ToString();
        }
    }
}
