using System.Collections.Generic;
using System.Globalization;
using SpeedStars.Core.Race;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Pure texts and rows of the live race order (T-076): the HUD's "3rd" and the Results table with every runner.
    /// No Unity types, so EditMode tests cover it.
    /// </summary>
    public static class RaceStandingsFormat
    {
        /// <summary>"1st", "2nd", "3rd", "4th" ... "11th", "12th", "13th", "21st"; a place below 1 reads as 1st.</summary>
        public static string Ordinal(int place)
        {
            if (place < 1) place = 1;
            int lastTwo = place % 100;
            string suffix;
            if (lastTwo >= 11 && lastTwo <= 13) suffix = "th";
            else
            {
                switch (place % 10)
                {
                    case 1: suffix = "st"; break;
                    case 2: suffix = "nd"; break;
                    case 3: suffix = "rd"; break;
                    default: suffix = "th"; break;
                }
            }
            return place.ToString(CultureInfo.InvariantCulture) + suffix;
        }

        /// <summary>The HUD text; empty (hidden) when the player runs alone or the rank is unknown.</summary>
        public static string PlaceText(int rank, int runnerCount)
        {
            if (runnerCount <= 1 || rank < 1) return string.Empty;
            return Ordinal(rank);
        }

        /// <summary>Race time of a finished runner, a dash while still running.</summary>
        public static string TimeText(RaceEntry entry) =>
            entry.Finished ? ResultsTexts.Time(entry.TimeSeconds) : ResultsTexts.NotFinished;

        /// <summary>
        /// Table rows for every runner in the order the standings give. The player's own row keeps what the results model
        /// worked out (name, final time, PB / DB badges); a cup goes only to a finished runner on places 1 to 3.
        /// </summary>
        public static List<ResultRowData> Rows(IReadOnlyList<RaceEntry> entries, ResultRowData playerRow)
        {
            var rows = new List<ResultRowData>(entries != null ? entries.Count : 0);
            if (entries == null) return rows;

            for (int i = 0; i < entries.Count; i++)
            {
                RaceEntry entry = entries[i];
                int rank = i + 1;
                if (entry.IsPlayer)
                {
                    rows.Add(new ResultRowData(rank, playerRow.Name, playerRow.TimeText, true, ResultsModel.TrophyFor(rank),
                        playerRow.ShowPersonalBest, playerRow.ShowDayBest, entry.CountryCode));
                    continue;
                }
                rows.Add(new ResultRowData(rank, entry.Name, TimeText(entry), false,
                    entry.Finished ? ResultsModel.TrophyFor(rank) : ResultTrophy.None, false, false, entry.CountryCode));
            }
            return rows;
        }
    }
}
