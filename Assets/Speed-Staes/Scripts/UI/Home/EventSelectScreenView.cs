using System.Collections.Generic;
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
    /// Event select screen: three category cards (Sprints / Hurdles / Relays), Race of the Day, Practice and Continue.
    /// All objects are prefab objects bound in the Inspector; this view only fills texts and flips art states. What is
    /// selected lives in <see cref="EventSelectModel"/> (pure C#); which race sits behind which button comes from the
    /// EventSelectLayout asset and the RaceEventCatalog. Back is UiPanel's close button. Continue writes the selection
    /// and goes back; on a race that is not playable it shakes instead (and looks dimmed).
    /// </summary>
    public sealed class EventSelectScreenView : UiScreen
    {
        [Tooltip("Content asset: heading, labels, which race sits behind which card / button, denied-shake numbers.")]
        [SerializeField] private EventSelectLayout layout;

        [Tooltip("Heading/Label: 'SELECT EVENT' text over the heading plate.")]
        [SerializeField] private Text headingText;

        [Tooltip("The category cards, in the same order as the layout asset's categories.")]
        [SerializeField] private EventCategoryCardView[] cards = new EventCategoryCardView[0];

        [Tooltip("RaceOfTheDay: bottom-row button.")]
        [SerializeField] private Button raceOfTheDayButton;

        [Tooltip("RaceOfTheDay/Label.")]
        [SerializeField] private Text raceOfTheDayLabel;

        [Tooltip("RaceOfTheDay outline: switched on while this button is the selection.")]
        [SerializeField] private Outline raceOfTheDayMark;

        [Tooltip("Practice: bottom-row button.")]
        [SerializeField] private Button practiceButton;

        [Tooltip("Practice/Label.")]
        [SerializeField] private Text practiceLabel;

        [Tooltip("Practice outline: switched on while this button is the selection.")]
        [SerializeField] private Outline practiceMark;

        [Tooltip("Continue: accepts the selection. Stays tappable even when dimmed, so the denied shake can play.")]
        [SerializeField] private Button continueButton;

        [Tooltip("Continue/Label.")]
        [SerializeField] private Text continueLabel;

        [Tooltip("Continue/Dimmed: grey layer over the button, shown while the selected race cannot be entered.")]
        [SerializeField] private GameObject continueDimmed;

        private RaceEventCatalog _catalog;
        private IRaceSelection _selection;
        private ScreenNavigator _navigator;
        private EventSelectModel _model;
        private Tween _shake;
        private Vector2 _continueBase;
        private bool _continueBaseKnown;

        [Inject]
        public void ConstructEventSelect(RaceEventCatalog catalog, IRaceSelection selection, ScreenNavigator navigator)
        {
            _catalog = catalog;
            _selection = selection;
            _navigator = navigator;
        }

        protected override void Awake()
        {
            base.Awake();
            raceOfTheDayButton.onClick.AddListener(OnRaceOfTheDay);
            practiceButton.onClick.AddListener(OnPractice);
            continueButton.onClick.AddListener(OnContinue);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildOnce();
            if (_model == null) return;
            _model.Restore(_selection.Selected);
            Refresh();
        }

        protected override void OnDisable()
        {
            StopShake();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (raceOfTheDayButton != null) raceOfTheDayButton.onClick.RemoveListener(OnRaceOfTheDay);
            if (practiceButton != null) practiceButton.onClick.RemoveListener(OnPractice);
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
            base.OnDestroy();
        }

        /// <summary>Texts, race lookup and card callbacks: once, on first open (the catalog and layout do not change at runtime).</summary>
        private void BuildOnce()
        {
            if (_model != null || _catalog == null || layout == null) return;

            headingText.text = layout.HeadingText;
            raceOfTheDayLabel.text = layout.RaceOfTheDayLabel;
            practiceLabel.text = layout.PracticeLabel;
            continueLabel.text = layout.ContinueLabel;

            var categories = new List<IReadOnlyList<EventSelectEntry>>();
            foreach (EventCategoryCardView card in cards)
            {
                EventCategoryEntry entry = FindCategory(card.Category);
                var entries = new List<EventSelectEntry>();
                var labels = new List<string>();
                if (entry != null)
                {
                    foreach (string id in entry.EventIds)
                    {
                        if (!_catalog.TryGet(new RaceEventId(id), out RaceEventDefinition definition))
                        {
                            Debug.LogWarning($"{name}: event '{id}' of category {card.Category} is not in the RaceEventCatalog; its button is hidden.", this);
                            continue;
                        }
                        entries.Add(new EventSelectEntry(definition.Id, definition.IsPlayable));
                        labels.Add(definition.DisplayName);
                    }
                }
                card.Bind(entry != null ? entry.Title : string.Empty, labels, OnCategory, OnDistance);
                categories.Add(entries);
            }

            _model = new EventSelectModel(categories, Lookup(layout.RaceOfTheDayId), Lookup(layout.PracticeId));
        }

        private EventCategoryEntry FindCategory(EventCategory category)
        {
            foreach (EventCategoryEntry entry in layout.Categories)
                if (entry.Category == category) return entry;
            return null;
        }

        private EventSelectEntry Lookup(string id)
        {
            if (_catalog.TryGet(new RaceEventId(id), out RaceEventDefinition definition))
                return new EventSelectEntry(definition.Id, definition.IsPlayable);
            return default;
        }

        private void Refresh()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                bool selected = _model.Target == EventSelectTarget.Category && _model.CategoryIndex == i;
                cards[i].SetSelected(selected, _model.DistanceIndex);
            }
            raceOfTheDayMark.enabled = _model.Target == EventSelectTarget.RaceOfTheDay;
            practiceMark.enabled = _model.Target == EventSelectTarget.Practice;
            continueDimmed.SetActive(!_model.CanContinue);
        }

        private void OnCategory(EventCategory category)
        {
            if (_model.SelectCategory(IndexOf(category))) Refresh();
        }

        private void OnDistance(EventCategory category, int distanceIndex)
        {
            if (_model.SelectDistance(IndexOf(category), distanceIndex)) Refresh();
        }

        private void OnRaceOfTheDay()
        {
            if (_model.SelectRaceOfTheDay()) Refresh();
        }

        private void OnPractice()
        {
            if (_model.SelectPractice()) Refresh();
        }

        private void OnContinue()
        {
            if (_model == null) return;
            if (!_model.CanContinue || !_model.TryGetCurrent(out EventSelectEntry entry))
            {
                PlayDenied();
                return;
            }
            _selection.Select(entry.Id);
            _navigator.Back();
        }

        private int IndexOf(EventCategory category)
        {
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].Category == category) return i;
            return -1;
        }

        private void PlayDenied()
        {
            var rect = (RectTransform)continueButton.transform;
            if (!_continueBaseKnown)
            {
                _continueBaseKnown = true;
                _continueBase = rect.anchoredPosition; // the designer's position; the shake always returns to it
            }

            StopShake();
            _shake = rect
                .DOShakePosition(layout.DeniedShakeDuration, new Vector3(layout.DeniedShakeStrength, 0f, 0f), layout.DeniedShakeVibrato, 0f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void StopShake()
        {
            if (_shake != null)
            {
                _shake.Kill();
                _shake = null;
            }
            if (_continueBaseKnown && continueButton != null) ((RectTransform)continueButton.transform).anchoredPosition = _continueBase;
        }
    }
}
