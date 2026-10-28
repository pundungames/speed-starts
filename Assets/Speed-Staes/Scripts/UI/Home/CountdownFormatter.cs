using System;

namespace SpeedStars.UI.Home
{
    /// <summary>Regen countdown text: <c>mm:ss</c>, with hours <c>h:mm:ss</c>. Whole seconds round up, so it never shows 00:00 while time is left.</summary>
    public static class CountdownFormatter
    {
        public static string Format(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "00:00";

            long totalSeconds = (long)Math.Ceiling(remaining.TotalSeconds);
            long hours = totalSeconds / 3600;
            long minutes = totalSeconds % 3600 / 60;
            long seconds = totalSeconds % 60;

            return hours > 0
                ? $"{hours}:{minutes:00}:{seconds:00}"
                : $"{minutes:00}:{seconds:00}";
        }
    }
}
