namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Where the UI font lives (T-100): the American Captain TTF, the TMP font asset made from it and the outline material
    /// presets. One list so the migration, the builders and the tests never disagree about a path.
    /// </summary>
    public static class UiFontPaths
    {
        public const string FontFolder = UiPrefabPaths.Root + "/Fonts/AmericanCaptain";

        /// <summary>The TTF (added by hand with its licence texts next to it).</summary>
        public const string AmericanCaptainTtf = FontFolder + "/AmericanCaptain.ttf";

        /// <summary>The dynamic TMP font asset; it is also the TMP Settings default font.</summary>
        public const string AmericanCaptainSdf = FontFolder + "/AmericanCaptain SDF.asset";

        /// <summary>Outline material presets (one per distinct colour + width).</summary>
        public const string PresetFolder = FontFolder + "/Presets";

        public const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>SDF sampling size and padding of the font asset; the outline width maths (OutlineSpec.WidthPerEm) assumes 90 / 9.</summary>
        public const int SamplingPointSize = 90;
        public const int AtlasPadding = 9;
        public const int AtlasSize = 1024;
    }
}
