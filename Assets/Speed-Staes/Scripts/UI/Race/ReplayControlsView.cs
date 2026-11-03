using SpeedStars.Core.Race;
using UnityEngine;
using UnityEngine.UI;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// The replay's controls, in the race HUD's look: top left the followed runner's race clock and speed on the HUD's telemetry panel
    /// with his name below; top right runner &lt; camera &gt; and Exit as the HUD's square buttons; along the bottom play/pause, the
    /// timeline (the HUD's energy bar, drag to seek) and the speed. The ReplayControls prefab (own overlay canvas, built by
    /// UiReplayPrefabs from <see cref="ReplayControlsSkin"/>) holds every object; this only binds and fills them. Results instantiates
    /// it when Watch Replay starts. Closes itself when the replay ends.
    /// </summary>
    public sealed class ReplayControlsView : MonoBehaviour
    {
        [Header("Clock")]
        [Tooltip("Clock/Time: the replay's race clock.")]
        [SerializeField] private Text timeText;

        [Tooltip("Clock/Speed: the followed runner's speed.")]
        [SerializeField] private Text speedText;

        [Tooltip("Name/Label: the followed runner's name.")]
        [SerializeField] private Text nameText;

        [Header("Top right buttons")]
        [Tooltip("Exit: leaves the replay.")]
        [SerializeField] private Button exitButton;

        [Tooltip("NextRunner: follows the next runner.")]
        [SerializeField] private Button nextRunnerButton;

        [Tooltip("Camera: next camera shot.")]
        [SerializeField] private Button cameraButton;

        [Tooltip("PrevRunner: follows the previous runner.")]
        [SerializeField] private Button prevRunnerButton;

        [Header("Bottom bar")]
        [Tooltip("Play: play / pause.")]
        [SerializeField] private Button playButton;

        [Tooltip("Play/Label: 'II' while playing, the play triangle while paused.")]
        [SerializeField] private Text playText;

        [Tooltip("Timeline: the seek bar (0..1 of the replay).")]
        [SerializeField] private Slider timeline;

        [Tooltip("Rate: cycles the playback speed.")]
        [SerializeField] private Button rateButton;

        [Tooltip("Rate/Label: the playback speed, like '1x'.")]
        [SerializeField] private Text rateText;

        private IRaceReplay _replay;

        /// <summary>Opens the controls over a replay that is already playing: an instance of the prefab, bound to the replay.</summary>
        public static ReplayControlsView Open(ReplayControlsView prefab, IRaceReplay replay)
        {
            ReplayControlsView view = Instantiate(prefab);
            view.Bind(replay);
            return view;
        }

        private void Bind(IRaceReplay replay)
        {
            _replay = replay;
            exitButton.onClick.AddListener(() => _replay.Leave());
            nextRunnerButton.onClick.AddListener(() => _replay.Follow(1));
            cameraButton.onClick.AddListener(() => _replay.NextShot(1));
            prevRunnerButton.onClick.AddListener(() => _replay.Follow(-1));
            playButton.onClick.AddListener(() => _replay.SetPlaying(!_replay.IsPlaying));
            timeline.onValueChanged.AddListener(v => _replay.Seek(v * _replay.Duration));
            rateButton.onClick.AddListener(() => _replay.NextSpeed());

            _replay.WatchEnded += OnWatchEnded;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_replay != null) _replay.WatchEnded -= OnWatchEnded;
        }

        private void OnWatchEnded() => Destroy(gameObject);

        private void Update() => Refresh();

        private void Refresh()
        {
            if (_replay == null) return;
            float duration = _replay.Duration;
            timeline.SetValueWithoutNotify(duration > 0f ? _replay.Time / duration : 0f);
            timeText.text = _replay.Time.ToString("0.00");
            speedText.text = _replay.FollowedSpeed.ToString("0.0") + " m/s";
            nameText.text = _replay.FollowedName;
            playText.text = _replay.IsPlaying ? "II" : "▶";
            rateText.text = _replay.Speed.ToString("0.##") + "x";
        }
    }
}
