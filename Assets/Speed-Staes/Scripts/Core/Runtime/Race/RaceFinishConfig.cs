using UnityEngine;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Numbers of the finish sequence in one asset (Data/Config/RaceFinishConfig): the slow motion at the line, how the runner stops and the
    /// steps it takes doing so, how it stands up, how long the celebration holds the Results back and where the camera goes. All first values,
    /// to be tuned by feel. The field defaults are what the seeder writes once; at runtime the asset counts.
    /// </summary>
    [CreateAssetMenu(fileName = "RaceFinishConfig", menuName = "Speed Stars/Config/Race Finish")]
    public sealed class RaceFinishConfig : ScriptableObject
    {
        [Header("Slow motion at the line")]
        [Tooltip("Game speed right after the line (1 = normal speed).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float slowMotionScale = 0.35f;

        [Tooltip("How long the slow motion holds (real seconds, whatever the game speed).")]
        [Min(0f)]
        [SerializeField] private float slowMotionSeconds = 0.6f;

        [Tooltip("How long the game takes to get back to normal speed after that (real seconds).")]
        [Min(0f)]
        [SerializeField] private float slowMotionReturnSeconds = 0.4f;

        [Header("Easing off: how the body stops")]
        [Tooltip("Game seconds from the line to standing still, coming over it at top speed. The sim's own stop is much longer and slides on planted feet: the body follows this instead.")]
        [Min(0.01f)]
        [SerializeField] private float stopSecondsFromTop = 2.1f;

        [Tooltip("The same, coming over it at walking speed; in between by speed.")]
        [Min(0.01f)]
        [SerializeField] private float stopSecondsFromWalk = 0.7f;

        [Tooltip("How the speed falls on the way: 1 = evenly, above 1 = quickly at first, gently into the stop.")]
        [Min(0.1f)]
        [SerializeField] private float stopCurve = 1.3f;

        [Header("Easing off: the steps after the line")]
        [Tooltip("How many steps the runner still takes after the line at most, each further apart. Without them it would slide to a stop on still legs.")]
        [Min(0)]
        [SerializeField] private int stepsAfterLine = 4;

        [Tooltip("No step starts later than this before the body stops (game seconds): it needs the time to land, or the legs would still be running on a standing body.")]
        [Min(0f)]
        [SerializeField] private float lastStepLeadSeconds = 0.35f;

        [Tooltip("Game seconds from the line to the first of those steps.")]
        [Min(0.05f)]
        [SerializeField] private float firstStepGapSeconds = 0.28f;

        [Tooltip("Each gap between steps is this many times the one before (above 1: the runner slows down).")]
        [Min(1f)]
        [SerializeField] private float stepGapGrowth = 1.25f;

        [Tooltip("How long the key of such a step stays down (game seconds). Long enough to read as a step, short enough not to become a hold.")]
        [Min(0.02f)]
        [SerializeField] private float stepPressSeconds = 0.11f;

        [Tooltip("With the head further forward than this (deg: a dive through the line) no step comes until the runner is up again.")]
        [SerializeField] private float stepHeadLimit = 30f;

        [Header("Standing up")]
        [Tooltip("Above this speed (m/s) the runner keeps its running stance; below it it starts to straighten up.")]
        [Min(0f)]
        [SerializeField] private float standFromSpeed = 4f;

        [Tooltip("At this speed (m/s) and under, it wants to stand fully.")]
        [Min(0f)]
        [SerializeField] private float standFullSpeed = 1f;

        [Tooltip("How long standing up takes at least (game seconds).")]
        [Min(0.01f)]
        [SerializeField] private float standSeconds = 0.7f;

        [Tooltip("Standing legs: the thigh from straight down (deg, + = forward).")]
        [SerializeField] private float standThigh = 4f;

        [Tooltip("Standing legs: the knee (deg, 180 = straight).")]
        [SerializeField] private float standKnee = 172f;

        [Tooltip("Standing hips: tilt (deg, + = forward).")]
        [SerializeField] private float standPelvisPitch = 0f;

        [Tooltip("Standing feet: how far further apart than at rest, each side (m).")]
        [SerializeField] private float standWidth = 0.07f;

        [Tooltip("Standing arms: upper arm swing (deg, + = forward).")]
        [SerializeField] private float standArmSwing = 4f;

        [Tooltip("Standing arms: elbow bend (deg, 0 = straight).")]
        [SerializeField] private float standElbow = 16f;

        [Header("Celebration")]
        [Tooltip("The emote and the reaction start when the body is this slow (m/s). Standing: on planted feet anything faster is a slide.")]
        [Min(0f)]
        [SerializeField] private float emoteStartSpeed = 0.01f;

        [Tooltip("The emote starts at the latest this long after the slow motion (real seconds), however fast the runner still is.")]
        [Min(0f)]
        [SerializeField] private float slowdownMaxSeconds = 4f;

        [Tooltip("Results wait this long after the runner has stopped when no emote plays (real seconds).")]
        [Min(0f)]
        [SerializeField] private float noEmoteSeconds = 1.2f;

        [Tooltip("Results wait for an emote at most this long (real seconds); a longer emote plays on behind them.")]
        [Min(0f)]
        [SerializeField] private float maxEmoteWaitSeconds = 3.5f;

        [Tooltip("Behind the Results the emote comes again after this pause (real seconds).")]
        [Min(0f)]
        [SerializeField] private float emoteRepeatGapSeconds = 2.5f;

        [Tooltip("A tap skips to the Results, but not this early (real seconds): a finger still mashing at the line must not skip what it came for.")]
        [Min(0f)]
        [SerializeField] private float skipLockSeconds = 1.2f;

        [Header("The body's own reaction (when no emote plays)")]
        [Tooltip("How long the reaction lasts before it flows back to standing; the Results wait for it like for an emote (game seconds).")]
        [Min(0.1f)]
        [SerializeField] private float reactionSeconds = 2.4f;

        [Tooltip("How long it takes to flow into the reaction and out of it (game seconds).")]
        [Min(0.01f)]
        [SerializeField] private float reactionBlendSeconds = 0.35f;

        [Tooltip("The winner: upper arm swing (deg, + = forward, 180 = straight up).")]
        [SerializeField] private float cheerArmSwing = 160f;

        [Tooltip("The winner: elbow bend (deg, 0 = straight).")]
        [SerializeField] private float cheerElbow = 35f;

        [Tooltip("The winner: how far the arms pump around that (deg).")]
        [Min(0f)]
        [SerializeField] private float cheerPumpDegrees = 14f;

        [Tooltip("The winner: pumps per second.")]
        [Min(0f)]
        [SerializeField] private float cheerPumpRate = 1.8f;

        [Tooltip("The others: hips bent forward (deg).")]
        [SerializeField] private float windedPelvisPitch = 26f;

        [Tooltip("The others: thigh from straight down (deg, + = forward).")]
        [SerializeField] private float windedThigh = 22f;

        [Tooltip("The others: knee (deg, 180 = straight).")]
        [SerializeField] private float windedKnee = 148f;

        [Tooltip("The others: upper arm swing (deg, + = forward).")]
        [SerializeField] private float windedArmSwing = 24f;

        [Tooltip("The others: elbow bend (deg, 0 = straight).")]
        [SerializeField] private float windedElbow = 10f;

        [Tooltip("The others: how far the hips rise and sink with each breath (deg).")]
        [Min(0f)]
        [SerializeField] private float windedBreathDegrees = 3f;

        [Tooltip("The others: breaths per second.")]
        [Min(0f)]
        [SerializeField] private float windedBreathRate = 0.9f;

        [Header("Camera")]
        [Tooltip("Where the camera ends up, from the runner's feet (m): x = the runner's right, y = up, z = ahead of the runner.")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(2.6f, 1.15f, 3.2f);

        [Tooltip("The height on the runner the camera ends up looking at (m above the feet).")]
        [Min(0f)]
        [SerializeField] private float cameraLookHeight = 1f;

        [Tooltip("Vertical field of view the camera ends up with (deg).")]
        [Range(10f, 90f)]
        [SerializeField] private float cameraFieldOfView = 38f;

        [Tooltip("How long the camera takes to swing from the side to its place (real seconds, from the end of the slow motion).")]
        [Min(0.01f)]
        [SerializeField] private float cameraMoveSeconds = 2.2f;

        [Tooltip("One frame never advances the sequence by more than this (real seconds), so a hitch cannot swallow the slow motion.")]
        [Min(0.01f)]
        [SerializeField] private float maxFrameSeconds = 0.1f;

        public float SlowMotionScale => slowMotionScale;
        public float SlowMotionSeconds => slowMotionSeconds;
        public float SlowMotionReturnSeconds => slowMotionReturnSeconds;
        public float StopSecondsFromTop => stopSecondsFromTop;
        public float StopSecondsFromWalk => stopSecondsFromWalk;
        public float StopCurve => stopCurve;
        public int StepsAfterLine => stepsAfterLine;
        public float LastStepLeadSeconds => lastStepLeadSeconds;
        public float FirstStepGapSeconds => firstStepGapSeconds;
        public float StepGapGrowth => stepGapGrowth;
        public float StepPressSeconds => stepPressSeconds;
        public float StepHeadLimit => stepHeadLimit;
        public float StandFromSpeed => standFromSpeed;
        public float StandFullSpeed => standFullSpeed;
        public float StandSeconds => standSeconds;
        public float StandThigh => standThigh;
        public float StandKnee => standKnee;
        public float StandPelvisPitch => standPelvisPitch;
        public float StandWidth => standWidth;
        public float StandArmSwing => standArmSwing;
        public float StandElbow => standElbow;
        public float EmoteStartSpeed => emoteStartSpeed;
        public float SlowdownMaxSeconds => slowdownMaxSeconds;
        public float NoEmoteSeconds => noEmoteSeconds;
        public float MaxEmoteWaitSeconds => maxEmoteWaitSeconds;
        public float EmoteRepeatGapSeconds => emoteRepeatGapSeconds;
        public float SkipLockSeconds => skipLockSeconds;
        public float ReactionSeconds => reactionSeconds;
        public float ReactionBlendSeconds => reactionBlendSeconds;
        public float CheerArmSwing => cheerArmSwing;
        public float CheerElbow => cheerElbow;
        public float CheerPumpDegrees => cheerPumpDegrees;
        public float CheerPumpRate => cheerPumpRate;
        public float WindedPelvisPitch => windedPelvisPitch;
        public float WindedThigh => windedThigh;
        public float WindedKnee => windedKnee;
        public float WindedArmSwing => windedArmSwing;
        public float WindedElbow => windedElbow;
        public float WindedBreathDegrees => windedBreathDegrees;
        public float WindedBreathRate => windedBreathRate;
        public Vector3 CameraOffset => cameraOffset;
        public float CameraLookHeight => cameraLookHeight;
        public float CameraFieldOfView => cameraFieldOfView;
        public float CameraMoveSeconds => cameraMoveSeconds;
        public float MaxFrameSeconds => maxFrameSeconds;
    }
}
