using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Race;
using SpeedStars.Meta.Tickets;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>
    /// T-082 scenarios 2 and 4. Scenario 2: Race again with no Entry Ticket, over the REAL entry gate and ticket service (the
    /// fake-gate case is in RaceFlowLogicTests). Scenario 4: the opponents' traces become Analysis series, and a bot that
    /// finishes after the player gets its time instead of DNF. Every number is a test input; only relations are asserted.
    /// </summary>
    public sealed class RaceFlowRegressionTests
    {
        private static readonly RaceEventId PaidEvent = new RaceEventId("reg-paid");
        private const float RaceDistance = 100f;
        private const float StepMeters = 10f;
        private const float ToleranceMeters = 1f;
        private const double PlayerSeconds = 10d;

        private TicketTestKit _kit;
        private FakeSession _session;
        private FakeResults _results;
        private FakePrompt _prompt;
        private FakeTraces _traces;
        private ScreenNavigator _navigator;
        private SceneFlow _sceneFlow;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession { TargetDistance = RaceDistance, Distance = RaceDistance, RaceTime = PlayerSeconds };
            _results = new FakeResults { SplitStepMeters = StepMeters, FinishToleranceMeters = ToleranceMeters };
            _prompt = new FakePrompt();
            _traces = new FakeTraces();
            _navigator = new ScreenNavigator();
            _navigator.Push(new FakeScreen());
            _sceneFlow = new SceneFlow(new FakeLoader(), new SceneFlowScenes("TestHome", "TestRace"));
        }

        [TearDown]
        public void TearDown() => _kit?.Dispose();

        // ---- scenario 2: Race again with no ticket, real gate -------------------------------------------------------

        private RaceFlowLogic MakeWithRealGate(int startTickets, out RaceEntryGate gate)
        {
            _kit = new TicketTestKit(startTickets);
            var paid = _kit.Fixture.Create<RaceEventDefinition>();
            ConfigFixture.SetString(paid, "eventId", PaidEvent.Value);
            ConfigFixture.SetBool(paid, "requiresTicket", true);
            ConfigFixture.SetBool(paid, "isPlayable", true);
            var catalog = _kit.Fixture.Create<RaceEventCatalog>();
            ConfigFixture.SetObjects(catalog, "events", paid);
            var tickets = new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
            gate = new RaceEntryGate(_kit.Repository, _kit.Clock, catalog, tickets);

            var lookup = new FakeLookup();
            lookup.Add(new RaceEventInfo(PaidEvent, "Paid", RaceDistance));
            return new RaceFlowLogic(_session, gate, new FakeSelection { Selected = PaidEvent }, lookup, _navigator, _sceneFlow,
                _results, new FakeRewards(), _prompt);
        }

        [Test]
        public void RaceAgain_WithNoEntryTicketLeft_OpensTheGateWindow_SpendsNothing_AndDoesNotStartARace()
        {
            RaceFlowLogic flow = MakeWithRealGate(1, out RaceEntryGate gate);
            Assert.AreEqual(EntryResult.EnteredWithTicket, gate.Enter(PaidEvent)); // the last ticket buys this race
            flow.Start();
            _session.Finish(new RaceOutcome(PaidEvent, PlayerSeconds, RaceDistance, 1, 1));
            Assert.AreEqual(0, _kit.Progress.EntryTickets, "the test needs the stock at zero");
            int requests = _session.Requests.Count;
            int writes = _kit.Store.Writes;

            _results.RaiseRaceAgain();

            CollectionAssert.AreEqual(new[] { PaidEvent }, _prompt.Opened);
            Assert.AreEqual(0, _kit.Progress.EntryTickets);
            Assert.AreEqual(writes, _kit.Store.Writes, "a refused entry saves nothing");
            Assert.AreEqual(requests, _session.Requests.Count, "no race starts");
            Assert.IsFalse(gate.HasPendingEntry);
            Assert.IsTrue(flow.ResultsOpen, "the player stays on the Results screen");
        }

        [Test]
        public void RaceAgain_WithATicketLeft_SpendsExactlyOne_AndStartsTheRace_WithoutTheGateWindow()
        {
            RaceFlowLogic flow = MakeWithRealGate(2, out RaceEntryGate gate);
            Assert.AreEqual(EntryResult.EnteredWithTicket, gate.Enter(PaidEvent));
            flow.Start();
            _session.Finish(new RaceOutcome(PaidEvent, PlayerSeconds, RaceDistance, 1, 1));
            int before = _kit.Progress.EntryTickets;
            int requests = _session.Requests.Count;

            _results.RaiseRaceAgain();

            Assert.AreEqual(before - 1, _kit.Progress.EntryTickets);
            Assert.AreEqual(requests + 1, _session.Requests.Count);
            Assert.IsEmpty(_prompt.Opened);
        }

        [Test]
        public void RaceAgain_PressedAgainAfterTheGateWindowOpened_StillSpendsNothing()
        {
            RaceFlowLogic flow = MakeWithRealGate(1, out RaceEntryGate gate);
            gate.Enter(PaidEvent);
            flow.Start();
            _session.Finish(new RaceOutcome(PaidEvent, PlayerSeconds, RaceDistance, 1, 1));

            _results.RaiseRaceAgain();
            _results.RaiseRaceAgain();

            Assert.AreEqual(0, _kit.Progress.EntryTickets);
            Assert.AreEqual(1, _session.Requests.Count);
            Assert.AreEqual(2, _prompt.Opened.Count, "each press asks the window again; neither one spends");
        }

        // ---- scenario 4: bot traces in the Analysis data ------------------------------------------------------------

        private RaceFlowLogic MakeWithTraces()
        {
            var gate = new FakeGate { Pending = PaidEvent, HasPendingEntry = true };
            var lookup = new FakeLookup();
            lookup.Add(new RaceEventInfo(PaidEvent, "Paid", RaceDistance));
            var flow = new RaceFlowLogic(_session, gate, new FakeSelection { Selected = PaidEvent }, lookup, _navigator, _sceneFlow,
                _results, new FakeRewards(), _prompt, traces: _traces);
            flow.Start();
            _session.RaiseStarted();
            return flow;
        }

        private static RaceTrace FinishedBot(string name, double seconds) =>
            new RaceTrace(name, Samples(seconds), true, seconds);

        private static RaceTrace RunningBot(string name) =>
            new RaceTrace(name, new[] { new RaceTraceSample(30f, 4d), new RaceTraceSample(40f, 5d) }, false, 0d);

        private static RaceTraceSample[] Samples(double finishSeconds) => new[]
        {
            new RaceTraceSample(RaceDistance * 0.5f, finishSeconds * 0.5d),
            new RaceTraceSample(RaceDistance, finishSeconds),
        };

        private void FinishPlayer() =>
            _session.Finish(new RaceOutcome(PaidEvent, PlayerSeconds, RaceDistance, 1, 1));

        private static AnalysisRunner Find(AnalysisData data, string name)
        {
            foreach (AnalysisRunner r in data.Runners)
                if (r.Name == name) return r;
            Assert.Fail("no runner named " + name);
            return default;
        }

        [Test]
        public void TheBotsRuns_BecomeAnalysisRunners_NextToThePlayer()
        {
            MakeWithTraces();
            _traces.Set(FinishedBot("Bot A", PlayerSeconds + 2d), RunningBot("Bot B"));

            FinishPlayer();

            Assert.IsNotNull(_results.Analysis);
            Assert.AreEqual(1 + 2, _results.Analysis.Runners.Count);
            Assert.AreEqual(1, CountPlayers(_results.Analysis));
            Assert.AreEqual(_traces.Traces.Count + 1, _results.Analysis.Runners.Count);
        }

        [Test]
        public void ABotWhoseTraceIsFinished_HasItsTimeAndAPlace()
        {
            MakeWithTraces();
            _traces.Set(FinishedBot("Bot A", PlayerSeconds + 2d));

            FinishPlayer();

            AnalysisRunner bot = Find(_results.Analysis, "Bot A");
            Assert.IsTrue(bot.Finished);
            Assert.AreNotEqual(AnalysisModel.DidNotFinish, bot.TimeText);
            Assert.Greater(bot.Rank, 0);
        }

        [Test]
        public void ABotStillRunningWhenThePlayerFinishes_IsDnfSoFar_WithNoPlace()
        {
            MakeWithTraces();
            _traces.Set(RunningBot("Bot B"));

            FinishPlayer();

            AnalysisRunner bot = Find(_results.Analysis, "Bot B");
            Assert.IsFalse(bot.Finished);
            Assert.AreEqual(AnalysisModel.DidNotFinish, bot.TimeText);
            Assert.AreEqual(0, bot.Rank);
        }

        [Test]
        public void ABotCrossingTheLineAfterThePlayer_GetsATimeInsteadOfDnf_WhileResultsIsUp()
        {
            MakeWithTraces();
            _traces.Set(RunningBot("Bot B"));
            FinishPlayer();
            int redraws = _results.AnalysisCalls;
            Assert.AreEqual(AnalysisModel.DidNotFinish, Find(_results.Analysis, "Bot B").TimeText);

            _traces.Set(FinishedBot("Bot B", PlayerSeconds + 1.5d));
            _traces.RaiseChanged();

            Assert.AreEqual(redraws + 1, _results.AnalysisCalls);
            AnalysisRunner bot = Find(_results.Analysis, "Bot B");
            Assert.IsTrue(bot.Finished);
            Assert.AreNotEqual(AnalysisModel.DidNotFinish, bot.TimeText);
            Assert.AreEqual(PlayerSeconds + 1.5d, bot.Series.FinishSeconds, 1e-6);
            Assert.AreEqual(ResultsTexts.Time(bot.Series.FinishSeconds), bot.TimeText);
        }

        [Test]
        public void ARunnerWhoFinishedFaster_RanksAbove_AndASlowerOneBelow_ThePlayer()
        {
            MakeWithTraces();
            _traces.Set(FinishedBot("Fast", PlayerSeconds - 1d), FinishedBot("Slow", PlayerSeconds + 1d));

            FinishPlayer();

            AnalysisData data = _results.Analysis;
            AnalysisRunner player = data.Runners[data.PlayerIndex];
            Assert.Less(Find(data, "Fast").Rank, player.Rank);
            Assert.Greater(Find(data, "Slow").Rank, player.Rank);
            for (int i = 1; i < data.Runners.Count; i++)
                if (data.Runners[i].Finished)
                    Assert.GreaterOrEqual(data.Runners[i].Series.FinishSeconds, data.Runners[i - 1].Series.FinishSeconds);
        }

        [Test]
        public void TheBotsLines_AreRecordedOnTheSameStepAsThePlayers()
        {
            MakeWithTraces();
            _traces.Set(FinishedBot("Bot A", PlayerSeconds + 2d));

            FinishPlayer();

            SplitSeries player = _results.Analysis.Runners[_results.Analysis.PlayerIndex].Series;
            SplitSeries bot = Find(_results.Analysis, "Bot A").Series;
            Assert.AreEqual(player.Points.Count, bot.Points.Count);
            for (int i = 0; i < player.Points.Count; i++)
                Assert.AreEqual(player.Points[i].Distance, bot.Points[i].Distance, 1e-3f);
        }

        [Test]
        public void ATraceChange_BeforeTheFinish_DrawsNothing()
        {
            MakeWithTraces();
            _traces.Set(FinishedBot("Bot A", PlayerSeconds - 3d));

            _traces.RaiseChanged();

            Assert.AreEqual(0, _results.AnalysisCalls);
        }

        [Test]
        public void ATraceChange_AfterANewRaceStarted_DoesNotRedrawTheOldResults()
        {
            MakeWithTraces();
            _traces.Set(RunningBot("Bot B"));
            FinishPlayer();
            _results.RaiseRaceAgain(); // the next race begins: the old Results data is no longer redrawn
            int calls = _results.AnalysisCalls;

            _traces.Set(FinishedBot("Bot B", PlayerSeconds + 1d));
            _traces.RaiseChanged();

            Assert.AreEqual(calls, _results.AnalysisCalls);
        }

        [Test]
        public void AfterDispose_ATraceChange_DoesNotRedraw()
        {
            RaceFlowLogic flow = MakeWithTraces();
            _traces.Set(RunningBot("Bot B"));
            FinishPlayer();
            int calls = _results.AnalysisCalls;

            flow.Dispose();
            _traces.Set(FinishedBot("Bot B", PlayerSeconds + 1d));
            _traces.RaiseChanged();

            Assert.AreEqual(calls, _results.AnalysisCalls);
            Assert.AreEqual(0, _traces.SubscriberCount);
        }

        [Test]
        public void WithoutATraceSource_TheAnalysisHoldsThePlayerAlone()
        {
            var gate = new FakeGate { Pending = PaidEvent, HasPendingEntry = true };
            var lookup = new FakeLookup();
            lookup.Add(new RaceEventInfo(PaidEvent, "Paid", RaceDistance));
            var flow = new RaceFlowLogic(_session, gate, new FakeSelection { Selected = PaidEvent }, lookup, _navigator, _sceneFlow,
                _results, new FakeRewards(), _prompt);
            flow.Start();
            _session.RaiseStarted();

            FinishPlayer();

            Assert.AreEqual(1, _results.Analysis.Runners.Count);
            Assert.IsTrue(_results.Analysis.Runners[0].IsPlayer);
        }

        private static int CountPlayers(AnalysisData data)
        {
            int n = 0;
            foreach (AnalysisRunner r in data.Runners)
                if (r.IsPlayer) n++;
            return n;
        }

        // ---- fakes ---------------------------------------------------------------------------------------------------

        private sealed class FakeTraces : IRaceTraces
        {
            private IReadOnlyList<RaceTrace> _traces = new RaceTrace[0];
            private Action _changed;

            public IReadOnlyList<RaceTrace> Traces => _traces;
            public int SubscriberCount => _changed == null ? 0 : _changed.GetInvocationList().Length;
            public event Action TracesChanged { add { _changed += value; } remove { _changed -= value; } }

            public void Set(params RaceTrace[] traces) => _traces = traces;
            public void RaiseChanged() => _changed?.Invoke();
        }

        private sealed class FakeSession : IRaceSession
        {
            public readonly List<RaceStartRequest> Requests = new List<RaceStartRequest>();
            public RaceSessionState State { get; set; }
            public double RaceTime { get; set; }
            public float Distance { get; set; }
            public float TargetDistance { get; set; }
            public event Action Started;
            public event Action<RaceOutcome> Finished;

            public void RequestStart(RaceStartRequest request) => Requests.Add(request);
            public void Finish(RaceOutcome outcome) => Finished?.Invoke(outcome);
            public void RaiseStarted() => Started?.Invoke();
        }

        private sealed class FakeGate : IRaceEntryGate
        {
            public RaceEventId Pending;
            public bool HasPendingEntry { get; set; }
            public EntryResult Enter(RaceEventId id) => EntryResult.EnteredFree;

            public bool TryTakePending(out RaceEventId id)
            {
                id = Pending;
                if (!HasPendingEntry) return false;
                HasPendingEntry = false;
                return true;
            }
        }

        private sealed class FakeSelection : IRaceSelection
        {
            public RaceEventId Selected { get; set; }
            public void Select(RaceEventId id) => Selected = id;
            public event Action Changed { add { } remove { } }
        }

        private sealed class FakeLookup : IRaceEventLookup
        {
            private readonly Dictionary<string, RaceEventInfo> _events = new Dictionary<string, RaceEventInfo>();
            public void Add(RaceEventInfo info) => _events[info.Id.Value] = info;
            public bool TryGet(RaceEventId id, out RaceEventInfo info) => _events.TryGetValue(id.Value, out info);
        }

        private sealed class FakeScreen : IUiView
        {
            public void Show() { }
            public void Hide() { }
        }

        private sealed class FakeRewards : IRaceRewardResolver
        {
            public int Resolve(RaceOutcome outcome, string payoutId) => 0;
        }

        private sealed class FakePrompt : IEntryGatePrompt
        {
            public readonly List<RaceEventId> Opened = new List<RaceEventId>();
            public void Open(RaceEventId id) => Opened.Add(id);
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public bool Load(string sceneName, Action onLoaded) => true;
        }

        private sealed class FakeResults : IResultsView, IAnalysisSink
        {
            public AnalysisData Analysis;
            public int AnalysisCalls;
            public float SplitStepMeters { get; set; }
            public float FinishToleranceMeters { get; set; }
            public event Action RaceAgainRequested;
            public event Action MainMenuRequested;

            public void Present(ResultsData data) { }
            public void PresentAnalysis(AnalysisData data)
            {
                Analysis = data;
                AnalysisCalls++;
            }

            public void NotifyDenied() { }
            public void Show() { }
            public void Hide() { }
            public void RaiseRaceAgain() => RaceAgainRequested?.Invoke();
            public void RaiseMainMenu() => MainMenuRequested?.Invoke();
        }
    }
}
