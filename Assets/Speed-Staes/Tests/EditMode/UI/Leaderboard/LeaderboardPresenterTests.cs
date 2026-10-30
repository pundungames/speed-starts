using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Leaderboard;

namespace SpeedStars.Tests.UI.Leaderboard
{
    public sealed class LeaderboardPresenterTests
    {
        private static readonly RaceEventId Sprint = new RaceEventId("sprint_a");
        private static readonly RaceEventId Hurdle = new RaceEventId("hurdle_a");

        /// <summary>Answers by hand (Flush) so tests decide the order of replies; Auto answers at once like the local source.</summary>
        private sealed class FakeLeaderboard : ILeaderboard
        {
            public readonly List<LeaderboardQuery> Queries = new List<LeaderboardQuery>();
            private readonly List<(LeaderboardQuery Query, Action<LeaderboardPage> Done, Action<string> Fail)> _pending =
                new List<(LeaderboardQuery, Action<LeaderboardPage>, Action<string>)>();

            public bool Auto;
            public bool AutoFail;
            public int Total = 25;
            public LeaderboardRow Own;

            public event Action Changed { add { } remove { } }

            public void Query(LeaderboardQuery query, Action<LeaderboardPage> onDone, Action<string> onError)
            {
                Queries.Add(query);
                if (Auto)
                {
                    if (AutoFail) onError("down");
                    else onDone(Page(query));
                    return;
                }
                _pending.Add((query, onDone, onError));
            }

            public int PendingCount => _pending.Count;

            public void Answer(int index) => _pending[index].Done(Page(_pending[index].Query));

            public void Fail(int index) => _pending[index].Fail("down");

            public LeaderboardPage Page(LeaderboardQuery query)
            {
                var rows = new List<LeaderboardRow>();
                for (int i = query.Offset; i < Math.Min(Total, query.Offset + query.Limit); i++)
                    rows.Add(Row(i));
                int next = query.Offset + query.Limit < Total ? query.Offset + query.Limit : -1;
                return new LeaderboardPage(rows, Total, next, Own, DateTime.UtcNow);
            }

            public bool TryGetStanding(RaceEventId eventId, out int rank, out int total) { rank = 0; total = 0; return false; }
            public bool TryGetBestOfDay(RaceEventId eventId, out LeaderboardRow best) { best = null; return false; }
        }

        private static LeaderboardRow Row(int index, bool local = false) =>
            new LeaderboardRow("e" + index, index + 1, "P" + index, "TR", 10000 + index * 10, new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc), local);

        private static LeaderboardPresenter Open(FakeLeaderboard fake, int page = 10)
        {
            var presenter = new LeaderboardPresenter(fake, page);
            presenter.Open(Sprint);
            return presenter;
        }

        [Test]
        public void Open_StartsGlobalAllTimeOnTheGivenEvent_AndShowsLoadingUntilAnswered()
        {
            var fake = new FakeLeaderboard();
            LeaderboardPresenter presenter = Open(fake);

            Assert.IsNull(presenter.CountryCode);
            Assert.AreEqual(LeaderboardPeriod.AllTime, presenter.Period);
            Assert.AreEqual(Sprint, presenter.EventId);
            Assert.AreEqual(LeaderboardState.Loading, presenter.State);
            Assert.AreEqual(0, presenter.Rows.Count);
            Assert.AreEqual(0, fake.Queries[0].Offset);
        }

        [Test]
        public void OlderAnswer_AfterAFilterChange_IsIgnored()
        {
            var fake = new FakeLeaderboard();
            LeaderboardPresenter presenter = Open(fake);
            presenter.SetPeriod(LeaderboardPeriod.Today); // second request, the first one is stale now

            fake.Answer(0);
            Assert.AreEqual(LeaderboardState.Loading, presenter.State, "stale reply must not show rows");
            Assert.AreEqual(0, presenter.Rows.Count);

            fake.Answer(1);
            Assert.AreEqual(LeaderboardState.Content, presenter.State);
            Assert.Greater(presenter.Rows.Count, 0);
        }

