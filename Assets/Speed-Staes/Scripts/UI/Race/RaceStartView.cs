using SpeedStars.Core.Audio;
using SpeedStars.Core.Race;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The READY / SET / GO! words and speed lines over the Race HUD (Yaris_Baslangic_UI_v1). It only reacts: the race session
    /// (IRaceStartSignal) owns the countdown and opens the runner's input; this view never starts, delays or times anything of the race.
    /// A phase that arrives early, a restart (Ready again) or a cancel (Idle) just restarts or hides the motion, so there is never a second
    /// GO! or a layer left on screen. The motion itself is RaceStartMotion (pure, tested); this class copies its pose onto the prefab's
    /// objects. Nothing here catches taps: every graphic has raycast off and the CanvasGroup blocks nothing.
    /// Sound (T-052): each phase change the signal raises plays its countdown beep through the optional <see cref="ISfx"/>, on the same
    /// event as the input gate (GO sounds exactly when the runner may move). A late bind that only catches up does not sound.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceStartView : MonoBehaviour
    {
        private const string ReadyWord = "READY";
        private const string SetWord = "SET";
        private const string GoWord = "GO!";

        [Tooltip("RaceStartConfig asset: motion numbers, colours, group scale.")]
        [SerializeField] private RaceStartConfig config;

        [Tooltip("Group: everything that shows and hides (1024x576, the art's canvas; scaled by the config).")]
        [SerializeField] private RectTransform group;

        [Tooltip("Group/EffectsRoot: the speed lines' shared parent; it scales during the GO! burst.")]
        [SerializeField] private RectTransform effectsRoot;

        [Tooltip("Group/TextRoot: the word's box (700x200); slide, squash and shake act on it.")]
        [SerializeField] private RectTransform textRoot;

        [Tooltip("Group/TextRoot/Word: the READY / SET / GO! text (legacy Text, bold italic).")]
        [SerializeField] private Text wordText;

        [Tooltip("The Outline on the Word: its colour follows the word's alpha.")]
        [SerializeField] private Outline wordOutline;

        [Tooltip("impact_01..03: the lines under READY when it lands.")]
        [SerializeField] private Image[] impactImages;

        [Tooltip("tension_left_01/02 and tension_right_01/02: the lines beside SET.")]
        [SerializeField] private Image[] tensionImages;

        [Tooltip("release_01..06: the lines that burst out with GO!.")]
        [SerializeField] private Image[] releaseImages;

        private IRaceStartSignal _signal;
        private RaceStartMotion _motion;
        private RaceStartPhase _phase;
        private float _seconds;
        private bool _subscribed;
        private bool _shown;
        private ISfx _sfx;

        [Inject]
        public void InjectStart(IRaceSession session)
        {
            Initialize(session as IRaceStartSignal);
        }

        /// <summary>Optional: without a bound sound system the countdown is silent.</summary>
        [Inject]
        public void InjectAudio([InjectOptional] ISfx sfx)
        {
            _sfx = sfx;
        }

        private void Awake() => Initialize();

        private void OnEnable() => Initialize();

        private void OnDisable()
        {
            Unsubscribe();
            Hide();
        }

        /// <summary>Idempotent setup (Awake, OnEnable and the Zenject inject all call it; EditMode tests call it directly).</summary>
        public void Initialize(IRaceStartSignal signal = null)
        {
            if (signal != null && signal != _signal)
            {
                Unsubscribe();
                _signal = signal;
            }
            if (_motion == null && config != null)
            {
                _motion = new RaceStartMotion(config);
                if (group != null) group.localScale = Vector3.one * config.GroupScale;
            }
            if (_signal == null || _subscribed || !isActiveAndEnabled) return;

            _signal.StartPhaseChanged += OnPhaseChanged;
            _subscribed = true;
            // Bound late (the countdown already began): show where it is instead of waiting for the next event.
            ShowPhase(_signal.StartPhase);
        }

        private void Unsubscribe()
        {
            if (_subscribed && _signal != null) _signal.StartPhaseChanged -= OnPhaseChanged;
            _subscribed = false;
        }

        private void OnPhaseChanged(RaceStartPhase phase)
        {
            ShowPhase(phase);
            if (_sfx == null) return;
            if (phase == RaceStartPhase.Ready) _sfx.Play(SfxId.CountdownReady);
            else if (phase == RaceStartPhase.Set) _sfx.Play(SfxId.CountdownSet);
            else if (phase == RaceStartPhase.Go) _sfx.Play(SfxId.CountdownGo);
        }

        /// <summary>Starts the motion of a phase from its first frame, replacing whatever was playing.</summary>
        public void ShowPhase(RaceStartPhase phase)
        {
            _phase = phase;
            _seconds = 0f;
            if (_motion == null) return;

            if (phase == RaceStartPhase.Idle)
            {
                Hide();
                return;
            }
            wordText.text = phase == RaceStartPhase.Ready ? ReadyWord : phase == RaceStartPhase.Set ? SetWord : GoWord;
            Apply(_motion.Sample(phase, 0f));
        }

        private void Update()
        {
            if (_phase == RaceStartPhase.Idle || _motion == null) return;

            // Unscaled: the motion keeps its look at any time scale; the session decides what a pause does to the race.
            _seconds += Time.unscaledDeltaTime;
            if (_motion.IsFinished(_phase, _seconds))
            {
                Hide();
                return;
            }
            Apply(_motion.Sample(_phase, _seconds));
        }

        private void Hide()
        {
            _shown = false;
            if (group != null) group.gameObject.SetActive(false);
            if (_phase == RaceStartPhase.Go) _phase = RaceStartPhase.Idle; // the burst is over; nothing more to animate
        }

        private void Apply(RaceStartPose pose)
        {
            if (!pose.Visible)
            {
                Hide();
                return;
            }
            if (!_shown)
            {
                _shown = true;
                if (group != null) group.gameObject.SetActive(true);
            }

            textRoot.anchoredPosition = new Vector2(pose.TextX, pose.TextY);
            textRoot.localScale = new Vector3(pose.TextScale.x, pose.TextScale.y, 1f);
            effectsRoot.localScale = new Vector3(pose.EffectsScale, pose.EffectsScale, 1f);

            Color fill = _phase == RaceStartPhase.Go ? config.GoColor : config.WordColor;
            fill.a *= pose.TextAlpha;
            wordText.color = fill;
            Color outline = config.OutlineColor;
            outline.a *= pose.TextAlpha;
            wordOutline.effectColor = outline;

            SetAlpha(impactImages, pose.ImpactAlpha);
            SetAlpha(tensionImages, pose.TensionAlpha);
            SetAlpha(releaseImages, pose.ReleaseAlpha);
        }

        private static void SetAlpha(Image[] images, float alpha)
        {
            if (images == null) return;
            var color = new Color(1f, 1f, 1f, alpha);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null) continue;
                image.enabled = alpha > 0.001f; // a fully faded layer costs no draw
                image.color = color;
            }
        }
    }
}
