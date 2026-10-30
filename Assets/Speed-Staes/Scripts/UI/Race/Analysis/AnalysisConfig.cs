using UnityEngine;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Numbers of the Analysis screen in one asset (Data/Config/AnalysisConfig): how densely the race is sampled, how the
    /// axes are rounded, how the lines look, how the cursor balloon behaves. The field defaults are the first values
    /// (AnalysisConfigSeed writes the asset once); at runtime the asset counts.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalysisConfig", menuName = "Speed Stars/Config/Analysis")]
    public sealed class AnalysisConfig : ScriptableObject
    {
        [Header("Recording")]
        [Tooltip("A split is kept every this many metres of the race (1 = one point per metre; 100 m gives 101 points).")]
        [Min(0.05f)]
        [SerializeField] private float splitStepMeters = 1f;

        [Tooltip("A race counts as finished when the last sampled distance is within this many metres of the event distance (frames are discrete).")]
        [Min(0f)]
        [SerializeField] private float finishToleranceMeters = 1f;

        [Header("Axes")]
        [Tooltip("About how many tick intervals each axis gets; the step is rounded to the nearest nice value at or above distance/this.")]
        [Min(2)]
        [SerializeField] private int targetTickCount = 5;

        [Tooltip("Nice multipliers of a power of ten for axis steps (1, 2, 2.5, 5, 10): ascending, ending with 10.")]
        [SerializeField] private float[] tickMultipliers = { 1f, 2f, 2.5f, 5f, 10f };

        [Header("Lines")]
        [Tooltip("Line colours by runner slot (teal, orange, violet, blue, ochre, slate from the art's layout.json). A runner keeps its colour whatever its place.")]
        [SerializeField] private Color[] lineColors =
        {
            new Color(0f, 0.714f, 0.690f, 1f),
            new Color(1f, 0.455f, 0.09f, 1f),
            new Color(0.549f, 0.224f, 0.914f, 1f),
            new Color(0.086f, 0.522f, 0.937f, 1f),
            new Color(0.82f, 0.627f, 0f, 1f),
            new Color(0.294f, 0.376f, 0.439f, 1f),
        };

        [Tooltip("Line width (canvas pixels) of a runner that is not selected.")]
        [Min(0.5f)]
        [SerializeField] private float lineWidth = 4f;

        [Tooltip("Line width (canvas pixels) of the selected runner.")]
        [Min(0.5f)]
        [SerializeField] private float selectedLineWidth = 7f;

        [Tooltip("Radius (canvas pixels) of the dots on the line.")]
        [Min(0f)]
        [SerializeField] private float pointRadius = 5f;

        [Tooltip("A dot is drawn at most this often along the line, in canvas pixels, so a 1 m step does not make a bead chain.")]
        [Min(1f)]
        [SerializeField] private float pointSpacing = 36f;

        [Tooltip("Grid line colour.")]
        [SerializeField] private Color gridColor = new Color(0.78f, 0.80f, 0.84f, 1f);

        [Tooltip("Grid line width (canvas pixels).")]
        [Min(0.5f)]
        [SerializeField] private float gridWidth = 2f;

        [Header("Motion")]
        [Tooltip("The line draws left to right over this many times the screen-open duration of UiMotionConfig.")]
        [Min(0f)]
        [SerializeField] private float lineDrawDurationFactor = 3f;

        [Header("Cursor balloon")]
        [Tooltip("The balloon stays this far (art pixels of the 1038x502 chart canvas) inside the chart's edges.")]
        [Min(0f)]
        [SerializeField] private float balloonEdgeMargin = 24f;

        public float SplitStepMeters => splitStepMeters;
        public float FinishToleranceMeters => finishToleranceMeters;
        public int TargetTickCount => targetTickCount;
        public float[] TickMultipliers => tickMultipliers;
        public float LineWidth => lineWidth;
        public float SelectedLineWidth => selectedLineWidth;
        public float PointRadius => pointRadius;
        public float PointSpacing => pointSpacing;
        public Color GridColor => gridColor;
        public float GridWidth => gridWidth;
        public float LineDrawDurationFactor => lineDrawDurationFactor;
        public float BalloonEdgeMargin => balloonEdgeMargin;

        /// <summary>Colour of a runner slot; slots past the palette wrap around.</summary>
        public Color LineColor(int slot)
        {
            if (lineColors == null || lineColors.Length == 0) return Color.white;
            int index = slot % lineColors.Length;
            if (index < 0) index += lineColors.Length;
            return lineColors[index];
        }

        public int PaletteSize => lineColors == null ? 0 : lineColors.Length;
    }
}
