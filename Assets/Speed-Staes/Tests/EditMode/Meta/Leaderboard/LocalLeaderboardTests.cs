using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.DailyRace;
using SpeedStars.Meta.Leaderboard;
using SpeedStars.Meta.Records;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Leaderboard
{
    /// <summary>
    /// Local leaderboard behaviour. Bot paces, counts, names and the player's times are chosen per test; the tests assert
    /// how results relate (order, windows, filters, paging), never fixed shipped numbers.
    /// </summary>
    public sealed class LocalLeaderboardTests
    {
        private static readonly RaceEventId Sprint = new RaceEventId("sprint");
        private static readonly RaceEventId NoDistance = new RaceEventId("nodistance");
        private static readonly DateTime Noon = new DateTime(2030, 1, 16, 12, 0, 0, DateTimeKind.Utc); // a Wednesday

        private TicketTestKit _kit;
        private LeaderboardConfig _config;
        private PersonalBestService _bests;
        private FakeSettings _settings;
        private LocalLeaderboard _board;
        private RaceEventCatalog _catalog;
        private int _raceCounter;

        [SetUp]
        public void SetUp()
        {
            _kit = new TicketTestKit(0);
            _kit.Clock.UtcNow = Noon;
            _config = _kit.Fixture.Create<LeaderboardConfig>();
            ConfigFixture.SetInt(_config, "botsPerEvent", 0);
            _bests = new PersonalBestService(_kit.Repository, _kit.Clock, _config);
            _settings = new FakeSettings { PlayerName = "Tester", FlagCode = "DE" };

            var catalog = _kit.Fixture.Create<RaceEventCatalog>();
            _catalog = catalog;
            var sprint = _kit.Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(sprint, "eventId", Sprint.Value);
            ConfigFixture.SetFloat(sprint, "distanceMeters", 100f);
            var none = _kit.Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(none, "eventId", NoDistance.Value);
            ConfigFixture.SetFloat(none, "distanceMeters", 0f);
            ConfigFixture.SetObjects(catalog, "events", sprint, none);

            _board = new LocalLeaderboard(_config, catalog, _bests, _settings, _kit.Clock);
            _raceCounter = 0;
        }

        [TearDown]
        public void TearDown()
        {
            _dailyBoard?.Dispose();
            _dailyBoard = null;
            _board.Dispose();
            _kit.Dispose();
        }

        // ---- helpers -------------------------------------------------------------------------------------------

        private void WithBots(int count, float best, float median, float worst, params string[] countries)
        {
            ConfigFixture.SetInt(_config, "botsPerEvent", count);
            ConfigFixture.SetFloat(_config, "bestSecondsPerMeter", best);
            ConfigFixture.SetFloat(_config, "medianSecondsPerMeter", median);
            ConfigFixture.SetFloat(_config, "worstSecondsPerMeter", worst);
            ConfigFixture.SetInt(_config, "botHistoryDays", 60);
            ConfigFixture.SetStrings(_config, "botNames", "Ann", "Bob", "Cy");
            ConfigFixture.Edit(_config, so =>
            {
                var list = ConfigFixture.Find(so, "botCountries");
                list.arraySize = countries.Length;
                for (int i = 0; i < countries.Length; i++)
                {
                    var entry = list.GetArrayElementAtIndex(i);
                    ConfigFixture.Find(so, entry.propertyPath + ".countryCode").stringValue = countries[i];
                    ConfigFixture.Find(so, entry.propertyPath + ".weight").floatValue = 1f;
                }
            });
        }

        private void Race(double seconds, int dayOffset = 0, RaceEventId? id = null)
        {
            _kit.Clock.UtcNow = Noon.AddDays(dayOffset);
            _bests.Record(id ?? Sprint, seconds, "r" + _raceCounter++);
            _kit.Clock.UtcNow = Noon;
        }

        private LeaderboardPage Ask(LeaderboardPeriod period, string country = null, int offset = 0, int limit = 50, RaceEventId? id = null)
        {
            LeaderboardPage page = null;
            string error = null;
            _board.Query(new LeaderboardQuery(id ?? Sprint, period, country, offset, limit), p => page = p, e => error = e);
            Assert.IsNull(error);
            Assert.IsNotNull(page);
            return page;
        }

        private sealed class FakeSettings : IPlayerSettings
        {
            public string PlayerName { get; set; }
            public string FlagCode { get; set; }
            public event Action Changed { add { } remove { } }
            public bool MusicOn => true;
            public bool SfxOn => true;
            public string LanguageCode => "en";
            public int MaxNameLength => 16;
            public IReadOnlyList<LanguageInfo> Languages => new List<LanguageInfo>();
            public IReadOnlyList<FlagInfo> Flags => new List<FlagInfo>();
            public float CameraDistance => 1f;
            public float CameraDistanceMin => 1f;
            public float CameraDistanceMax => 1f;
            // T-088 settings v2 members (not used by this test).
            public CompetitorType CompetitorType => CompetitorType.Bots;
            public float CompetitorDifficulty => 0.5f;
            public float CompetitorSkillRange => 0.5f;
            public bool HasCompetitorTuning => false;
            public int LanePreference => 0;
            public int LaneChoiceCount => 1;
            public float HurdleSensitivity => 0.5f;
            public float SwipeUpDistance => 0.06f;
            public bool ParticleEffects => true;
            public void SetCompetitorType(CompetitorType type) { }
            public void SetCompetitorDifficulty(float value) { }
            public void SetCompetitorSkillRange(float value) { }
            public void SetLanePreference(int lane) { }
            public void SetHurdleSensitivity(float value) { }
            public void SetParticleEffects(bool on) { }
            public bool ShowEmoteButton => true;
            public bool AutoEmoteSelf => false;
            public bool AutoEmoteCompetitors => false;
            public void SetShowEmoteButton(bool on) { }
            public void SetAutoEmoteSelf(bool on) { }
            public void SetAutoEmoteCompetitors(bool on) { }
            public void SetCameraDistance(float value) { }
            public void SetMusic(bool on) { }
            public void SetSfx(bool on) { }
            public bool SetLanguage(string code) => false;
            public bool SetFlag(string code) => false;
            public PlayerNameResult ValidateName(string raw) => PlayerNameResult.Valid;
            public PlayerNameResult TrySetName(string raw) => PlayerNameResult.Valid;
        }

        // ---- windows -------------------------------------------------------------------------------------------

        [Test]
        public void PeriodStarts_AreUtc_WeekOnMonday_MonthOnTheFirst()
        {
            for (int d = 0; d < 40; d++)
            {
                DateTime now = Noon.AddDays(d).AddHours(7);
                DateTime today = LocalLeaderboard.PeriodStart(LeaderboardPeriod.Today, now);
                DateTime week = LocalLeaderboard.PeriodStart(LeaderboardPeriod.ThisWeek, now);
                DateTime month = LocalLeaderboard.PeriodStart(LeaderboardPeriod.ThisMonth, now);

                Assert.AreEqual(now.Date, today.Date);
                Assert.AreEqual(TimeSpan.Zero, today.TimeOfDay);
                Assert.AreEqual(DayOfWeek.Monday, week.DayOfWeek);
                Assert.LessOrEqual(week, today);
                Assert.Less((today - week).TotalDays, 7d);
                Assert.AreEqual(1, month.Day);
                Assert.AreEqual(now.Month, month.Month);
                Assert.AreEqual(DateTimeKind.Utc, today.Kind);
            }
            Assert.AreEqual(DateTime.MinValue, LocalLeaderboard.PeriodStart(LeaderboardPeriod.AllTime, Noon));
        }

        [Test]
        public void Query_ReportsThePeriodStart()
        {
            Race(10d);
            Assert.AreEqual(LocalLeaderboard.PeriodStart(LeaderboardPeriod.ThisWeek, Noon), Ask(LeaderboardPeriod.ThisWeek).PeriodStartUtc);
        }

        [Test]
        public void PlayerHasOneRowPerPeriod_TheBestInsideThatPeriod()
        {
            Race(9d, -40);  // fast, but before this month and this week
            Race(12d, -1);  // yesterday (a Tuesday: this week and this month)
            Race(11d, 0);   // today, faster than yesterday

            LeaderboardPage all = Ask(LeaderboardPeriod.AllTime);
            LeaderboardPage month = Ask(LeaderboardPeriod.ThisMonth);
            LeaderboardPage week = Ask(LeaderboardPeriod.ThisWeek);
            LeaderboardPage today = Ask(LeaderboardPeriod.Today);

            Assert.AreEqual(1, all.Rows.Count);
            Assert.AreEqual(1, month.Rows.Count);
            Assert.AreEqual(1, week.Rows.Count);
            Assert.AreEqual(1, today.Rows.Count);
            Assert.Less(all.Rows[0].ElapsedMilliseconds, month.Rows[0].ElapsedMilliseconds, "the old fast race stays in AllTime only");
            Assert.AreEqual(month.Rows[0].ElapsedMilliseconds, week.Rows[0].ElapsedMilliseconds);
            Assert.AreEqual(week.Rows[0].ElapsedMilliseconds, today.Rows[0].ElapsedMilliseconds);
            Assert.IsTrue(all.Rows[0].IsLocalPlayer);
            Assert.AreEqual("Tester", all.Rows[0].DisplayName);
            Assert.AreEqual("DE", all.Rows[0].CountryCode);
            Assert.AreEqual(Noon.AddDays(-40).Date, all.Rows[0].AchievedAtUtc.Date, "the date is the one of the race, not of the query");
        }

        [Test]
        public void ARecordOutsideThePeriod_DoesNotAppearInIt()
        {
            Race(9d, -40);
            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime).Rows.Count);
            Assert.AreEqual(0, Ask(LeaderboardPeriod.ThisMonth).Rows.Count);
            Assert.AreEqual(0, Ask(LeaderboardPeriod.Today).Rows.Count);
            Assert.IsNull(Ask(LeaderboardPeriod.Today).OwnEntry);
        }

        // ---- ordering ------------------------------------------------------------------------------------------

        [Test]
        public void Rows_AreOrderedByRawTime_ThenOlderDate_ThenEntryId()
        {
            WithBots(40, 0.1f, 0.1f, 0.1f, "TR", "US"); // every bot has the same raw time: only the tie-break orders them
            LeaderboardPage page = Ask(LeaderboardPeriod.AllTime, limit: 100);

            Assert.AreEqual(40, page.Rows.Count);
            for (int i = 1; i < page.Rows.Count; i++)
            {
                LeaderboardRow a = page.Rows[i - 1];
                LeaderboardRow b = page.Rows[i];
                Assert.AreEqual(a.Rank + 1, b.Rank);
                Assert.LessOrEqual(a.ElapsedMilliseconds, b.ElapsedMilliseconds);
                if (a.ElapsedMilliseconds == b.ElapsedMilliseconds)
                {
                    Assert.LessOrEqual(a.AchievedAtUtc, b.AchievedAtUtc);
                    if (a.AchievedAtUtc == b.AchievedAtUtc) Assert.Less(string.CompareOrdinal(a.EntryId, b.EntryId), 0);
                }
            }
        }

        [Test]
        public void EqualTime_PlayerWhoSetItLater_RanksBehindTheBots()
        {
            WithBots(30, 0.1f, 0.1f, 0.1f, "TR");
            long botMs = Ask(LeaderboardPeriod.AllTime).Rows[0].ElapsedMilliseconds;
            Race(botMs / 1000d); // exactly the bots' raw time, set now (newer than any bot)

            LeaderboardPage page = Ask(LeaderboardPeriod.AllTime);

            Assert.IsNotNull(page.OwnEntry);
            Assert.AreEqual(botMs, page.OwnEntry.ElapsedMilliseconds);
            Assert.AreEqual(page.TotalCount, page.OwnEntry.Rank);
        }

        [Test]
        public void FasterPlayer_RanksFirst_SlowerPlayer_RanksLast()
        {
            WithBots(30, 0.1f, 0.14f, 0.2f, "TR", "US");
            long fastestBot = Ask(LeaderboardPeriod.AllTime).Rows[0].ElapsedMilliseconds;
            long slowestBot = Ask(LeaderboardPeriod.AllTime, offset: 29, limit: 5).Rows[0].ElapsedMilliseconds;

            Race((fastestBot - 500) / 1000d);
            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime).OwnEntry.Rank);

            Race((slowestBot + 500) / 1000d, -100); // a slower race on an earlier day does not move the best
            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime).OwnEntry.Rank);
        }

        // ---- region --------------------------------------------------------------------------------------------

        [Test]
        public void CountryFilter_ShowsOnlyThatCountry_WithRanksInsideIt()
        {
            WithBots(80, 0.1f, 0.14f, 0.2f, "TR", "US", "JP");
            LeaderboardPage global = Ask(LeaderboardPeriod.AllTime, limit: 100);
            LeaderboardPage tr = Ask(LeaderboardPeriod.AllTime, "TR", limit: 100);

            Assert.Greater(tr.Rows.Count, 0);
            Assert.Less(tr.TotalCount, global.TotalCount);
            Assert.AreEqual(tr.TotalCount, tr.Rows.Count);
            for (int i = 0; i < tr.Rows.Count; i++)
            {
                Assert.AreEqual("TR", tr.Rows[i].CountryCode);
                Assert.AreEqual(i + 1, tr.Rows[i].Rank);
            }
        }

        [Test]
        public void CountryRank_OfThePlayer_IsInsideTheirCountryNotGlobal()
        {
            WithBots(80, 0.1f, 0.14f, 0.2f, "DE", "US", "JP");
            Race(14.5d);

            LeaderboardPage global = Ask(LeaderboardPeriod.AllTime);
            LeaderboardPage de = Ask(LeaderboardPeriod.AllTime, "DE");

            Assert.IsNotNull(global.OwnEntry);
            Assert.IsNotNull(de.OwnEntry);
            Assert.LessOrEqual(de.OwnEntry.Rank, global.OwnEntry.Rank);
            Assert.Less(de.TotalCount, global.TotalCount);
        }

        [Test]
        public void OtherCountry_PlayerIsNotRanked()
        {
            WithBots(40, 0.1f, 0.14f, 0.2f, "TR", "US");
            Race(14.5d); // the player's flag is DE

            Assert.IsNotNull(Ask(LeaderboardPeriod.AllTime).OwnEntry);
            Assert.IsNull(Ask(LeaderboardPeriod.AllTime, "TR").OwnEntry);
        }

        [Test]
        public void CountryCode_IsCaseInsensitive_AndUkIsGb()
        {
            _settings.FlagCode = "GB";
            Race(10d);

            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime, "gb").Rows.Count);
            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime, "UK").Rows.Count);
            Assert.AreEqual(0, Ask(LeaderboardPeriod.AllTime, "US").Rows.Count);
        }

        // ---- paging --------------------------------------------------------------------------------------------

        [Test]
        public void Paging_WalksTheWholeRanking_WithoutRepeats()
        {
            WithBots(47, 0.1f, 0.14f, 0.2f, "TR", "US");
            Race(13d);

            var seen = new HashSet<string>();
            int offset = 0;
            int expectedRank = 1;
            int total = -1;
            int guard = 0;
            while (offset >= 0 && guard++ < 50)
            {
                LeaderboardPage page = Ask(LeaderboardPeriod.AllTime, offset: offset, limit: 10);
                total = page.TotalCount;
                Assert.LessOrEqual(page.Rows.Count, 10);
                foreach (LeaderboardRow row in page.Rows)
                {
                    Assert.IsTrue(seen.Add(row.EntryId), "repeated " + row.EntryId);
                    Assert.AreEqual(expectedRank++, row.Rank);
                }
                Assert.IsNotNull(page.OwnEntry, "the own row comes with every page");
                if (page.NextOffset >= 0) Assert.AreEqual(offset + page.Rows.Count, page.NextOffset);
                offset = page.NextOffset;
            }
            Assert.AreEqual(-1, offset, "the walk must end");
            Assert.AreEqual(total, seen.Count);
        }

        [Test]
        public void OffsetBeyondTheEnd_GivesNoRowsAndNoNextPage()
        {
            WithBots(5, 0.1f, 0.14f, 0.2f, "TR");
            LeaderboardPage page = Ask(LeaderboardPeriod.AllTime, offset: 500);
            Assert.AreEqual(0, page.Rows.Count);
            Assert.AreEqual(-1, page.NextOffset);
            Assert.AreEqual(5, page.TotalCount);
        }

        [Test]
        public void Limit_IsClampedToThePageCap()
        {
            WithBots(30, 0.1f, 0.14f, 0.2f, "TR");
            ConfigFixture.SetInt(_config, "maxPageSize", 7);
            Assert.AreEqual(7, Ask(LeaderboardPeriod.AllTime, limit: 1000).Rows.Count);
        }

        // ---- own entry, standing, best of day ---------------------------------------------------------------------

        [Test]
        public void NoRecord_OwnEntryIsNull_AndThereIsNoStanding()
        {
            WithBots(10, 0.1f, 0.14f, 0.2f, "TR");

            Assert.IsNull(Ask(LeaderboardPeriod.AllTime).OwnEntry);
            Assert.IsFalse(_board.TryGetStanding(Sprint, out _, out _));
        }

        [Test]
        public void Standing_MatchesTheGlobalAllTimeRank_AndTotalCountsEveryone()
        {
            WithBots(25, 0.1f, 0.14f, 0.2f, "TR", "US");
            Race(14d);

            Assert.IsTrue(_board.TryGetStanding(Sprint, out int rank, out int total));
            LeaderboardPage page = Ask(LeaderboardPeriod.AllTime);
            Assert.AreEqual(page.OwnEntry.Rank, rank);
            Assert.AreEqual(page.TotalCount, total);
            Assert.GreaterOrEqual(rank, 1);
            Assert.LessOrEqual(rank, total);
        }

        [Test]
        public void BestOfDay_IsToday_AndNotYesterday()
        {
            Assert.IsFalse(_board.TryGetBestOfDay(Sprint, out _));

            Race(10d, -1);
            Assert.IsFalse(_board.TryGetBestOfDay(Sprint, out _), "only yesterday has a result");

            Race(11d, 0);
            Assert.IsTrue(_board.TryGetBestOfDay(Sprint, out LeaderboardRow best));
            Assert.IsTrue(best.IsLocalPlayer);
            Assert.AreEqual(1, best.Rank);
        }

        [Test]
        public void BestOfDay_CanBeABot()
        {
            WithBots(3000, 0.1f, 0.14f, 0.2f, "TR"); // enough that some bot surely ran in the last 12 hours
            Race(60d); // the player is slow

            Assert.IsTrue(_board.TryGetBestOfDay(Sprint, out LeaderboardRow best));
            Assert.IsFalse(best.IsLocalPlayer);
            Assert.AreEqual(Noon.Date, best.AchievedAtUtc.Date);
        }

        // ---- bots ----------------------------------------------------------------------------------------------

        [Test]
        public void BotTable_IsTheSameEveryTime_AndAcrossInstances()
        {
            WithBots(60, 0.1f, 0.14f, 0.2f, "TR", "US", "JP");
            LeaderboardPage first = Ask(LeaderboardPeriod.AllTime, limit: 100);

            var other = new LocalLeaderboard(_config, _catalog, _bests, _settings, _kit.Clock); // a "new launch": empty caches
            LeaderboardPage again = null;
            other.Query(new LeaderboardQuery(Sprint, LeaderboardPeriod.AllTime, null, 0, 100), p => again = p, null);
            other.Dispose();
            Assert.IsNotNull(again);

            Assert.AreEqual(first.Rows.Count, again.Rows.Count);
            for (int i = 0; i < first.Rows.Count; i++)
            {
                Assert.AreEqual(first.Rows[i].EntryId, again.Rows[i].EntryId);
                Assert.AreEqual(first.Rows[i].ElapsedMilliseconds, again.Rows[i].ElapsedMilliseconds);
                Assert.AreEqual(first.Rows[i].DisplayName, again.Rows[i].DisplayName);
                Assert.AreEqual(first.Rows[i].CountryCode, again.Rows[i].CountryCode);
            }
        }

        [Test]
        public void BotTimes_StayBetweenTheBestAndWorstPace()
        {
            WithBots(100, 0.1f, 0.15f, 0.3f, "TR");
            LeaderboardPage page = Ask(LeaderboardPeriod.AllTime, limit: 100);

            foreach (LeaderboardRow row in page.Rows)
            {
                Assert.GreaterOrEqual(row.ElapsedMilliseconds, 0.1d * 100d * 1000d - 1d);
                Assert.LessOrEqual(row.ElapsedMilliseconds, 0.3d * 100d * 1000d + 1d);
                Assert.LessOrEqual(row.AchievedAtUtc, Noon);
                Assert.GreaterOrEqual(row.AchievedAtUtc, Noon.AddDays(-60));
            }
        }

        [Test]
        public void ShorterWindows_HaveFewerBots()
        {
            WithBots(200, 0.1f, 0.14f, 0.2f, "TR");
            int all = Ask(LeaderboardPeriod.AllTime).TotalCount;
            int month = Ask(LeaderboardPeriod.ThisMonth).TotalCount;
            int week = Ask(LeaderboardPeriod.ThisWeek).TotalCount;
            int today = Ask(LeaderboardPeriod.Today).TotalCount;

            Assert.GreaterOrEqual(all, month);
            Assert.GreaterOrEqual(month, week);
            Assert.GreaterOrEqual(week, today);
            Assert.Less(today, all);
        }

        [Test]
        public void EventWithoutDistance_HasNoBots()
        {
            WithBots(50, 0.1f, 0.14f, 0.2f, "TR");
            Assert.AreEqual(0, Ask(LeaderboardPeriod.AllTime, id: NoDistance).TotalCount);
        }

        // ---- events and errors ---------------------------------------------------------------------------------

        [Test]
        public void EmptyEventId_IsAnError()
        {
            string error = null;
            bool done = false;
            _board.Query(new LeaderboardQuery(new RaceEventId(""), LeaderboardPeriod.AllTime, null, 0, 10), _ => done = true, e => error = e);

            Assert.IsFalse(done);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Changed_FiresWhenThePlayerSetsANewDayBest()
        {
            int fired = 0;
            _board.Changed += () => fired++;

            Race(12d);
            Assert.AreEqual(1, fired);
            Race(13d); // slower the same day: nothing changes
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Events_KeepSeparateRankings()
        {
            Race(10d, 0, Sprint);
            Assert.AreEqual(1, Ask(LeaderboardPeriod.AllTime).TotalCount);
            Assert.AreEqual(0, Ask(LeaderboardPeriod.AllTime, id: NoDistance).TotalCount);
        }

        // ---- Race of the Day (T-093) ---------------------------------------------------------------------------

        private static readonly float[] DailyDistances = { 60f, 100f, 150f, 200f, 300f };

        private RaceOfTheDayService _daily;
        private LocalLeaderboard _dailyBoard;

        /// <summary>A board that knows the daily race, over the same clock, config and day bests as the plain one.</summary>
        private void WithDaily()
        {
            WithBots(8, 0.09f, 0.1f, 0.12f, "DE", "FR");
            _daily = new RaceOfTheDayService(_kit.Clock, "race_of_the_day", DailyDistances, new List<RaceOfTheDayLook>(), _config);
            _dailyBoard = new LocalLeaderboard(_config, _catalog, _bests, _settings, _kit.Clock, _daily);
        }

        private LeaderboardPage AskDaily(LeaderboardPeriod period)
        {
            LeaderboardPage page = null;
            string error = null;
            _dailyBoard.Query(new LeaderboardQuery(_daily.EventId, period, null, 0, 50), p => page = p, e => error = e);
            Assert.IsNull(error);
            Assert.IsNotNull(page);
            return page;
        }

        [Test]
        public void RaceOfTheDay_ListsTheDaysBotField_AndItsFirstRowIsTheDailyWorldRecord()
        {
            WithDaily();
            LeaderboardPage page = AskDaily(LeaderboardPeriod.AllTime);

            Assert.Greater(page.Rows.Count, 0, "the daily list is not empty");
            Assert.IsTrue(_daily.TryGetFieldRecord(out DailyFieldRecord record));
            Assert.AreEqual(record.ElapsedMilliseconds, page.Rows[0].ElapsedMilliseconds, "the list's best is the result screen's Daily World Record");
            for (int i = 1; i < page.Rows.Count; i++)
                Assert.GreaterOrEqual(page.Rows[i].ElapsedMilliseconds, page.Rows[i - 1].ElapsedMilliseconds);
        }

        [Test]
        public void RaceOfTheDay_BotTimesScaleWithTheDaysDistance()
        {
            WithDaily();
            float distance = _daily.Today.DistanceMeters;
            LeaderboardPage page = AskDaily(LeaderboardPeriod.AllTime);

            double secondsPerMeter = page.Rows[0].ElapsedMilliseconds / 1000d / distance;
            Assert.GreaterOrEqual(secondsPerMeter, _config.BestSecondsPerMeter - 0.001d);
            Assert.LessOrEqual(secondsPerMeter, _config.WorstSecondsPerMeter + 0.001d);
        }

        [Test]
        public void RaceOfTheDay_ShowsThePlayersTimeOfTodaysDistanceOnly()
        {
            WithDaily();
            float distance = _daily.Today.DistanceMeters;
            _bests.Record(_daily.RecordIdFor(distance), 500d, "mine");
            _bests.Record(_daily.EventId, 1d, "bare");                       // the bare event id is not where times are stored
            _bests.Record(_daily.RecordIdFor(distance + 1000f), 2d, "other"); // another distance's record is another list

            LeaderboardPage page = AskDaily(LeaderboardPeriod.AllTime);

            int own = 0;
            foreach (LeaderboardRow row in page.Rows)
            {
                if (!row.IsLocalPlayer) continue;
                own++;
                Assert.AreEqual(500000L, row.ElapsedMilliseconds);
            }
            Assert.AreEqual(1, own);
            Assert.IsNotNull(page.OwnEntry);
        }

        [Test]
        public void RaceOfTheDay_PeriodFilterWorksOnTheDistanceRecord()
        {
            WithDaily();
            _bests.Record(_daily.RecordIdFor(_daily.Today.DistanceMeters), 500d, "mine");

            foreach (LeaderboardPeriod period in new[] { LeaderboardPeriod.Today, LeaderboardPeriod.ThisWeek, LeaderboardPeriod.ThisMonth, LeaderboardPeriod.AllTime })
                Assert.IsNotNull(AskDaily(period).OwnEntry, period.ToString());
        }

        [Test]
        public void RaceOfTheDay_NextDayMovesTheListToThatDaysDistanceAndField()
        {
            WithDaily();
            RaceOfTheDayInfo first = _daily.Today;
            _bests.Record(_daily.RecordIdFor(first.DistanceMeters), 500d, "mine");
            Assert.IsNotNull(AskDaily(LeaderboardPeriod.AllTime).OwnEntry);

            // Walk forward until the distance changes (the rule never repeats a distance on neighbouring days).
            int days = 0;
            while (_daily.Today.DistanceMeters == first.DistanceMeters && days < 10)
            {
                _kit.Clock.UtcNow = _kit.Clock.UtcNow.AddDays(1);
                days++;
            }
            Assert.AreNotEqual(first.DistanceMeters, _daily.Today.DistanceMeters);

            LeaderboardPage page = AskDaily(LeaderboardPeriod.AllTime);
            Assert.IsNull(page.OwnEntry, "yesterday's distance record does not carry over");
            Assert.IsTrue(_daily.TryGetFieldRecord(out DailyFieldRecord record));
            Assert.AreEqual(record.ElapsedMilliseconds, page.Rows[0].ElapsedMilliseconds);
        }

        [Test]
        public void RaceOfTheDay_WithoutTheService_TheBoardKeepsItsPlainBehaviour()
        {
            WithDaily();
            // The plain board has no daily service and the catalog has no such event, so the id is just an unknown event.
            LeaderboardPage page = null;
            _board.Query(new LeaderboardQuery(_daily.EventId, LeaderboardPeriod.AllTime, null, 0, 50), p => page = p, e => { });
            Assert.IsNotNull(page);
            Assert.AreEqual(0, page.Rows.Count);

            // Other events are untouched by the daily board.
            LeaderboardPage plain = Ask(LeaderboardPeriod.AllTime);
            LeaderboardPage viaDaily = null;
            _dailyBoard.Query(new LeaderboardQuery(Sprint, LeaderboardPeriod.AllTime, null, 0, 50), p => viaDaily = p, e => { });
            Assert.AreEqual(plain.Rows.Count, viaDaily.Rows.Count);
            for (int i = 0; i < plain.Rows.Count; i++)
                Assert.AreEqual(plain.Rows[i].ElapsedMilliseconds, viaDaily.Rows[i].ElapsedMilliseconds);
        }
    }
}
