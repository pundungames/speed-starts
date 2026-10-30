using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Rewards
{
    /// <summary>
    /// Daily Entry Ticket gift. The saved day key is the double-claim guard: one grant per UTC day. A full inventory gives
    /// nothing and leaves the key alone, so the gift can be claimed later the same day once a ticket was used.
    /// </summary>
    public sealed class DailyGiftService : IDailyGift
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly EconomyConfig _config;
        private readonly IEntryTickets _tickets;
        private readonly IEntryTicketSource _source;
        private string _seenFullDayKey = string.Empty;

        public DailyGiftService(ProgressRepository repository, IClock clock, EconomyConfig config,
            IEntryTickets tickets, IEntryTicketSource source)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public event Action Changed;

        public int Amount => _config.DailyGiftEntryTickets;

        /// <summary>True once the Full notice was dismissed today (this session only).</summary>
        public bool FullNoticeSeenToday => _seenFullDayKey == RewardDay.Key(_clock.UtcNow);

        public DailyGiftState State
        {
            get
            {
                if (Amount < 1 || Progress.DailyGiftDayKey == RewardDay.Key(_clock.UtcNow)) return DailyGiftState.NotDue;
                return _tickets.IsFull ? DailyGiftState.Full : DailyGiftState.Available;
            }
        }

        public DailyGiftResult Claim()
        {
            string today = RewardDay.Key(_clock.UtcNow);
            if (Progress.DailyGiftDayKey == today) return DailyGiftResult.AlreadyClaimed;
            if (Amount < 1) return DailyGiftResult.NotDue;

            switch (_source.TryGrant(TicketGrantSource.DailyGift))
            {
                case TicketGrantResult.Granted: break;
                case TicketGrantResult.Full: return DailyGiftResult.Full;
                default: return DailyGiftResult.NotDue;
            }

            // Ticket and day key are written in one save.
            Progress.DailyGiftDayKey = today;
            _repository.Save();
            Changed?.Invoke();
            return DailyGiftResult.Granted;
        }

        public void MarkSeenFull() => _seenFullDayKey = RewardDay.Key(_clock.UtcNow);

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the daily gift is used.");
    }
}
