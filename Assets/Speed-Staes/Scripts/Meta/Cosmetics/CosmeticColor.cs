using System;
using UnityEngine;

namespace SpeedStars.Meta.Cosmetics
{
    /// <summary>Colour text as the save and the catalog write it: six hex digits, upper case, no '#'.</summary>
    public static class CosmeticColor
    {
        /// <summary>"RRGGBB" (upper case) for "#rrggbb" / "RRGGBB"; empty when the text is not a colour.</summary>
        public static string Normalize(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return string.Empty;
            string text = hex.Trim();
            if (text.StartsWith("#")) text = text.Substring(1);
            if (text.Length != 6) return string.Empty;
            foreach (char c in text)
                if (!Uri.IsHexDigit(c)) return string.Empty;
            return text.ToUpperInvariant();
        }

        public static bool TryParse(string hex, out Color color)
        {
            string normalized = Normalize(hex);
            if (normalized.Length == 0)
            {
                color = Color.white;
                return false;
            }
            byte r = Convert.ToByte(normalized.Substring(0, 2), 16);
            byte g = Convert.ToByte(normalized.Substring(2, 2), 16);
            byte b = Convert.ToByte(normalized.Substring(4, 2), 16);
            color = new Color32(r, g, b, 255);
            return true;
        }

        public static string ToHex(Color color) => ColorUtility.ToHtmlStringRGB(color);
    }
}