        [Test]
        public void FilterChange_StartsANewRequestId_AndClearsRows()
        {
            var fake = new FakeLeaderboard { Auto = true };
            LeaderboardPresenter presenter = Open(fake);
            int before = presenter.RequestId;
            Assert.Greater(presenter.Rows.Count, 0);

            fake.Auto = false;
            presenter.SetCountry("TR");
            Assert.Greater(presenter.RequestId, before);
            Assert.AreEqual(0, presenter.Rows.Count, "no stale rows under the new filter");
            Assert.AreEqual("TR", fake.Queries[fake.Queries.Count - 1].CountryCode);
            Assert.AreEqual(0, fake.Queries[fake.Queries.Count - 1].Offset);
        }

        [Test]
        public void SettingTheSameFilter_DoesNotReload()
        {
            var fake = new FakeLeaderboard { Auto = true };
            LeaderboardPresenter presenter = Open(fake);
            int queries = fake.Queries.Count;

            presenter.SetPeriod(LeaderboardPeriod.AllTime);
            presenter.SetCountry(null);
            presenter.ApplyEvent(Sprint);
            Assert.AreEqual(queries, fake.Queries.Count);
        }

        [Test]
        public void UkIsTreatedAsGb()
        {
            var fake = new FakeLeaderboard { Auto = true };
            LeaderboardPresenter presenter = Open(fake);
            presenter.SetCountry("uk");
            Assert.AreEqual("GB", presenter.CountryCode);
            Assert.AreEqual("GB", fake.Queries[fake.Queries.Count - 1].CountryCode);
        }

        [Test]
        public void Pages_AreMerged_WithoutDuplicates_AndStopAtTheEnd()
        {
            var fake = new FakeLeaderboard { Auto = true, Total = 25 };
            LeaderboardPresenter presenter = Open(fake, 10);
            Assert.AreEqual(10, presenter.Rows.Count);

            presenter.LoadMore();
            presenter.LoadMore();
            Assert.AreEqual(25, presenter.Rows.Count);
            Assert.IsFalse(presenter.CanLoadMore);

            var ids = new HashSet<string>();
            foreach (LeaderboardRow row in presenter.Rows) Assert.IsTrue(ids.Add(row.EntryId), "duplicate " + row.EntryId);
        }

        [Test]
        public void LoadMore_WhilePageIsOnItsWay_AsksOnlyOnce()
        {
            var fake = new FakeLeaderboard { Total = 40 };
            LeaderboardPresenter presenter = Open(fake, 10);
            fake.Answer(0);

            presenter.LoadMore();
            presenter.LoadMore();
            Assert.AreEqual(2, fake.Queries.Count);
            Assert.IsTrue(presenter.IsLoadingMore);
            Assert.AreEqual(10, fake.Queries[1].Offset);
        }

        [Test]
        public void MoreRowsAnswer_ThatRepeatsEntries_AddsEachOnce()
        {
            var fake = new FakeLeaderboard { Total = 40 };
            LeaderboardPresenter presenter = Open(fake, 10);
            fake.Answer(0);
            presenter.LoadMore();
            fake.Answer(1);
            // a second reply for the same page (source repeated itself) cannot be applied: it is the same request id, same entries
            presenter.LoadMore();
            fake.Answer(2);
            Assert.AreEqual(30, presenter.Rows.Count);
        }

        [Test]
        public void LoadMoreReply_ForAnOldFilter_IsDropped()
        {
            var fake = new FakeLeaderboard { Total = 40 };
            LeaderboardPresenter presenter = Open(fake, 10);
            fake.Answer(0);
            presenter.LoadMore();          // request 1: page 2
            presenter.SetPeriod(LeaderboardPeriod.ThisWeek); // request 2: new filter
            fake.Answer(1);                // late page 2 of the old filter
            Assert.AreEqual(0, presenter.Rows.Count);
            Assert.AreEqual(LeaderboardState.Loading, presenter.State);
        }

        [Test]
        public void NoRows_IsEmpty_AndError_ShowsErrorUntilRetry()
        {
            var fake = new FakeLeaderboard { Auto = true, Total = 0 };
            LeaderboardPresenter presenter = Open(fake);
            Assert.AreEqual(LeaderboardState.Empty, presenter.State);

            fake.Total = 5;
            fake.AutoFail = true;
            presenter.Retry();
            Assert.AreEqual(LeaderboardState.Error, presenter.State);

            presenter.SetPeriod(LeaderboardPeriod.Today);
            presenter.SetPeriod(LeaderboardPeriod.AllTime);
            fake.AutoFail = false;
            presenter.Retry();
            Assert.AreEqual(LeaderboardState.Content, presenter.State);
        }

