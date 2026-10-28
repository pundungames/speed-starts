using System;
using NUnit.Framework;
using SpeedStars.Meta.Clock;

namespace SpeedStars.Tests.Meta.Clock
{
    /// <summary>
    /// ClockService with a fake device clock and a fake "last seen" store standing in for the save.
    /// A new ClockService over the same store plays the part of the next app launch.
    /// </summary>
    public sealed class ClockServiceTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

        private DateTime _device;
        private long _lastSeen;
        private int _writes;

        [SetUp]
        public void SetUp()
        {
            _device = T0;
            _lastSeen = 0;
            _writes = 0;
        }

        private ClockService NewClock()
        {
            return new ClockService(() => _lastSeen, t =>
            {
                _writes++;
                _lastSeen = t;
            }, () => _device);
        }

        [Test]
        public void UtcNow_FollowsDeviceClock_AndRecordsLastSeen()
        {
            ClockService clock = NewClock();

            Assert.AreEqual(T0, clock.UtcNow);
            Assert.AreEqual(T0.Ticks, _lastSeen);

            _device = T0.AddMinutes(3);
            Assert.AreEqual(T0.AddMinutes(3), clock.UtcNow);
            Assert.AreEqual(T0.AddMinutes(3).Ticks, _lastSeen);
            Assert.IsFalse(clock.IsRolledBack);
        }

        [Test]
        public void UtcNow_IsUtcKind()
        {
            Assert.AreEqual(DateTimeKind.Utc, NewClock().UtcNow.Kind);
        }

        [Test]
        public void UtcNow_SameDeviceTime_DoesNotRewriteLastSeen()
        {
            ClockService clock = NewClock();
            DateTime unused = clock.UtcNow;
            int writes = _writes;

            unused = clock.UtcNow;

            Assert.AreEqual(writes, _writes);
        }

        [Test]
        public void RolledBackDeviceClock_NeverGoesBelowLastSeen()
        {
            ClockService clock = NewClock();
            _device = T0.AddHours(2);
            DateTime latest = clock.UtcNow;

            _device = T0;

            Assert.AreEqual(latest, clock.UtcNow);
            Assert.AreEqual(latest.Ticks, _lastSeen);
            Assert.IsTrue(clock.IsRolledBack);
        }

        [Test]
        public void RolledBackDeviceClock_IsReportedOnce_WithTheGap()
        {
            ClockService clock = NewClock();
            _device = T0.AddHours(2);
            DateTime unused = clock.UtcNow;
            int raised = 0;
            TimeSpan gap = TimeSpan.Zero;
            clock.RolledBack += g =>
            {
                raised++;
                gap = g;
            };

            _device = T0;
            unused = clock.UtcNow;
            unused = clock.UtcNow;
            _device = T0.AddMinutes(1);
            unused = clock.UtcNow;

            Assert.AreEqual(1, raised);
            Assert.AreEqual(TimeSpan.FromHours(2), gap);
        }

        [Test]
        public void AfterDeviceCatchesUp_ClockMovesAgain_AndANewRollbackIsReportedAgain()
        {
            ClockService clock = NewClock();
            _device = T0.AddHours(2);
            DateTime unused = clock.UtcNow;
            int raised = 0;
            clock.RolledBack += _ => raised++;

            _device = T0;
            unused = clock.UtcNow;
            _device = T0.AddHours(3);
            Assert.AreEqual(T0.AddHours(3), clock.UtcNow);
            Assert.IsFalse(clock.IsRolledBack);

            _device = T0.AddHours(1);
            unused = clock.UtcNow;

            Assert.AreEqual(2, raised);
        }

        [Test]
        public void TimeKeepsRunningWhileTheAppIsClosed()
        {
            DateTime closedAt = NewClock().UtcNow;
            TimeSpan away = TimeSpan.FromHours(7) + TimeSpan.FromMinutes(5);

            _device = closedAt + away;
            ClockService nextLaunch = NewClock();

            Assert.AreEqual(away, nextLaunch.UtcNow - closedAt);
            Assert.IsFalse(nextLaunch.IsRolledBack);
        }

        [Test]
        public void RollbackWhileTheAppWasClosed_IsCaughtOnNextLaunch()
        {
            _device = T0.AddDays(1);
            DateTime closedAt = NewClock().UtcNow;

            _device = T0;
            ClockService nextLaunch = NewClock();
            int raised = 0;
            nextLaunch.RolledBack += _ => raised++;

            Assert.AreEqual(closedAt, nextLaunch.UtcNow);
            Assert.IsTrue(nextLaunch.IsRolledBack);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void LocalDeviceTime_IsConvertedToUtc()
        {
            DateTime local = T0.ToLocalTime();
            var clock = new ClockService(() => 0L, _ => { }, () => local);

            DateTime now = clock.UtcNow;

            Assert.AreEqual(T0, now);
            Assert.AreEqual(DateTimeKind.Utc, now.Kind);
        }

        [Test]
        public void Constructor_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new ClockService(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() => new ClockService(() => 0L, null));
        }

        [Test]
        public void DefaultDeviceClock_IsSystemUtc()
        {
            var clock = new ClockService(() => 0L, _ => { });
            DateTime before = DateTime.UtcNow;

            DateTime now = clock.UtcNow;

            Assert.That(now, Is.GreaterThanOrEqualTo(before));
            Assert.That(now, Is.LessThanOrEqualTo(DateTime.UtcNow));
        }
    }
}
