using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.Ads
{
    /// <summary>
    /// Numbers of the fake rewarded ad (dev only): how long the "ad" runs and how it ends. Lives outside the economy
    /// config on purpose: nothing here is game balance.
    /// </summary>
    [CreateAssetMenu(fileName = "AdsDevConfig", menuName = "Speed Stars/Meta/Ads Dev Config")]
    public sealed class AdsDevConfig : ScriptableObject
    {
        [Tooltip("Seconds the fake ad takes before it ends (real time, not affected by Time.timeScale).")]
        [Min(0f)]
        [SerializeField] private float fakeAdSeconds = 1f;

        [Tooltip("How the fake ad ends. Completed pays the reward; change it to test the Skipped / Failed paths.")]
        [SerializeField] private AdResult fakeResult = AdResult.Completed;

        public float FakeAdSeconds => fakeAdSeconds < 0f ? 0f : fakeAdSeconds;
        public AdResult FakeResult => fakeResult;
    }
}
