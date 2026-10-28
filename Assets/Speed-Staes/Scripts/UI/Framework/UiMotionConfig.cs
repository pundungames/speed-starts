using DG.Tweening;
using UnityEngine;

namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Every UI motion number in one asset (agent-kit/05 §0-§1): changing the feel of panels, popups, lists, buttons or
    /// counters is an asset edit that applies to every screen at once. Code holds no durations. The field defaults
    /// below are the kit's approved values and only matter when the asset is first created; at runtime the asset
    /// (bound by the installer) is what counts.
    /// </summary>
    [CreateAssetMenu(fileName = "UiMotionConfig", menuName = "Speed Stars/UI/Ui Motion Config")]
    public sealed class UiMotionConfig : ScriptableObject
    {
        [Header("Screens (UiScreen)")]
        [Tooltip("Screen opening: fades in and scales from 'scale' x designed scale up to the designed scale.")]
        [SerializeField] private UiTransition screenOpen = new UiTransition(0.22f, 0.90f, Ease.OutBack);

        [Tooltip("Screen closing: fades out and shrinks to 'scale' x designed scale, then deactivates. Keep it shorter than the open.")]
        [SerializeField] private UiTransition screenClose = new UiTransition(0.16f, 0.95f, Ease.InQuad);

        [Header("Popups (UiPopup)")]
        [Tooltip("Popup opening: same recipe as screens, separate numbers so popups can be tuned on their own.")]
        [SerializeField] private UiTransition popupOpen = new UiTransition(0.22f, 0.90f, Ease.OutBack);

        [Tooltip("Popup closing: fades out and shrinks to 'scale' x designed scale, then deactivates. Keep it shorter than the open.")]
        [SerializeField] private UiTransition popupClose = new UiTransition(0.16f, 0.95f, Ease.InQuad);

        [Header("Stagger (list items, on open only)")]
        [Tooltip("Wanted delay between two list items (s). Shrinks automatically for long lists (see max total).")]
        [Min(0f)]
        [SerializeField] private float staggerInterval = 0.05f;

        [Tooltip("Reveal length of one item (s).")]
        [Min(0f)]
        [SerializeField] private float staggerItemDuration = 0.20f;

        [Tooltip("Start scale of an item, times its designed scale.")]
        [Min(0f)]
        [SerializeField] private float staggerScaleFrom = 0.70f;

        [Tooltip("Easing of an item's reveal.")]
        [SerializeField] private Ease staggerEase = Ease.OutBack;

        [Tooltip("Cap for the whole list's delays (s): with many items the interval becomes cap / count. 0 = no cap.")]
        [Min(0f)]
        [SerializeField] private float staggerMaxTotal = 0.60f;

        [Header("Button punch (ButtonFeedback)")]
        [Tooltip("Punch size as a fraction of the button's designed scale (0.12 = +12%).")]
        [Min(0f)]
        [SerializeField] private float buttonPunchScale = 0.12f;

        [Tooltip("Punch length (s).")]
        [Min(0f)]
        [SerializeField] private float buttonPunchDuration = 0.18f;

        [Tooltip("How many times the punch wobbles.")]
        [Min(0)]
        [SerializeField] private int buttonPunchVibrato = 6;

        [Header("Counter (numbers tick up, never jump)")]
        [Tooltip("Time a changing number takes to count to its new value (s).")]
        [Min(0f)]
        [SerializeField] private float counterDuration = 0.35f;

        [Tooltip("Easing of the count. Kit: OutCubic.")]
        [SerializeField] private Ease counterEase = Ease.OutCubic;

        public UiTransition ScreenOpen => screenOpen;
        public UiTransition ScreenClose => screenClose;
        public UiTransition PopupOpen => popupOpen;
        public UiTransition PopupClose => popupClose;

        public float StaggerInterval => staggerInterval;
        public float StaggerItemDuration => staggerItemDuration;
        public float StaggerScaleFrom => staggerScaleFrom;
        public Ease StaggerEase => staggerEase;
        public float StaggerMaxTotal => staggerMaxTotal;

        public float ButtonPunchScale => buttonPunchScale;
        public float ButtonPunchDuration => buttonPunchDuration;
        public int ButtonPunchVibrato => buttonPunchVibrato;

        public float CounterDuration => counterDuration;
        public Ease CounterEase => counterEase;

        /// <summary>Delay between items for a list of <paramref name="count"/>, already squeezed under the cap.</summary>
        public float StaggerIntervalFor(int count) => UiStagger.Interval(staggerInterval, staggerMaxTotal, count);
    }
}
