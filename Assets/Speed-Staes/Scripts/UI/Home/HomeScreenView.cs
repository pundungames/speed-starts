using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Rewards;
using TMPro;
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
        [SerializeField] private TMP_Text eventNameText;

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

        [Tooltip("CharacterCard/Previous: selects the previous racer preset (wraps around).")]
        [SerializeField] private Button previousRacerButton;

        [Tooltip("CharacterCard/Next: selects the next racer preset (wraps around).")]
        [SerializeField] private Button nextRacerButton;

        [Header("Character card")]
        [Tooltip("CharacterCard/Select/Name: racer preset name.")]
        [SerializeField] private TMP_Text racerNameText;

        [Tooltip("CharacterCard/PowerBar/Fill: Filled image; the stat system is not built, the builder sets the look.")]
        [SerializeField] private Image powerFill;

        [Tooltip("CharacterCard/StaminaBar/Fill: Filled image; the stat system is not built, the builder sets the look.")]
        [SerializeField] private Image staminaFill;

        [Tooltip("CharacterCard/HeightField/Label.")]
        [SerializeField] private TMP_Text heightText;
        [SerializeField] private SpeedStars.UI.Costume.CostumeEditorConfig statsConfig;

        [Tooltip("CharacterCard/PlayerName: the player's name from Settings, under the Edit racer and Emote buttons; empty when no name is set. Optional.")]
        [SerializeField] private TMP_Text playerNameText;

        [Header("Racer of the day")]
        [Tooltip("CharacterCard/RacerOfTheDay: the 'Racer of the day' label, shown only while Race of the Day is the selected event. Optional.")]
        [SerializeField] private GameObject racerOfTheDayRoot;

        [Tooltip("Card parts hidden while Race of the Day is selected: the preset name and arrows, Edit racer, Emote, the player name, the stat bars and their icon strip (the day's racer cannot be changed).")]
        [SerializeField] private GameObject[] hiddenForRacerOfTheDay = new GameObject[0];

        [Header("Relay team")]
        [Tooltip("CharacterCard/RelayLeg: the leg row (label and arrows), shown only while a relay event is selected. Optional.")]
        [SerializeField] private GameObject relayLegRoot;

        [Tooltip("CharacterCard/RelayLeg/Label: '1st leg' ... '4th leg', the leg the card edits. Optional.")]
        [SerializeField] private TMP_Text relayLegText;

        [Tooltip("CharacterCard/RelayLeg/Previous: edits the previous leg (wraps around). Optional.")]
        [SerializeField] private Button previousLegButton;

        [Tooltip("CharacterCard/RelayLeg/Next: edits the next leg (wraps around). Optional.")]
        [SerializeField] private Button nextLegButton;

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
        private IRaceOfTheDay _raceOfTheDay;
        private ICosmeticPreview _preview;
        private IRelayTeam _relayTeam;
        private bool _dailyOutfitShown;
        private bool _relayShown;
        private bool _adoptSelectedPreset;
        private int _presetBeforeRelay;
        private RectTransform _responsiveCard;
        private readonly Vector3[] _playCorners = new Vector3[4];

        private void LateUpdate() => RefreshCharacterCardLayout();

        public void RefreshCharacterCardLayout()
        {
            if (_responsiveCard == null)
                _responsiveCard = transform.Find("CharacterCard") as RectTransform;
            if (_responsiveCard == null || playButton == null) return;

            // The card's top-right pivot stays fixed. Uniform scaling preserves the
            // artwork and text while leaving a clear gap above the Play button.
            var parent = _responsiveCard.parent as RectTransform;
            var playRect = playButton.transform as RectTransform;
            if (parent == null || playRect == null || _responsiveCard.rect.height <= 0f) return;
            playRect.GetWorldCorners(_playCorners);
            float playTop = parent.InverseTransformPoint(_playCorners[1]).y;
            float cardTop = parent.InverseTransformPoint(
                _responsiveCard.TransformPoint(new Vector3(_responsiveCard.rect.xMax, _responsiveCard.rect.yMax, 0f))).y;
            float availableHeight = Mathf.Max(0f, cardTop - playTop - 24f);
            float scale = Mathf.Min(1f, availableHeight / _responsiveCard.rect.height);
            var fittedScale = new Vector3(scale, scale, 1f);
            if (_responsiveCard.localScale != fittedScale)
                _responsiveCard.localScale = fittedScale;
        }

        /// <summary>Optional so a scene without Race of the Day still opens; the card then never changes.</summary>
        [Inject]
        public void ConstructHomeDaily([InjectOptional] IRaceOfTheDay raceOfTheDay, [InjectOptional] ICosmeticPreview preview)
        {
            _raceOfTheDay = raceOfTheDay;
            _preview = preview;
        }

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

        /// <summary>Optional so a scene without the relay team still opens; the leg row then stays hidden and the card is the player's own.</summary>
        [Inject]
        public void ConstructHomeRelay([InjectOptional] IRelayTeam relayTeam)
        {
            _relayTeam = relayTeam;
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
            if (previousLegButton != null) previousLegButton.onClick.AddListener(OnPreviousLeg);
            if (nextLegButton != null) nextLegButton.onClick.AddListener(OnNextLeg);
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
            if (_settings != null) _settings.Changed += RefreshPlayerName;
            if (_cosmetics != null) _cosmetics.Changed += RefreshPresets;
            if (_sevenDay != null) _sevenDay.Changed += RefreshSevenDayDot;
            if (_relayTeam != null) _relayTeam.Changed += RefreshRelay;
            // Coming back from another screen mid-relay: a preset picked there (the wardrobe has preset tabs) is the edited leg's new racer.
            _adoptSelectedPreset = _relayShown;
            RefreshEventName();
            RefreshSound();
            RefreshPlayerName();
            RefreshPresets();
            RefreshSevenDayDot();
            _adoptSelectedPreset = false;
        }

        protected override void OnDisable()
        {
            _selection.Changed -= RefreshEventName;
            if (_settings != null) _settings.Changed -= RefreshSound;
            if (_settings != null) _settings.Changed -= RefreshPlayerName;
            if (_cosmetics != null) _cosmetics.Changed -= RefreshPresets;
            if (_sevenDay != null) _sevenDay.Changed -= RefreshSevenDayDot;
            if (_relayTeam != null) _relayTeam.Changed -= RefreshRelay;
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
            if (previousLegButton != null) previousLegButton.onClick.RemoveListener(OnPreviousLeg);
            if (nextLegButton != null) nextLegButton.onClick.RemoveListener(OnNextLeg);
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

        /// <summary>
        /// Race of the Day selected: the card shows "Racer of the day" with the day's outfit on the character, and Edit racer, Emote and the
        /// preset arrows are hidden. Any other event: the player's own card and outfit (the preview is released).
        /// </summary>
        private void RefreshRacerOfTheDay()
        {
            if (_raceOfTheDay == null) return;
            bool daily = _raceOfTheDay.IsRaceOfTheDay(_selection.Selected);
            if (racerOfTheDayRoot != null) racerOfTheDayRoot.SetActive(daily);
            if (hiddenForRacerOfTheDay != null)
                foreach (GameObject part in hiddenForRacerOfTheDay)
                    if (part != null) part.SetActive(!daily);

            switch (RacerCardRules.Decide(daily, _dailyOutfitShown))
            {
                case RacerCardAction.ShowDailyOutfit:
                    if (_preview != null) _preview.Show(_raceOfTheDay.Today.Outfit);
                    _dailyOutfitShown = _preview != null;
                    break;
                case RacerCardAction.RestoreOwnOutfit:
                    if (_preview != null) _preview.Reset();
                    _dailyOutfitShown = false;
                    break;
            }
        }

        /// <summary>
        /// A relay event selected (and it is not the Race of the Day): the leg row shows and the card is the selected leg's racer, so the
        /// name, stat bars, Edit racer and Emote follow that leg through the selected preset. The player's own preset is remembered when
        /// the relay starts and comes back when it ends.
        /// </summary>
        private void RefreshRelay()
        {
            if (_relayTeam == null || _cosmetics == null) return;
            bool daily = _raceOfTheDay != null && _raceOfTheDay.IsRaceOfTheDay(_selection.Selected);
            bool relayEvent = _catalog.TryGet(_selection.Selected, out RaceEventDefinition definition) && definition.IsRelay;
            bool relay = RelayCardRules.ShowsTeam(relayEvent, daily, _relayTeam.LegCount);

            switch (RelayCardRules.Decide(relay, _relayShown))
            {
                case RelayCardAction.EnterRelay:
                    _presetBeforeRelay = _cosmetics.SelectedPreset;
                    _relayShown = true;
                    break;
                case RelayCardAction.LeaveRelay:
                    _relayShown = false;
                    _cosmetics.SelectPreset(_presetBeforeRelay);
                    break;
            }

            if (relayLegRoot != null) relayLegRoot.SetActive(relay);
            if (!relay) return;
            if (_adoptSelectedPreset)
            {
                _adoptSelectedPreset = false;
                if (_cosmetics.SelectedPreset != _relayTeam.GetPreset(_relayTeam.SelectedLeg))
                    _relayTeam.SetPreset(_relayTeam.SelectedLeg, _cosmetics.SelectedPreset); // raises Changed: this method runs again
            }
            if (relayLegText != null) relayLegText.text = HomeTexts.RelayLeg(_relayTeam.SelectedLeg);
            int preset = _relayTeam.GetPreset(_relayTeam.SelectedLeg);
            if (_cosmetics.SelectedPreset != preset) _cosmetics.SelectPreset(preset);
        }

        private void OnPreviousLeg() => StepLeg(-1);

        private void OnNextLeg() => StepLeg(1);

        private void StepLeg(int direction)
        {
            if (_relayTeam == null || !_relayShown) return;
            _relayTeam.SelectLeg(PresetStepper.Step(_relayTeam.SelectedLeg, _relayTeam.LegCount, direction));
        }

        private void RefreshEventName()
        {
            RefreshRacerOfTheDay();
            RefreshRelay();
            if (racerNameText != null && _cosmetics == null && string.IsNullOrEmpty(racerNameText.text)) racerNameText.text = HomeTexts.DefaultRacerName;
            if (heightText != null && string.IsNullOrEmpty(heightText.text)) heightText.text = HomeTexts.RacerHeight;
            eventNameText.text = _catalog.TryGet(_selection.Selected, out RaceEventDefinition definition)
                ? definition.DisplayName
                : string.Empty;
        }

        /// <summary>Racer name = the selected preset's name; the arrows only work with more than one preset.</summary>
        private void RefreshPresets()
        {
            if(statsConfig != null)
            {
                SetStatBar(powerFill,statsConfig.PowerFill);
                SetStatBar(staminaFill,statsConfig.StaminaFill);
                if(heightText != null)heightText.text="Height: "+statsConfig.HeightText.Replace("Height:",string.Empty).Trim();
            }
            if (_cosmetics == null) return;
            if (racerNameText != null) racerNameText.text = _cosmetics.GetPresetName(_cosmetics.SelectedPreset);
            bool steppable = _cosmetics.PresetCount > 1;
            if (previousRacerButton != null) previousRacerButton.interactable = steppable;
            if (nextRacerButton != null) nextRacerButton.interactable = steppable;
        }

        private static void SetStatBar(Image fill,float amount)
        {
            if(!fill)return;
            fill.fillAmount=Mathf.Clamp01(amount);
            fill.rectTransform.anchorMin=Vector2.zero;
            fill.rectTransform.anchorMax=new Vector2(fill.fillAmount,1);
            fill.rectTransform.offsetMin=new Vector2(6,6);
            fill.rectTransform.offsetMax=new Vector2(-6,-6);
        }

        private void OnPreviousRacer() => StepPreset(-1);

        private void OnNextRacer() => StepPreset(1);

        private void StepPreset(int direction)
        {
            if (_cosmetics == null) return;
            int next = PresetStepper.Step(_cosmetics.SelectedPreset, _cosmetics.PresetCount, direction);
            _cosmetics.SelectPreset(next);
            // In the relay the arrows choose the racer of the edited leg.
            if (_relayShown && _relayTeam != null) _relayTeam.SetPreset(_relayTeam.SelectedLeg, next);
        }

        /// <summary>The calendar's red dot is on while a 7-day reward waits to be claimed.</summary>
        private void RefreshSevenDayDot()
        {
            if (_sevenDay != null && sevenDayRedDot != null) sevenDayRedDot.SetActive(_sevenDay.HasClaimable);
        }

        /// <summary>The Settings "Player Name" shows under the card buttons; it changes through the settings Changed event.</summary>
        private void RefreshPlayerName()
        {
            if (_settings == null || playerNameText == null) return;
            playerNameText.text = RacerCardRules.PlayerNameLine(_settings.PlayerName);
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

        private void OnEmote()
        {
            if (cosmeticsScreen is SpeedStars.UI.Costume.CostumeEditorScreenView wardrobe)
                wardrobe.OpenEmoteTab();
            PushScreen(cosmeticsScreen, nameof(cosmeticsScreen));
        }

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
