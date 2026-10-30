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
        [Tooltip("Size of the speed lines (and the pose offsets) against the art's own 1024x576 canvas (1 = the art as drawn). The words themselves follow Banner Scale. Read when the prefab is built (rebuild the RaceStart prefab after a change).")]
        [Min(0.05f)]
        [SerializeField] private float groupScale = 1.7f;

        [Tooltip("Where the words' centre sits, as a share of the screen height measured from the top (0.38 = the runner's head height, clear of the HUD's top row). Horizontally always the screen centre. Read when the prefab is built.")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float groupCenterFromTopShare = 0.38f;

        [Tooltip("READY and SET fill colour: navy (#061A41) on the cream banner (T-064). Replaces the old cream Word Color, which an existing asset still holds but nothing reads.")]
        [SerializeField] private Color readySetColor = new Color(0.024f, 0.102f, 0.255f, 1f);

        [Tooltip("GO! fill colour (#FF202D).")]
        [SerializeField] private Color goColor = new Color(1f, 0.125f, 0.176f, 1f);

        [Tooltip("Outline colour of the words that have one (#061A41).")]
        [SerializeField] private Color outlineColor = new Color(0.024f, 0.102f, 0.255f, 1f);

        [Tooltip("Font size of the words (Kanit Black Italic) in the banner's own 864x184 units; the real size is this x Banner Scale, so READY (~3.6 em wide) fills ~90% of the banner's 540-unit text area. Read when the prefab is built (rebuild the RaceStart prefab after a change).")]
        [Min(20f)]
        [SerializeField] private float bannerWordFontSize = 132f;

        [Tooltip("Outline thickness as a share of the font size (the art's preview: 7 px on a 110 px word = 0.065).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float outlineThicknessRatio = 0.065f;

        [Tooltip("How many copies go around the word for the outline. 12-16 stays smooth when thick; the view applies it to the prefab's ThickOutline.")]
        [Range(4, 32)]
        [SerializeField] private int outlineDirections = 16;

        [Tooltip("While a word fades out its outline fades faster: the outline's share of the visible outline = word alpha ^ this. Without it the 16 stacked outline copies stay almost opaque and turn a fading GO! into a dark blue blot. 1 = same as the word; higher = the outline goes first.")]
        [Min(1f)]
        [SerializeField] private float outlineFadePower = 3f;

        [Header("Banner (T-064)")]
        [Tooltip("Size of the cream banner (864x184 art) as a multiplier: 1.39 makes it ~50% of a 2400-wide screen. The flag and the word's text area scale with it. Read when the prefab is built.")]
        [Min(0.1f)]
        [SerializeField] private float bannerScale = 1.39f;

        [Tooltip("Colour of the full-screen dim behind the banner (the art's 6,26,65,135). A flat colour on an Image, no texture. Read when the prefab is built.")]
        [SerializeField] private Color dimColor = new Color(6f / 255f, 26f / 255f, 65f / 255f, 135f / 255f);

        [Tooltip("Off: READY and SET are plain navy on the cream banner (as approved); only GO! (red) keeps the navy outline.")]
        [SerializeField] private bool readySetOutline;

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
        public float GroupCenterFromTopShare => groupCenterFromTopShare;
        public float OutlineFadePower => outlineFadePower;
        public Color ReadySetColor => readySetColor;
        public Color GoColor => goColor;
        public Color OutlineColor => outlineColor;
        public float BannerWordFontSize => bannerWordFontSize;
        public float BannerScale => bannerScale;
        public Color DimColor => dimColor;
        public bool ReadySetOutline => readySetOutline;
        public float OutlineThicknessRatio => outlineThicknessRatio;
        public int OutlineDirections => outlineDirections;
        public bool ReduceMotion => reduceMotion;
        public float ReducedFadeSeconds => reducedFadeSeconds;
    }
}
