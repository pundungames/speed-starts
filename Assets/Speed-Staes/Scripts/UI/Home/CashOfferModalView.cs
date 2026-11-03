using System.Globalization;
using DG.Tweening;
using SpeedStars.Core.Audio;
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
    /// The Cash offer window (prefab CashOfferModal, a UiPopup), opened by the "+" of the resource bar's Cash box: the player's
    /// Cash, "+200" for one ad, the "0/3" ads-today counter and one WATCH AD button. The button is closed (greyed, with a line that
    /// says why) when today's ads are used up, no ad is ready or the offer is off in the config. A granted ad pays at once; the
    /// window then flies a diamond from the offer to the Cash value, counts it up and punches it (the wallet already holds the Cash,
    /// the flight only shows it). Any other outcome shakes the window. No real-money rows. What the window shows is decided by
    /// <see cref="CashOfferModel"/>; the ad rules live in <c>IRewardedCash</c>. Binds existing objects, creates nothing.
    /// </summary>
    public sealed class CashOfferModalView : UiPopup
    {
        [Tooltip("Window: shaken when the ad did not pay.")]
        [SerializeField] private RectTransform window;

        [Tooltip("Window/Title: GET CASH on the header.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Window/Balance/Value: the player's Cash.")]
        [SerializeField] private TMP_Text balanceText;

        [Tooltip("Window/Balance: the Cash row; the flying diamond lands here and it punches.")]
        [SerializeField] private RectTransform balanceBox;

        [Tooltip("Window/Offer/Icon: where the diamond takes off.")]
        [SerializeField] private RectTransform offerIcon;

        [Tooltip("Window/Offer/Amount: '+200'.")]
        [SerializeField] private TMP_Text amountText;

        [Tooltip("Window/Offer/Caption: 'Watch an ad'.")]
        [SerializeField] private TMP_Text captionText;

        [Tooltip("Window/Offer/Counter: 'ads watched today / daily limit', e.g. 0/3.")]
        [SerializeField] private TMP_Text counterText;

        [Tooltip("Window/WatchAd: not interactable while the offer is closed or an ad runs.")]
        [SerializeField] private Button watchAdButton;

        [Tooltip("Window/WatchAd/Label: WATCH AD; hidden while the spinner shows.")]
        [SerializeField] private TMP_Text watchAdLabel;

        [Tooltip("Window/WatchAd/Spinner: turns while an ad runs; inactive otherwise.")]
        [SerializeField] private RectTransform watchAdSpinner;

        [Tooltip("Window/Note: why the button is closed (limit reached, no ad); hidden while it works.")]
        [SerializeField] private TMP_Text noteText;

        [Tooltip("CashFlight: the diamond that flies from the offer to the Cash value after a granted ad.")]
        [SerializeField] private CashFlightIcon flight;

        [Header("Loading spinner")]
        [Tooltip("Seconds one full turn of the loading spinner takes.")]
        [Min(0.1f)]
        [SerializeField] private float spinnerTurnSeconds = 1f;

        [Header("Refusal shake")]
        [Tooltip("Seconds the window shakes when the ad did not pay.")]
        [Min(0f)]
        [SerializeField] private float shakeDuration = 0.35f;

        [Tooltip("Sideways shake size in canvas pixels.")]
        [Min(0f)]
        [SerializeField] private float shakeStrength = 24f;

        [Tooltip("How many times the shake swings.")]
        [Min(1)]
        [SerializeField] private int shakeVibrato = 18;

        private IWallet _wallet;
        private ScreenNavigator _navigator;
        private UiMotionConfig _motion;
        private IRewardedCash _cash;
        private ISfx _sfx;
        private IHaptics _haptics;
        private Tween _shake;
        private Tween _spin;
        private Tween _count;
        private Tween _punch;
        private Vector3 _balanceBaseScale = Vector3.one;
        private int _pendingPayment; // Cash already paid but not yet shown in the balance (the diamond is still flying)
        private int? _frozenBalance; // while an ad runs the balance keeps this value: the payment lands in the wallet before the diamond flies

        [Inject]
        public void ConstructCashOffer(IWallet wallet, ScreenNavigator navigator, UiMotionConfig motion)
        {
            _wallet = wallet;
            _navigator = navigator;
            _motion = motion;
        }

        /// <summary>Optional so a build without the ad service (or before its WIRING) shows the window with the button closed.</summary>
        [Inject]
        public void ConstructCashOfferAds([InjectOptional] IRewardedCash cash, [InjectOptional] ISfx sfx, [InjectOptional] IHaptics haptics)
        {
            _cash = cash;
            _sfx = sfx;
            _haptics = haptics;
        }

        /// <summary>Opens the window through the navigator's popup layer.</summary>
        public void Open()
        {
            Refresh();
            _navigator.OpenPopup(this);
        }

        protected override void Awake()
        {
            base.Awake();
            watchAdButton.onClick.AddListener(OnWatchAd);
            titleText.text = CashOfferTexts.Title;
            captionText.text = CashOfferTexts.Caption;
            watchAdLabel.text = CashOfferTexts.WatchAd;
            _balanceBaseScale = balanceBox.localScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _wallet.Changed += Refresh;
            if (_cash != null) _cash.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            _wallet.Changed -= Refresh;
            if (_cash != null) _cash.Changed -= Refresh;
            StopShow(); // an ad that is still running keeps going and pays; the balance shows the final value next time
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
            base.OnDestroy();
        }

        private void Refresh()
        {
            CashOfferState state = _cash != null
                ? new CashOfferState(_cash.Amount, _cash.DailyLimit, _cash.RemainingToday, _cash.IsBusy, _cash.IsAvailable)
                : new CashOfferState(0, 0, 0, false, false);
            CashOfferModel model = CashOfferModel.Build(state);

            amountText.text = model.AmountText;
            counterText.text = model.CounterText;
            watchAdButton.interactable = model.WatchInteractable;
            watchAdLabel.gameObject.SetActive(!model.ShowSpinner);
            SetSpinner(model.ShowSpinner);
            noteText.gameObject.SetActive(model.NoteText.Length > 0);
            noteText.text = model.NoteText;
            if (_count == null) balanceText.text = (_frozenBalance ?? _wallet.Cash - _pendingPayment).ToString(CultureInfo.InvariantCulture);
        }

        private void OnWatchAd()
        {
            if (_cash == null || !_cash.IsAvailable) return; // closed offer, or an ad already runs (double tap)

            int before = _wallet.Cash;
            _frozenBalance = before;
            if (_cash.TryWatch(outcome => OnAdFinished(outcome, before))) Refresh(); // the spinner (a result that came at once has already refreshed)
            else _frozenBalance = null;
        }

        /// <summary>
        /// The ad ended, maybe after the window was closed. The service has already paid by now; this only shows it, and only
        /// while the window is up.
        /// </summary>
        private void OnAdFinished(RewardedCashOutcome outcome, int cashBefore)
        {
            if (this == null) return;
            _frozenBalance = null;
            if (!isActiveAndEnabled) return;

            if (outcome != RewardedCashOutcome.Granted)
            {
                Refresh();
                Shake();
                return;
            }

            _sfx?.Play(SfxId.Claim);
            _haptics?.Tap();
            int paid = Mathf.Max(0, _wallet.Cash - cashBefore);
            _pendingPayment = paid; // the balance keeps its old value until the diamond lands
            Refresh();
            if (flight == null || _motion == null)
            {
                OnDiamondLanded(paid);
                return;
            }
            flight.Play(_motion, offerIcon, balanceBox, () => OnDiamondLanded(paid));
        }

        /// <summary>The diamond arrived: the balance counts up to the wallet value and punches.</summary>
        private void OnDiamondLanded(int paid)
        {
            _pendingPayment = 0;
            int target = _wallet.Cash;
            PunchBalance();
            if (_motion == null || paid <= 0)
            {
                balanceText.text = target.ToString(CultureInfo.InvariantCulture);
                return;
            }

            int shown = target - paid;
            Tween count = DOTween.To(() => shown, v =>
            {
                shown = v;
                balanceText.text = v.ToString(CultureInfo.InvariantCulture);
            }, target, _motion.CounterDuration).SetEase(_motion.CounterEase).SetUpdate(true).SetLink(gameObject);
            count.OnComplete(() =>
            {
                _count = null;
                Refresh();
            });
            _count = count;
        }

        private void PunchBalance()
        {
            if (_motion == null) return;
            KillPunch();
            balanceBox.localScale = _balanceBaseScale;
            _punch = balanceBox
                .DOPunchScale(_balanceBaseScale * _motion.ButtonPunchScale, _motion.ButtonPunchDuration, _motion.ButtonPunchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void SetSpinner(bool running)
        {
            watchAdSpinner.gameObject.SetActive(running);
            if (!running)
            {
                KillSpin();
                return;
            }
            if (_spin != null) return;

            watchAdSpinner.localRotation = Quaternion.identity;
            _spin = watchAdSpinner
                .DOLocalRotate(new Vector3(0f, 0f, -360f), spinnerTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void Shake()
        {
            KillShake();
            _shake = window.DOShakePosition(shakeDuration, new Vector3(shakeStrength, 0f, 0f), shakeVibrato, 0f, false, true)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        /// <summary>Window closed: every running show ends on its final look (no half-counted balance, no shifted window).</summary>
        private void StopShow()
        {
            KillShake();
            KillSpin();
            KillPunch();
            if (flight != null) flight.Cancel();
            if (_count != null)
            {
                Tween running = _count;
                _count = null;
                running.Kill();
            }
            _pendingPayment = 0;
            _frozenBalance = null;
        }

        private void KillSpin()
        {
            if (_spin == null) return;
            _spin.Kill();
            _spin = null;
        }

        private void KillPunch()
        {
            if (_punch != null)
            {
                _punch.Kill();
                _punch = null;
            }
            if (balanceBox != null) balanceBox.localScale = _balanceBaseScale;
        }

        private void KillShake()
        {
            if (_shake == null) return;
            Tween running = _shake;
            _shake = null;
            running.Kill(true); // complete: the window ends on its designed position
        }
    }
}
