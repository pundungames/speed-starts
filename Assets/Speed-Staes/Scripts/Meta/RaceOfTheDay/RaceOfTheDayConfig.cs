using System.Collections.Generic;
using UnityEngine;

namespace SpeedStars.Meta.DailyRace
{
    /// <summary>
    /// The lists Race of the Day draws from (T-087): the distances and the racer looks. The rule that picks from them is
    /// <see cref="RaceOfTheDayRule"/>; the numbers live here, in the asset (the seeder only fills it the first time).
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Race of the Day", fileName = "RaceOfTheDayConfig")]
    public sealed class RaceOfTheDayConfig : ScriptableObject
    {
        [Tooltip("Id of the event in the Race Event Catalog.")]
        [SerializeField] private string eventId = "race_of_the_day";

        [Tooltip("Race lengths (m) one day can pick, flat races only. Each comes round once per cycle in a shuffled order.")]
        [SerializeField] private float[] distancesMeters = new float[0];

        [Tooltip("Ready-made racers one day can pick; everybody in that day's race wears it.")]
        [SerializeField] private RaceOfTheDayLook[] looks = new RaceOfTheDayLook[0];

        public string EventId => string.IsNullOrEmpty(eventId) ? "race_of_the_day" : eventId;

        /// <summary>The usable (positive) distances in list order.</summary>
        public IReadOnlyList<float> Distances
        {
            get
            {
                var list = new List<float>();
                if (distancesMeters == null) return list;
                foreach (float meters in distancesMeters)
                    if (meters > 0f && !float.IsNaN(meters) && !float.IsInfinity(meters)) list.Add(meters);
                return list;
            }
        }

        /// <summary>The looks in list order (empty slots dropped).</summary>
        public IReadOnlyList<RaceOfTheDayLook> Looks
        {
            get
            {
                var list = new List<RaceOfTheDayLook>();
                if (looks == null) return list;
                foreach (RaceOfTheDayLook look in looks)
                    if (look != null) list.Add(look);
                return list;
            }
        }
    }
}
