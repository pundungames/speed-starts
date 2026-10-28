using System;
using DG.Tweening;
using SpeedStars.UI.Home;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Results screen (MD 2.1): event title, Personal Best / World Record (empty until Faz 6), the "Results" list with
    /// the player's one line, and the buttons. Watch replay, High scores and Practice are not built in this phase, so
    /// they start greyed out. The row comes from a row prefab under a content parent; nothing is built in code.
    /// </summary>
    public sealed class ResultsScreenView : UiScreen, IResultsView
    {
        private const string PlayerName = "You";

        [Header("Texts")]
        [Tooltip("LeftPanel/EventTitle/Label: event name.")]
        [SerializeField] private Text eventTitleText;

        [Tooltip("LeftPanel/PersonalBest/Value.")]
        [SerializeField] private Text personalBestText;

        [Tooltip("LeftPanel/WorldRecord/Value.")]
        [SerializeField] private Text worldRecordText;

        [Header("Results list")]
        [Tooltip("LeftPanel/Results: parent the row is created under.")]
        [SerializeField] private Transform resultsContent;

        [Tooltip("HighScoreRow prefab with a ResultRowView on its root.")]
        [SerializeField] private ResultRowView rowPrefab;

        [Header("Gem reward")]
        [Tooltip("LeftPanel/GemReward: the '+N' line, visible only when the race paid Purple Gems.")]
        [SerializeField] private GameObject gemRewardRoot;

        [Tooltip("LeftPanel/GemReward/Value: '+N'.")]
        [SerializeField] private Text gemRewardText;

        [Header("Buttons")]
        [Tooltip("Buttons/WatchReplay: greyed out in this phase.")]
        [SerializeField] private Button watchReplayButton;

        [Tooltip("Buttons/RaceAgain.")]
        [SerializeField] private Button raceAgainButton;

        [Tooltip("Buttons/HighScores: greyed out in this phase.")]
        [SerializeField] private Button highScoresButton;

        [Tooltip("Buttons/Practice: greyed out in this phase.")]
        [SerializeField] private Button practiceButton;

        [Tooltip("Buttons/MainMenu.")]
        [SerializeField] private Button mainMenuButton;

        private UiMotionConfig _motion;
        private ResultRowView _row;
        private Tween _denied;
        private Vector3 _deniedBase;
        private Tween _gemPunch;
        private bool _punchGems;

        public event Action RaceAgainRequested;
        public event Action MainMenuRequested;

        [Inject]
        public void InjectMotion(UiMotionConfig motion)
        {
            _motion = motion;
        }

        protected override void Awake()
        {
            base.Awake();
            if (watchReplayButton != null) watchReplayButton.interactable = false;
            if (highScoresButton != null) highScoresButton.interactable = false;
            if (practiceButton != null) practiceButton.interactable = false;
            if (raceAgainButton != null) raceAgainButton.onClick.AddListener(OnRaceAgainClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_punchGems) PlayGemPunch();
            _punchGems = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            KillDenied();
            KillGemPunch();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillDenied();
            KillGemPunch();
            if (raceAgainButton != null) raceAgainButton.onClick.RemoveListener(OnRaceAgainClicked);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        public void Present(RaceResult result)
        {
            if (eventTitleText != null) eventTitleText.text = result.EventTitle;
            if (personalBestText != null) personalBestText.text = result.PersonalBestText;
            if (worldRecordText != null) worldRecordText.text = result.WorldRecordText;


            if (resultsContent == null || rowPrefab == null)
            {
                Debug.LogWarning($"{name}: results list content or row prefab is not assigned; the result line is missing.", this);
                return;
            }
            if (_row == null) _row = Instantiate(rowPrefab, resultsContent); // one line, reused for every race
            _row.Fill(1, PlayerName, result.TimeText, result.IsPersonalBest);
        }

        public void NotifyDenied()
        {
            if (raceAgainButton == null || _motion == null || !isActiveAndEnabled) return;

            KillDenied();
            Transform target = raceAgainButton.transform;
            _deniedBase = target.localPosition;
            float width = ((RectTransform)target).rect.width;
            Tween shake = target
                .DOPunchPosition(Vector3.right * (width * _motion.ButtonPunchScale), _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
            shake.OnKill(() =>
            {
                if (_denied == shake) _denied = null;
                if (target != null) target.localPosition = _deniedBase;
            });
            _denied = shake;
        }

        public void ShowGemReward(int gems)
        {
            bool paid = gems > 0;
            if (gemRewardRoot != null) gemRewardRoot.SetActive(paid);
            if (paid && gemRewardText != null) gemRewardText.text = HomeTexts.GemReward(gems);
            _punchGems = paid; // the screen is not on yet: the punch plays when it opens
        }

        private void PlayGemPunch()
        {
            if (gemRewardRoot == null || _motion == null) return;

            KillGemPunch();
            Transform target = gemRewardRoot.transform;
            target.localScale = Vector3.one;
            _gemPunch = target
                .DOPunchScale(Vector3.one * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void KillGemPunch()
        {
            if (_gemPunch != null)
            {
                _gemPunch.Kill();
                _gemPunch = null;
            }
            if (gemRewardRoot != null) gemRewardRoot.transform.localScale = Vector3.one;
        }

        private void OnRaceAgainClicked() => RaceAgainRequested?.Invoke();

        private void OnMainMenuClicked() => MainMenuRequested?.Invoke();

        private void KillDenied()
        {
            if (_denied == null) return;
            Tween running = _denied;
            _denied = null;
            running.Kill();
        }
    }
}
