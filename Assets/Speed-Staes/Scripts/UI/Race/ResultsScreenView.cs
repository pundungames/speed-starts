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

        [Tooltip("ResultsPanel/Rows: parent the row prefabs are created under.")]
        [SerializeField] private Transform rowsContent;

        [Tooltip("ResultsTableRow prefab with a ResultsTableRowView on its root.")]
        [SerializeField] private ResultsTableRowView rowPrefab;

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
            PlayReveal();
        }

        protected override void OnDisable()
        {
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

            FillRows(data.Rows);
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

        private void FillRows(IReadOnlyList<ResultRowData> rows)
        {
            foreach (ResultsTableRowView old in _rows)
            {
                if (old == null) continue;
                old.gameObject.SetActive(false); // out of the layout now, destroyed at the end of the frame
                Destroy(old.gameObject);
            }
            _rows.Clear();

            if (rowsContent == null || rowPrefab == null)
            {
                Debug.LogWarning($"{name}: results rows content or row prefab is not assigned; the table is empty.", this);
                return;
            }
            foreach (ResultRowData row in rows)
            {
                ResultsTableRowView view = Instantiate(rowPrefab, rowsContent);
                view.Fill(row);
                _rows.Add(view);
            }
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
