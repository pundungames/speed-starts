using System;
using System.Globalization;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Pure text and fraction rules of the race HUD and Results (no Unity types, so EditMode tests cover them).
    /// Stopwatch style: centiseconds are truncated, never rounded up, so a displayed time is never better than the real one.
    /// </summary>
    public static class RaceDisplayFormat
    {
        private const int SecondsPerMinute = 60;

        /// <summary>Whole centiseconds of a time; negative, NaN and infinity count as 0.</summary>
        public static long ToCentiseconds(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d) return 0L;
            return (long)Math.Floor(seconds * 100d + 1e-9);
        }

        /// <summary>"ss.cc" ("9.58"), or "m:ss.cc" ("1:02.05") from one minute on.</summary>
        public static string Time(double seconds) => Time(ToCentiseconds(seconds));

        public static string Time(long centiseconds)
        {
            if (centiseconds < 0L) centiseconds = 0L;
            long totalSeconds = centiseconds / 100L;
            long cc = centiseconds % 100L;
            long minutes = totalSeconds / SecondsPerMinute;
            long seconds = totalSeconds % SecondsPerMinute;
            return minutes > 0L
                ? string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2:00}", minutes, seconds, cc)
                : string.Format(CultureInfo.InvariantCulture, "{0}.{1:00}", seconds, cc);
        }

        /// <summary>Distance / target clamped to 0..1; no target (or a bad number) gives 0.</summary>
        public static float Progress(float distance, float target)
        {
            if (float.IsNaN(distance) || float.IsNaN(target) || target <= 0f || distance <= 0f) return 0f;
            return distance >= target ? 1f : distance / target;
        }

        /// <summary>Whole meters run, never above the target ("37 m").</summary>
        public static int WholeMeters(float distance, float target)
        {
            if (float.IsNaN(distance) || distance <= 0f) return 0;
            float capped = target > 0f && distance > target ? target : distance;
            return (int)Math.Floor(capped);
        }

        public static string Meters(int meters) => meters.ToString(CultureInfo.InvariantCulture) + " m";
    }
}
