using System;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Tickets;

namespace SpeedStars.Meta.Race
{
    /// <summary>
    /// The one place a race entry is paid for (MD §13). Order: unknown event, not playable, free event, running timed
    /// access (spends nothing), else one Entry Ticket. A spent ticket is saved at once (no refund on quit, PDF §1.2).
    /// A confirmed entry stays pending until the race scene takes it, so a double tap on the same event pays once.
    /// </summary>
    public sealed class RaceEntryGate : IRaceEntryGate, IDailyRaceEntryStatus
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly RaceEventCatalog _catalog;
        private readonly EntryTicketService _tickets;
        private readonly IRaceOfTheDay _daily;

        private RaceEventId _pendingId;
        private EntryResult _pendingResult;

        public RaceEntryGate(ProgressRepository repository, IClock clock, RaceEventCatalog catalog,
            EntryTicketService tickets, IRaceOfTheDay daily = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
            _daily = daily;
        }

        public bool HasPendingEntry { get; private set; }

        public TimeSpan DailyRaceCooldown
        {
            get
            {
                long started = _repository.Current.LastDailyRaceEntryUtcTicks;
                if (started <= 0) return TimeSpan.Zero;
                long elapsed = Math.Max(0L, _clock.UtcNow.Ticks - started);
                return TimeSpan.FromTicks(Math.Max(0L, TimeSpan.TicksPerDay - elapsed));
            }
        }

        public EntryResult Enter(RaceEventId id)
        {
            if (HasPendingEntry && _pendingId == id) return _pendingResult;

            if (!_catalog.TryGet(id, out RaceEventDefinition definition)) return EntryResult.UnknownEvent;
            if (!definition.IsPlayable) return EntryResult.NotPlayable;

            if (IsDaily(id) && DailyRaceCooldown > TimeSpan.Zero) return EntryResult.NotPlayable;
            if (!definition.RequiresTicket) return Confirm(id, EntryResult.EnteredFree);

            PlayerProgress progress = _repository.Current
                ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the gate is used.");
            if (progress.TimedAccessExpiresUtcTicks > _clock.UtcNow.Ticks)
                return Confirm(id, EntryResult.EnteredWithTimedAccess);

            if (!_tickets.TrySpend()) return EntryResult.NoTicket;
            _repository.Save();
            return Confirm(id, EntryResult.EnteredWithTicket);
        }

        public bool TryTakePending(out RaceEventId id)
        {
            id = _pendingId;
            if (!HasPendingEntry)
            {
                id = default;
                return false;
            }
            HasPendingEntry = false;
            _pendingId = default;
            if (IsDaily(id))
            {
                _repository.Current.LastDailyRaceEntryUtcTicks = _clock.UtcNow.Ticks;
                _repository.Save();
            }
            return true;
        }

        private bool IsDaily(RaceEventId id) => _daily != null && _daily.IsRaceOfTheDay(id);

        private EntryResult Confirm(RaceEventId id, EntryResult result)
        {
            HasPendingEntry = true;
            _pendingId = id;
            _pendingResult = result;
            return result;
        }
    }
}
