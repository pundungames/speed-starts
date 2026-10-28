using System;
using SpeedStars.Core.Race;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>
    /// Entry Tickets (Adet Bileti, PDF §1.2). Count is kept in the save; regen is real time and applied lazily, so a
    /// closed app still earns tickets. Cap, interval and daily gift come from EconomyConfig, never from code.
    /// </summary>
    public interface IEntryTickets
    {
        /// <summary>Tickets held now, after any regen that is due.</summary>
        int Count { get; }
        int Cap { get; }
        bool IsFull { get; }

        /// <summary>When the next ticket arrives; null at the cap (the timer stops, PDF §1.2).</summary>
        DateTime? NextTicketUtc { get; }

        event Action Changed;

        /// <summary>Applies regen that is due. The UI ticker calls it about once a second; readers may call it too.</summary>
        void Refresh();
    }

    /// <summary>Read side of the wallet for the resource bar (PDF §2.2). Earning and spending arrive in Faz 3.</summary>
    public interface IWallet
    {
        int Cash { get; }
        int PurpleGems { get; }

        /// <summary>Unused Timed Tickets (Süreli Bilet stock).</summary>
        int TimedTickets { get; }

        /// <summary>True while a used Timed Ticket's real-time window is running (PDF §1.1).</summary>
        bool TimedAccessActive { get; }
        DateTime? TimedAccessEndsUtc { get; }

        event Action Changed;
    }

    /// <summary>Which race event the player has picked on the Home screen (survives scene loads, not saved).</summary>
    public interface IRaceSelection
    {
        RaceEventId Selected { get; }
        void Select(RaceEventId id);
        event Action Changed;
    }

    public enum EntryResult
    {
        /// <summary>Event needs no ticket.</summary>
        EnteredFree,
        /// <summary>One Entry Ticket was spent (PDF §1.2: no refund on quit, loss or restart).</summary>
        EnteredWithTicket,
        /// <summary>Timed access is running: nothing is spent (PDF §1.1).</summary>
        EnteredWithTimedAccess,
        /// <summary>Ticket needed and none held; nothing changed.</summary>
        NoTicket,
        /// <summary>Event exists but is not playable yet ("coming soon").</summary>
        NotPlayable,
        /// <summary>No event with that id in the catalog.</summary>
        UnknownEvent
    }

    /// <summary>
    /// The single place a race entry is paid for (MD §13 RaceEntryGate). Every button (Play, Race again) goes through
    /// Enter; a double tap cannot pay twice because a confirmed entry stays pending until the race scene takes it.
    /// </summary>
    public interface IRaceEntryGate
    {
        /// <summary>
        /// Pays for and confirms an entry. While an entry for the same event is still pending, returns the same result
        /// again without paying. Saves immediately when something was spent.
        /// </summary>
        EntryResult Enter(RaceEventId id);

        bool HasPendingEntry { get; }

        /// <summary>The race scene takes the confirmed entry exactly once; false when there is none.</summary>
        bool TryTakePending(out RaceEventId id);
    }
}
