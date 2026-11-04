using System.Collections.Generic;
using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Settings;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Settings
{
    /// <summary>
    /// Settings window (prefab SettingsPopup, a UiPopup on the home popup layer): Camera slider, Flag grid,
    /// Player Name field, Done and X. Binds the objects of the prefab and fills them from <see cref="IPlayerSettings"/>;
    /// it builds no hierarchy: rows and cells are instances of their prefabs under the lists' Content objects.
    /// Camera and flag are saved the moment they change. The name is committed by Done only (validated first:
    /// a refused name turns the field red, shows the reason and shakes it); X (the base close button) just closes, and
    /// because every opening reloads the saved name, whatever was typed and not committed is discarded.
    /// </summary>
    public sealed class SettingsPopupView : UiPopup
    {
        [Header("Texts")]
        [Tooltip("Header/Title.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Label of the Flag row.")]
        [SerializeField] private TMP_Text flagLabel;

        [Tooltip("Label of the Player Name row.")]
        [SerializeField] private TMP_Text nameLabel;

        [Tooltip("Label of the Done button.")]
        [SerializeField] private TMP_Text doneLabel;

        [Header("Camera")]
        [Tooltip("Label of the Camera Distance row.")]
        [SerializeField] private TMP_Text cameraDistanceLabel;

        [Tooltip("Slider in the Camera Distance row.")]
        [SerializeField] private Slider cameraDistanceSlider;

        [Tooltip("The slider moves in steps of this size, so a drag saves only when the value really changes.")]
        [Min(0.001f)]
        [SerializeField] private float cameraDistanceStep = 0.05f;

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
        [SerializeField] private TMP_Text nameErrorText;

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

        [Header("Resets")]
        [Tooltip("Reset Personal Bests row tap area.")]
        [SerializeField] private SettingsActionRowView resetBestsRow;

        [Tooltip("Reset All Progress row tap area.")]
        [SerializeField] private SettingsActionRowView resetAllRow;

        [Tooltip("PopupLayer/ConfirmDialog: the yes/no window of the resets. Starts inactive.")]
        [SerializeField] private ConfirmDialogView confirmDialog;

        [Header("Settings v2 sections")]
        [Tooltip("ExtraSections node: the Competitors, Gameplay, Visuals, Emote and Reset options rows. Empty = the window has none of them.")]
        [SerializeField] private SettingsExtraSectionsView extraSections;

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
        private readonly List<FlagCellView> _flagCells = new List<FlagCellView>();
        private bool _listsBuilt;
        private bool _nameRefused;
        private bool _nameFocused;
        private Tween _shake;
        private Vector2 _shakeBase;
        private Tween _closeDelay;
        private IProgressReset _reset;
        private SceneFlow _sceneFlow;
        private readonly ResetConfirmFlow _confirm = new ResetConfirmFlow();
        private RaceEventCatalog _events;
        private RaceEventId _pendingEvent;
        private string _pendingEventName = string.Empty;

        [Inject]
        public void ConstructSettings(IPlayerSettings settings, UiMotionConfig motion, IInstantiator instantiator,
            [InjectOptional] ScreenNavigator navigator,
            [InjectOptional] IProgressReset reset, [InjectOptional] SceneFlow sceneFlow,
            [InjectOptional] RaceEventCatalog events)
        {
            _events = events;
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
            flagButton.onClick.AddListener(ToggleFlagWindow);
            flagCatcher.onClick.AddListener(CloseFlagWindow);
            doneButton.onClick.AddListener(OnDone);
            nameInput.onValueChanged.AddListener(OnNameEdited);
            if (cameraDistanceSlider != null) cameraDistanceSlider.onValueChanged.AddListener(OnCameraDistance);
            _shakeBase = nameShakeTarget.anchoredPosition;
            FillFixedTexts();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BuildListsOnce();
            _settings.Changed += RefreshValues;
            nameInput.characterLimit = _settings.MaxNameLength;
            nameInput.text = _settings.PlayerName; // opening always starts from the saved name: X discards a typed one
            ClearNameError();
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
            UnbindExtra();
            if (confirmDialog != null) confirmDialog.Hide();
            if (flagWindow != null) flagWindow.SetActive(false); // null while the scene is being destroyed
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (flagButton != null) flagButton.onClick.RemoveListener(ToggleFlagWindow);
            if (flagCatcher != null) flagCatcher.onClick.RemoveListener(CloseFlagWindow);
            if (doneButton != null) doneButton.onClick.RemoveListener(OnDone);
            if (nameInput != null) nameInput.onValueChanged.RemoveListener(OnNameEdited);
            if (cameraDistanceSlider != null) cameraDistanceSlider.onValueChanged.RemoveListener(OnCameraDistance);
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
            if (cameraDistanceLabel != null) cameraDistanceLabel.text = SettingsTexts.CameraDistance;
            flagLabel.text = SettingsTexts.Flag;
            nameLabel.text = SettingsTexts.PlayerName;
            doneLabel.text = SettingsTexts.Done;
            namePlaceholder.text = SettingsTexts.NamePlaceholder;
        }

        private void OnCameraDistance(float value)
        {
            _settings.SetCameraDistance(Mathf.Round(value / cameraDistanceStep) * cameraDistanceStep);
        }

        private void BuildListsOnce()
        {
            if (_listsBuilt) return;
            _listsBuilt = true;

            foreach (FlagInfo info in _settings.Flags)
            {
                FlagCellView cell = _instantiator.InstantiatePrefabForComponent<FlagCellView>(flagCellPrefab.gameObject, flagContent);
                cell.Bind(info, false, OnFlagPicked);
                _flagCells.Add(cell);
            }
        }

        private void RefreshValues()
        {
            if (cameraDistanceSlider != null)
            {
                cameraDistanceSlider.minValue = _settings.CameraDistanceMin;
                cameraDistanceSlider.maxValue = _settings.CameraDistanceMax;
                cameraDistanceSlider.SetValueWithoutNotify(_settings.CameraDistance);
            }

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

        // ---------------------------------------------------------------- resets

        private void BindActionRows()
        {
            bool canReset = _reset != null;
            resetBestsRow.Bind(SettingsTexts.ResetPersonalBests, () => BeginReset(ResetKind.PersonalBests), canReset);
            resetAllRow.Bind(SettingsTexts.ResetAllProgress, () => BeginReset(ResetKind.AllProgress), canReset);

            UnbindExtra();
            if (extraSections == null) return;
            extraSections.Bind(_settings, _events, canReset);
            extraSections.ClearRacersRequested += OnClearRacersRequested;
            extraSections.ResetBestRequested += OnResetBestRequested;
        }

        private void UnbindExtra()
        {
            if (extraSections == null) return;
            extraSections.ClearRacersRequested -= OnClearRacersRequested;
            extraSections.ResetBestRequested -= OnResetBestRequested;
        }

        private void OnClearRacersRequested() => BeginReset(ResetKind.CustomRacers);

        private void OnResetBestRequested(RaceEventId eventId, string eventName)
        {
            _pendingEvent = eventId;
            _pendingEventName = eventName ?? string.Empty;
            BeginReset(ResetKind.OnePersonalBest);
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
                SettingsTexts.ConfirmBody(_confirm.Kind, _confirm.Step, _pendingEventName),
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

            if (run == ResetKind.OnePersonalBest)
            {
                _reset.ResetPersonalBest(_pendingEvent);
                return;
            }

            if (run == ResetKind.CustomRacers)
            {
                _reset.ClearCustomRacers();
                if (_sceneFlow != null) _sceneFlow.GoTo(GameScene.Home); // the menu character and the preset cards show the old outfits: load Home again
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

        // ---------------------------------------------------------------- flag

        private void ToggleFlagWindow()
        {
            if (flagWindow.activeSelf) CloseFlagWindow();
            else
            {
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
