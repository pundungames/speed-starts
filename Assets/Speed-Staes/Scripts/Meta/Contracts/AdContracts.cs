using System;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>How one rewarded ad ended. Only <see cref="Completed"/> earns the reward.</summary>
    public enum AdResult
    {
        Completed,
        Skipped,
        Failed
    }

    /// <summary>
    /// Rewarded video ads seam. No SDK exists yet: only a dev fake implements it, and in a release build nothing is bound,
    /// so the views treat a missing service as "no ads".
    /// </summary>
    public interface IRewardedAds
    {
        /// <summary>An ad can be shown right now.</summary>
        bool IsReady { get; }

        /// <summary>Plays one ad; <paramref name="done"/> is called exactly once (possibly later, possibly in the same call).</summary>
        void Show(Action<AdResult> done);
    }
}
