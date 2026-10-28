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
    /// Event select screen: lists the catalog's events with the EventRow prefab under the existing list Content (once,
    /// on first open; the catalog does not change at runtime). Tapping a playable row selects it and goes back.
    /// Never builds layout: Content, the row prefab and the Back button are scene/prefab objects bound in the Inspector.
    /// </summary>
    public sealed class EventSelectScreenView : UiScreen
    {
        [Tooltip("Panel/List/Viewport/Content: the empty list content the rows are created under.")]
        [SerializeField] private RectTransform content;

        [Tooltip("EventRow prefab asset (Assets/Speed-Staes/UI).")]
        [SerializeField] private EventRowView rowPrefab;

        [Tooltip("Back: returns to Home.")]
        [SerializeField] private Button backButton;

        private IInstantiator _instantiator;
        private RaceEventCatalog _catalog;
        private IRaceSelection _selection;
        private ScreenNavigator _navigator;
        private bool _built;

        [Inject]
        public void ConstructEventSelect(IInstantiator instantiator, RaceEventCatalog catalog, IRaceSelection selection, ScreenNavigator navigator)
        {
            _instantiator = instantiator;
            _catalog = catalog;
            _selection = selection;
            _navigator = navigator;
        }

        protected override void Awake()
        {
            base.Awake();
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildRows();
        }

        protected override void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBack);
            base.OnDestroy();
        }

        private void BuildRows()
        {
            if (_built || _catalog == null) return;
            _built = true;

            foreach (RaceEventDefinition definition in _catalog.GetEvents())
            {
                EventRowView row = _instantiator.InstantiatePrefabForComponent<EventRowView>(rowPrefab, content);
                row.Bind(definition, OnPicked);
            }
        }

        private void OnPicked(RaceEventId id)
        {
            _selection.Select(id);
            _navigator.Back();
        }

        private void OnBack() => _navigator.Back();
    }
}
