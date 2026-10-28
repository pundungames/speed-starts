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
    /// Home screen (prefab HomeScreen, the root screen of the Home scene). Binds the existing buttons and decides what
    /// Play does through <see cref="HomeEntryRules"/>; the environment button opens the theme select screen. Edit racer, High scores and the 7-day icon only open their
    /// screen/popup (their content is a later phase); a target left empty in the Inspector is skipped with a warning.
    /// </summary>
    public sealed class HomeScreenView : UiScreen
    {
        [Tooltip("Play: starts the race (through the entry gate modal for ticketed events).")]
        [SerializeField] private Button playButton;

        [Tooltip("EventSelect (root button): opens the event select screen.")]
        [SerializeField] private Button eventSelectButton;

        [Tooltip("RaceSelect/Label: shows the selected event's name.")]
        [SerializeField] private Text eventNameText;

        [Tooltip("EditRacer: opens the cosmetics screen.")]
        [SerializeField] private Button editRacerButton;

        [Tooltip("HighScores: opens the high scores screen.")]
        [SerializeField] private Button highScoresButton;

        [Tooltip("DailyReward (root button): opens the 7-day popup.")]
        [SerializeField] private Button sevenDayButton;

        [Tooltip("DailyReward/Notification: red dot, shown while a 7-day reward may be waiting. Optional.")]
        [SerializeField] private GameObject sevenDayRedDot;

        [Tooltip("Environment (top right, picture icon): opens the theme select screen.")]
        [SerializeField] private Button environmentButton;

        [Header("Buttons without a system yet (shown, not interactable)")]
        [Tooltip("Settings: no settings system yet.")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Sound: no sound system yet.")]
        [SerializeField] private Button soundButton;

        [Tooltip("Upgrade: no upgrade system yet.")]
        [SerializeField] private Button upgradeButton;

        [Tooltip("Players: no multiplayer system yet.")]
        [SerializeField] private Button playersButton;

        [Tooltip("CharacterCard/Emote: no emote system yet.")]
        [SerializeField] private Button emoteButton;

        [Tooltip("CharacterCard/Previous: racer presets arrive in a later phase.")]
        [SerializeField] private Button previousRacerButton;

        [Tooltip("CharacterCard/Next: racer presets arrive in a later phase.")]
        [SerializeField] private Button nextRacerButton;

        [Header("Character card")]
        [Tooltip("CharacterCard/Select/Name: racer preset name.")]
        [SerializeField] private Text racerNameText;

        [Tooltip("CharacterCard/PowerBar/Fill: Filled image; the stat system is not built, the builder sets the look.")]
        [SerializeField] private Image powerFill;

        [Tooltip("CharacterCard/StaminaBar/Fill: Filled image; the stat system is not built, the builder sets the look.")]
        [SerializeField] private Image staminaFill;

        [Tooltip("CharacterCard/HeightField/Label.")]
        [SerializeField] private Text heightText;

        [Header("Targets")]
        [Tooltip("Event select screen view (scene object, starts inactive).")]
        [SerializeField] private EventSelectScreenView eventSelectScreen;

        [Tooltip("Entry gate modal view (scene object, starts inactive).")]
        [SerializeField] private EntryGateModalView entryGateModal;

        [Tooltip("Cosmetics (Edit racer) screen: any UiScreen; its view arrives in a later phase.")]
        [SerializeField] private UiScreen cosmeticsScreen;

        [Tooltip("High scores screen: any UiScreen; its view arrives in a later phase.")]
        [SerializeField] private UiScreen highScoresScreen;

        [Tooltip("Theme select screen: any UiScreen; its view arrives with the theme select UI (T-027).")]
        [SerializeField] private UiScreen themeSelectScreen;

        [Tooltip("7-day reward popup: any UiPopup; its view arrives in a later phase.")]
        [SerializeField] private UiPopup sevenDayPopup;

        private ScreenNavigator _navigator;
        private IRaceSelection _selection;
        private RaceEventCatalog _catalog;
        private IWallet _wallet;
        private HomeEntryFlow _entryFlow;

        [Inject]
        public void ConstructHome(ScreenNavigator navigator, IRaceSelection selection, RaceEventCatalog catalog, IWallet wallet, HomeEntryFlow entryFlow)
        {
            _navigator = navigator;
            _selection = selection;
            _catalog = catalog;
            _wallet = wallet;
            _entryFlow = entryFlow;
        }

        protected override void Awake()
        {
            base.Awake();
            playButton.onClick.AddListener(OnPlay);
            eventSelectButton.onClick.AddListener(OnEventSelect);
            editRacerButton.onClick.AddListener(OnEditRacer);
            highScoresButton.onClick.AddListener(OnHighScores);
            sevenDayButton.onClick.AddListener(OnSevenDay);
            if (environmentButton != null) environmentButton.onClick.AddListener(OnEnvironment);
            DisableUnbuilt(settingsButton, soundButton, upgradeButton, playersButton, emoteButton,
                previousRacerButton, nextRacerButton);
        }

        /// <summary>Designed buttons whose system does not exist yet stay visible but dead (they may be left empty).</summary>
        private static void DisableUnbuilt(params Button[] buttons)
        {
            foreach (Button button in buttons)
            {
                if (button != null) button.interactable = false;
            }
        }

        private void Start()
        {
            // The root screen of the stack: pushing the screen that is already on top changes nothing.
            _navigator.Push(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureSelection();
            _selection.Changed += RefreshEventName;
            RefreshEventName();
        }

        protected override void OnDisable()
        {
            _selection.Changed -= RefreshEventName;
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (eventSelectButton != null) eventSelectButton.onClick.RemoveListener(OnEventSelect);
            if (editRacerButton != null) editRacerButton.onClick.RemoveListener(OnEditRacer);
            if (highScoresButton != null) highScoresButton.onClick.RemoveListener(OnHighScores);
            if (sevenDayButton != null) sevenDayButton.onClick.RemoveListener(OnSevenDay);
            if (environmentButton != null) environmentButton.onClick.RemoveListener(OnEnvironment);
            base.OnDestroy();
        }

        /// <summary>No selection yet (first launch): pick the first playable event so Play and the label agree.</summary>
        private void EnsureSelection()
        {
            if (!_selection.Selected.IsEmpty) return;
            foreach (RaceEventDefinition definition in _catalog.GetEvents())
            {
                if (!definition.IsPlayable) continue;
                _selection.Select(definition.Id);
                return;
            }
        }

        private void RefreshEventName()
        {
            if (racerNameText != null && string.IsNullOrEmpty(racerNameText.text)) racerNameText.text = HomeTexts.DefaultRacerName;
            if (heightText != null && string.IsNullOrEmpty(heightText.text)) heightText.text = HomeTexts.RacerHeight;
            eventNameText.text = _catalog.TryGet(_selection.Selected, out RaceEventDefinition definition)
                ? definition.DisplayName
                : string.Empty;
        }

        private void OnPlay()
        {
            bool exists = _catalog.TryGet(_selection.Selected, out RaceEventDefinition definition);
            PlayStep step = HomeEntryRules.DecidePlay(
                exists,
                exists && definition.IsPlayable,
                exists && definition.RequiresTicket,
                _wallet.TimedAccessActive);

            switch (step)
            {
                case PlayStep.OpenGate:
                    entryGateModal.Open(definition);
                    break;
                case PlayStep.EnterNow:
                    _entryFlow.TryEnter(definition.Id);
                    break;
            }
        }

        private void OnEnvironment() => PushScreen(themeSelectScreen, nameof(themeSelectScreen));

        private void OnEventSelect() => _navigator.Push(eventSelectScreen);

        private void OnEditRacer() => PushScreen(cosmeticsScreen, nameof(cosmeticsScreen));

        private void OnHighScores() => PushScreen(highScoresScreen, nameof(highScoresScreen));

        private void OnSevenDay()
        {
            if (sevenDayPopup == null)
            {
                UnityEngine.Debug.LogWarning($"{name}: {nameof(sevenDayPopup)} is not assigned.", this);
                return;
            }
            _navigator.OpenPopup(sevenDayPopup);
        }

        private void PushScreen(UiScreen screen, string fieldName)
        {
            if (screen == null)
            {
                UnityEngine.Debug.LogWarning($"{name}: {fieldName} is not assigned.", this);
                return;
            }
            _navigator.Push(screen);
        }
    }
}
