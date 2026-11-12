using UnityEngine;

namespace SpeedStars.Physical
{
    /// <summary>
    /// The energy numbers as an asset (Physical/EnergyTuning.asset): the runner, its race copies and the bots read the same
    /// one, so an acceptance test changes energy, capacity, use and recovery in one file. See <see cref="EnergyModel"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "EnergyTuning", menuName = "Speed Stars/Physical/Energy Tuning")]
    public sealed class EnergyTuning : ScriptableObject
    {
        [Tooltip("Every energy number: full energy, capacity at empty, the best rhythm's share, use at full and empty, over-pace cost, hold use, recovery.")]
        public EnergyModel model = new EnergyModel();
    }
}
