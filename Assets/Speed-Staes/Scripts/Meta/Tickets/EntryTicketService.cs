using System;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Meta.Tickets
{
    /// <summary>
    /// Entry Tickets (PDF §1.2) over the save. The count and the next-ticket deadline live in <c>PlayerProgress</c>;
    /// cap and interval are read from <c>EconomyConfig</c> on every call. Regen is lazy: every read first applies the
    /// intervals that have passed (also while the app was closed), never past the cap. At the cap the timer stops
    /// (due = 0); dropping below the cap starts a fresh full interval. A regen interval of 0 means regen is off.
    /// The clock never goes back (<c>IClock</c>), so a rolled-back device clock cannot produce tickets.
    /// </summary>
    public sealed class EntryTicketService : IEntryTickets
    {
        private readonly ProgressRepository _repository;
        private readonly IClock _clock;
        private readonly EconomyConfig _config;

        public EntryTicketService(ProgressRepository repository, IClock clock, EconomyConfig config)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public event Action Changed;

        public int Count
        {
            get
            {
                Refresh();
                return Progress.EntryTickets;
            }
        }

        public int Cap => _config.EntryTicketCap;

        public bool IsFull => Count >= Cap;

        public DateTime? NextTicketUtc
        {
            get
            {
                Refresh();
                PlayerProgress p = Progress;
                if (p.EntryTickets >= Cap || p.EntryRegenDueUtcTicks <= 0) return null;
                return new DateTime(p.EntryRegenDueUtcTicks, DateTimeKind.Utc);
            }
        }

        public void Refresh()
        {
            PlayerProgress p = Progress;
            long interval = _config.EntryTicketRegenInterval.Ticks;
            int cap = Cap;
            long now = _clock.UtcNow.Ticks;
            bool changed = false;

            if (p.EntryTickets >= cap || interval <= 0)
            {
                // At the cap (or regen off) no timer runs.
                if (p.EntryRegenDueUtcTicks != 0)
                {
                    p.EntryRegenDueUtcTicks = 0;
                    changed = true;
                }
            }
            else if (p.EntryRegenDueUtcTicks <= 0)
            {
                // Below the cap with no timer (fresh save, or the cap was raised): start a full interval.
                p.EntryRegenDueUtcTicks = now + interval;
                changed = true;
            }
            else if (now >= p.EntryRegenDueUtcTicks)
            {
                long passed = (now - p.EntryRegenDueUtcTicks) / interval + 1;
                long missing = cap - p.EntryTickets;
                long gained = Math.Min(passed, missing);
                p.EntryTickets += (int)gained;
                p.EntryRegenDueUtcTicks = p.EntryTickets >= cap
                    ? 0
                    : p.EntryRegenDueUtcTicks + passed * interval;
                changed = true;
            }

            if (!changed) return;
            // Persist so a timer started now is not restarted by the next launch. A refused save is harmless:
            // the state is recomputed from the same deadline.
            _repository.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// Spends one ticket. False (nothing changed) when none is held. Does NOT save: the caller that charges the
        /// player (<c>RaceEntryGate</c>) saves right after.
        /// </summary>
        public bool TrySpend()
        {
            Refresh();
            PlayerProgress p = Progress;
            if (p.EntryTickets <= 0) return false;

            p.EntryTickets--;
            if (p.EntryTickets < Cap && p.EntryRegenDueUtcTicks <= 0 && _config.EntryTicketRegenInterval.Ticks > 0)
                p.EntryRegenDueUtcTicks = _clock.UtcNow.Ticks + _config.EntryTicketRegenInterval.Ticks;

            Changed?.Invoke();
            return true;
        }

        private PlayerProgress Progress => _repository.Current
            ?? throw new InvalidOperationException("ProgressRepository.Load() must run before the ticket service is used.");
    }
}
