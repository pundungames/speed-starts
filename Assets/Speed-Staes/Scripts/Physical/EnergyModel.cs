using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// Every number of the runner's energy, in one place (an <see cref="EnergyTuning"/> asset holds one), so acceptance tests
    /// change and check them from one file. Energy sets the runner's capacity: the step tempo it can keep, the cadence limit
    /// (presses per second) and the top speed all scale with it, and the best rhythm is <see cref="optimalShare"/> of that
    /// limit. Pressing at the best rhythm uses energy the faster the fuller it is (approaching a steady floor as it empties);
    /// pressing faster than the best rhythm costs extra; pressing slower (or standing) gives some back, more when fuller.
    /// No thresholds: everything is a smooth function of the energy share (0..1). Pure C#.
    /// </summary>
    [System.Serializable]
    public sealed class EnergyModel
    {
        [Header("Energy")]
        [Tooltip("Full energy, at the start of a run.")]
        [Min(1f)] public float energyMax = 100f;

        [Header("Capacity (what the energy allows)")]
        [Tooltip("Capacity at empty energy, as a share of full: the step tempo, the cadence limit and the top speed all drop to this. 0.6 = an empty runner keeps 60% of its tempo and top speed.")]
        [Range(0.1f, 1f)] public float capacityAtEmpty = 0.6f;
        [Tooltip("Shape of capacity over energy: capacity = lerp(capacityAtEmpty, 1, energy share ^ this). 1 = straight; above 1 the runner keeps more of its capacity until late, then fades.")]
        [Range(0.2f, 4f)] public float capacityPower = 1f;
        [Tooltip("The best rhythm as a share of the cadence limit: pressing in turn at this share of the most presses per second the runner can make right now gives the best run (each press lands on time).")]
        [Range(0.1f, 1f)] public float optimalShare = 0.5f;

        [Header("Use (at the best rhythm)")]
        [Tooltip("Energy used per second when pressing at the best rhythm with full energy.")]
        [Min(0f)] public float drainAtFull = 1.2f;
        [Tooltip("Energy used per second at the best rhythm as the energy runs out: the use falls from drainAtFull toward this steady level.")]
        [Min(0f)] public float drainAtEmpty = 0.5f;
        [Tooltip("Shape of that fall: use = lerp(drainAtEmpty, drainAtFull, energy share ^ this). 1 = straight.")]
        [Range(0.2f, 4f)] public float drainPower = 1f;
        [Tooltip("Extra use of a press that comes faster than the best rhythm: × (1 + this × how much faster), up to 3 times as fast. 0 = mashing costs no more per press.")]
        [Min(0f)] public float overPaceCost = 1.5f;
        [Tooltip("Energy used per second while a key is held (one key; both = twice).")]
        [Min(0f)] public float holdDrainPerSecond = 2f;

        [Header("Recovery (slower than the best rhythm, or standing)")]
        [Tooltip("Energy recovered per second standing still with (nearly) full energy. Pressing below the best rhythm recovers the standing rate × how far below it (half the rhythm = half the rate).")]
        [Min(0f)] public float regenAtFull = 4f;
        [Tooltip("Energy recovered per second standing still with empty energy: recovery slows as the energy runs out.")]
        [Min(0f)] public float regenAtEmpty = 1.5f;

        /// <summary>0..1 share of full energy.</summary>
        public float Share(float energy) => Mathf.Clamp01(energy / Mathf.Max(1f, energyMax));

        /// <summary>What the energy allows (capacityAtEmpty..1): step tempo, cadence limit and top speed scale with it.</summary>
        public float Capacity(float share) => Mathf.Lerp(capacityAtEmpty, 1f, Mathf.Pow(Mathf.Clamp01(share), capacityPower));

        /// <summary>Energy used per second at the best rhythm.</summary>
        public float DrainPerSecond(float share) => Mathf.Lerp(drainAtEmpty, drainAtFull, Mathf.Pow(Mathf.Clamp01(share), drainPower));

        /// <summary>Energy recovered per second standing still.</summary>
        public float RegenPerSecond(float share) => Mathf.Lerp(regenAtEmpty, regenAtFull, Mathf.Clamp01(share));

        /// <summary>
        /// What one press costs: at the best rhythm the presses of a second together use <see cref="DrainPerSecond"/>; a press that
        /// came sooner than <paramref name="optimalGap"/> after the last one costs more by how much faster it was (up to 3 times).
        /// </summary>
        public float PressCost(float share, float optimalGap, float gap)
        {
            float perPress = DrainPerSecond(share) * Mathf.Max(0.01f, optimalGap);
            float faster = gap > 0.001f ? Mathf.Clamp(optimalGap / gap - 1f, 0f, 2f) : 2f;
            return perPress * (1f + overPaceCost * faster);
        }
    }
}
