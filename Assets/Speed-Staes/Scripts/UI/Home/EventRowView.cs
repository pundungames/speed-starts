using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// One row of the event select list (prefab EventRow). Binds the row's existing children; the screen view instantiates
    /// the prefab and calls <see cref="Bind"/>. Root carries the Button (the whole row is the tap target).
    /// </summary>
    public sealed class EventRowView : MonoBehaviour
    {
        [Tooltip("Root Button of the row prefab (the whole row is the tap target).")]
        [SerializeField] private Button button;

        [Tooltip("Row/Name: event display name.")]
        [SerializeField] private Text nameText;

        [Tooltip("Row/Distance: race length, e.g. 100m.")]
        [SerializeField] private Text distanceText;

        [Tooltip("Row/TicketBadge: shown when the event needs an Entry Ticket.")]
        [SerializeField] private GameObject ticketBadge;

        [Tooltip("Row/Lock: shown (and the row made untappable) when the event is not playable yet.")]
        [SerializeField] private GameObject lockOverlay;

        private RaceEventId _id;
        private Action<RaceEventId> _onPicked;

        public void Bind(RaceEventDefinition definition, Action<RaceEventId> onPicked)
        {
            _id = definition.Id;
            _onPicked = onPicked;

            nameText.text = definition.DisplayName;
            distanceText.text = $"{definition.DistanceMeters:0}m";
            ticketBadge.SetActive(definition.RequiresTicket);
            lockOverlay.SetActive(!definition.IsPlayable);
            button.interactable = definition.IsPlayable;

            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked() => _onPicked?.Invoke(_id);
    }
}
