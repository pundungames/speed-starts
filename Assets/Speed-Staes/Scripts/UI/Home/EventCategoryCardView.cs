using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Home
{
    /// <summary>One distance button of a category card: its tap target, the two art states and its label.</summary>
    [Serializable]
    public sealed class EventDistanceSlot
    {
        [Tooltip("Root Button of the distance button.")]
        [SerializeField] private Button button;

        [Tooltip("Art layer of the normal state.")]
        [SerializeField] private GameObject normalLayer;

        [Tooltip("Art layer of the selected (red) state.")]
        [SerializeField] private GameObject selectedLayer;

        [Tooltip("Label drawn over the button (the race's display name).")]
        [SerializeField] private Text label;

        public Button Button => button;
        public GameObject NormalLayer => normalLayer;
        public GameObject SelectedLayer => selectedLayer;
        public Text Label => label;
    }

    /// <summary>
    /// One category card (prefab object under EventSelectScreen). Binds the card's existing children and flips between the
    /// art states; the screen view owns the selection (EventSelectModel) and tells the card what to show. Never builds
    /// layout: slots without a race are just switched off.
    /// </summary>
    public sealed class EventCategoryCardView : MonoBehaviour
    {
        [Tooltip("Which category this card is (matches the layout asset's entry).")]
        [SerializeField] private EventCategory category;

        [Tooltip("Card root Button: tapping anywhere on the card selects it.")]
        [SerializeField] private Button button;

        [Tooltip("Card/Normal: white card art.")]
        [SerializeField] private GameObject cardNormal;

        [Tooltip("Card/Selected: card art with the red frame.")]
        [SerializeField] private GameObject cardSelected;

        [Tooltip("Card/HeaderNormal: cream header art.")]
        [SerializeField] private GameObject headerNormal;

        [Tooltip("Card/HeaderSelected: red header art.")]
        [SerializeField] private GameObject headerSelected;

        [Tooltip("Card/Title: header text.")]
        [SerializeField] private Text titleText;

        [Tooltip("Card/Distances: parent of the distance buttons, visible only while the card is selected.")]
        [SerializeField] private GameObject distanceRoot;

        [Tooltip("The three distance buttons, left to right.")]
        [SerializeField] private EventDistanceSlot[] distances = new EventDistanceSlot[0];

        [Tooltip("Text color on a selected (red) header / distance button.")]
        [SerializeField] private Color selectedTextColor = Color.white;

        [Tooltip("Text color on a normal (cream) header / distance button.")]
        [SerializeField] private Color normalTextColor = new Color(0.04f, 0.12f, 0.36f, 1f);

        private Action<EventCategory> _onCard;
        private Action<EventCategory, int> _onDistance;
        private int _distanceCount;
        private bool _bound;

        public EventCategory Category => category;

        /// <summary>Fills the card once: title, one label per race, and the tap callbacks. Slots beyond the labels are switched off.</summary>
        public void Bind(string title, IReadOnlyList<string> distanceLabels, Action<EventCategory> onCard, Action<EventCategory, int> onDistance)
        {
            _onCard = onCard;
            _onDistance = onDistance;
            _distanceCount = Mathf.Min(distanceLabels.Count, distances.Length);
            titleText.text = title;

            for (int i = 0; i < distances.Length; i++)
            {
                EventDistanceSlot slot = distances[i];
                slot.Button.gameObject.SetActive(i < _distanceCount);
                if (i < _distanceCount) slot.Label.text = distanceLabels[i];
            }

            if (_bound) return;
            _bound = true;
            button.onClick.AddListener(OnCardClicked);
            for (int i = 0; i < distances.Length; i++)
            {
                int index = i; // each button reports its own slot
                distances[i].Button.onClick.AddListener(() => _onDistance?.Invoke(category, index));
            }
        }

        /// <summary>Card on/off and, while on, which distance button is the active one.</summary>
        public void SetSelected(bool selected, int distanceIndex)
        {
            cardNormal.SetActive(!selected);
            cardSelected.SetActive(selected);
            headerNormal.SetActive(!selected);
            headerSelected.SetActive(selected);
            titleText.color = selected ? selectedTextColor : normalTextColor;
            distanceRoot.SetActive(selected);

            for (int i = 0; i < _distanceCount; i++)
            {
                bool active = selected && i == distanceIndex;
                EventDistanceSlot slot = distances[i];
                slot.NormalLayer.SetActive(!active);
                slot.SelectedLayer.SetActive(active);
                slot.Label.color = active ? selectedTextColor : normalTextColor;
            }
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnCardClicked);
            for (int i = 0; i < distances.Length; i++)
                if (distances[i].Button != null) distances[i].Button.onClick.RemoveAllListeners();
        }

        private void OnCardClicked() => _onCard?.Invoke(category);
    }
}
