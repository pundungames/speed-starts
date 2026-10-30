using System;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Home
{
    /// <summary>How one "Watch ad" attempt of the entry gate ended.</summary>
    public enum AdWatchOutcome
    {
        /// <summary>The ad was completed and the ticket was granted.</summary>
        Granted,
        /// <summary>The ad was completed but the inventory is full: no ticket.</summary>
        Full,
        Skipped,
        Failed,
        /// <summary>The ad was completed but the ticket source gives nothing (config closed).</summary>
        Unavailable
    }

    /// <summary>
    /// The rewarded-ad rules of the entry gate, without any UI: when the offer is open, one ad at a time, a completed ad
    /// asks <see cref="IEntryTicketSource"/> for the ticket exactly once, everything else pays nothing. The result is
    /// handled here even when the window has closed meanwhile; only the callback touches the view (and the view checks
    /// itself first).
    /// </summary>
    public sealed class EntryGateAdFlow
    {
        private readonly IRewardedAds _ads;
        private readonly IEntryTicketSource _source;
        private readonly EconomyConfig _economy;
        private readonly Action _granted;

        /// <param name="ads">Null in a build without an ad service: the offer stays closed.</param>
        /// <param name="granted">Called after a ticket was granted (the caller saves the progress).</param>
        public EntryGateAdFlow(IRewardedAds ads, IEntryTicketSource source, EconomyConfig economy, Action granted = null)
        {
            _ads = ads;
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _economy = economy;
            _granted = granted;
        }

        /// <summary>An ad is running; no second one can start.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>The offer is open: an ad service exists and is ready, and the config gives a ticket for it.</summary>
        public bool IsOffered => _ads != null && _economy != null && _ads.IsReady && _economy.TryGetRewardedAdEntryTickets(out _);

        public bool IsAvailable => !IsBusy && IsOffered;

        /// <summary>
        /// Starts one ad. False (nothing started) when the offer is closed or an ad is already running. <paramref name="done"/>
        /// gets the outcome once; it may run before this call returns.
        /// </summary>
        public bool TryStart(Action<AdWatchOutcome> done)
        {
            if (!IsAvailable) return false;

            IsBusy = true;
            bool answered = false;
            try
            {
                _ads.Show(result =>
                {
                    if (answered) return; // an ad answers once; a repeat is ignored
                    answered = true;
                    IsBusy = false;
                    AdWatchOutcome outcome = Resolve(result);
                    done?.Invoke(outcome);
                });
            }
            catch
            {
                IsBusy = false;
                throw;
            }
            return true;
        }

        private AdWatchOutcome Resolve(AdResult result)
        {
            switch (result)
            {
                case AdResult.Completed:
                    break;
                case AdResult.Skipped:
                    return AdWatchOutcome.Skipped;
                default:
                    return AdWatchOutcome.Failed;
            }

            switch (_source.TryGrant(TicketGrantSource.RewardedAd))
            {
                case TicketGrantResult.Granted:
                    _granted?.Invoke();
                    return AdWatchOutcome.Granted;
                case TicketGrantResult.Full:
                    return AdWatchOutcome.Full;
                default:
                    return AdWatchOutcome.Unavailable;
            }
        }
    }
}
