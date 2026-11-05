using SpeedStars.Core.Audio;
using SpeedStars.Core.Race;
using SpeedStars.UI.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The READY / SET / GO! words and speed lines over the Race HUD (Yaris_Baslangic_UI_v1), and on the same banner the word at the finish
    /// line (the flag is already on it). It only reacts: the race session
    /// (IRaceStartSignal) owns the countdown and opens the runner's input; this view never starts, delays or times anything of the race.
    /// A phase that arrives early, a restart (Ready again) or a cancel (Idle) just restarts or hides the motion, so there is never a second
    /// GO! or a layer left on screen. The motion itself is RaceStartMotion (pure, tested); this class copies its pose onto the prefab's
    /// objects. Nothing here catches taps: every graphic has raycast off and the CanvasGroup blocks nothing.
    /// Sound (T-052): each phase change the signal raises plays its countdown beep through the optional <see cref="ISfx"/>, on the same
    /// event as the input gate (GO sounds exactly when the runner may move). A late bind that only catches up does not sound.
    /// T-120: GO adds the pistol and the crowd, a false start has its own sound, and the line plays the place's sound (RaceSfxRules) and the crowd.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceStartView : MonoBehaviour
    {
        private const string ReadyWord = "READY";
        private const string SetWord = "SET";
        private const string GoWord = "GO!";
        private const string FalseStartWord = "FAIL";

        [Tooltip("RaceStartConfig asset: motion numbers, colours, group scale.")]
        [SerializeField] private RaceStartConfig config;

        [Tooltip("BackdropRoot: the full-screen dim and the banner (background + flag) behind the words (T-064). A sibling under the Group, never a parent of the word, so the word's squash and growth never touch it. It shows and hides with the Group.")]
        [SerializeField] private GameObject backdropRoot;

        [Tooltip("Group: the speed lines and the word (the art's 1024x576 canvas x the config's group scale, built into its size); shows and hides with the countdown.")]
        [SerializeField] private RectTransform group;

        [Tooltip("Group/EffectsRoot: the speed lines' shared parent; it scales during the GO! burst.")]
        [SerializeField] private RectTransform effectsRoot;

        [Tooltip("Group/TextRoot: the word's box (700x200); slide, squash and shake act on it.")]
        [SerializeField] private RectTransform textRoot;

        [Tooltip("Group/TextRoot/Word: the READY / SET / GO! text (TextMeshPro; its outline is set on the TMP material).")]
        [SerializeField] private TMP_Text wordText;

        [Tooltip("impact_01..03: the lines under READY when it lands.")]
        [SerializeField] private Image[] impactImages;

        [Tooltip("tension_left_01/02 and tension_right_01/02: the lines beside SET.")]
        [SerializeField] private Image[] tensionImages;

        [Tooltip("release_01..06: the lines that burst out with GO!.")]
        [SerializeField] private Image[] releaseImages;

        private IRaceSession _session;
        private string _finishWord = string.Empty;
        private IRaceStartSignal _signal;
        private RaceStartMotion _motion;
        private RaceStartPhase _phase;
        private float _seconds;
        private bool _subscribed;
        private bool _shown;
        private bool _preview;
        private ISfx _sfx;

        [Inject]
        public void InjectStart(IRaceSession session)
        {
            if (_subscribed && _session != null) _session.Finished -= OnFinished;
            _session = session;
            if (_subscribed && _session != null) _session.Finished += OnFinished;
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
                PrepareWord();
            }
            if (_signal == null || _subscribed || !isActiveAndEnabled) return;

            _signal.StartPhaseChanged += OnPhaseChanged;
            if (_session != null) _session.Finished += OnFinished;
            _subscribed = true;
            // Bound late (the countdown already began): show where it is instead of waiting for the next event.
            ShowPhase(_signal.StartPhase);
        }

        /// <summary>
        /// Outline width and colour from the config (set on the word's own TMP material instance, never on the shared preset) and the
        /// three words' glyphs asked from the font asset now, so the very first frame of READY is drawn with its letters.
        /// </summary>
        private void PrepareWord()
        {
            if (wordText == null) return;
            wordText.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline); // fontMaterial is this word's instance
            wordText.outlineWidth = config.OutlineWidth;
            wordText.outlineColor = config.OutlineColor;
            if (wordText.font != null && wordText.font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                wordText.font.TryAddCharacters(ReadyWord + SetWord + GoWord + FalseStartWord);
        }

        private void Unsubscribe()
        {
            if (_subscribed && _signal != null) _signal.StartPhaseChanged -= OnPhaseChanged;
            if (_subscribed && _session != null) _session.Finished -= OnFinished;
            _subscribed = false;
        }

        /// <summary>The line: the place (or FINISH!) with its own sound. The session has just cancelled what was left of GO!.</summary>
        private void OnFinished(RaceOutcome outcome)
        {
            _finishWord = RaceStandingsFormat.FinishWord(outcome.Rank, outcome.RunnerCount);
            if (wordText != null && wordText.font != null && wordText.font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                wordText.font.TryAddCharacters(_finishWord);
            ShowPhase(RaceStartPhase.Finish);
            if (_sfx == null) return;
            _sfx.Play(RaceSfxRules.FinishFor(outcome.Rank, outcome.RunnerCount));
            _sfx.Play(SfxId.CrowdEnd);
        }

        private void OnPhaseChanged(RaceStartPhase phase)
        {
            ShowPhase(phase);
            if (_sfx == null) return;
            if (phase == RaceStartPhase.Ready) _sfx.Play(SfxId.CountdownReady);
            else if (phase == RaceStartPhase.Set) _sfx.Play(SfxId.CountdownSet);
            else if (phase == RaceStartPhase.Go) PlayGo();
            else if (phase == RaceStartPhase.FalseStart) _sfx.Play(SfxId.FalseStart);
        }

        /// <summary>GO: the beep, the starter's pistol and the crowd, together (the runner may move from this very moment).</summary>
        private void PlayGo()
        {
            _sfx.Play(SfxId.CountdownGo);
            _sfx.Play(SfxId.StartGun);
            _sfx.Play(SfxId.CrowdStart);
        }

        /// <summary>Starts the motion of a phase from its first frame, replacing whatever was playing.</summary>
        public void ShowPhase(RaceStartPhase phase)
        {
            _preview = false;
            _phase = phase;
            _seconds = 0f;
            if (_motion == null) return;

            if (phase == RaceStartPhase.Idle)
            {
                Hide();
                return;
            }
            wordText.text = phase == RaceStartPhase.Ready ? ReadyWord : phase == RaceStartPhase.Set ? SetWord
                : phase == RaceStartPhase.FalseStart ? FalseStartWord : phase == RaceStartPhase.Finish ? _finishWord : GoWord;
            Apply(_motion.Sample(phase, 0f));
        }

        /// <summary>
        /// Evidence shots (PlayMode screenshot test): shows the pose of <paramref name="phase"/> at exactly <paramref name="seconds"/> and
        /// stops following the clock until the next phase event. Never used by the game.
        /// </summary>
        public void PreviewAt(RaceStartPhase phase, float seconds)
        {
            if (_motion == null || phase == RaceStartPhase.Idle) return;
            ShowPhase(phase);
            _preview = true;
            _seconds = Mathf.Max(0f, seconds);
            Apply(_motion.Sample(phase, _seconds));
        }

        private void Update()
        {
            if (_phase == RaceStartPhase.Idle || _motion == null || _preview) return;

            // Unscaled: the motion keeps its look at any time scale; the session decides what a pause does to the race.
            // One frame never counts more than the session's own frame limit: the load hitch before the first frame must not
            // swallow READY's slide-in and rebound, which would show it already settled (or not at all).
            _seconds += Mathf.Min(Time.unscaledDeltaTime, config.MaxFrameSeconds);
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
            if (backdropRoot != null) backdropRoot.SetActive(false);
            if (_phase == RaceStartPhase.Go || _phase == RaceStartPhase.Finish) _phase = RaceStartPhase.Idle; // the burst is over; nothing more to animate
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
                if (backdropRoot != null) backdropRoot.SetActive(true);
            }

            // The pose offsets are in the art's units; the words are built group-scale times bigger, so the moves are too.
            textRoot.anchoredPosition = new Vector2(pose.TextX, pose.TextY) * config.GroupScale;
            textRoot.localScale = new Vector3(pose.TextScale.x, pose.TextScale.y, 1f);
            effectsRoot.localScale = new Vector3(pose.EffectsScale, pose.EffectsScale, 1f);

            // READY / SET: navy on the cream banner; GO!, FAIL and the finish word: red with the navy outline.
            bool red = _phase == RaceStartPhase.Go || _phase == RaceStartPhase.FalseStart || _phase == RaceStartPhase.Finish;
            Color fill = red ? config.GoColor : config.ReadySetColor;
            fill.a *= pose.TextAlpha;
            wordText.color = fill;
            // The TMP outline is one pass under the fill (no stacked copies), so it fades with the word's own alpha.
            Color outline = config.OutlineColor;
            outline.a *= pose.TextAlpha;
            // Navy words on cream need no outline (the approved art has none); stacked navy copies would only fatten the letters.
            if (!red && !config.ReadySetOutline) outline.a = 0f;
            wordText.outlineColor = outline;

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
