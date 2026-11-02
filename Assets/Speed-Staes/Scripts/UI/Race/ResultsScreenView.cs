using System;
using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Leaderboard;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Post-race results screen (Oyun_Bitis_UI_v1): Cash, the Personal Best / World Position / Percentile cards, the
    /// results table (one row prefab under a content parent), the Best of the day panel and the left button column.
    /// Race Again, Main Menu and Stats (opens the Analysis screen) work; Watch Replay, High Scores and Practice have no feature behind them yet and
    /// start disabled. The prefab holds every object; this only binds and fills them. On open the cards and panels
    /// reveal one after another (UiMotionConfig stagger) and a new record punches its card.
    /// </summary>
    public sealed class ResultsScreenView : UiScreen, IResultsView, IGemRewardView, IAnalysisSink
    {
        [Header("Cash")]
        [Tooltip("Diamond/Value: the player's Cash.")]
        [SerializeField] private Text cashText;

        [Header("Cards")]
        [Tooltip("TopCards/PersonalBest: the card that punches on a new record.")]
        [SerializeField] private RectTransform personalBestCard;

        [Tooltip("PersonalBest/Title: 'PERSONAL BEST', or 'NEW RECORD' after an improvement.")]
        [SerializeField] private Text personalBestTitleText;

        [Tooltip("PersonalBest/Value: the best time.")]
        [SerializeField] private Text personalBestValueText;

        [Tooltip("PersonalBest/Event: event name.")]
        [SerializeField] private Text personalBestEventText;

        [Tooltip("Title colour while the card says NEW RECORD.")]
        [SerializeField] private Color newRecordColor = new Color(0.92f, 0.12f, 0.14f, 1f);

        [Tooltip("WorldPosition/Value: '#rank' on the leaderboard, a dash without a record.")]
        [SerializeField] private Text worldPositionText;

        [Tooltip("Percentile/Value: 'TOP n%' on the leaderboard, a dash without a record.")]
        [SerializeField] private Text percentileText;

        [Header("Results table")]
        [Tooltip("ResultsPanel/Header/Title: event name.")]
        [SerializeField] private Text eventTitleText;

        [Tooltip("ResultsPanel/RowsScroll/Viewport/Rows: parent the row prefabs are created under (the scrolling content).")]
        [SerializeField] private Transform rowsContent;

        [Tooltip("ResultsTableRow prefab with a ResultsTableRowView on its root.")]
        [SerializeField] private ResultsTableRowView rowPrefab;

        [Tooltip("ResultsPanel/RowsScroll: scrolls the table when more runners than the panel has room for are listed. May stay empty (then the table just overflows).")]
        [SerializeField] private ScrollRect rowsScroll;

        [Tooltip("Flag art of the countries that have one (the same entries as the leaderboard; only the icon is used).")]
        [SerializeField] private LeaderboardFlagEntry[] flags = new LeaderboardFlagEntry[0];

        [Tooltip("Flag art for a country without its own (the neutral flag).")]
        [SerializeField] private LeaderboardFlagEntry unknownFlag;

        [Header("Best of the day")]
        [Tooltip("DailyBest/Row/Label: today's best (name and time) from the leaderboard, an empty-state text without data.")]
        [SerializeField] private Text dailyBestText;

        [Header("Gem reward")]
        [Tooltip("GemReward: the '+N Purple Gems' line, visible only when the race paid Purple Gems.")]
        [SerializeField] private GameObject gemRewardRoot;

        [Tooltip("GemReward/Label.")]
        [SerializeField] private Text gemRewardText;

        [Header("Buttons")]
        [Tooltip("Nav/RaceAgain.")]
        [SerializeField] private Button raceAgainButton;

        [Tooltip("Nav/MainMenu.")]
        [SerializeField] private Button mainMenuButton;

        [Tooltip("Nav/WatchReplay: disabled, there is no replay system yet.")]
        [SerializeField] private Button watchReplayButton;

        [Tooltip("Nav/HighScores: opens the leaderboard screen (disabled while that screen is not assigned).")]
        [SerializeField] private Button highScoresButton;

        [Tooltip("The leaderboard screen of this scene (HighScoresScreen prefab instance, starts inactive).")]
        [SerializeField] private UiScreen highScoresScreen;

        [Tooltip("Nav/Stats: opens the Analysis screen; disabled while no Analysis screen is assigned.")]
        [SerializeField] private Button statsButton;

        [Tooltip("Nav/Practice: disabled, the training event is not playable yet.")]
        [SerializeField] private Button practiceButton;

        [Header("Analysis")]
        [Tooltip("The Analysis screen of this scene (a sibling prefab instance, wired by SceneWiring). Stats opens it; the flow hands it the race's split data.")]
        [SerializeField] private AnalysisScreenView analysisScreen;

        [Header("Open animation")]
        [Tooltip("Cards, panels and buttons that reveal one after another on open, in this order (each needs a CanvasGroup). Inactive ones are skipped.")]
        [SerializeField] private CanvasGroup[] revealItems;

        private UiMotionConfig _resultsMotion;
        private ScreenNavigator _resultsNavigator;
        private ILeaderboard _resultsLeaderboard;
        private IRaceSelection _resultsSelection;
        private readonly List<ResultsTableRowView> _rows = new List<ResultsTableRowView>();
        private IRaceStandings _standings;
        private LeaderboardFlagLibrary _flagLibrary;
        private ResultsData _presented;
        private bool _hasPresented;
        private Vector3[] _baseScales;
        private Color _normalTitleColor;
        private Sequence _reveal;
        private Tween _denied;
        private Vector3 _deniedBase;
        private bool _punchRecord;
        private bool _punchGems;

        public event Action RaceAgainRequested;
        public event Action MainMenuRequested;

        [Inject]
        public void InjectMotion(UiMotionConfig motion)
        {
            _resultsMotion = motion;
        }

        /// <summary>Optional so a scene without the leaderboard keeps the cards as dashes and the High Scores button off.</summary>
        [Inject]
        public void InjectResultsNavigator([InjectOptional] ScreenNavigator navigator, [InjectOptional] ILeaderboard leaderboard, [InjectOptional] IRaceSelection selection)
        {
            _resultsNavigator = navigator;
            _resultsLeaderboard = leaderboard;
            _resultsSelection = selection;
        }

        /// <summary>Optional: without opponents (no standings) the table shows the player's own race as before.</summary>
        [Inject]
        public void InjectStandings([InjectOptional] IRaceStandings standings)
        {
            _standings = standings;
        }

        public float SplitStepMeters => analysisScreen != null ? analysisScreen.SplitStepMeters : 0f;

        public float FinishToleranceMeters => analysisScreen != null ? analysisScreen.FinishToleranceMeters : 0f;

        public void PresentAnalysis(AnalysisData data)
        {
            if (analysisScreen != null) analysisScreen.PresentAnalysis(data);
        }

        protected override void Awake()
        {
            base.Awake();
            if (personalBestTitleText != null) _normalTitleColor = personalBestTitleText.color;
            CaptureScales();

            if (watchReplayButton != null) watchReplayButton.interactable = false;
            if (highScoresButton != null)
            {
                highScoresButton.interactable = highScoresScreen != null;
                highScoresButton.onClick.AddListener(OnHighScoresClicked);
            }
            if (statsButton != null)
            {
                statsButton.interactable = analysisScreen != null;
                statsButton.onClick.AddListener(OnStatsClicked);
            }
            if (practiceButton != null) practiceButton.interactable = false;
            if (raceAgainButton != null) raceAgainButton.onClick.AddListener(OnRaceAgainClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_standings != null)
            {
                _standings.Changed += OnStandingsChanged;
                if (_hasPresented) RefreshRows(animateNew: false); // a runner may have finished between Present and opening
            }
            PlayReveal();
        }

        protected override void OnDisable()
        {
            if (_standings != null) _standings.Changed -= OnStandingsChanged;
            base.OnDisable();
            KillDenied();
            KillReveal();
            RestoreItems();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillDenied();
            KillReveal();
            if (highScoresButton != null) highScoresButton.onClick.RemoveListener(OnHighScoresClicked);
            if (raceAgainButton != null) raceAgainButton.onClick.RemoveListener(OnRaceAgainClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            if (statsButton != null) statsButton.onClick.RemoveListener(OnStatsClicked);
        }

        public void Present(ResultsData data)
        {
            if (cashText != null) cashText.text = data.CashText;
            if (eventTitleText != null) eventTitleText.text = data.EventTitle;
            if (worldPositionText != null) worldPositionText.text = data.WorldPositionText;
            if (percentileText != null) percentileText.text = data.PercentileText;
            if (dailyBestText != null) dailyBestText.text = data.DailyBestText;

            if (personalBestValueText != null) personalBestValueText.text = data.PersonalBestValue;
            if (personalBestEventText != null) personalBestEventText.text = data.PersonalBestEvent;
            if (personalBestTitleText != null)
            {
                if (_normalTitleColor.a <= 0f) _normalTitleColor = personalBestTitleText.color; // Present before Awake: the prefab colour
                personalBestTitleText.text = data.PersonalBestTitle;
                personalBestTitleText.color = data.IsNewRecord ? newRecordColor : _normalTitleColor;
            }
            _punchRecord = data.IsNewRecord; // the screen is not on yet: the punch plays when it opens

            _presented = data;
            _hasPresented = true;
            RefreshRows(animateNew: false);
            ShowLeaderboardCards();
        }

        /// <summary>WORLD POSITION / PERCENTILE / BEST OF THE DAY from the leaderboard; without data the model's dashes stay.</summary>
        private void ShowLeaderboardCards()
        {
            if (_resultsLeaderboard == null || _resultsSelection == null) return;
            ResultsCardTexts cards = ResultsLeaderboardCards.Build(_resultsLeaderboard, _resultsSelection.Selected);
            if (worldPositionText != null) worldPositionText.text = cards.WorldPosition;
            if (percentileText != null) percentileText.text = cards.Percentile;
            if (dailyBestText != null) dailyBestText.text = cards.BestOfTheDay;
        }

        private void OnHighScoresClicked()
        {
            if (_resultsNavigator != null && highScoresScreen != null) _resultsNavigator.Push(highScoresScreen);
        }

        public void ShowGemReward(int gems)
        {
            bool paid = gems > 0;
            if (gemRewardRoot != null) gemRewardRoot.SetActive(paid);
            if (paid && gemRewardText != null) gemRewardText.text = ResultsTexts.GemReward(gems);
            _punchGems = paid;
        }

        public void NotifyDenied()
        {
            if (raceAgainButton == null || _resultsMotion == null || !isActiveAndEnabled) return;

            KillDenied();
            Transform target = raceAgainButton.transform;
            _deniedBase = target.localPosition;
            float width = ((RectTransform)target).rect.width;
            Tween shake = target
                .DOPunchPosition(Vector3.right * (width * _resultsMotion.ButtonPunchScale), _resultsMotion.ButtonPunchDuration, _resultsMotion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            shake.OnKill(() =>
            {
                if (_denied == shake) _denied = null;
                if (target != null) target.localPosition = _deniedBase;
            });
            _denied = shake;
        }

        private void OnStandingsChanged() => RefreshRows(animateNew: true);

        /// <summary>
        /// The table: every runner of the live standings when opponents run, else the player's own race from the model.
        /// Existing lines are refilled in place; only lines beyond the old count are created (and fade in when the table is open).
        /// </summary>
        private void RefreshRows(bool animateNew)
        {
            if (!_hasPresented) return;

            IReadOnlyList<ResultRowData> rows = _presented.Rows;
            ResultRowData playerRow = default;
            bool hasPlayerRow = false;
            foreach (ResultRowData candidate in _presented.Rows)
            {
                if (!candidate.IsPlayer) continue;
                playerRow = candidate;
                hasPlayerRow = true;
                break;
            }
            if (_standings != null && hasPlayerRow && _standings.RunnerCount > 1)
                rows = RaceStandingsFormat.Rows(_standings.Entries, playerRow);

            if (rowsContent == null || rowPrefab == null)
            {
                Debug.LogWarning($"{name}: results rows content or row prefab is not assigned; the table is empty.", this);
                return;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                bool created = i >= _rows.Count;
                ResultsTableRowView view = created ? Instantiate(rowPrefab, rowsContent) : _rows[i];
                if (created) _rows.Add(view);
                view.Fill(rows[i], _resultsMotion, FlagIcon(rows[i].CountryCode));
                if (created && animateNew && isActiveAndEnabled) view.Reveal(_resultsMotion);
            }

            for (int i = _rows.Count - 1; i >= rows.Count; i--)
            {
                ResultsTableRowView old = _rows[i];
                _rows.RemoveAt(i);
                if (old == null) continue;
                old.gameObject.SetActive(false); // out of the layout now, destroyed at the end of the frame
                Destroy(old.gameObject);
            }

            if (hasPlayerRow) KeepPlayerRowVisible(rows);
        }

        /// <summary>The flag icon of a country; no code = no flag, a code without art = the neutral flag.</summary>
        private Sprite FlagIcon(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) return null;
            if (_flagLibrary == null) _flagLibrary = new LeaderboardFlagLibrary(flags, unknownFlag);
            LeaderboardFlagEntry entry = _flagLibrary.Get(countryCode);
            return entry != null ? entry.Icon : null;
        }

        /// <summary>With more lines than the panel shows, scrolls just far enough that the player's own line is in view.</summary>
        private void KeepPlayerRowVisible(IReadOnlyList<ResultRowData> rows)
        {
            if (rowsScroll == null || rowsScroll.viewport == null || rowsScroll.content == null) return;
            int at = -1;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].IsPlayer) { at = i; break; }
            if (at < 0 || at >= _rows.Count || _rows[at] == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsScroll.content);
            RectTransform viewport = rowsScroll.viewport;
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, (RectTransform)_rows[at].transform);
            Rect view = viewport.rect;
            float shift = 0f;
            if (bounds.min.y < view.yMin) shift = view.yMin - bounds.min.y;      // line below the view: content moves up
            else if (bounds.max.y > view.yMax) shift = view.yMax - bounds.max.y; // line above the view: content moves down
            if (Mathf.Approximately(shift, 0f)) return;

            Vector2 position = rowsScroll.content.anchoredPosition;
            position.y += shift;
            rowsScroll.content.anchoredPosition = position;
        }

        // ---------------------------------------------------------------- open animation

        private void CaptureScales()
        {
            if (revealItems == null) return;
            _baseScales = new Vector3[revealItems.Length];
            for (int i = 0; i < revealItems.Length; i++)
                _baseScales[i] = revealItems[i] != null ? revealItems[i].transform.localScale : Vector3.one;
        }

        private void PlayReveal()
        {
            KillReveal();
            if (_resultsMotion == null || revealItems == null || revealItems.Length == 0)
            {
                RestoreItems();
                return;
            }
            if (_baseScales == null) CaptureScales();

            int count = 0;
            foreach (CanvasGroup item in revealItems)
                if (item != null && item.gameObject.activeSelf) count++;
            float interval = _resultsMotion.StaggerIntervalFor(count);
            float duration = _resultsMotion.StaggerItemDuration;

            Sequence seq = DOTween.Sequence();
            int index = 0;
            for (int i = 0; i < revealItems.Length; i++)
            {
                CanvasGroup group = revealItems[i];
                if (group == null || !group.gameObject.activeSelf) continue;

                Vector3 scale = _baseScales[i];
                Transform target = group.transform;
                group.alpha = 0f;
                target.localScale = scale * _resultsMotion.StaggerScaleFrom;

                float at = index * interval;
                seq.Insert(at, target.DOScale(scale, duration).SetEase(_resultsMotion.StaggerEase));
                seq.Insert(at, DOTween.To(() => group.alpha, a => group.alpha = a, 1f, duration).SetEase(Ease.OutQuad));
                index++;
            }

            float end = Mathf.Max(0f, (count - 1) * interval) + duration;
            if (_punchRecord && personalBestCard != null)
                seq.Insert(end, personalBestCard.DOPunchScale(Vector3.one * _resultsMotion.ButtonPunchScale, _resultsMotion.ButtonPunchDuration, _resultsMotion.ButtonPunchVibrato));
            if (_punchGems && gemRewardRoot != null)
                seq.Insert(end, gemRewardRoot.transform.DOPunchScale(Vector3.one * _resultsMotion.ButtonPunchScale, _resultsMotion.ButtonPunchDuration, _resultsMotion.ButtonPunchVibrato));

            _punchRecord = false; // once per presented result
            _punchGems = false;
            seq.SetUpdate(true);
            seq.SetLink(gameObject);
            _reveal = seq;
        }

        private void KillReveal()
        {
            if (_reveal == null) return;
            Sequence running = _reveal;
            _reveal = null;
            running.Kill();
        }

        /// <summary>Back to the designed look: a closed or interrupted reveal must not leave items faded or shrunk.</summary>
        private void RestoreItems()
        {
            if (revealItems == null || _baseScales == null) return;
            for (int i = 0; i < revealItems.Length; i++)
            {
                if (revealItems[i] == null) continue;
                revealItems[i].alpha = 1f;
                revealItems[i].transform.localScale = _baseScales[i];
            }
        }

        private void OnRaceAgainClicked() => RaceAgainRequested?.Invoke();

        private void OnMainMenuClicked() => MainMenuRequested?.Invoke();

        private void OnStatsClicked()
        {
            if (analysisScreen != null && _resultsNavigator != null) _resultsNavigator.Push(analysisScreen); // Back on the Analysis screen returns here
        }

        private void KillDenied()
        {
            if (_denied == null) return;
            Tween running = _denied;
            _denied = null;
            running.Kill();
        }
    }
}
