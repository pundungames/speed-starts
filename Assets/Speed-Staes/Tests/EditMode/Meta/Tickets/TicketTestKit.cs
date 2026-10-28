using System;
using System.Collections.Generic;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Persistence;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Tickets
{
    internal sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        public void Advance(TimeSpan span) => UtcNow += span;
    }

    internal sealed class MemoryStore : ISaveStore
    {
        private readonly Dictionary<string, string> _data = new Dictionary<string, string>();
        public int Writes { get; private set; }

        public bool TryRead(string key, out string contents) => _data.TryGetValue(key, out contents);

        public void Write(string key, string contents)
        {
            Writes++;
            _data[key] = contents;
        }

        public void Delete(string key) => _data.Remove(key);
    }

    /// <summary>
    /// Builds a repository, clock and economy config with test-chosen values. The numbers here are arbitrary test
    /// inputs; tests assert relations between them, never the shipped asset values.
    /// </summary>
    internal sealed class TicketTestKit
    {
        public const int Cap = 5;
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(7);

        public readonly ConfigFixture Fixture = new ConfigFixture();
        public readonly FakeClock Clock = new FakeClock();
        public readonly MemoryStore Store = new MemoryStore();
        public readonly EconomyConfig Economy;
        public readonly ProgressRepository Repository;

        public TicketTestKit(int startingTickets)
        {
            Economy = Fixture.Create<EconomyConfig>();
            ConfigFixture.SetInt(Economy, "entryTicketCap", Cap);
            ConfigFixture.SetFloat(Economy, "entryTicketRegenMinutes", (float)Interval.TotalMinutes);
            Repository = new ProgressRepository(Store, () => new StartingBalances(0, 0, startingTickets, 0));
            Repository.Load();
        }

        public PlayerProgress Progress => Repository.Current;

        public void Dispose() => Fixture.DestroyAll();
    }
}
