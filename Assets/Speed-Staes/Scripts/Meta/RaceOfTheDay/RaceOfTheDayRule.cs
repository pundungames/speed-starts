using System;
using System.Globalization;

namespace SpeedStars.Meta.DailyRace
{
    /// <summary>
    /// The deterministic part of Race of the Day: which list entry a UTC day picks. Pure (no clock, no Unity), so the same
    /// day key gives the same answer on every launch and device. Day by day the pick walks a seeded shuffle of the list, so
    /// every entry comes round once per cycle and two neighbouring days never pick the same entry (with two or more entries).
    /// </summary>
    public static class RaceOfTheDayRule
    {
        /// <summary>Salt of the distance list; the look list uses another, so the two picks do not move together.</summary>
        public const int DistanceSalt = 1;
        public const int LookSalt = 2;

        /// <summary>Days since 0001-01-01 of a UTC instant (the same day boundary the leaderboard uses).</summary>
        public static long DayKey(DateTime utcNow) => utcNow.Date.Ticks / TimeSpan.TicksPerDay;

        /// <summary>The list index of the day; -1 for an empty list.</summary>
        public static int IndexFor(long dayKey, int count, int salt)
        {
            if (count <= 0) return -1;
            if (count == 1) return 0;
            if (count == 2) return (int)Mod(dayKey + salt, 2);

            long cycle = FloorDiv(dayKey, count);
            int position = (int)Mod(dayKey, count);
            int[] order = Raw(cycle, count, salt);
            // The first day of a cycle must differ from the last day of the previous one. Only slot 0 may move
            // (count >= 3, so the last slot, which the next cycle compares against, is never touched).
            int previousLast = Raw(cycle - 1, count, salt)[count - 1];
            if (order[0] == previousLast)
            {
                int swapped = order[0];
                order[0] = order[1];
                order[1] = swapped;
            }
            return order[position];
        }

        /// <summary>A seeded Fisher-Yates shuffle of 0..count-1 (own mixer, so no runtime's Random decides it).</summary>
        private static int[] Raw(long cycle, int count, int salt)
        {
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            ulong state = Mix(unchecked((ulong)cycle * 0x9E3779B97F4A7C15UL + (ulong)salt * 0xD1B54A32D192ED03UL));
            for (int i = count - 1; i > 0; i--)
            {
                state = Mix(state);
                int j = (int)(state % (ulong)(i + 1));
                int held = order[i];
                order[i] = order[j];
                order[j] = held;
            }
            return order;
        }

        private static ulong Mix(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        private static long Mod(long a, long m) => ((a % m) + m) % m;

        private static long FloorDiv(long a, long m) => (a - Mod(a, m)) / m;

        /// <summary>"race_of_the_day:150m": the store id of a distance's times.</summary>
        public static string RecordId(string eventId, float distanceMeters) =>
            (eventId ?? string.Empty) + ":" + ((int)Math.Round(Math.Max(0f, distanceMeters))).ToString(CultureInfo.InvariantCulture) + "m";
    }
}
