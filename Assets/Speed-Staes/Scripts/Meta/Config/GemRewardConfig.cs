using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Purple Gems by single-race rank (PDF §1.3). One table for every event whose definition has AwardsPurpleGems;
    /// there are no per-event copies, so the amounts can never drift apart. Ranks past the table pay nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Gem Reward", fileName = "GemRewardConfig")]
    public sealed class GemRewardConfig : ScriptableObject
    {
        [Tooltip("Element 0 = 1st place, element 1 = 2nd place ... Every rank past the last element gets 0.")]
        [SerializeField, Min(0)] private int[] purpleGemsByRank = new int[0];

        /// <summary>Purple Gems for a 1-based rank (1 = winner); 0 outside the table (rank below 1 or past the last row).</summary>
        public int GetPurpleGems(int rank)
        {
            if (purpleGemsByRank == null || rank < 1 || rank > purpleGemsByRank.Length) return 0;
            return purpleGemsByRank[rank - 1];
        }
    }
}
