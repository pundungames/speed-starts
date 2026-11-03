using DG.Tweening;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Emotes;
using SpeedStars.UI.Framework;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Race HUD (UI v2): clock, speed, energy, cadence, cash and the Exit / Restart buttons. Binds objects that already
    /// exist in the RaceHud prefab and only fills them; text is rewritten only when the shown value changes (no
    /// per-frame string garbage). Telemetry, wallet and the flow actions are optional: a missing one leaves its part
    /// of the HUD still. The old distance bar fields stay optional too.
    /// </summary>
    public sealed class RaceHudView : UiScreen
    {
        [Tooltip("Telemetry/Time: the race clock text.")]
        [SerializeField] private Text timeText;

        [Tooltip("Telemetry/Speed: speed text, e.g. '7.4 m/s'.")]
        [SerializeField] private Text speedText;

        [Tooltip("Cash/Value: the player's Cash.")]
        [SerializeField] private Text cashText;

        [Tooltip("Cash/Plus: shown, not interactable (no shop yet).")]
        [SerializeField] private Button cashPlusButton;

        [Tooltip("Energy/Fill: Filled-type image, fillAmount = remaining energy 0..1.")]
        [SerializeField] private Image energyFill;

        [Tooltip("Cadence/Track: the bar the marker slides along (its width is the full scale).")]
        [SerializeField] private RectTransform cadenceTrack;

        [Tooltip("Cadence/Marker: moved along the track from its left end by presses per second over the full scale.")]
        [SerializeField] private RectTransform cadenceMarker;

        [Tooltip("Presses per second at the right end of the cadence bar.")]
        [Min(0.1f)]
        [SerializeField] private float cadenceMaxPressesPerSecond = 10f;

        [Tooltip("Exit: back to the main menu (no ticket refund).")]
        [SerializeField] private Button exitButton;

        [Tooltip("Restart: reset the current attempt and begin the countdown again.")]
        [SerializeField] private Button restartButton;

        [Header("Optional (old distance bar)")]
        [Tooltip("Progress/Bar/Fill: Filled-type image, fillAmount = distance / target distance. May stay empty.")]
        [SerializeField] private Image progressFill;

        [Tooltip("Progress/Distance: meters run, e.g. '37 m'. May stay empty.")]
        [SerializeField] private Text distanceText;

        [Header("Place (opponents)")]
        [Tooltip("Place: the panel that holds the player's place; hidden while nobody else runs.")]
        [SerializeField] private GameObject placeRoot;

        [Tooltip("Place/Label: the player's place, e.g. '3rd'.")]
        [SerializeField] private Text placeText;

        [Header("Emote (T-089)")]
        [Tooltip("Emote: the button that plays the equipped emote on the player's runner. Hidden when the player turned it off, when no emote is equipped, or when no emote control is bound.")]
        [SerializeField] private Button emoteButton;

        [Tooltip("Emote/Name: the equipped emote's name, shown for a moment after a press. May stay empty.")]
        [SerializeField] private Text emoteNameText;

        [Tooltip("Seconds the emote name stays on screen after a press.")]
        [Min(0.1f)]
        [SerializeField] private float emoteNameSeconds = 1.5f;

        private IRaceSession _session;
        private IRaceStandings _standings;
        private UiMotionConfig _motion;
        private string _shownPlace;
        private Tween _placePunch;
        private IRaceTelemetry _telemetry;
        private IWallet _wallet;
        private IRaceHudActions _actions;
        private long _shownCentiseconds = -1;
        private int _shownMeters = -1;
        private int _shownSpeedTenths = -1;
        private int _shownCash = -1;
        private float _shownCadence = -1f;
        private bool _warnedMissing;
        private IRaceEmoteControl _emotes;
        private IEmoteRaceOptions _emoteOptions;
        private bool _emotesSubscribed;
        private Tween _emoteNameHide;

        [Inject]
        public void InjectHud(IRaceSession session, [InjectOptional] IRaceTelemetry telemetry, [InjectOptional] IWallet wallet,
            [InjectOptional] IRaceHudActions actions)
        {
            _session = session;
            _telemetry = telemetry;
            _wallet = wallet;
            _actions = actions;
        }

        /// <summary>Optional: without opponents the Place panel stays hidden; without the motion config the place changes without a pop.</summary>
        [Inject]
        public void InjectStandings([InjectOptional] IRaceStandings standings, [InjectOptional] UiMotionConfig motion)
        {
            _standings = standings;
            _motion = motion;
        }

        /// <summary>Optional: without the emote control (or without an emote equipped) the Emote button stays hidden.</summary>
        [Inject]
        public void InjectEmotes([InjectOptional] IRaceEmoteControl emotes, [InjectOptional] IEmoteRaceOptions options)
        {
            _emotes = emotes;
            _emoteOptions = options ?? new DefaultEmoteRaceOptions();
        }

        protected override void Awake()
        {
            base.Awake();
            if (emoteButton != null) emoteButton.onClick.AddListener(OnEmote);
            if (emoteNameText != null) emoteNameText.gameObject.SetActive(false);
            if (exitButton != null) exitButton.onClick.AddListener(OnExit);
            if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
            if (cashPlusButton != null) cashPlusButton.transform.parent.gameObject.SetActive(false); // no Cash box in a race: it looked like a reward, and there is no shop behind the plus
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _shownCentiseconds = -1;
            _shownMeters = -1;
            _shownSpeedTenths = -1;
            _shownCash = -1;
            _shownCadence = -1f;

            _shownPlace = null;
            if (_standings != null) _standings.Changed += OnStandingsChanged;
            RefreshPlace(punch: false);
            SubscribeEmotes();
        }

        protected override void OnDisable()
        {
            if (_standings != null) _standings.Changed -= OnStandingsChanged;
            KillPlacePunch();
            UnsubscribeEmotes();
            HideEmoteName();
            base.OnDisable();
        }

        private void SubscribeEmotes()
        {
            if (_emotesSubscribed || _emotes == null) return;
            _emotesSubscribed = true;
            _emotes.Changed += RefreshEmote;
            _emoteOptions.Changed += RefreshEmote;
            RefreshEmote();
        }

        private void UnsubscribeEmotes()
        {
            if (!_emotesSubscribed) return;
            _emotesSubscribed = false;
            _emotes.Changed -= RefreshEmote;
            _emoteOptions.Changed -= RefreshEmote;
        }

        /// <summary>Visible only while the switch is on and an emote is equipped; pressable from GO on (the control decides).</summary>
        private void RefreshEmote()
        {
            if (emoteButton == null) return;
            bool visible = _emotes != null && RaceEmoteRules.ButtonVisible(_emoteOptions.ShowEmoteButton, _emotes.HasEmote);
            if (emoteButton.gameObject.activeSelf != visible) emoteButton.gameObject.SetActive(visible);
            if (visible) emoteButton.interactable = _emotes.CanEmote;
        }

        private void OnEmote()
        {
            if (_emotes == null || !_emotes.RequestEmote()) return; // a refused press already got its denied sound from the button
            if (emoteNameText == null) return;
            emoteNameText.text = _emotes.EmoteName;
            emoteNameText.gameObject.SetActive(true);
            _emoteNameHide?.Kill();
            _emoteNameHide = DOVirtual.DelayedCall(emoteNameSeconds, HideEmoteName).SetLink(gameObject);
        }

        private void HideEmoteName()
        {
            _emoteNameHide?.Kill();
            _emoteNameHide = null;
            if (emoteNameText != null) emoteNameText.gameObject.SetActive(false);
        }

        private void OnStandingsChanged() => RefreshPlace(punch: true);

        /// <summary>"3rd" while opponents run, hidden otherwise; the label pops when the place changes.</summary>
        private void RefreshPlace(bool punch)
        {
            string text = _standings != null ? RaceStandingsFormat.PlaceText(_standings.PlayerRank, _standings.RunnerCount) : string.Empty;
            if (placeRoot != null) placeRoot.SetActive(text.Length > 0);
            if (text == _shownPlace) return;

            bool hadPlace = !string.IsNullOrEmpty(_shownPlace);
            _shownPlace = text;
            if (placeText != null) placeText.text = text;
            if (punch && hadPlace && text.Length > 0) PunchPlace();
        }

        private void PunchPlace()
        {
            if (_motion == null || placeText == null || !isActiveAndEnabled) return;
            KillPlacePunch();
            Transform target = placeRoot != null ? placeRoot.transform : placeText.transform;
            Tween punch = target.DOPunchScale(Vector3.one * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetLink(gameObject);
            punch.OnKill(() =>
            {
                if (_placePunch == punch) _placePunch = null;
                if (target != null) target.localScale = Vector3.one;
            });
            _placePunch = punch;
        }

        private void KillPlacePunch()
        {
            if (_placePunch == null) return;
            Tween running = _placePunch;
            _placePunch = null;
            running.Kill();
        }

        protected override void OnDestroy()
        {
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
            if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
            if (emoteButton != null) emoteButton.onClick.RemoveListener(OnEmote);
            base.OnDestroy();
        }

        private void OnExit() => _actions?.RequestExit();

        private void OnRestart() => _actions?.RequestRestart();

        private void Update()
        {
            if (!_emotesSubscribed) SubscribeEmotes(); // the control may be injected after OnEnable
            if (_session == null) return;
            if (timeText == null)
            {
                if (!_warnedMissing)
                {
                    _warnedMissing = true;
                    Debug.LogWarning($"{name}: RaceHudView.timeText is not assigned in the prefab.", this);
                }
                return;
            }

            long cc = RaceDisplayFormat.ToCentiseconds(_session.RaceTime);
            if (cc != _shownCentiseconds)
            {
                _shownCentiseconds = cc;
                timeText.text = RaceDisplayFormat.Time(cc);
            }

            UpdateProgress();
            UpdateTelemetry();
            UpdateCash();
        }

        private void UpdateProgress()
        {
            float target = _session.TargetDistance;
            if (progressFill != null) progressFill.fillAmount = RaceDisplayFormat.Progress(_session.Distance, target);
            if (distanceText == null) return;

            int meters = RaceDisplayFormat.WholeMeters(_session.Distance, target);
            if (meters == _shownMeters) return;
            _shownMeters = meters;
            distanceText.text = RaceDisplayFormat.Meters(meters);
        }

        private void UpdateTelemetry()
        {
            if (_telemetry == null) return;

            if (speedText != null)
            {
                int tenths = RaceDisplayFormat.SpeedTenths(_telemetry.SpeedMetersPerSecond);
                if (tenths != _shownSpeedTenths)
                {
                    _shownSpeedTenths = tenths;
                    speedText.text = RaceDisplayFormat.Speed(_telemetry.SpeedMetersPerSecond);
                }
            }

            if (energyFill != null) energyFill.fillAmount = Mathf.Clamp01(_telemetry.Energy01);

            if (cadenceTrack != null && cadenceMarker != null)
            {
                float fraction = RaceDisplayFormat.CadenceFraction(_telemetry.PressesPerSecond, cadenceMaxPressesPerSecond);
                if (!Mathf.Approximately(fraction, _shownCadence))
                {
                    _shownCadence = fraction;
                    Rect rect = cadenceTrack.rect;
                    // World position, so the marker follows the track whatever its own anchors and parent are.
                    Vector3 world = cadenceTrack.TransformPoint(new Vector3(rect.xMin + fraction * rect.width, rect.center.y, 0f));
                    cadenceMarker.position = new Vector3(world.x, world.y, cadenceMarker.position.z);
                }
            }
        }

        private void UpdateCash()
        {
            if (_wallet == null || cashText == null) return;
            int cash = _wallet.Cash;
            if (cash == _shownCash) return;
            _shownCash = cash;
            cashText.text = cash.ToString();
        }
    }
}
