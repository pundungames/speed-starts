using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Settings;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// Settings window (prefab SettingsPopup, a UiPopup on the home popup layer): Music switch, Language list, Flag grid,
    /// Player Name field, Done and X. Binds the objects of the prefab and fills them from <see cref="IPlayerSettings"/>;
    /// it builds no hierarchy: rows and cells are instances of their prefabs under the lists' Content objects.
    /// Music, language and flag are saved the moment they change. The name is committed by Done only (validated first:
    /// a refused name turns the field red, shows the reason and shakes it); X (the base close button) just closes, and
    /// because every opening reloads the saved name, whatever was typed and not committed is discarded.
    /// </summary>
    public sealed class SettingsPopupView : UiPopup
    {
        [Header("Texts")]
        [Tooltip("Header/Title.")]
        [SerializeField] private Text titleText;

        [Tooltip("Label of the Music row.")]
        [SerializeField] private Text musicLabel;

        [Tooltip("Label of the Language row.")]
        [SerializeField] private Text languageLabel;

        [Tooltip("Label of the Flag row.")]
        [SerializeField] private Text flagLabel;

        [Tooltip("Label of the Player Name row.")]
        [SerializeField] private Text nameLabel;

        [Tooltip("Label of the Done button.")]
        [SerializeField] private Text doneLabel;

        [Header("Music")]
        [Tooltip("Switch in the Music row.")]
        [SerializeField] private SettingsToggleView musicToggle;

        [Header("Language")]
        [Tooltip("Closed Language field: opens the list.")]
        [SerializeField] private Button languageButton;

        [Tooltip("Closed Language field: shows the chosen language's name.")]
        [SerializeField] private Text languageValueText;

        [Tooltip("Arrow of the Language field (down while closed, up while open).")]
        [SerializeField] private Image languageArrow;

        [Tooltip("PopupLayer/LanguagePopup: whole-screen catcher + the list window. Starts inactive.")]
        [SerializeField] private GameObject languageWindow;

        [Tooltip("LanguagePopup catcher button: a tap outside the list closes it.")]
        [SerializeField] private Button languageCatcher;

        [Tooltip("Language list Content: LanguageRow prefabs are put here.")]
        [SerializeField] private RectTransform languageContent;

        [Tooltip("LanguageRow prefab.")]
        [SerializeField] private LanguageRowView languageRowPrefab;

        [Header("Flag")]
        [Tooltip("Closed Flag field: opens the grid.")]
        [SerializeField] private Button flagButton;

        [Tooltip("Closed Flag field: shows the chosen flag.")]
        [SerializeField] private Image flagValueImage;

        [Tooltip("Arrow of the Flag field (down while closed, up while open).")]
        [SerializeField] private Image flagArrow;

        [Tooltip("PopupLayer/FlagPopup: whole-screen catcher + the grid window. Starts inactive.")]
        [SerializeField] private GameObject flagWindow;

        [Tooltip("FlagPopup catcher button: a tap outside the grid closes it.")]
        [SerializeField] private Button flagCatcher;

        [Tooltip("Flag grid Content: FlagCell prefabs are put here.")]
        [SerializeField] private RectTransform flagContent;

        [Tooltip("FlagCell prefab.")]
        [SerializeField] private FlagCellView flagCellPrefab;

        [Tooltip("Arrow sprite while a list is closed.")]
        [SerializeField] private Sprite arrowDown;

        [Tooltip("Arrow sprite while a list is open.")]
        [SerializeField] private Sprite arrowUp;

        [Header("Player name")]
        [Tooltip("Name input (legacy InputField).")]
        [SerializeField] private InputField nameInput;

        [Tooltip("Field background: normal / focused / error sprite.")]
        [SerializeField] private Image nameFieldImage;

        [Tooltip("Placeholder text of the name input.")]
        [SerializeField] private Text namePlaceholder;

        [Tooltip("Body/ErrorText: the reason a name was refused.")]
        [SerializeField] private Text nameErrorText;

        [Tooltip("Field rect that shakes when a name is refused.")]
        [SerializeField] private RectTransform nameShakeTarget;

        [Tooltip("Field sprite while idle.")]
        [SerializeField] private Sprite nameNormalSprite;

        [Tooltip("Field sprite while typing.")]
        [SerializeField] private Sprite nameFocusedSprite;

        [Tooltip("Field sprite after a refused name.")]
        [SerializeField] private Sprite nameErrorSprite;

        [Header("Done")]
        [Tooltip("Done: validates and saves the name, then closes.")]
        [SerializeField] private Button doneButton;

        [Header("Links and resets")]
        [Tooltip("Privacy Policy row tap area.")]
        [SerializeField] private SettingsActionRowView privacyRow;

        [Tooltip("Terms of Use row tap area.")]
        [SerializeField] private SettingsActionRowView termsRow;

        [Tooltip("Support row tap area.")]
        [SerializeField] private SettingsActionRowView supportRow;

        [Tooltip("Reset Personal Bests row tap area.")]
        [SerializeField] private SettingsActionRowView resetBestsRow;

        [Tooltip("Reset All Progress row tap area.")]
        [SerializeField] private SettingsActionRowView resetAllRow;

        [Tooltip("PopupLayer/ConfirmDialog: the yes/no window of the resets. Starts inactive.")]
        [SerializeField] private ConfirmDialogView confirmDialog;

        [Header("Refusal shake")]
        [Tooltip("Seconds the name field shakes after a refused name.")]
        [Min(0f)]
        [SerializeField] private float shakeSeconds = 0.3f;

        [Tooltip("Sideways shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float shakeStrength = 14f;

        [Tooltip("How many times the shake swings.")]
        [Min(1)]
        [SerializeField] private int shakeVibrato = 20;

        private IPlayerSettings _settings;
        private UiMotionConfig _settingsMotion;
        private IInstantiator _instantiator;
        private ScreenNavigator _navigator;
        private readonly List<LanguageRowView> _languageRows = new List<LanguageRowView>();
        private readonly List<FlagCellView> _flagCells = new List<FlagCellView>();
        private bool _listsBuilt;
        private bool _nameRefused;
        private bool _nameFocused;
        private Tween _shake;
        private Vector2 _shakeBase;
        private Tween _closeDelay;
        private SettingsConfig _config;
        private IProgressReset _reset;
        private SceneFlow _sceneFlow;
        private readonly ResetConfirmFlow _confirm = new ResetConfirmFlow();

        [Inject]
        public void ConstructSettings(IPlayerSettings settings, UiMotionConfig motion, IInstantiator instantiator,
            [InjectOptional] ScreenNavigator navigator, [InjectOptional] SettingsConfig config,
            [InjectOptional] IProgressReset reset, [InjectOptional] SceneFlow sceneFlow)
        {
            _config = config;
            _reset = reset;
            _sceneFlow = sceneFlow;
            _settings = settings;
            _settingsMotion = motion;
            _instantiator = instantiator;
            _navigator = navigator;
        }

        protected override void Awake()
        {
            base.Awake();
            languageButton.onClick.AddListener(ToggleLanguageWindow);
            languageCatcher.onClick.AddListener(CloseLanguageWindow);
            flagButton.onClick.AddListener(ToggleFlagWindow);
            flagCatcher.onClick.AddListener(CloseFlagWindow);
            doneButton.onClick.AddListener(OnDone);
            nameInput.onValueChanged.AddListener(OnNameEdited);
            _shakeBase = nameShakeTarget.anchoredPosition;
            FillFixedTexts();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildListsOnce();
            _settings.Changed += RefreshValues;
            musicToggle.Bind(_settings.MusicOn, OnMusicChanged);
            nameInput.characterLimit = _settings.MaxNameLength;
            nameInput.text = _settings.PlayerName; // opening always starts from the saved name: X discards a typed one
            ClearNameError();
            CloseLanguageWindow();
            CloseFlagWindow();
            RefreshValues();
            BindActionRows();
            EndConfirm();
        }

        protected override void OnDisable()
        {
            if (_settings != null) _settings.Changed -= RefreshValues;
            KillShake();
            KillCloseDelay();
            _confirm.Cancel();
            if (confirmDialog != null) confirmDialog.Hide();
            if (languageWindow != null) languageWindow.SetActive(false); // null while the scene is being destroyed
            if (flagWindow != null) flagWindow.SetActive(false);
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (languageButton != null) languageButton.onClick.RemoveListener(ToggleLanguageWindow);
            if (languageCatcher != null) languageCatcher.onClick.RemoveListener(CloseLanguageWindow);
            if (flagButton != null) flagButton.onClick.RemoveListener(ToggleFlagWindow);
            if (flagCatcher != null) flagCatcher.onClick.RemoveListener(CloseFlagWindow);
            if (doneButton != null) doneButton.onClick.RemoveListener(OnDone);
            if (nameInput != null) nameInput.onValueChanged.RemoveListener(OnNameEdited);
            base.OnDestroy();
        }

        private void Update()
        {
            // The field shows "typing" while it has focus, unless a refused name keeps it red.
            bool focused = nameInput.isFocused;
            if (focused == _nameFocused) return;
            _nameFocused = focused;
            RefreshNameSprite();
        }

        // ---------------------------------------------------------------- fill

        private void FillFixedTexts()
        {
            titleText.text = SettingsTexts.Title;
            musicLabel.text = SettingsTexts.Music;
            languageLabel.text = SettingsTexts.Language;
            flagLabel.text = SettingsTexts.Flag;
            nameLabel.text = SettingsTexts.PlayerName;
            doneLabel.text = SettingsTexts.Done;
            namePlaceholder.text = SettingsTexts.NamePlaceholder;
        }

        private void BuildListsOnce()
        {
            if (_listsBuilt) return;
            _listsBuilt = true;

            foreach (LanguageInfo info in _settings.Languages)
            {
                LanguageRowView row = _instantiator.InstantiatePrefabForComponent<LanguageRowView>(languageRowPrefab.gameObject, languageContent);
                row.Bind(info, false, OnLanguagePicked);
                _languageRows.Add(row);
            }

            foreach (FlagInfo info in _settings.Flags)
            {
                FlagCellView cell = _instantiator.InstantiatePrefabForComponent<FlagCellView>(flagCellPrefab.gameObject, flagContent);
                cell.Bind(info, false, OnFlagPicked);
                _flagCells.Add(cell);
            }
        }

        private void RefreshValues()
        {
            musicToggle.SetOn(_settings.MusicOn, animate: false); // no-op when the toggle already shows it, so its own slide is not cut

            string language = _settings.LanguageCode;
            languageValueText.text = string.Empty;
            foreach (LanguageInfo info in _settings.Languages)
            {
                if (info.Code == language) languageValueText.text = info.DisplayName;
            }
            foreach (LanguageRowView row in _languageRows) row.SetSelected(row.Code == language);

            string flag = _settings.FlagCode;
            Sprite flagSprite = null;
            foreach (FlagInfo info in _settings.Flags)
            {
                if (info.Code == flag) flagSprite = info.Sprite;
            }
            flagValueImage.sprite = flagSprite;
            flagValueImage.enabled = flagSprite != null;
            foreach (FlagCellView cell in _flagCells) cell.SetSelected(cell.Code == flag);
        }

        // ---------------------------------------------------------------- links and resets

        private void BindActionRows()
        {
            BindLink(privacyRow, SettingsTexts.PrivacyPolicy, SettingsLink.PrivacyPolicy);
            BindLink(termsRow, SettingsTexts.TermsOfUse, SettingsLink.TermsOfUse);
            BindLink(supportRow, SettingsTexts.Support, SettingsLink.Support);

            bool canReset = _reset != null;
            resetBestsRow.Bind(SettingsTexts.ResetPersonalBests, () => BeginReset(ResetKind.PersonalBests), canReset);
            resetAllRow.Bind(SettingsTexts.ResetAllProgress, () => BeginReset(ResetKind.AllProgress), canReset);
        }

        private void BindLink(SettingsActionRowView row, string caption, SettingsLink link)
        {
            bool usable = _config != null && !string.IsNullOrEmpty(_config.GetLinkUrl(link));
            row.Bind(caption, () => OpenLink(link), usable);
        }

        private void OpenLink(SettingsLink link)
        {
            string url = _config != null ? _config.GetLinkUrl(link) : string.Empty;
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }

        private void BeginReset(ResetKind kind)
        {
            _confirm.Begin(kind);
            ShowConfirm();
        }

        private void ShowConfirm()
        {
            if (!_confirm.IsOpen)
            {
                confirmDialog.Hide();
                return;
            }

            bool final = _confirm.IsFinalStep;
            confirmDialog.Show(SettingsTexts.ConfirmTitle(_confirm.Kind, _confirm.Step),
                SettingsTexts.ConfirmBody(_confirm.Kind, _confirm.Step),
                SettingsTexts.ConfirmButton(final), final, OnConfirmYes, EndConfirm);
        }

        private void OnConfirmYes()
        {
            ResetKind run = _confirm.Confirm();
            if (run == ResetKind.None)
            {
                ShowConfirm(); // first question answered: ask the next one
                return;
            }

            confirmDialog.Hide();
            if (_reset == null) return;

            if (run == ResetKind.PersonalBests)
            {
                _reset.ResetPersonalBests();
                return;
            }

            _reset.ResetAll();
            // Every screen shows the old progress: load Home again. Without the flow the window just closes.
            if (_sceneFlow != null) _sceneFlow.GoTo(GameScene.Home);
            else if (_navigator != null) PanelCloseRule.Close(_navigator, this, true);
        }

        private void EndConfirm()
        {
            _confirm.Cancel();
            confirmDialog.Hide();
        }

        // ---------------------------------------------------------------- music, language, flag

        private void OnMusicChanged(bool on) => _settings.SetMusic(on);

        private void ToggleLanguageWindow()
        {
            if (languageWindow.activeSelf) CloseLanguageWindow();
            else
            {
                CloseFlagWindow();
                languageWindow.SetActive(true);
                languageArrow.sprite = arrowUp;
            }
        }

        private void CloseLanguageWindow()
        {
            languageWindow.SetActive(false);
            languageArrow.sprite = arrowDown;
        }

        private void OnLanguagePicked(string code)
        {
            _settings.SetLanguage(code);
            RefreshValues();
            CloseLanguageWindow();
        }

        private void ToggleFlagWindow()
        {
            if (flagWindow.activeSelf) CloseFlagWindow();
            else
            {
                CloseLanguageWindow();
                flagWindow.SetActive(true);
                flagArrow.sprite = arrowUp;
            }
        }

        private void CloseFlagWindow()
        {
            KillCloseDelay();
            flagWindow.SetActive(false);
            flagArrow.sprite = arrowDown;
        }

        private void OnFlagPicked(string code)
        {
            _settings.SetFlag(code);
            RefreshValues();
            foreach (FlagCellView cell in _flagCells)
            {
                if (cell.Code != code) continue;
                cell.Punch(_settingsMotion.ButtonPunchScale, _settingsMotion.ButtonPunchDuration, _settingsMotion.ButtonPunchVibrato);
            }

            // Let the punch play before the grid goes away.
            KillCloseDelay();
            _closeDelay = DOVirtual.DelayedCall(_settingsMotion.ButtonPunchDuration, CloseFlagWindow).SetUpdate(true).SetLink(gameObject);
        }

        // ---------------------------------------------------------------- name, done

        private void OnNameEdited(string _)
        {
            if (_nameRefused) ClearNameError();
        }

        private void OnDone()
        {
            PlayerNameResult result = _settings.TrySetName(nameInput.text);
            if (result != PlayerNameResult.Valid)
            {
                ShowNameError(result);
                return;
            }

            if (_navigator != null) PanelCloseRule.Close(_navigator, this, true);
        }

        private void ShowNameError(PlayerNameResult result)
        {
            _nameRefused = true;
            nameErrorText.text = SettingsTexts.NameError(result, _settings.MaxNameLength);
            RefreshNameSprite();

            KillShake();
            _shake = nameShakeTarget.DOShakePosition(shakeSeconds, new Vector3(shakeStrength, 0f, 0f), shakeVibrato, 0f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void ClearNameError()
        {
            _nameRefused = false;
            nameErrorText.text = string.Empty;
            RefreshNameSprite();
        }

        private void RefreshNameSprite()
        {
            nameFieldImage.sprite = _nameRefused ? nameErrorSprite : _nameFocused ? nameFocusedSprite : nameNormalSprite;
        }

        private void KillShake()
        {
            if (_shake != null)
            {
                Tween running = _shake;
                _shake = null;
                running.Kill();
            }
            if (nameShakeTarget != null) nameShakeTarget.anchoredPosition = _shakeBase; // never leave the field off its designed place
        }

        private void KillCloseDelay()
        {
            if (_closeDelay == null) return;
            Tween running = _closeDelay;
            _closeDelay = null;
            running.Kill();
        }
    }
}
