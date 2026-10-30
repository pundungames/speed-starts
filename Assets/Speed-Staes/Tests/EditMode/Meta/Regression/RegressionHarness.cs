using System;
using SpeedStars.Core.Race;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Clock;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Cosmetics;
using SpeedStars.Meta.Economy;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Persistence;
using SpeedStars.Meta.Records;
using SpeedStars.Meta.Rewards;
using SpeedStars.Meta.Settings;
using SpeedStars.Meta.Themes;
using SpeedStars.Meta.Tickets;
using SpeedStars.Meta.Wallet;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Regression
{
    /// <summary>
    /// One whole meta stack over a shared save: the real repository and services, the real <c>ClockService</c> (its last-seen
    /// stamp lives in the save, like in the game) on a settable device clock. <c>Boot()</c> builds everything again from the
    /// store, which is exactly what a killed and reopened app does. Every number below is an arbitrary test input; tests
    /// assert relations between them (or read them back from the configs), never shipped asset values.
    /// </summary>
    internal sealed class RegressionHarness
    {
        public const int TicketCap = 5;
        public const float RegenMinutes = 7f;
        public const int GiftAmount = 2;
        public const float TimedWindowMinutes = 11f;
        public const int StartCash = 100;
        public const int StartGems = 100;
        public const int StartTimedTickets = 1;
        public const int CashDayOne = 30;
        public const int CashDayTwo = 70;
        public const int HatPrice = 9;
        public const string HatId = "reg-hat";
        public const string ThemeDefault = "theme-a";
        public const string ThemePaid = "theme-b";
        public const int ThemePrice = 40;

        public static readonly RaceEventId GemEvent = new RaceEventId("reg-gems");
        public static readonly DateTime Start = new DateTime(2030, 5, 10, 8, 0, 0, DateTimeKind.Utc);

        public readonly ConfigFixture Fixture = new ConfigFixture();
        public readonly ISaveStore Store;
        public readonly EconomyConfig Economy;
        public readonly SevenDayRewardConfig SevenDayConfig;
        public readonly CosmeticCatalog CosmeticCatalog;
        public readonly ThemeCatalog ThemeCatalog;
        public readonly SettingsConfig SettingsConfig;
        public readonly LeaderboardConfig LeaderboardConfig;
        public readonly RaceEventCatalog Events;
        public readonly GemRewardConfig Gems;

        /// <summary>The device clock the game reads; tests move it forward and back.</summary>
        public DateTime Device = Start;

        private Session _current;

        public RegressionHarness(ISaveStore store = null, int startEntryTickets = TicketCap, float regenMinutes = RegenMinutes)
        {
            Store = store ?? new MemoryStore();

            Economy = Fixture.Create<EconomyConfig>();
            ConfigFixture.SetInt(Economy, "entryTicketCap", TicketCap);
            ConfigFixture.SetFloat(Economy, "entryTicketRegenMinutes", regenMinutes);
            ConfigFixture.SetInt(Economy, "dailyGiftEntryTickets", GiftAmount);
            ConfigFixture.SetFloat(Economy, "timedAccessMinutes", TimedWindowMinutes);
            ConfigFixture.SetInt(Economy, "startingCash", StartCash);
            ConfigFixture.SetInt(Economy, "startingPurpleGems", StartGems);
            ConfigFixture.SetInt(Economy, "startingEntryTickets", startEntryTickets);
            ConfigFixture.SetInt(Economy, "startingTimedTickets", StartTimedTickets);

            var hat = Fixture.Create<CosmeticDefinition>();
            ConfigFixture.SetString(hat, "cosmeticId", HatId);
            ConfigFixture.SetInt(hat, "category", (int)CosmeticCategory.Headwear);
            ConfigFixture.SetBool(hat, "isPremium", true);
            ConfigFixture.SetInt(hat, "price", HatPrice);
            ConfigFixture.SetInt(hat, "currency", (int)CosmeticCurrency.PurpleGem);
            ConfigFixture.SetStrings(hat, "partNames", "TEST");
            CosmeticCatalog = Fixture.Create<CosmeticCatalog>();
            ConfigFixture.SetObjects(CosmeticCatalog, "cosmetics", hat);

            SevenDayConfig = Fixture.Create<SevenDayRewardConfig>();
            SetDays(
                (SevenDayRewardKind.Cash, CashDayOne, null),
                (SevenDayRewardKind.Cash, CashDayTwo, null),
                (SevenDayRewardKind.TimedTicket, 2, null),
                (SevenDayRewardKind.Cash, CashDayOne, null),
                (SevenDayRewardKind.TimedTicket, 2, null),
                (SevenDayRewardKind.Cash, CashDayTwo, null),
                (SevenDayRewardKind.Cosmetic, 0, hat));

            ThemeCatalog = Fixture.Create<ThemeCatalog>();
            ConfigFixture.Edit(ThemeCatalog, so =>
            {
                var list = ConfigFixture.Find(so, "themes");
                string[] ids = { ThemeDefault, ThemePaid };
                int[] prices = { 0, ThemePrice };
                list.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++)
                {
                    var e = list.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("themeId").stringValue = ids[i];
                    e.FindPropertyRelative("displayName").stringValue = ids[i].ToUpperInvariant();
                    e.FindPropertyRelative("showcaseIndex").intValue = i;
                    e.FindPropertyRelative("cashPrice").intValue = prices[i];
                }
                ConfigFixture.Find(so, "defaultThemeId").stringValue = ThemeDefault;
            });

            SettingsConfig = Fixture.Create<SettingsConfig>();
            ConfigFixture.Edit(SettingsConfig, so =>
            {
                ConfigFixture.Find(so, "defaultMusicOn").boolValue = true;
                ConfigFixture.Find(so, "defaultSfxOn").boolValue = true;
                ConfigFixture.Find(so, "defaultPlayerName").stringValue = "Anon";
            });

            LeaderboardConfig = Fixture.Create<LeaderboardConfig>();
            ConfigFixture.SetInt(LeaderboardConfig, "botsPerEvent", 0);

            var gemEvent = Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(gemEvent, "eventId", GemEvent.Value);
            ConfigFixture.SetBool(gemEvent, "awardsPurpleGems", true);
            ConfigFixture.SetBool(gemEvent, "isPlayable", true);
            ConfigFixture.SetFloat(gemEvent, "distanceMeters", 100f);
            Events = Fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(Events, "events", gemEvent);

            Gems = Fixture.Create<GemRewardConfig>();
            ConfigFixture.SetInts(Gems, "purpleGemsByRank", 9, 6, 3);
        }

        private void SetDays(params (SevenDayRewardKind kind, int amount, CosmeticDefinition item)[] days) =>
            ConfigFixture.Edit(SevenDayConfig, so =>
            {
                var list = ConfigFixture.Find(so, "days");
                list.arraySize = days.Length;
                for (int i = 0; i < days.Length; i++)
                {
                    var day = list.GetArrayElementAtIndex(i);
                    day.FindPropertyRelative("kind").intValue = (int)days[i].kind;
                    day.FindPropertyRelative("amount").intValue = days[i].amount;
                    day.FindPropertyRelative("cosmetic").objectReferenceValue = days[i].item;
                }
            });

        public TimeSpan RegenInterval => Economy.EntryTicketRegenInterval;
        public TimeSpan TimedWindow => Economy.TimedAccessDuration;

        /// <summary>Kills the app and opens it again: every service is rebuilt from what the store holds.</summary>
        public Session Boot()
        {
            _current?.Board.Dispose();

            var repository = new ProgressRepository(Store, () => new StartingBalances(
                Economy.StartingCash, Economy.StartingPurpleGems, Economy.StartingEntryTickets, Economy.StartingTimedTickets));
            repository.Load();

            var clock = new ClockService(
                () => repository.Current.LastSeenUtcTicks,
                ticks => repository.Current.LastSeenUtcTicks = ticks,
                () => Device);

            var s = new Session { Repository = repository, Clock = clock };
            s.Wallet = new WalletService(repository, clock);
            s.Ledger = new EconomyLedger(repository, s.Wallet, Economy);
            s.Tickets = new EntryTicketService(repository, clock, Economy);
            s.Timed = new TimedAccessService(repository, clock, Economy, s.Ledger, s.Wallet);
            s.Gift = new DailyGiftService(repository, clock, Economy, s.Tickets, s.Tickets);
            s.Cosmetics = new CosmeticService(repository, s.Ledger, CosmeticCatalog);
            s.SevenDay = new SevenDayRewardService(repository, clock, SevenDayConfig, s.Ledger, s.Cosmetics);
            s.Themes = new ThemeService(ThemeCatalog, repository, s.Ledger, false);
            s.Settings = new PlayerSettingsService(SettingsConfig, repository);
            s.Bests = new PersonalBestService(repository, clock, LeaderboardConfig);
            s.Board = new LocalLeaderboard(LeaderboardConfig, Events, s.Bests, s.Settings, clock);
            s.Rewards = new RaceRewardResolver(Events, Gems, s.Ledger, repository);
            s.Reset = new ProgressResetService(repository);
            _current = s;
            return s;
        }

        public void Dispose()
        {
            _current?.Board.Dispose();
            Fixture.DestroyAll();
        }

        public RaceOutcome Outcome(int rank, double seconds = 12.5) => new RaceOutcome(GemEvent, seconds, 100f, rank, 8);

        internal sealed class Session
        {
            public ProgressRepository Repository;
            public ClockService Clock;
            public WalletService Wallet;
            public EconomyLedger Ledger;
            public EntryTicketService Tickets;
            public TimedAccessService Timed;
            public DailyGiftService Gift;
            public CosmeticService Cosmetics;
            public SevenDayRewardService SevenDay;
            public ThemeService Themes;
            public PlayerSettingsService Settings;
            public PersonalBestService Bests;
            public LocalLeaderboard Board;
            public RaceRewardResolver Rewards;
            public ProgressResetService Reset;

            public PlayerProgress Progress => Repository.Current;
        }
    }
}
