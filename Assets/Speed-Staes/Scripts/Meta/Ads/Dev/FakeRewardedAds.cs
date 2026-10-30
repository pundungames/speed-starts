#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.Ads
{
    /// <summary>
    /// Stand-in for an ad SDK (dev only): "plays" for a short real-time wait, then reports the configured result. The wait
    /// runs on a hidden helper object that outlives scene changes, so a result arriving after the gate closed still lands.
    /// A second Show during a running one fails at once (never two ads).
    /// </summary>
    public sealed class FakeRewardedAds : IRewardedAds
    {
        private readonly AdsDevConfig _config;
        private bool _showing;

        public FakeRewardedAds(AdsDevConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsReady => !_showing;

        public void Show(Action<AdResult> done)
        {
            if (done == null) throw new ArgumentNullException(nameof(done));
            if (_showing)
            {
                done(AdResult.Failed);
                return;
            }

            _showing = true;
            AdResult result = _config.FakeResult;
            var host = new GameObject("FakeRewardedAds") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<FakeRewardedAdsPump>().Run(_config.FakeAdSeconds, () =>
            {
                _showing = false;
                done(result);
            });
        }
    }
}
#endif
