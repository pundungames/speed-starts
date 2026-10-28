using System;
using SpeedStars.Core.Services;

namespace SpeedStars.Meta.Clock
{
    /// <summary>
    /// <c>IClock</c> on the device's UTC clock. Real-time deadlines (timed access, ticket regen, daily gift) are stored as
    /// UTC and compared with this, so they keep running while the app is closed (PDF §1.1) — no game or session time.
    /// The latest time handed out is written to an outside store (the save) and read back next launch. If the device
    /// clock is ever behind it (clock rolled back, by hand or by a bad sync), <c>UtcNow</c> holds at that latest time:
    /// countdowns pause instead of jumping back, and nothing can be earned by winding the clock back and forth.
    /// The rollback is reported once per occurrence through <c>RolledBack</c>; it ends when the device clock catches up.
    /// </summary>
    public sealed class ClockService : IClock
    {
        private readonly Func<long> _readLastSeenUtcTicks;
        private readonly Action<long> _writeLastSeenUtcTicks;
        private readonly Func<DateTime> _deviceUtcNow;

        /// <param name="readLastSeenUtcTicks">Latest handed-out UTC as ticks (0 = none yet), e.g. from the save.</param>
        /// <param name="writeLastSeenUtcTicks">Stores a newer latest UTC; persisted whenever the save is written.</param>
        /// <param name="deviceUtcNow">Device clock; tests pass a fake. Null = <c>DateTime.UtcNow</c>.</param>
        public ClockService(Func<long> readLastSeenUtcTicks, Action<long> writeLastSeenUtcTicks,
            Func<DateTime> deviceUtcNow = null)
        {
            _readLastSeenUtcTicks = readLastSeenUtcTicks ?? throw new ArgumentNullException(nameof(readLastSeenUtcTicks));
            _writeLastSeenUtcTicks = writeLastSeenUtcTicks ?? throw new ArgumentNullException(nameof(writeLastSeenUtcTicks));
            _deviceUtcNow = deviceUtcNow ?? (() => DateTime.UtcNow);
        }

        /// <summary>Raised once when the device clock is found behind the latest handed-out time; argument = how far.</summary>
        public event Action<TimeSpan> RolledBack;

        /// <summary>True while the device clock is still behind the latest handed-out time.</summary>
        public bool IsRolledBack { get; private set; }

        public DateTime UtcNow
        {
            get
            {
                long device = ToUtc(_deviceUtcNow()).Ticks;
                long lastSeen = Math.Min(Math.Max(_readLastSeenUtcTicks(), 0L), DateTime.MaxValue.Ticks);

                if (device >= lastSeen)
                {
                    IsRolledBack = false;
                    if (device > lastSeen) _writeLastSeenUtcTicks(device);
                    return new DateTime(device, DateTimeKind.Utc);
                }

                if (!IsRolledBack)
                {
                    IsRolledBack = true;
                    RolledBack?.Invoke(TimeSpan.FromTicks(lastSeen - device));
                }
                return new DateTime(lastSeen, DateTimeKind.Utc);
            }
        }

        private static DateTime ToUtc(DateTime time)
        {
            return time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time;
        }
    }
}
