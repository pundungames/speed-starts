using System;
using System.Collections.Generic;
using SpeedStars.Meta.Config;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>Daily Entry Ticket gift for today (UTC day). Full = the inventory is at the cap, the day's right is kept.</summary>
    public enum DailyGiftState { NotDue, Available, Full }

    public enum DailyGiftResult { Granted, Full, AlreadyClaimed, NotDue }

    /// <summary>The once-a-day Entry Ticket gift (PDF §1.2, §2.1). A full inventory gives nothing and does not use up the day.</summary>
    public interface IDailyGift
    {
        /// <summary>State for today.</summary>
        DailyGiftState State { get; }

        /// <summary>Entry Tickets one claim gives (from the economy config).</summary>
        int Amount { get; }

        /// <summary>Granted: the ticket was added and the day saved as claimed.</summary>
        DailyGiftResult Claim();

        /// <summary>The Full notice was shown (OK). Does not use up the day; only stops this session from showing it again.</summary>
        void MarkSeenFull();

        event Action Changed;
    }

    public enum SevenDayCardState { Claimed, Available, Locked }

    /// <summary>One day card of the 7-day check-in. <c>Kind</c> is the config enum (<see cref="SevenDayRewardKind"/>): Cash, TimedTicket, Cosmetic (None = empty day).</summary>
    public readonly struct SevenDayCard
    {
        public SevenDayCard(int dayNumber, SevenDayCardState state, SevenDayRewardKind kind, int amount, string cosmeticId)
        {
            DayNumber = dayNumber;
            State = state;
            Kind = kind;
            Amount = amount;
            CosmeticId = cosmeticId ?? string.Empty;
        }

        /// <summary>1-based.</summary>
        public int DayNumber { get; }
        public SevenDayCardState State { get; }
        public SevenDayRewardKind Kind { get; }

        /// <summary>Cash / ticket amount; 0 for a Cosmetic day.</summary>
        public int Amount { get; }

        /// <summary>Item id of a Cosmetic day; empty otherwise.</summary>
        public string CosmeticId { get; }
    }

    public enum SevenDayClaimResult { Claimed, NothingAvailable, Failed }

    /// <summary>The 7-day check-in: one card a day, claimed by hand, missed days do not reset.</summary>
    public interface ISevenDayRewards
    {
        /// <summary>The 7 cards in day order, by today's state.</summary>
        IReadOnlyList<SevenDayCard> Cards { get; }

        /// <summary>A card can be claimed now (red dot).</summary>
        bool HasClaimable { get; }

        /// <summary>Claims the Available card. A failed claim changes nothing.</summary>
        SevenDayClaimResult Claim();

        event Action Changed;
    }
}
