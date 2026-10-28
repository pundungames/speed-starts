using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Race;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>RaceFlowLogic decisions with fake session, gate, lookup and views (no scene, no Zenject, no DOTween).</summary>
    public sealed class RaceFlowLogicTests
    {
        private static readonly RaceEventId Dash = new RaceEventId("dash100");
        private static readonly RaceEventId Other = new RaceEventId("dash400");

        private FakeSession _session;
        private FakeGate _gate;
        private FakeSelection _selection;
        private FakeLookup _lookup;
        private FakeResults _results;
        private FakeScreen _hud;
        private FakeLoader _loader;
        private ScreenNavigator _navigator;
        private SceneFlow _sceneFlow;
        private FakePrompt _prompt;
        private FakeRewards _rewards;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession();
            _gate = new FakeGate();
            _selection = new FakeSelection { Selected = Other };
            _lookup = new FakeLookup();
            _lookup.Add(new RaceEventInfo(Dash, "100m dash", 100f));
            _lookup.Add(new RaceEventInfo(Other, "400m dash", 400f));
            _results = new FakeResults();
            _hud = new FakeScreen();
            _loader = new FakeLoader();
            _navigator = new ScreenNavigator();
            _navigator.Push(_hud);
            _sceneFlow = new SceneFlow(_loader, new SceneFlowScenes("TestHome", "TestRace"));
            _prompt = new FakePrompt();
            _rewards = new FakeRewards();
        }

        private RaceFlowLogic Make(bool withPrompt = true) =>
            new RaceFlowLogic(_session, _gate, _selection, _lookup, _navigator, _sceneFlow, _results, _rewards, withPrompt ? _prompt : null);

        private static RaceOutcome Outcome(RaceEventId id, double seconds) => new RaceOutcome(id, seconds, 100f, 1, 1);

        [Test]
        public void Start_WithPendingEntry_StartsThatEventWithTheCatalogDistance()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;

            Make().Start();

            Assert.AreEqual(1, _session.Requests.Count);
            Assert.AreEqual(Dash, _session.Requests[0].EventId);
            Assert.AreEqual(100f, _session.Requests[0].DistanceMeters);
        }

        [Test]
        public void Start_WithoutPendingEntry_StartsTheSelectedEventForFreeAndWarnsOnce()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No pending entry"));

            Make().Start();

            Assert.AreEqual(Other, _session.Requests[0].EventId);
            Assert.AreEqual(400f, _session.Requests[0].DistanceMeters);
            Assert.AreEqual(0, _gate.EnterCalls.Count, "a direct start never goes through the gate");
        }

        [Test]
        public void Start_CalledTwice_StartsOnce()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            var flow = Make();

            flow.Start();
            flow.Start();

            Assert.AreEqual(1, _session.Requests.Count);
        }

        [Test]
        public void Start_UnknownEvent_LogsErrorAndDoesNotStart()
        {
            _gate.Pending = new RaceEventId("nope");
            _gate.HasPendingEntry = true;
            LogAssert.Expect(LogType.Error, new Regex("not in the catalog"));

            Make().Start();

            Assert.AreEqual(0, _session.Requests.Count);
        }

        [Test]
        public void Finished_PresentsTheResultAndShowsTheResultsScreenOnTop()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            var flow = Make();
            flow.Start();

            _session.Finish(Outcome(Dash, 9.589));

            Assert.AreEqual("100m dash", _results.Presented.EventTitle);
            Assert.AreEqual(1, _results.Presented.Rows.Count);
            Assert.AreEqual("9.58s", _results.Presented.Rows[0].TimeText);
            Assert.IsTrue(_results.Presented.Rows[0].IsPlayer);
            Assert.AreSame(_results, _navigator.CurrentScreen);
            Assert.IsTrue(flow.ResultsOpen);
        }

        [Test]
        public void Finished_RecordsTheTimeOnce_AndPresentsTheNewRecord()
        {
            var records = new FakeRecords();
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            var flow = new RaceFlowLogic(_session, _gate, _selection, _lookup, _navigator, _sceneFlow, _results, _rewards, _prompt, records);
            flow.Start();

            _session.Finish(Outcome(Dash, 9.589));
            _session.Finish(Outcome(Dash, 7d)); // a repeated finish of the same race

            Assert.AreEqual(1, records.Calls.Count);
            Assert.AreEqual(Dash, records.Calls[0].Event);
            Assert.AreEqual(9.589, records.Calls[0].Seconds, 1e-9);
            Assert.IsFalse(string.IsNullOrEmpty(records.Calls[0].RaceId));
            Assert.IsTrue(_results.Presented.IsNewRecord);
            Assert.AreEqual("9.58s", _results.Presented.PersonalBestValue);
        }

        [Test]
        public void Finished_WithoutRecords_ShowsNoBestAndNoRecord()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            Make().Start();

            _session.Finish(Outcome(Dash, 9.589));

            Assert.IsFalse(_results.Presented.IsNewRecord);
            Assert.AreEqual(ResultsTexts.NoValue, _results.Presented.PersonalBestValue);
        }

        [Test]
        public void Finished_Twice_ShowsResultsOnce()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            Make().Start();

            _session.Finish(Outcome(Dash, 10d));
            _session.Finish(Outcome(Dash, 11d));

            Assert.AreEqual(1, _results.PresentCount);
        }

        [Test]
        public void Finished_PaysTheRewardOnce_WithTheRaceOutcome_AndShowsTheGems()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            _rewards.Gems = 3;
            Make().Start();

            RaceOutcome outcome = Outcome(Dash, 9.5);
            _session.Finish(outcome);
            _session.Finish(outcome); // a repeated finish of the same race

            Assert.AreEqual(1, _rewards.Calls.Count);
            Assert.AreEqual(Dash, _rewards.Calls[0].Outcome.EventId);
            Assert.AreEqual(3, _results.GemsShown);
        }

        [Test]
        public void Finished_NoGems_TellsTheScreenZero()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            _rewards.Gems = 0;
            Make().Start();

            _session.Finish(Outcome(Dash, 9.5));

            Assert.AreEqual(0, _results.GemsShown);
            Assert.AreEqual(1, _results.GemCalls);
        }

        [Test]
        public void EachRace_UsesItsOwnPayoutId()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            Make().Start();
            _session.Finish(Outcome(Dash, 9.5));
            _gate.NextResult = EntryResult.EnteredFree;
            _results.RaiseRaceAgain();

            _session.Finish(Outcome(Dash, 9.7));

            Assert.AreEqual(2, _rewards.Calls.Count);
            Assert.IsNotEmpty(_rewards.Calls[0].PayoutId);
            Assert.AreNotEqual(_rewards.Calls[0].PayoutId, _rewards.Calls[1].PayoutId);
        }

        [TestCase(EntryResult.EnteredFree)]
        [TestCase(EntryResult.EnteredWithTicket)]
        [TestCase(EntryResult.EnteredWithTimedAccess)]
        public void RaceAgain_WhenEntered_TakesTheEntryRestartsAndReturnsToTheHud(EntryResult entered)
        {
            StartAndFinish();
            _gate.NextResult = entered;

            _results.RaiseRaceAgain();

            CollectionAssert.AreEqual(new[] { Dash }, _gate.EnterCalls);
            Assert.AreEqual(1, _gate.TakeAfterEnter, "the confirmed entry must be taken or the next Enter would not pay");
            Assert.AreEqual(2, _session.Requests.Count);
            Assert.AreEqual(Dash, _session.Requests[1].EventId);
            Assert.AreSame(_hud, _navigator.CurrentScreen);
        }

        [Test]
        public void RaceAgain_PressedTwice_StartsOnce()
        {
            StartAndFinish();
            _gate.NextResult = EntryResult.EnteredWithTicket;

            _results.RaiseRaceAgain();
            _results.RaiseRaceAgain();

            Assert.AreEqual(1, _gate.EnterCalls.Count);
            Assert.AreEqual(2, _session.Requests.Count);
        }

        [Test]
        public void RaceAgain_NoTicket_OpensTheEntryGateModalAndDoesNotRestart()
        {
            StartAndFinish();
            _gate.NextResult = EntryResult.NoTicket;

            _results.RaiseRaceAgain();

            CollectionAssert.AreEqual(new[] { Dash }, _prompt.Opened);
            Assert.AreEqual(1, _results.DeniedCount);
            Assert.AreEqual(1, _session.Requests.Count);
            Assert.AreSame(_results, _navigator.CurrentScreen);
        }

        [Test]
        public void RaceAgain_NoTicketAndNoModal_WarnsInsteadOfThrowing()
        {
            StartAndFinish(withPrompt: false);
            _gate.NextResult = EntryResult.NoTicket;
            LogAssert.Expect(LogType.Warning, new Regex("no Entry Gate modal"));

            _results.RaiseRaceAgain();

            Assert.AreEqual(1, _results.DeniedCount);
            Assert.AreEqual(1, _session.Requests.Count);
        }

        [TestCase(EntryResult.NotPlayable)]
        [TestCase(EntryResult.UnknownEvent)]
        public void RaceAgain_Refused_WarnsAndStays(EntryResult refused)
        {
            StartAndFinish();
            _gate.NextResult = refused;
            LogAssert.Expect(LogType.Warning, new Regex("refused"));

            _results.RaiseRaceAgain();

            Assert.AreEqual(1, _session.Requests.Count);
            Assert.AreSame(_results, _navigator.CurrentScreen);
        }

        [Test]
        public void RaceAgain_BeforeAnyFinish_IsIgnored()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            Make().Start();

            _results.RaiseRaceAgain();

            Assert.AreEqual(0, _gate.EnterCalls.Count);
        }

        [Test]
        public void MainMenu_MovesToHome()
        {
            StartAndFinish();

            _results.RaiseMainMenu();

            CollectionAssert.AreEqual(new[] { "TestHome" }, _loader.Requested);
        }

        [Test]
        public void MainMenu_WhileSceneMoveRuns_LoadsOnce()
        {
            StartAndFinish();

            _results.RaiseMainMenu();
            _results.RaiseMainMenu();

            Assert.AreEqual(1, _loader.Requested.Count);
        }

        [Test]
        public void Dispose_StopsReactingToTheViewAndTheSession()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            var flow = Make();
            flow.Start();

            flow.Dispose();
            _session.Finish(Outcome(Dash, 9d));
            _results.RaiseMainMenu();

            Assert.AreEqual(0, _results.PresentCount);
            Assert.AreEqual(0, _loader.Requested.Count);
        }

        [Test]
        public void Restart_WhileTheRaceRuns_EntersAgainAndRestartsWithoutTouchingResults()
        {
            RaceFlowLogic flow = StartRunning();
            _gate.NextResult = EntryResult.EnteredWithTicket;

            flow.Restart();

            CollectionAssert.AreEqual(new[] { Dash }, _gate.EnterCalls);
            Assert.AreEqual(2, _session.Requests.Count);
            Assert.AreSame(_hud, _navigator.CurrentScreen);
            Assert.AreEqual(0, _results.DeniedCount);
        }

        [Test]
        public void Restart_WithNoTicketWhileRunning_OpensTheGateModalWithoutShakingResults()
        {
            RaceFlowLogic flow = StartRunning();
            _gate.NextResult = EntryResult.NoTicket;

            flow.Restart();

            CollectionAssert.AreEqual(new[] { Dash }, _prompt.Opened);
            Assert.AreEqual(0, _results.DeniedCount);
            Assert.AreEqual(1, _session.Requests.Count);
        }

        [Test]
        public void Restart_FromResults_ReturnsToTheHud()
        {
            RaceFlowLogic flow = StartAndFinish();
            _gate.NextResult = EntryResult.EnteredFree;

            flow.Restart();

            Assert.AreSame(_hud, _navigator.CurrentScreen);
            Assert.IsFalse(flow.ResultsOpen);
        }

        [Test]
        public void Restart_BeforeTheFirstStart_IsIgnored()
        {
            Make().Restart();

            Assert.AreEqual(0, _gate.EnterCalls.Count);
        }

        [Test]
        public void Exit_WhileTheRaceRuns_MovesToHomeOnce()
        {
            RaceFlowLogic flow = StartRunning();

            flow.Exit();
            flow.Exit();

            CollectionAssert.AreEqual(new[] { "TestHome" }, _loader.Requested);
        }

        private RaceFlowLogic StartRunning()
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            RaceFlowLogic flow = Make();
            flow.Start();
            return flow;
        }

        private RaceFlowLogic StartAndFinish(bool withPrompt = true)
        {
            _gate.Pending = Dash;
            _gate.HasPendingEntry = true;
            RaceFlowLogic flow = Make(withPrompt);
            flow.Start();
            _session.Finish(Outcome(Dash, 9.58d));
            return flow;
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
            public readonly List<RaceEventId> EnterCalls = new List<RaceEventId>();
            public EntryResult NextResult = EntryResult.EnteredFree;
            public RaceEventId Pending;
            public int TakeAfterEnter;
            public bool HasPendingEntry { get; set; }

            public EntryResult Enter(RaceEventId id)
            {
                EnterCalls.Add(id);
                if (NextResult == EntryResult.EnteredFree || NextResult == EntryResult.EnteredWithTicket
                    || NextResult == EntryResult.EnteredWithTimedAccess)
                {
                    Pending = id;
                    HasPendingEntry = true;
                }
                return NextResult;
            }

            public bool TryTakePending(out RaceEventId id)
            {
                id = Pending;
                if (!HasPendingEntry) return false;
                HasPendingEntry = false;
                if (EnterCalls.Count > 0) TakeAfterEnter++;
                return true;
            }
        }

        private sealed class FakeSelection : IRaceSelection
        {
            public RaceEventId Selected { get; set; }
            public void Select(RaceEventId id) => Selected = id;
            public event Action Changed;
            public void RaiseChanged() => Changed?.Invoke();
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
            public readonly List<(RaceOutcome Outcome, string PayoutId)> Calls = new List<(RaceOutcome, string)>();
            public int Gems;

            public int Resolve(RaceOutcome outcome, string payoutId)
            {
                Calls.Add((outcome, payoutId));
                return Gems;
            }
        }

        private sealed class FakeResults : IResultsView, IGemRewardView
        {
            public int GemsShown = -1;
            public int GemCalls;
            public ResultsData Presented;
            public int PresentCount;
            public int DeniedCount;
            public event Action RaceAgainRequested;
            public event Action MainMenuRequested;

            public void Present(ResultsData data)
            {
                Presented = data;
                PresentCount++;
            }

            public void ShowGemReward(int gems)
            {
                GemsShown = gems;
                GemCalls++;
            }

            public void NotifyDenied() => DeniedCount++;
            public void Show() { }
            public void Hide() { }
            public void RaiseRaceAgain() => RaceAgainRequested?.Invoke();
            public void RaiseMainMenu() => MainMenuRequested?.Invoke();
        }

        /// <summary>Keeps the first result per race id (like the real service) and says "new record" for the first time of an event.</summary>
        private sealed class FakeRecords : IPersonalBests
        {
            public readonly List<(RaceEventId Event, double Seconds, string RaceId)> Calls = new List<(RaceEventId, double, string)>();

            public bool TryGetBest(RaceEventId eventId, out double seconds)
            {
                seconds = 0d;
                return false;
            }

            public PersonalBestUpdate Record(RaceEventId eventId, double seconds, string raceId)
            {
                Calls.Add((eventId, seconds, raceId));
                return new PersonalBestUpdate(true, true, seconds);
            }
        }

        private sealed class FakePrompt : IEntryGatePrompt
        {
            public readonly List<RaceEventId> Opened = new List<RaceEventId>();
            public void Open(RaceEventId id) => Opened.Add(id);
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public readonly List<string> Requested = new List<string>();

            public bool Load(string sceneName, Action onLoaded)
            {
                Requested.Add(sceneName);
                return true; // the move stays "running" until a callback, which these tests never fire
            }
        }
    }
}
