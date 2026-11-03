using System.Globalization;

namespace SpeedStars.UI.Home
{
    /// <summary>The fixed sentences of the Cash offer window (plain ASCII).</summary>
    public static class CashOfferTexts
    {
        public const string Title = "GET CASH";
        public const string Caption = "Watch an ad";
        public const string WatchAd = "WATCH AD";
        public const string YourCash = "Your Cash";
        public const string LimitReached = "All ads for today are watched. Come back tomorrow!";
        public const string NoAd = "No ad is ready right now. Try again in a moment.";
        public const string NotOffered = "Cash ads are not available.";
        public const string Loading = "Loading the ad...";

        /// <summary>"+200": what one ad pays.</summary>
        public static string Amount(int amount) => "+" + amount.ToString(CultureInfo.InvariantCulture);

        /// <summary>"0/3": ads watched today over the daily limit.</summary>
        public static string Counter(int watched, int limit) =>
            watched.ToString(CultureInfo.InvariantCulture) + "/" + limit.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>What the Cash offer window knows about the rewarded Cash ad (read from <c>IRewardedCash</c> plus whether an ad service is ready).</summary>
    public readonly struct CashOfferState
    {
        public readonly int Amount;
        public readonly int DailyLimit;
        public readonly int RemainingToday;
        public readonly bool Busy;

        /// <summary><c>IRewardedCash.IsAvailable</c>: amount and limit configured, an ad ready, rights left, none running.</summary>
        public readonly bool Available;

        public CashOfferState(int amount, int dailyLimit, int remainingToday, bool busy, bool available)
        {
            Amount = amount;
            DailyLimit = dailyLimit;
            RemainingToday = remainingToday;
            Busy = busy;
            Available = available;
        }
    }

    /// <summary>
    /// What the Cash offer window shows for one moment (Nakit "+" button): "+200", the "0/3" counter, the Watch ad button state and
    /// the line that says why it is closed. Pure values; the view only writes them. The ad itself is never started from here.
    /// </summary>
    public readonly struct CashOfferModel
    {
        public readonly string AmountText;
        public readonly string CounterText;

        /// <summary>The button can be pressed: the offer is open and no ad runs.</summary>
        public readonly bool WatchInteractable;

        /// <summary>The loading spinner shows instead of the button label.</summary>
        public readonly bool ShowSpinner;

        /// <summary>Why the button is closed; empty while it works.</summary>
        public readonly string NoteText;

        private CashOfferModel(string amount, string counter, bool interactable, bool spinner, string note)
        {
            AmountText = amount;
            CounterText = counter;
            WatchInteractable = interactable;
            ShowSpinner = spinner;
            NoteText = note;
        }

        public static CashOfferModel Build(CashOfferState state)
        {
            int limit = state.DailyLimit < 0 ? 0 : state.DailyLimit;
            int remaining = state.RemainingToday < 0 ? 0 : state.RemainingToday;
            if (remaining > limit) remaining = limit;
            bool offered = state.Amount > 0 && limit > 0;

            string note;
            if (state.Busy) note = CashOfferTexts.Loading;
            else if (state.Available) note = string.Empty;
            else if (!offered) note = CashOfferTexts.NotOffered;
            else if (remaining <= 0) note = CashOfferTexts.LimitReached;
            else note = CashOfferTexts.NoAd;

            return new CashOfferModel(
                CashOfferTexts.Amount(state.Amount < 0 ? 0 : state.Amount),
                CashOfferTexts.Counter(limit - remaining, limit),
                interactable: state.Available && !state.Busy,
                spinner: state.Busy,
                note: note);
        }
    }
}
