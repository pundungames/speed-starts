using System.Globalization;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Leaderboard
{
    /// <summary>
    /// The seed id of Race of the Day's bot field: event id plus UTC day key. The result screen's Daily World Record and the
    /// High Scores list both generate the field from this id and the day's distance, so the list's best bot is that record.
    /// </summary>
    public static class DailyBotField
    {
        public static RaceEventId FieldId(string eventId, long dayKey) =>
            new RaceEventId(eventId + ":" + dayKey.ToString(CultureInfo.InvariantCulture));
    }
}