        [Test]
        public void Retry_KeepsTheFilters()
        {
            var fake = new FakeLeaderboard { Auto = true, AutoFail = true };
            LeaderboardPresenter presenter = Open(fake);
            presenter.SetPeriod(LeaderboardPeriod.ThisMonth);
            presenter.SetCountry("DE");
            fake.AutoFail = false;
            presenter.Retry();

            LeaderboardQuery last = fake.Queries[fake.Queries.Count - 1];
            Assert.AreEqual(LeaderboardPeriod.ThisMonth, last.Period);
            Assert.AreEqual("DE", last.CountryCode);
            Assert.AreEqual(Sprint, last.EventId);
        }

        [Test]
        public void ErrorWhileLoadingMore_KeepsTheRows()
        {
            var fake = new FakeLeaderboard { Total = 40 };
            LeaderboardPresenter presenter = Open(fake, 10);
            fake.Answer(0);
            presenter.LoadMore();
            fake.Fail(1);

            Assert.AreEqual(LeaderboardState.Content, presenter.State);
            Assert.AreEqual(10, presenter.Rows.Count);
            Assert.IsFalse(presenter.IsLoadingMore);
            Assert.IsTrue(presenter.CanLoadMore);
        }

        [Test]
        public void NoEvent_ShowsEmpty_WithoutAsking()
        {
            var fake = new FakeLeaderboard { Auto = true };
            var presenter = new LeaderboardPresenter(fake, 10);
            presenter.Open(default);
            Assert.AreEqual(LeaderboardState.Empty, presenter.State);
            Assert.AreEqual(0, fake.Queries.Count);
        }

        [Test]
        public void MissingSource_ShowsError()
        {
            var presenter = new LeaderboardPresenter(null, 10);
            presenter.Open(Sprint);
            Assert.AreEqual(LeaderboardState.Error, presenter.State);
        }

        [Test]
        public void FindMe_NotRanked_IsDisabled_AndYouRowSaysNotRanked()
        {
            var fake = new FakeLeaderboard { Auto = true, Own = null };
            LeaderboardPresenter presenter = Open(fake);
            Assert.IsFalse(presenter.CanFindMe);

            LeaderboardRowData you = LeaderboardFormat.You(presenter.OwnEntry, null);
            Assert.AreEqual(LeaderboardTexts.NotRanked, you.Name);
            Assert.AreEqual(LeaderboardTexts.NoValue, you.TimeText);
            Assert.AreEqual(LeaderboardTexts.NoValue, you.DateText);
        }

        [Test]
        public void FindMe_LoadsPagesUntilTheOwnRowArrives_ThenScrollsToIt()
        {
            var fake = new FakeLeaderboard { Auto = true, Total = 35 };
            fake.Own = new LeaderboardRow("e27", 28, "Me", "TR", 10270, DateTime.UtcNow, true);
            LeaderboardPresenter presenter = Open(fake, 10);
            Assert.IsTrue(presenter.CanFindMe);

            int scrolledTo = -1;
            presenter.ScrollToRowRequested += i => scrolledTo = i;
            presenter.FindMe();

            Assert.AreEqual(27, scrolledTo);
            Assert.AreEqual(30, presenter.Rows.Count, "whole pages are loaded until the own row is among them");
        }

        [Test]
        public void FindMe_WhenAlreadyLoaded_ScrollsWithoutAsking()
        {
            var fake = new FakeLeaderboard { Auto = true, Total = 35 };
            fake.Own = new LeaderboardRow("e3", 4, "Me", "TR", 10030, DateTime.UtcNow, true);
            LeaderboardPresenter presenter = Open(fake, 10);
            int queries = fake.Queries.Count;

            int scrolledTo = -1;
            presenter.ScrollToRowRequested += i => scrolledTo = i;
            presenter.FindMe();
            Assert.AreEqual(3, scrolledTo);
            Assert.AreEqual(queries, fake.Queries.Count);
        }

