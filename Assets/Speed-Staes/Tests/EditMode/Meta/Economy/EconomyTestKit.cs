using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Race;
using SpeedStars.Meta.Tickets;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Economy
{
    /// <summary>
    /// Real economy services over a memory save, fake clock and configs built here. All numbers are arbitrary test
    /// inputs; tests assert relations between them, never shipped asset values.
    /// </summary>
    internal sealed class EconomyTestKit
    {
        public static readonly TimeSpan TimedWindow = TimeSpan.FromMinutes(11);
        public const int TransactionCap = 4;

        public static readonly RaceEventId GemEvent = new RaceEventId("test-gems");
        public static readonly RaceEventId PlainEvent = new RaceEventId("test-plain");
        public static readonly RaceEventId PaidEvent = new RaceEventId("test-paid");

        public readonly TicketTestKit Kit;
        public readonly RaceEventCatalog Events;
        public readonly GemRewardConfig Gems;
        public WalletService Wallet;
        public EconomyLedger Ledger;
        public TimedAccessService Timed;
        public EntryTicketService Tickets;
        public EntryTicketShop Shop;
        public RaceRewardResolver Rewards;
        public RaceEntryGate Gate;

        public EconomyTestKit(int entryTickets = 0)
        {
            Kit = new TicketTestKit(entryTickets);
            ConfigFixture.SetFloat(Kit.Economy, "timedAccessMinutes", (float)TimedWindow.TotalMinutes);
            ConfigFixture.SetInt(Kit.Economy, "processedTransactionCap", TransactionCap);

            Events = Kit.Fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(Events, "events",
                Event("test-gems", true, false), Event("test-plain", false, false), Event("test-paid", false, true));

            Gems = Kit.Fixture.Create<GemRewardConfig>();
            ConfigFixture.SetInts(Gems, "purpleGemsByRank", 9, 6, 3);

            Build(Kit.Repository);
        }

        public PlayerProgress Progress => Kit.Progress;

        /// <summary>(Re)builds every service over a repository: a new one after "restart".</summary>
        public void Build(ProgressRepository repository)
        {
            Wallet = new WalletService(repository, Kit.Clock);
            Ledger = new EconomyLedger(repository, Wallet, Kit.Economy);
            Timed = new TimedAccessService(repository, Kit.Clock, Kit.Economy, Ledger, Wallet);
            Tickets = new EntryTicketService(repository, Kit.Clock, Kit.Economy);
            Shop = new EntryTicketShop(Kit.Economy, Ledger, Tickets, repository);
            Rewards = new RaceRewardResolver(Events, Gems, Ledger, repository);
            Gate = new RaceEntryGate(repository, Kit.Clock, Events, Tickets);
        }

        public ProgressRepository Restart()
        {
            var reloaded = new ProgressRepository(Kit.Store, () => new StartingBalances(0, 0, 0, 0));
            reloaded.Load();
            Build(reloaded);
            return reloaded;
        }

        public RaceOutcome Outcome(RaceEventId id, int rank, double seconds = 12.5)
            => new RaceOutcome(id, seconds, 100f, rank, 8);

        private RaceEventDefinition Event(string id, bool awardsGems, bool requiresTicket)
        {
            var e = Kit.Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(e, "eventId", id);
            ConfigFixture.SetBool(e, "awardsPurpleGems", awardsGems);
            ConfigFixture.SetBool(e, "requiresTicket", requiresTicket);
            ConfigFixture.SetBool(e, "isPlayable", true);
            return e;
        }

        public void Dispose() => Kit.Dispose();
    }
}
