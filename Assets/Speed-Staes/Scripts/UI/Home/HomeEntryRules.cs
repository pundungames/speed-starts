using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Home
{
    public enum PlayStep
    {
        /// <summary>Nothing to do (no event, or not playable).</summary>
        None,
        /// <summary>Ticketed event: ask first in the entry gate modal.</summary>
        OpenGate,
        /// <summary>Free event, or Timed access is running: enter directly (nothing to confirm).</summary>
        EnterNow
    }

    /// <summary>What the UI does with an <see cref="EntryResult"/>.</summary>
    public enum EntryOutcome
    {
        StartRace,
        NoTicket,
        NotPlayable,
        UnknownEvent
    }

    /// <summary>UI-independent decisions of the Home entry flow, kept pure so they run in EditMode tests.</summary>
    public static class HomeEntryRules
    {
        /// <summary>What pressing Play does for the selected event.</summary>
        public static PlayStep DecidePlay(bool eventExists, bool isPlayable, bool requiresTicket, bool timedAccessActive)
        {
            if (!eventExists || !isPlayable) return PlayStep.None;
            if (!requiresTicket || timedAccessActive) return PlayStep.EnterNow;
            return PlayStep.OpenGate;
        }

        public static EntryOutcome Resolve(EntryResult result)
        {
            switch (result)
            {
                case EntryResult.EnteredFree:
                case EntryResult.EnteredWithTicket:
                case EntryResult.EnteredWithTimedAccess:
                    return EntryOutcome.StartRace;
                case EntryResult.NoTicket:
                    return EntryOutcome.NoTicket;
                case EntryResult.NotPlayable:
                    return EntryOutcome.NotPlayable;
                default:
                    return EntryOutcome.UnknownEvent;
            }
        }

        public static string WarningFor(EntryOutcome outcome)
        {
            switch (outcome)
            {
                case EntryOutcome.NoTicket: return HomeTexts.NoTicketWarning;
                case EntryOutcome.NotPlayable: return HomeTexts.NotPlayableWarning;
                case EntryOutcome.UnknownEvent: return HomeTexts.UnknownEventWarning;
                default: return string.Empty;
            }
        }
    }
}