        [Test]
        public void YouRow_ShowsTheRealRank()
        {
            var own = new LeaderboardRow("x", 128, "Me", "TR", 11240, new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), true);
            LeaderboardRowData you = LeaderboardFormat.You(own, null);
            Assert.AreEqual("128", you.RankText);
            Assert.AreEqual(LeaderboardTexts.You, you.Name);
            Assert.AreEqual("11.24 s", you.TimeText);
            Assert.AreEqual("26 OCT 2026", you.DateText);
        }

        // ---------------------------------------------------------------- formats

        [Test]
        public void Time_HasTwoDecimals_AndNeverRoundsUp()
        {
            Assert.AreEqual("10.42 s", LeaderboardFormat.Time(10420));
            Assert.AreEqual("10.42 s", LeaderboardFormat.Time(10429));
            Assert.AreEqual("9.05 s", LeaderboardFormat.Time(9050));
            Assert.AreEqual("1:02.05 s", LeaderboardFormat.Time(62050));
            Assert.AreEqual("0.00 s", LeaderboardFormat.Time(-5));
        }

        [Test]
        public void Date_IsTheUtcDay_WithEnglishMonth()
        {
            Assert.AreEqual("28 OCT 2026", LeaderboardFormat.Date(new DateTime(2026, 10, 28, 23, 59, 0, DateTimeKind.Utc)));
            Assert.AreEqual("08 OCT 2026", LeaderboardFormat.Date(new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual("01 JAN 2027", LeaderboardFormat.Date(new DateTime(2027, 1, 1, 0, 0, 1, DateTimeKind.Utc)));
        }

        [Test]
        public void Row_UsesTheServersRank()
        {
            LeaderboardRowData data = LeaderboardFormat.Row(Row(41));
            Assert.AreEqual("42", data.RankText);
            Assert.AreEqual("P41", data.Name);
        }

        [Test]
        public void PeriodLabels_AreDistinct()
        {
            var seen = new HashSet<string>();
            foreach (LeaderboardPeriod period in Enum.GetValues(typeof(LeaderboardPeriod)))
                Assert.IsTrue(seen.Add(LeaderboardTexts.Period(period)));
        }

        // ---------------------------------------------------------------- event picker

        private static List<LeaderboardEventGroup> Groups()
        {
            var layout = new List<LeaderboardEventLayoutEntry>
            {
                new LeaderboardEventLayoutEntry("SPRINTS", new[] { "sprint_a", "sprint_b", "gone" }),
                new LeaderboardEventLayoutEntry("HURDLES", new[] { "hurdle_a" }),
                new LeaderboardEventLayoutEntry("RELAYS", new[] { "relay_missing" }),
            };
            return LeaderboardEventGroups.Build(layout, id => id.EndsWith("_a") || id.EndsWith("_b") ? id.ToUpperInvariant() : null);
        }

        [Test]
        public void Groups_LeaveOutRacesThatAreNotOnOffer()
        {
            List<LeaderboardEventGroup> groups = Groups();
            Assert.AreEqual(3, groups.Count);
            Assert.AreEqual(2, groups[0].Options.Count);
            Assert.AreEqual(0, groups[2].Options.Count);
        }

        [Test]
        public void Resolve_PrefersTheGivenRace_ElseTheFirstOnOffer_ElseNothing()
        {
            List<LeaderboardEventGroup> groups = Groups();
            Assert.AreEqual(Hurdle, LeaderboardEventGroups.Resolve(groups, Hurdle));
            Assert.AreEqual(Sprint, LeaderboardEventGroups.Resolve(groups, new RaceEventId("unknown")));
            Assert.AreEqual(Sprint, LeaderboardEventGroups.Resolve(groups, default));
            Assert.IsTrue(LeaderboardEventGroups.Resolve(new List<LeaderboardEventGroup>(), Sprint).IsEmpty);
        }

        [Test]
        public void FilterLabel_JoinsCategoryAndDistance()
        {
            string label = LeaderboardEventGroups.FilterLabel(Groups(), Sprint);
            Assert.IsTrue(label.StartsWith("SPRINTS"));
            Assert.IsTrue(label.EndsWith("SPRINT_A"));
            Assert.AreEqual(string.Empty, LeaderboardEventGroups.FilterLabel(Groups(), new RaceEventId("zzz")));
        }

        [Test]
        public void Picker_BeginsOnTheActiveRace_AndAllowsApply()
        {
            var picker = new LeaderboardEventPicker(Groups());
            picker.Begin(Hurdle);
            Assert.AreEqual(1, picker.Category);
            Assert.AreEqual(Hurdle, picker.Chosen);
            Assert.IsTrue(picker.CanApply);
        }

        [Test]
        public void Picker_ChangingCategory_DoesNotCarryTheDistance_AndDisablesApply()
        {
            var picker = new LeaderboardEventPicker(Groups());
            picker.Begin(Sprint);
            picker.SelectCategory(1);
            Assert.IsFalse(picker.CanApply);
            Assert.IsTrue(picker.Chosen.IsEmpty);

            Assert.IsFalse(picker.Choose(Sprint), "a race of another category cannot be chosen");
            Assert.IsTrue(picker.Choose(Hurdle));
            Assert.IsTrue(picker.CanApply);
        }

        [Test]
        public void Picker_EmptyCategory_HasNoOptions_AndNoApply()
        {
            var picker = new LeaderboardEventPicker(Groups());
            picker.Begin(Sprint);
            picker.SelectCategory(2);
            Assert.AreEqual(0, picker.Options.Count);
            Assert.IsFalse(picker.CanApply);
        }

        [Test]
        public void Picker_UnknownActiveRace_StartsWithNothingChosen()
        {
            var picker = new LeaderboardEventPicker(Groups());
            picker.Begin(new RaceEventId("zzz"));
            Assert.AreEqual(0, picker.Category);
            Assert.IsFalse(picker.CanApply);
        }

        // ---------------------------------------------------------------- country search

        [Test]
        public void CountrySearch_IgnoresCaseAndAccents_AndEmptyListsAll()
        {
            var all = new List<LeaderboardCountry>
            {
                new LeaderboardCountry("TR", "Türkiye"),
                new LeaderboardCountry("GB", "United Kingdom"),
                new LeaderboardCountry("DE", "Germany"),
            };
            Assert.AreEqual(3, LeaderboardCountryList.Filter(all, "").Count);
            Assert.AreEqual(3, LeaderboardCountryList.Filter(all, null).Count);
            Assert.AreEqual("TR", LeaderboardCountryList.Filter(all, "turk")[0].Code);
            Assert.AreEqual("TR", LeaderboardCountryList.Filter(all, "TÜRK")[0].Code);
            Assert.AreEqual("GB", LeaderboardCountryList.Filter(all, "KINGD")[0].Code);
            Assert.AreEqual(0, LeaderboardCountryList.Filter(all, "xyz").Count);
            Assert.AreEqual("TURKIYE", LeaderboardCountryList.Normalize("Türkiye"));
        }

        // ---------------------------------------------------------------- results cards

        [Test]
        public void Percentile_RoundsUp_AndStaysBetween1And100()
        {
            Assert.AreEqual("TOP 1%", ResultsLeaderboardCards.Percentile(1, 1000));
            Assert.AreEqual("TOP 9%", ResultsLeaderboardCards.Percentile(81, 1000)); // 8.1% rounds up
            Assert.AreEqual("TOP 100%", ResultsLeaderboardCards.Percentile(50, 50));
            Assert.AreEqual("TOP 50%", ResultsLeaderboardCards.Percentile(5, 10));
            Assert.AreEqual("#7", ResultsLeaderboardCards.Position(7));
        }

        [Test]
        public void Cards_WithoutData_KeepTheDashes()
        {
            ResultsCardTexts texts = ResultsLeaderboardCards.Build(new FakeLeaderboard(), Sprint);
            Assert.AreEqual(SpeedStars.UI.Race.ResultsTexts.NoValue, texts.WorldPosition);
            Assert.AreEqual(SpeedStars.UI.Race.ResultsTexts.NoValue, texts.Percentile);
            Assert.AreEqual(SpeedStars.UI.Race.ResultsTexts.NoDailyBest, texts.BestOfTheDay);
            Assert.AreEqual(SpeedStars.UI.Race.ResultsTexts.NoValue, ResultsLeaderboardCards.Build(null, Sprint).WorldPosition);
        }
    }
}
