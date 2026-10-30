using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Rewards;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Home screen (prefab HomeScreen, the root screen of the Home scene). Binds the existing buttons and decides what
    /// Play does through <see cref="HomeEntryRules"/>; the environment button opens the theme select screen; the gear opens the settings window,
    /// the speaker switches sound effects, the card arrows step the racer presets. Edit racer, High scores and the 7-day icon only open their
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

        [Tooltip("DailyReward/Notification: red dot, shown while a 7-day reward is waiting to be claimed (ISevenDayRewards.HasClaimable). Optional.")]
        [SerializeField] private GameObject sevenDayRedDot;

        [Tooltip("Environment (top right, picture icon): opens the theme select screen.")]
        [SerializeField] private Button environmentButton;

        [Tooltip("Settings (top right, gear): opens the settings window.")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Sound (top right, speaker): switches sound effects on or off; the icon shows the muted speaker while they are off.")]
        [SerializeField] private Button soundButton;

        [Tooltip("Sound/Icon image: shows the speaker; its prefab sprite is the 'on' picture.")]
        [SerializeField] private Image soundIcon;

        [Tooltip("Muted speaker sprite (same canvas as the 'on' icon), shown while sound effects are off.")]
        [SerializeField] private Sprite soundOffSprite;

        [Tooltip("CharacterCard/Emote: opens the emote screen.")]
        [SerializeField] private Button emoteButton;

        [Header("Buttons without a system yet (shown, not interactable)")]

        [Tooltip("Upgrade: no upgrade system yet.")]
        [SerializeField] private Button upgradeButton;

        [Tooltip("Players: no multiplayer system yet.")]
        [SerializeField] private Button playersButton;

        [Tooltip("CharacterCard/Previous: selects the previous racer preset (wraps around).")]
        [SerializeField] private Button previousRacerButton;

        [Tooltip("CharacterCard/Next: selects the next racer preset (wraps around).")]
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

        [Tooltip("High scores screen: the leaderboard screen (HighScoresScreen prefab instance, a UiScreen).")]
        [SerializeField] private UiScreen highScoresScreen;

        [Tooltip("Theme select screen: any UiScreen; its view arrives with the theme select UI (T-027).")]
        [SerializeField] private UiScreen themeSelectScreen;

        [Tooltip("Emote screen: any UiScreen (EmoteScreenView).")]
        [SerializeField] private UiScreen emoteScreen;

        [Tooltip("7-day reward popup: any UiPopup; its view arrives in a later phase.")]
        [SerializeField] private UiPopup sevenDayPopup;

        [Tooltip("Settings popup (scene object, starts inactive).")]
        [SerializeField] private UiPopup settingsPopup;

        [Tooltip("Daily gift popup (DailyGiftPopup prefab instance, starts inactive): opens by itself when Home opens and the gift is due. Scene-bound; empty = it never opens.")]
        [SerializeField] private DailyGiftPopupView dailyGiftPopup;

        private ScreenNavigator _navigator;
        private IRaceSelection _selection;
        private RaceEventCatalog _catalog;
        private IWallet _wallet;
        private HomeEntryFlow _entryFlow;
        private IPlayerSettings _settings;
        private ICosmeticService _cosmetics;
        private Sprite _soundOnSprite;
        private IDailyGift _dailyGift;
        private ISevenDayRewards _sevenDay;

        [Inject]
        public void ConstructHome(ScreenNavigator navigator, IRaceSelection selection, RaceEventCatalog catalog, IWallet wallet, HomeEntryFlow entryFlow)
        {
            _navigator = navigator;
            _selection = selection;
            _catalog = catalog;
            _wallet = wallet;
            _entryFlow = entryFlow;
        }

        /// <summary>Optional so a scene without these services still opens; the settings and preset buttons then do nothing.</summary>
        [Inject]
        public void ConstructHomeExtras([InjectOptional] IPlayerSettings settings, [InjectOptional] ICosmeticService cosmetics)
        {
            _settings = settings;
            _cosmetics = cosmetics;
        }

        /// <summary>Optional so a scene without the reward services still opens; the daily gift then stays closed and the red dot as built.</summary>
        [Inject]
        public void ConstructHomeRewards([InjectOptional] IDailyGift dailyGift, [InjectOptional] ISevenDayRewards sevenDay)
        {
            _dailyGift = dailyGift;
            _sevenDay = sevenDay;
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
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            if (soundButton != null)
            {
                soundButton.onClick.AddListener(OnSound);
                if (soundIcon != null) _soundOnSprite = soundIcon.sprite;
            }
            if (previousRacerButton != null) previousRacerButton.onClick.AddListener(OnPreviousRacer);
            if (nextRacerButton != null) nextRacerButton.onClick.AddListener(OnNextRacer);
            if (emoteButton != null) emoteButton.onClick.AddListener(OnEmote);
            DisableUnbuilt(upgradeButton, playersButton);
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
            TryOpenDailyGift();
        }

        /// <summary>
        /// Home just opened: a due daily gift (or the tickets-full notice) comes first, before anything else can be tapped.
        /// Nothing else is open at this point, and the 7-day panel never opens by itself.
        /// </summary>
        private void TryOpenDailyGift()
        {
            if (_dailyGift == null) return;

            bool open = RewardPanelsModel.ShouldAutoOpenDailyGift(
                _dailyGift.State, ReferenceEquals(_navigator.CurrentScreen, this), _navigator.PopupCount > 0);
            if (!open) return;
            if (dailyGiftPopup == null)
            {
                UnityEngine.Debug.LogWarning($"{name}: {nameof(dailyGiftPopup)} is not assigned.", this);
                return;
            }
            _navigator.OpenPopup(dailyGiftPopup);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureSelection();
            _selection.Changed += RefreshEventName;
            if (_settings != null) _settings.Changed += RefreshSound;
            if (_cosmetics != null) _cosmetics.Changed += RefreshPresets;
            if (_sevenDay != null) _sevenDay.Changed += RefreshSevenDayDot;
            RefreshEventName();
            RefreshSound();
            RefreshPresets();
            RefreshSevenDayDot();
        }

        protected override void OnDisable()
        {
            _selection.Changed -= RefreshEventName;
            if (_settings != null) _settings.Changed -= RefreshSound;
            if (_cosmetics != null) _cosmetics.Changed -= RefreshPresets;
            if (_sevenDay != null) _sevenDay.Changed -= RefreshSevenDayDot;
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
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
            if (soundButton != null) soundButton.onClick.RemoveListener(OnSound);
            if (previousRacerButton != null) previousRacerButton.onClick.RemoveListener(OnPreviousRacer);
            if (nextRacerButton != null) nextRacerButton.onClick.RemoveListener(OnNextRacer);
            if (emoteButton != null) emoteButton.onClick.RemoveListener(OnEmote);
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
            if (racerNameText != null && _cosmetics == null && string.IsNullOrEmpty(racerNameText.text)) racerNameText.text = HomeTexts.DefaultRacerName;
            if (heightText != null && string.IsNullOrEmpty(heightText.text)) heightText.text = HomeTexts.RacerHeight;
            eventNameText.text = _catalog.TryGet(_selection.Selected, out RaceEventDefinition definition)
                ? definition.DisplayName
                : string.Empty;
        }

        /// <summary>Racer name = the selected preset's name; the arrows only work with more than one preset.</summary>
        private void RefreshPresets()
        {
            if (_cosmetics == null) return;
            if (racerNameText != null) racerNameText.text = _cosmetics.GetPresetName(_cosmetics.SelectedPreset);
            bool steppable = _cosmetics.PresetCount > 1;
            if (previousRacerButton != null) previousRacerButton.interactable = steppable;
            if (nextRacerButton != null) nextRacerButton.interactable = steppable;
        }

        private void OnPreviousRacer() => StepPreset(-1);

        private void OnNextRacer() => StepPreset(1);

        private void StepPreset(int direction)
        {
            if (_cosmetics == null) return;
            _cosmetics.SelectPreset(PresetStepper.Step(_cosmetics.SelectedPreset, _cosmetics.PresetCount, direction));
        }

        /// <summary>The calendar's red dot is on while a 7-day reward waits to be claimed.</summary>
        private void RefreshSevenDayDot()
        {
            if (_sevenDay != null && sevenDayRedDot != null) sevenDayRedDot.SetActive(_sevenDay.HasClaimable);
        }

        private void RefreshSound()
        {
            if (_settings == null) return;
            if (soundIcon == null || soundOffSprite == null) return;
            soundIcon.sprite = _settings.SfxOn ? _soundOnSprite : soundOffSprite;
        }

        private void OnSound()
        {
            if (_settings != null) _settings.SetSfx(!_settings.SfxOn);
        }

        private void OnSettings()
        {
            if (settingsPopup == null)
            {
                UnityEngine.Debug.LogWarning($"{name}: {nameof(settingsPopup)} is not assigned.", this);
                return;
            }
            _navigator.OpenPopup(settingsPopup);
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

        private void OnEmote() => PushScreen(emoteScreen, nameof(emoteScreen));

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
