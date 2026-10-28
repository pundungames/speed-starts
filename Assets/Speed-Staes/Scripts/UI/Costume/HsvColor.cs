using System;

namespace SpeedStars.UI.Costume
{
    /// <summary>
    /// Hue / saturation / value of the colour picker, each 0..1 (hue 1 = hue 0, the circle closes). Pure maths, no Unity
    /// types: the picker view converts to and from the "RRGGBB" text the outfit stores through this struct.
    /// </summary>
    public readonly struct HsvColor : IEquatable<HsvColor>
    {
        private const int HexLength = 6;
        private const float Epsilon = 0.0001f;
        private const float ByteMax = 255f;
        private const float SectorCount = 6f;

        public HsvColor(float hue, float saturation, float value)
        {
            Hue = Clamp01(hue);
            Saturation = Clamp01(saturation);
            Value = Clamp01(value);
        }

        public float Hue { get; }
        public float Saturation { get; }
        public float Value { get; }

        public HsvColor WithHue(float hue) => new HsvColor(hue, Saturation, Value);

        public HsvColor WithSaturationValue(float saturation, float value) => new HsvColor(Hue, saturation, value);

        /// <summary>The fully saturated, full-brightness colour of this hue: the flat base under the picker's two overlays.</summary>
        public void HueToRgb(out float r, out float g, out float b) => new HsvColor(Hue, 1f, 1f).ToRgb(out r, out g, out b);

        public void ToRgb(out float r, out float g, out float b)
        {
            float scaled = (Hue >= 1f ? 0f : Hue) * SectorCount;
            int sector = (int)scaled;
            float fraction = scaled - sector;
            float p = Value * (1f - Saturation);
            float q = Value * (1f - Saturation * fraction);
            float t = Value * (1f - Saturation * (1f - fraction));
            switch (sector)
            {
                case 0: r = Value; g = t; b = p; break;
                case 1: r = q; g = Value; b = p; break;
                case 2: r = p; g = Value; b = t; break;
                case 3: r = p; g = q; b = Value; break;
                case 4: r = t; g = p; b = Value; break;
                default: r = Value; g = p; b = q; break;
            }
        }

        /// <summary>"RRGGBB", upper case, no '#': the format of the outfit slot's colour.</summary>
        public string ToHex()
        {
            ToRgb(out float r, out float g, out float b);
            return ToByte(r).ToString("X2") + ToByte(g).ToString("X2") + ToByte(b).ToString("X2");
        }

        /// <summary>
        /// Colour to picker values. A grey or black colour has no hue of its own, so <paramref name="fallbackHue"/> keeps the
        /// hue strip where the player left it instead of snapping to red.
        /// </summary>
        public static HsvColor FromRgb(float r, float g, float b, float fallbackHue)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;
            float value = max;
            float saturation = max <= Epsilon ? 0f : delta / max;
            if (delta <= Epsilon || saturation <= Epsilon) return new HsvColor(fallbackHue, saturation, value);

            float hue;
            if (max == r) hue = ((g - b) / delta) % SectorCount;
            else if (max == g) hue = (b - r) / delta + 2f;
            else hue = (r - g) / delta + 4f;
            hue /= SectorCount;
            if (hue < 0f) hue += 1f;
            return new HsvColor(hue, saturation, value);
        }

        /// <summary>False for text that is not six hex digits (an optional leading '#' is allowed).</summary>
        public static bool TryFromHex(string hex, float fallbackHue, out HsvColor color)
        {
            color = new HsvColor(fallbackHue, 0f, 1f);
            if (string.IsNullOrEmpty(hex)) return false;

            string text = hex.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal)) text = text.Substring(1);
            if (text.Length != HexLength) return false;

            int r, g, b;
            if (!TryByte(text, 0, out r) || !TryByte(text, 2, out g) || !TryByte(text, 4, out b)) return false;
            color = FromRgb(r / ByteMax, g / ByteMax, b / ByteMax, fallbackHue);
            return true;
        }

        public bool Equals(HsvColor other) => Hue == other.Hue && Saturation == other.Saturation && Value == other.Value;

        public override bool Equals(object obj) => obj is HsvColor other && Equals(other);

        public override int GetHashCode() => (Hue.GetHashCode() * 31 + Saturation.GetHashCode()) * 31 + Value.GetHashCode();

        private static bool TryByte(string text, int start, out int value)
        {
            return int.TryParse(text.Substring(start, 2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static int ToByte(float channel) => (int)Math.Round(Clamp01(channel) * ByteMax);

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
