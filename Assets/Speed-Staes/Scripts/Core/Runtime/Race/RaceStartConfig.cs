using UnityEngine;

namespace SpeedStars.Core.Race
{
    /// <summary>
    /// Numbers of the race start in one asset (Data/Config/RaceStartConfig): how long READY and SET last and how the READY / SET / GO!
    /// words move (Yaris_Baslangic_UI_v1, IMPLEMENTATION.md). The countdown numbers are first values, to be tuned by feel; the motion
    /// numbers are the art's. The field defaults are what RaceStartConfigSeed writes once; at runtime the asset counts.
    /// </summary>
    [CreateAssetMenu(fileName = "RaceStartConfig", menuName = "Speed Stars/Config/Race Start")]
    public sealed class RaceStartConfig : ScriptableObject
    {
        [Header("Countdown (the session's clock)")]
        [Tooltip("How long READY shows before SET (seconds).")]
        [Min(0.05f)]
        [SerializeField] private float readySeconds = 0.8f;

        [Tooltip("Shortest SET wait before GO (seconds). The wait is random between this and the longest, so GO cannot be timed.")]
        [Min(0f)]
        [SerializeField] private float setWaitMinSeconds = 0.6f;

        [Tooltip("Longest SET wait before GO (seconds). Below the shortest it is swapped with it.")]
        [Min(0f)]
        [SerializeField] private float setWaitMaxSeconds = 1.4f;

        [Tooltip("One frame never advances the countdown by more than this (seconds), so a loading hitch cannot swallow READY and SET.")]
        [Min(0.01f)]
        [SerializeField] private float maxFrameSeconds = 0.1f;

        [Header("READY")]
        [Tooltip("READY slides in from above and settles (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float readyEnterSeconds = 0.12f;

        [Tooltip("How far above its place READY starts (UI units, before the group scale).")]
        [SerializeField] private float readyEnterOffsetY = 108f;

        [Tooltip("The impact squash and rebound after READY lands (seconds). The impact lines fade over the same time.")]
        [Min(0.01f)]
        [SerializeField] private float reboundSeconds = 0.10f;

        [Tooltip("Squash of the word at the start of the rebound (x wide, y flat); eases back to 1,1.")]
        [SerializeField] private Vector2 reboundScale = new Vector2(1.12f, 0.76f);

        [Tooltip("How high the word hops during the rebound (UI units).")]
        [SerializeField] private float reboundLiftY = 12f;

        [Header("SET")]
        [Tooltip("The word is replaced and squeezed to the SET scale (seconds). The tension lines fade in over the same time.")]
        [Min(0.01f)]
        [SerializeField] private float setCompressSeconds = 0.12f;

        [Tooltip("Squeeze of the SET word (x wide, y flat).")]
        [SerializeField] private Vector2 setScale = new Vector2(1.12f, 0.78f);

        [Tooltip("While SET waits, the tension lines pulse between this alpha and 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float holdTensionMinAlpha = 0.6f;

        [Tooltip("Width of the small horizontal shake while SET waits (UI units; at most 2 per the art's notes).")]
        [Range(0f, 2f)]
        [SerializeField] private float holdShakeAmplitude = 2f;

        [Tooltip("How long one shake lasts (two quick swings, seconds).")]
        [Min(0.02f)]
        [SerializeField] private float holdShakeSeconds = 0.12f;

        [Tooltip("The shake and the tension pulse repeat at this period (seconds).")]
        [Min(0.1f)]
        [SerializeField] private float holdCycleSeconds = 0.5f;

        [Header("GO!")]
        [Tooltip("GO! grows and fades out over this time (seconds); the speed lines burst out over the same time.")]
        [Min(0.05f)]
        [SerializeField] private float goSeconds = 0.30f;

        [Tooltip("GO! stays fully opaque for this long, then fades for the rest of the time (seconds).")]
        [Min(0f)]
        [SerializeField] private float goOpaqueSeconds = 0.105f;

        [Tooltip("GO! scale at the first frame.")]
        [Min(0.01f)]
        [SerializeField] private float goScaleFrom = 0.75f;

        [Tooltip("GO! scale at the last frame (a fast pass, it must not cover the screen for long).")]
        [Min(0.01f)]
        [SerializeField] private float goScaleTo = 1.75f;

        [Tooltip("The speed lines' scale at the last frame (they start at 1).")]
        [Min(0.01f)]
        [SerializeField] private float releaseScaleTo = 1.35f;

        [Header("Look")]
        [Tooltip("Scale of the whole group (words and lines). The art's own canvas is 1024x576.")]
        [Min(0.05f)]
        [SerializeField] private float groupScale = 0.55f;

        [Tooltip("READY and SET fill colour (#FFFCEF).")]
        [SerializeField] private Color wordColor = new Color(1f, 0.988f, 0.937f, 1f);

        [Tooltip("GO! fill colour (#FF202D).")]
        [SerializeField] private Color goColor = new Color(1f, 0.125f, 0.176f, 1f);

        [Tooltip("Outline colour of all three words (#061A41).")]
        [SerializeField] private Color outlineColor = new Color(0.024f, 0.102f, 0.255f, 1f);

        [Header("Reduced motion")]
        [Tooltip("On: no slide, squash, shake or lines; words only fade in. The words and the start moment stay as they are.")]
        [SerializeField] private bool reduceMotion;

        [Tooltip("Fade-in of a word while reduced motion is on (seconds).")]
        [Min(0.01f)]
        [SerializeField] private float reducedFadeSeconds = 0.08f;

        public float ReadySeconds => readySeconds;
        public float SetWaitMinSeconds => setWaitMinSeconds;
        public float SetWaitMaxSeconds => setWaitMaxSeconds;
        public float MaxFrameSeconds => maxFrameSeconds;
        public float ReadyEnterSeconds => readyEnterSeconds;
        public float ReadyEnterOffsetY => readyEnterOffsetY;
        public float ReboundSeconds => reboundSeconds;
        public Vector2 ReboundScale => reboundScale;
        public float ReboundLiftY => reboundLiftY;
        public float SetCompressSeconds => setCompressSeconds;
        public Vector2 SetScale => setScale;
        public float HoldTensionMinAlpha => holdTensionMinAlpha;
        public float HoldShakeAmplitude => holdShakeAmplitude;
        public float HoldShakeSeconds => holdShakeSeconds;
        public float HoldCycleSeconds => holdCycleSeconds;
        public float GoSeconds => goSeconds;
        public float GoOpaqueSeconds => goOpaqueSeconds;
        public float GoScaleFrom => goScaleFrom;
        public float GoScaleTo => goScaleTo;
        public float ReleaseScaleTo => releaseScaleTo;
        public float GroupScale => groupScale;
        public Color WordColor => wordColor;
        public Color GoColor => goColor;
        public Color OutlineColor => outlineColor;
        public bool ReduceMotion => reduceMotion;
        public float ReducedFadeSeconds => reducedFadeSeconds;
    }
}
