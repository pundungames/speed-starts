using System.Globalization;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Settings
{
    /// <summary>
    /// Pure name rules: spaces around the name are cut, then it must be min..max characters (a surrogate pair counts as
    /// one, so a rare Chinese character is one character) made of letters, digits (any script), marks that belong to
    /// letters, spaces, '_' and '-'. Control characters are never allowed. The limits come from the settings config.
    /// </summary>
    public static class PlayerNameRules
    {
        public static string Normalize(string raw) => raw == null ? string.Empty : raw.Trim();

        public static PlayerNameResult Validate(string raw, int minLength, int maxLength)
        {
            string name = Normalize(raw);
            int count = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (!IsAllowedAt(name, i)) return PlayerNameResult.InvalidCharacters;
                if (char.IsHighSurrogate(name[i])) i++; // second half of the pair
                count++;
            }
            if (count < minLength) return PlayerNameResult.TooShort;
            if (count > maxLength) return PlayerNameResult.TooLong;
            return PlayerNameResult.Valid;
        }

        private static bool IsAllowedAt(string text, int index)
        {
            char c = text[index];
            if (c == ' ' || c == '_' || c == '-') return true;
            if (char.IsLetterOrDigit(text, index)) return true;
            if (char.IsSurrogate(c)) return false; // a pair that is not a letter or digit (emoji, symbols)
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark;
        }
    }
}
