using System;
using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Home;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>
    /// Leaderboard screen (prefab HighScoresScreen; opened from Home and from the Results screen). Binds the objects of
    /// the prefab and draws what <see cref="LeaderboardPresenter"/> says; the filter windows are real popups of the
    /// navigator (so Back closes the open window before it leaves the screen, and only one is open at a time). Rows and
    /// list entries are prefab instances under their content parents; nothing is built here.
    /// Opens on Global / All Time / the race the player comes from (the selected race: on Home it is the picked event,
    /// after a race it is the race just run); when that race is not on offer, the first playable race.
    /// </summary>
    public sealed class LeaderboardScreenView : UiScreen
    {
        [Header("Header")]
        [Tooltip("Diamond/Value: the player's Cash.")]
        [SerializeField] private Text cashText;

        [Tooltip("Title/Label: 'LEADERBOARD'.")]
        [SerializeField] private Text titleText;

        [Header("Filter fields")]
        [Tooltip("Region field button.")]
        [SerializeField] private Button regionButton;

        [Tooltip("Region field label: GLOBAL or the country name.")]
        [SerializeField] private Text regionLabel;

        [Tooltip("Region field: globe layer, shown while Global.")]
        [SerializeField] private GameObject regionGlobe;

        [Tooltip("Region field: small flag, shown while a country is chosen.")]
        [SerializeField] private Image regionFlag;

        [Tooltip("Period field button.")]
        [SerializeField] private Button periodButton;

        [Tooltip("Period field label.")]
        [SerializeField] private Text periodLabel;

        [Tooltip("Event field button.")]
        [SerializeField] private Button eventButton;

        [Tooltip("Event field label: 'SPRINTS . 100m'.")]
        [SerializeField] private Text eventLabel;

        [Header("Table")]
        [Tooltip("Table ScrollRect (rows).")]
        [SerializeField] private ScrollRect scroll;

        [Tooltip("Table/List/Viewport/Content: row prefabs are created here.")]
        [SerializeField] private RectTransform rowsContent;

        [Tooltip("LeaderboardRow prefab.")]
        [SerializeField] private LeaderboardRowView rowPrefab;

        [Tooltip("Small loading indicator at the bottom of the table while the next page loads (turns around the icon's centre like the big one).")]
        [SerializeField] private GameObject loadMoreIndicator;

        [Tooltip("YouRow: the pinned line under the table (a LeaderboardRow instance).")]
        [SerializeField] private LeaderboardRowView youRow;

        [Tooltip("FIND ME button.")]
        [SerializeField] private Button findMeButton;

        [Header("Status (loading / empty / error)")]
        [Tooltip("Status/Loading group.")]
        [SerializeField] private GameObject loadingGroup;

        [Tooltip("Status/Loading/Spinner: rotates around the icon's centre while loading (its pivot is set to that point).")]
        [SerializeField] private RectTransform spinner;

        [Tooltip("Status/Empty group.")]
        [SerializeField] private GameObject emptyGroup;

        [Tooltip("Status/Error group.")]
        [SerializeField] private GameObject errorGroup;

        [Tooltip("Status/Error/Retry button.")]
        [SerializeField] private Button retryButton;

        [Header("Region window")]
        [Tooltip("RegionPopup: the window (a popup of the navigator).")]
        [SerializeField] private LeaderboardPopupPanel regionPopup;

        [Tooltip("Search field (legacy InputField).")]
        [SerializeField] private InputField searchInput;

        [Tooltip("Search field background: normal / focused sprite.")]
        [SerializeField] private Image searchBackground;

        [Tooltip("Search field sprite while idle.")]
        [SerializeField] private Sprite searchNormalSprite;

        [Tooltip("Search field sprite while typing.")]
        [SerializeField] private Sprite searchFocusedSprite;

        [Tooltip("Placeholder text of the search field.")]
        [SerializeField] private Text searchPlaceholder;

        [Tooltip("Clear (X) button of the search field; visible only while there is text.")]
        [SerializeField] private Button searchClearButton;

        [Tooltip("Region list Content: country row prefabs are created here.")]
        [SerializeField] private RectTransform countriesContent;

        [Tooltip("Country row prefab (LeaderboardChoiceView with flag and globe layers).")]
        [SerializeField] private LeaderboardChoiceView countryRowPrefab;

        [Tooltip("'NO COUNTRIES FOUND' text.")]
        [SerializeField] private Text noCountriesText;

        [Tooltip("RegionPopup header text ('REGION').")]
        [SerializeField] private Text regionHeaderText;

        [Header("Period window")]
        [Tooltip("PeriodPopup: the window.")]
        [SerializeField] private LeaderboardPopupPanel periodPopup;

        [Tooltip("The four period rows, in enum order: All Time, This Month, This Week, Today.")]
        [SerializeField] private LeaderboardChoiceView[] periodRows = new LeaderboardChoiceView[0];

        [Tooltip("PeriodPopup header text ('PERIOD').")]
        [SerializeField] private Text periodHeaderText;

        [Tooltip("Hint line under the period list.")]
        [SerializeField] private Text periodHintText;

        [Header("Event window")]
        [Tooltip("EventModal: the modal (a popup of the navigator).")]
        [SerializeField] private LeaderboardPopupPanel eventModal;

        [Tooltip("The category tabs (Sprints, Hurdles, Relays), in layout order.")]
        [SerializeField] private LeaderboardChoiceView[] categoryTabs = new LeaderboardChoiceView[0];

        [Tooltip("Distance grid Content: distance option prefabs are created here.")]
        [SerializeField] private RectTransform distancesContent;

        [Tooltip("Distance option prefab (LeaderboardChoiceView with a check mark).")]
        [SerializeField] private LeaderboardChoiceView distanceOptionPrefab;

        [Tooltip("'NO EVENTS AVAILABLE' text.")]
        [SerializeField] private Text noEventsText;

        [Tooltip("APPLY button (disabled until a race of the shown category is chosen).")]
        [SerializeField] private Button applyButton;

        [Tooltip("CANCEL button: closes the modal, the active filter stays.")]
        [SerializeField] private Button cancelButton;

        [Tooltip("X button of the modal: same as cancel.")]
        [SerializeField] private Button modalCloseButton;

        [Tooltip("EventModal header text ('EVENT').")]
        [SerializeField] private Text eventHeaderText;

        [Tooltip("Labels of APPLY / CANCEL / RETRY / FIND ME, in that order.")]
        [SerializeField] private Text[] buttonLabels = new Text[0];

        [Header("Content and art")]
        [Tooltip("Which race sits behind which category tab (the event select screen's content asset).")]
        [SerializeField] private EventSelectLayout eventLayout;

        [Tooltip("Flag art of the countries that have one.")]
        [SerializeField] private LeaderboardFlagEntry[] flags = new LeaderboardFlagEntry[0];

        [Tooltip("Flag art for a country without its own (the neutral flag).")]
        [SerializeField] private LeaderboardFlagEntry unknownFlag;

        [Header("Tuning")]
        [Tooltip("Rows asked per page.")]
        [Min(1)]
        [SerializeField] private int pageSize = 20;

        [Tooltip("The next page is asked when the list is scrolled this close to its end (0 = at the end, 1 = at the start).")]
        [Range(0f, 1f)]
        [SerializeField] private float loadMoreThreshold = 0.15f;

        [Tooltip("Spinner turn speed (degrees per second).")]
        [SerializeField] private float spinnerSpeed = 360f;

        [Tooltip("FIND ME scroll length (s).")]
        [Min(0f)]
        [SerializeField] private float findMeSeconds = 0.35f;

        [Tooltip("FIND ME scroll easing.")]
        [SerializeField] private Ease findMeEase = Ease.OutCubic;

        private LeaderboardPresenter _presenter;
        private ILeaderboard _leaderboard;
        private IWallet _wallet;
        private ScreenNavigator _navigator;
        private IPlayerSettings _settings;
        private IRaceSelection _selection;
        private RaceEventCatalog _catalog;
        private LeaderboardFlagLibrary _flagLibrary;
        private LeaderboardEventPicker _picker;
        private List<LeaderboardEventGroup> _groups = new List<LeaderboardEventGroup>();
        private List<LeaderboardCountry> _countries = new List<LeaderboardCountry>();
        private readonly List<LeaderboardRowView> _rowViews = new List<LeaderboardRowView>();
        private readonly List<LeaderboardChoiceView> _countryRows = new List<LeaderboardChoiceView>();
        private readonly List<LeaderboardChoiceView> _distanceRows = new List<LeaderboardChoiceView>();
        private bool _countryRowsBuilt;
        private bool _searchFocused;
        private Tween _scrollTween;

        [Inject]
        public void ConstructLeaderboard(ScreenNavigator navigator, IWallet wallet)
        {
            _navigator = navigator;
            _wallet = wallet;
        }

        /// <summary>Optional so a scene missing one of them still opens (the table then shows the error state / no countries).</summary>
        [Inject]
        public void ConstructLeaderboardExtras([InjectOptional] ILeaderboard leaderboard, [InjectOptional] IPlayerSettings settings,
            [InjectOptional] IRaceSelection selection, [InjectOptional] RaceEventCatalog catalog)
        {
            _leaderboard = leaderboard;
            _settings = settings;
            _selection = selection;
            _catalog = catalog;
        }

        protected override void Awake()
        {
            base.Awake();
            regionButton.onClick.AddListener(OnRegionButton);
            periodButton.onClick.AddListener(OnPeriodButton);
            eventButton.onClick.AddListener(OnEventButton);
            retryButton.onClick.AddListener(OnRetry);
            findMeButton.onClick.AddListener(OnFindMe);
            searchInput.onValueChanged.AddListener(OnSearchChanged);
            searchClearButton.onClick.AddListener(OnSearchClear);
            applyButton.onClick.AddListener(OnApply);
            cancelButton.onClick.AddListener(CloseEventModal);
            modalCloseButton.onClick.AddListener(CloseEventModal);
            regionPopup.Hidden += RestoreSearch;
            FillFixedTexts();
        }

        /// <summary>Everything that needs the injected services, done once on the first opening.</summary>
        private void EnsureReady()
        {
            if (_presenter != null) return;
            if (_leaderboard == null) Debug.LogWarning($"{name}: no ILeaderboard is bound; the table shows the error state.", this);
            _flagLibrary = new LeaderboardFlagLibrary(flags, unknownFlag);
            _presenter = new LeaderboardPresenter(_leaderboard, pageSize);
            _presenter.Changed += Redraw;
            _presenter.ScrollToRowRequested += ScrollToRow;

            BuildEventGroups();
            _picker = new LeaderboardEventPicker(_groups);
            for (int i = 0; i < categoryTabs.Length; i++) BindCategoryTab(i);
            BuildCountryList();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureReady();
            if (_wallet != null) _wallet.Changed += RefreshCash;
            RefreshCash();
            RaceEventId start = LeaderboardEventGroups.Resolve(_groups, _selection != null ? _selection.Selected : default);
            _presenter.Open(start); // every opening starts from the defaults
        }

        private void OnRetry() => _presenter.Retry();

        private void OnFindMe() => _presenter.FindMe();

        protected override void OnDisable()
        {
            if (_wallet != null) _wallet.Changed -= RefreshCash;
            KillScrollTween();
            CloseWindows();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.Changed -= Redraw;
                _presenter.ScrollToRowRequested -= ScrollToRow;
            }
            if (regionButton != null) regionButton.onClick.RemoveListener(OnRegionButton);
            if (periodButton != null) periodButton.onClick.RemoveListener(OnPeriodButton);
            if (eventButton != null) eventButton.onClick.RemoveListener(OnEventButton);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (findMeButton != null) findMeButton.onClick.RemoveListener(OnFindMe);
            if (searchInput != null) searchInput.onValueChanged.RemoveListener(OnSearchChanged);
            if (searchClearButton != null) searchClearButton.onClick.RemoveListener(OnSearchClear);
            if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(CloseEventModal);
            if (modalCloseButton != null) modalCloseButton.onClick.RemoveListener(CloseEventModal);
            if (regionPopup != null) regionPopup.Hidden -= RestoreSearch;
            KillScrollTween();
            base.OnDestroy();
        }

        private void Update()
        {
            if (_presenter == null) return;
            if (spinner != null && loadingGroup.activeSelf) spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
            if (loadMoreIndicator.activeSelf) loadMoreIndicator.transform.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);

            // Next page near the end of the list, or when the first page does not even fill the viewport.
            if (_presenter.CanLoadMore && !_presenter.IsLoadingMore)
            {
                bool nearEnd = scroll.verticalNormalizedPosition <= loadMoreThreshold;
                bool notFilled = rowsContent.rect.height <= scroll.viewport.rect.height;
                if (nearEnd || notFilled) _presenter.LoadMore();
            }

            bool focused = searchInput.isFocused;
            if (focused != _searchFocused)
            {
                _searchFocused = focused;
                RestoreSearch();
            }
        }

        // ---------------------------------------------------------------- build once

        private void FillFixedTexts()
        {
            titleText.text = LeaderboardTexts.Title;
            regionHeaderText.text = LeaderboardTexts.RegionHeader;
            periodHeaderText.text = LeaderboardTexts.PeriodHeader;
            eventHeaderText.text = LeaderboardTexts.EventHeader;
            searchPlaceholder.text = LeaderboardTexts.SearchPlaceholder;
            noCountriesText.text = LeaderboardTexts.NoCountries;
            noEventsText.text = LeaderboardTexts.NoEvents;
            periodHintText.text = LeaderboardTexts.PeriodHint;
            string[] captions = { LeaderboardTexts.Apply, LeaderboardTexts.Cancel, LeaderboardTexts.Retry, LeaderboardTexts.FindMe };
            for (int i = 0; i < buttonLabels.Length && i < captions.Length; i++) buttonLabels[i].text = captions[i];
        }

        private void BuildEventGroups()
        {
            var layout = new List<LeaderboardEventLayoutEntry>();
            if (eventLayout != null)
            {
                foreach (EventCategoryEntry entry in eventLayout.Categories)
                    layout.Add(new LeaderboardEventLayoutEntry(entry.Title, entry.EventIds));
            }
            if (layout.Count > categoryTabs.Length)
                Debug.LogWarning($"{name}: the layout has {layout.Count} categories but only {categoryTabs.Length} tabs; the extra ones are not shown.", this);

            _groups = LeaderboardEventGroups.Build(layout, LabelOfRace);
        }

        /// <summary>Button label of a race that exists and can be played; null otherwise (it is not offered).</summary>
        private string LabelOfRace(string id)
        {
            if (_catalog == null || !_catalog.TryGet(new RaceEventId(id), out RaceEventDefinition definition) || !definition.IsPlayable) return null;
            return definition.DisplayName;
        }

        private void BuildCountryList()
        {
            _countries = new List<LeaderboardCountry> { new LeaderboardCountry(string.Empty, LeaderboardTexts.Global) };
            if (_settings != null)
            {
                foreach (FlagInfo flag in _settings.Flags)
                    _countries.Add(new LeaderboardCountry(flag.Code, LeaderboardCountryList.Normalize(flag.DisplayName)));
            }
        }

        private void EnsureCountryRows()
        {
            if (_countryRowsBuilt) return;
            _countryRowsBuilt = true;
            foreach (LeaderboardCountry country in _countries)
            {
                LeaderboardChoiceView row = Instantiate(countryRowPrefab, countriesContent);
                _countryRows.Add(row);
            }
        }

        // ---------------------------------------------------------------- drawing

        private void RefreshCash()
        {
            if (_wallet != null) cashText.text = _wallet.Cash.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private void Redraw()
        {
            DrawFilters();
            DrawState();
            DrawRows();
            DrawYou();
        }

        private void DrawFilters()
        {
            string country = _presenter.CountryCode;
            bool global = country == null;
            regionGlobe.SetActive(global);
            regionFlag.enabled = !global;
            if (global)
            {
                regionLabel.text = LeaderboardTexts.Global;
            }
            else
            {
                regionLabel.text = CountryLabel(country);
                LeaderboardFlagEntry flag = _flagLibrary.Get(country);
                regionFlag.sprite = flag != null ? flag.Icon : null;
                regionFlag.enabled = regionFlag.sprite != null;
            }
            periodLabel.text = LeaderboardTexts.Period(_presenter.Period);
            eventLabel.text = LeaderboardEventGroups.FilterLabel(_groups, _presenter.EventId);
        }

        private string CountryLabel(string code)
        {
            foreach (LeaderboardCountry country in _countries)
                if (string.Equals(country.Code, code, StringComparison.Ordinal)) return country.Label;
            return code;
        }

        private void DrawState()
        {
            LeaderboardState state = _presenter.State;
            loadingGroup.SetActive(state == LeaderboardState.Loading);
            emptyGroup.SetActive(state == LeaderboardState.Empty);
            errorGroup.SetActive(state == LeaderboardState.Error);
            scroll.gameObject.SetActive(state == LeaderboardState.Content);
            loadMoreIndicator.SetActive(_presenter.IsLoadingMore);
            findMeButton.interactable = _presenter.CanFindMe;
        }

        /// <summary>Syncs the row instances with the loaded rows: a shorter list rebuilds, a longer one appends.</summary>
        private void DrawRows()
        {
            IReadOnlyList<LeaderboardRow> rows = _presenter.Rows;
            if (rows.Count < _rowViews.Count)
            {
                foreach (LeaderboardRowView old in _rowViews)
                {
                    if (old == null) continue;
                    old.gameObject.SetActive(false); // out of the layout now, destroyed at the end of the frame
                    Destroy(old.gameObject);
                }
                _rowViews.Clear();
                scroll.verticalNormalizedPosition = 1f; // a new filter starts at the top
            }

            for (int i = _rowViews.Count; i < rows.Count; i++)
            {
                LeaderboardRowView view = Instantiate(rowPrefab, rowsContent);
                LeaderboardRow row = rows[i];
                view.Bind(LeaderboardFormat.Row(row), RowFlag(row.CountryCode), row.IsLocalPlayer);
                _rowViews.Add(view);
            }
        }

        private void DrawYou()
        {
            LeaderboardState state = _presenter.State;
            bool show = state == LeaderboardState.Content || state == LeaderboardState.Empty;
            youRow.gameObject.SetActive(show);
            if (!show) return;

            LeaderboardRow own = _presenter.OwnEntry;
            LeaderboardRowData data = LeaderboardFormat.You(own, null);
            youRow.Bind(data, own != null ? RowFlag(own.CountryCode) : null, true);
            youRow.SetSeparator(false);
        }

        private Sprite RowFlag(string countryCode)
        {
            LeaderboardFlagEntry entry = _flagLibrary.Get(countryCode);
            return entry != null ? entry.TableRow : null;
        }

        // ---------------------------------------------------------------- windows

        private void OnRegionButton()
        {
            EnsureCountryRows();
            searchInput.text = string.Empty;
            RebuildCountries(string.Empty);
            OpenWindow(regionPopup);
        }

        private void OnPeriodButton()
        {
            for (int i = 0; i < periodRows.Length; i++)
            {
                var period = (LeaderboardPeriod)i;
                periodRows[i].Bind(LeaderboardTexts.Period(period), period == _presenter.Period, true, () => PickPeriod(period));
            }
            OpenWindow(periodPopup);
        }

        private void OnEventButton()
        {
            _picker.Begin(_presenter.EventId);
            DrawEventModal();
            OpenWindow(eventModal);
        }

        /// <summary>One window at a time: any other of ours is closed first.</summary>
        private void OpenWindow(LeaderboardPopupPanel window)
        {
            if (_navigator == null) return;
            foreach (LeaderboardPopupPanel other in new[] { regionPopup, periodPopup, eventModal })
                if (other != window) _navigator.ClosePopup(other);
            _navigator.OpenPopup(window);
        }

        private void CloseWindows()
        {
            if (_navigator == null) return;
            _navigator.ClosePopup(regionPopup);
            _navigator.ClosePopup(periodPopup);
            _navigator.ClosePopup(eventModal);
        }

        private void PickPeriod(LeaderboardPeriod period)
        {
            _presenter.SetPeriod(period);
            _navigator.ClosePopup(periodPopup);
        }

        private void PickCountry(string code)
        {
            _presenter.SetCountry(code);
            _navigator.ClosePopup(regionPopup);
        }

        private void OnSearchChanged(string text)
        {
            searchClearButton.gameObject.SetActive(!string.IsNullOrEmpty(text));
            RebuildCountries(text);
        }

        private void OnSearchClear() => searchInput.text = string.Empty;

        private void RestoreSearch()
        {
            if (searchNormalSprite != null && searchFocusedSprite != null)
                searchBackground.sprite = _searchFocused ? searchFocusedSprite : searchNormalSprite;
        }

        /// <summary>Shows the rows that match the query (the rows exist once; the others are switched off).</summary>
        private void RebuildCountries(string query)
        {
            searchClearButton.gameObject.SetActive(!string.IsNullOrEmpty(query));
            List<LeaderboardCountry> matches = LeaderboardCountryList.Filter(_countries, query);
            var shown = new HashSet<string>(StringComparer.Ordinal);
            foreach (LeaderboardCountry match in matches) shown.Add(match.Code);

            string active = _presenter.CountryCode ?? string.Empty;
            for (int i = 0; i < _countryRows.Count && i < _countries.Count; i++)
            {
                LeaderboardCountry country = _countries[i];
                LeaderboardChoiceView row = _countryRows[i];
                bool visible = shown.Contains(country.Code);
                row.gameObject.SetActive(visible);
                if (!visible) continue;

                bool global = country.Code.Length == 0;
                LeaderboardFlagEntry flag = global ? null : _flagLibrary.Get(country.Code);
                string code = country.Code;
                row.Bind(country.Label, string.Equals(code, active, StringComparison.Ordinal), true, () => PickCountry(code),
                    flag != null ? flag.ListRow : null, global);
            }
            noCountriesText.gameObject.SetActive(matches.Count == 0);
        }

        // ---------------------------------------------------------------- event modal

        private void BindCategoryTab(int index)
        {
            LeaderboardChoiceView tab = categoryTabs[index];
            bool exists = index < _groups.Count;
            tab.gameObject.SetActive(exists);
            if (!exists) return;
            int captured = index;
            tab.Bind(_groups[index].Title, false, true, () => OnCategory(captured));
        }

        private void OnCategory(int index)
        {
            _picker.SelectCategory(index);
            DrawEventModal();
        }

        private void DrawEventModal()
        {
            for (int i = 0; i < categoryTabs.Length && i < _groups.Count; i++)
                categoryTabs[i].SetState(i == _picker.Category, true);

            IReadOnlyList<LeaderboardEventOption> options = _picker.Options;
            while (_distanceRows.Count < options.Count) _distanceRows.Add(Instantiate(distanceOptionPrefab, distancesContent));
            RaceEventId chosen = _picker.Chosen;
            for (int i = 0; i < _distanceRows.Count; i++)
            {
                bool used = i < options.Count;
                _distanceRows[i].gameObject.SetActive(used);
                if (!used) continue;
                RaceEventId id = options[i].Id;
                _distanceRows[i].Bind(options[i].Label, id == chosen, true, () => OnDistance(id));
            }
            noEventsText.gameObject.SetActive(options.Count == 0);
            applyButton.interactable = _picker.CanApply;
        }

        private void OnDistance(RaceEventId id)
        {
            if (_picker.Choose(id)) DrawEventModal();
        }

        private void OnApply()
        {
            if (!_picker.CanApply) return;
            _presenter.ApplyEvent(_picker.Chosen);
            CloseEventModal();
        }

        private void CloseEventModal()
        {
            if (_navigator != null) _navigator.ClosePopup(eventModal);
        }

        // ---------------------------------------------------------------- FIND ME

        /// <summary>Eases the list so the row at <paramref name="index"/> sits in the middle of the viewport.</summary>
        private void ScrollToRow(int index)
        {
            if (index < 0 || index >= _rowViews.Count) return;
            Canvas.ForceUpdateCanvases(); // rows added this frame have no position yet
            RectTransform row = _rowViews[index].RectTransform;
            float centerFromTop = -rowsContent.InverseTransformPoint(row.TransformPoint(row.rect.center)).y;
            float scrollable = rowsContent.rect.height - scroll.viewport.rect.height;
            if (scrollable <= 0f) return;

            float offset = Mathf.Clamp(centerFromTop - scroll.viewport.rect.height * 0.5f, 0f, scrollable);
            float target = 1f - offset / scrollable;
            KillScrollTween();
            _scrollTween = DOTween.To(() => scroll.verticalNormalizedPosition, v => scroll.verticalNormalizedPosition = v, target, findMeSeconds)
                .SetEase(findMeEase)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillScrollTween()
        {
            if (_scrollTween == null) return;
            Tween running = _scrollTween;
            _scrollTween = null;
            running.Kill();
        }
    }
}
