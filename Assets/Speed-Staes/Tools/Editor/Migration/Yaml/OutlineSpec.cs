using System;
using System.Globalization;

namespace SpeedStars.Tools.Migration
{
    /// <summary>
    /// One TMP outline look (colour + width) that replaces a legacy Outline / ThickOutline. Width is quantised so that
    /// near-equal outlines share one material preset instead of producing dozens of them.
    /// </summary>
    public readonly struct OutlineSpec : IEquatable<OutlineSpec>
    {
        /// <summary>
        /// TMP outline width (0..1) is a fraction of the SDF padding: with sampling 90 and padding 9 the full range is 0.1 em,
        /// so width = thickness(px) / fontSize * 10. Kept here so the converter and the font asset creation agree.
        /// </summary>
        public const float WidthPerEm = 10f;

        public const float MinWidth = 0.05f;

        /// <summary>Beyond this the outline reaches the SDF padding and gets clipped.</summary>
        public const float MaxWidth = 0.8f;

        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;
        public readonly float Width;

        public OutlineSpec(byte r, byte g, byte b, byte a, float width)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            Width = width;
        }

        /// <summary>Builds a spec from a legacy outline: its colour (0..1), its reach in px and the font size it sits on.</summary>
        public static OutlineSpec FromLegacy(double r, double g, double b, double a, double distancePx, double fontSize)
        {
            double size = fontSize <= 0.0 ? 1.0 : fontSize;
            double raw = distancePx / size * WidthPerEm;
            double stepped = Math.Round(raw * 20.0) / 20.0;
            float width = (float)Math.Min(MaxWidth, Math.Max(MinWidth, stepped));
            return new OutlineSpec(ToByte(r), ToByte(g), ToByte(b), ToByte(a), width);
        }

        /// <summary>Stable id: used in the material preset name, so a re-run finds the preset it made before.</summary>
        public string Key =>
            string.Format(CultureInfo.InvariantCulture, "Outline {0:X2}{1:X2}{2:X2}{3:X2} w{4:00}", R, G, B, A, (int)Math.Round(Width * 100f));

        public bool Equals(OutlineSpec other) => Key == other.Key;
        public override bool Equals(object obj) => obj is OutlineSpec other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();

        private static byte ToByte(double v) => (byte)Math.Round(Math.Min(1.0, Math.Max(0.0, v)) * 255.0);
    }
}
