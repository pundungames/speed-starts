using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>
    /// T-092: a Practice run ends on the line without Results, reward or records; Restart starts a new run; any other event is unchanged.
    /// Which event is Practice is a test input (the real rule, "no opponents", is read from the catalog by the RaceFlow shell).
    /// </summary>
    public sealed class RaceFlowPracticeTests
    {
        private static readonly RaceEventId Practice = new RaceEventId("t-practice");
        private static readonly RaceEventId Normal = new RaceEventId("t-normal");

        private FakeSession _session;
        private FakeResults _results;
        private FakeRecords _records;
        private FakeRewards _rewards;
        private FakeGate _gate;
        private FakeLookup _lookup;
        private ScreenNavigator _navigator;
        private FakeScreen _hud;
        private SceneFlow _sceneFlow;

        [SetUp]
        public void SetUp()
        {
            _session = new FakeSession();
            _results = new FakeResults();
            _records = new FakeRecords();
            _rewards = new FakeRewards();
            _gate = new FakeGate();
            _lookup = new FakeLookup();
            _lookup.Add(new RaceEventInfo(Practice, "Practice", 100f));
            _lookup.Add(new RaceEventInfo(Normal, "Normal", 100f));
            _hud = new FakeScreen();
            _navigator = new ScreenNavigator();
            _navigator.Push(_hud);
            _sceneFlow = new SceneFlow(new FakeLoader(), new SceneFlowScenes("TestHome", "TestRace"));
        }

        private RaceFlowLogic Start(RaceEventId id, bool withPracticeRule = true)
        {
            _gate.Pending = id;
            _gate.HasPendingEntry = true;
            var flow = new RaceFlowLogic(_session, _gate, new FakeSelection(), _lookup, _navigator, _sceneFlow, _results, _rewards, null,
                _records, isPractice: withPracticeRule ? (Func<RaceEventId, bool>)(e => e == Practice) : null);
            flow.Start();
            return flow;
        }

        private static RaceOutcome Outcome(RaceEventId id) => new RaceOutcome(id, 9.5d, 100f, 1, 1);

        [Test]
        public void PracticeFinish_OpensNoResults_PaysAndRecordsNothing_AndKeepsTheHud()
        {
            RaceFlowLogic flow = Start(Practice);

            _session.Finish(Outcome(Practice));

            Assert.AreEqual(0, _results.PresentCount, "no Results data");
            Assert.IsFalse(flow.ResultsOpen);
            Assert.IsTrue(flow.PracticeFinished);
            Assert.AreSame(_hud, _navigator.CurrentScreen, "the HUD stays on top");
            Assert.AreEqual(0, _rewards.Calls, "no reward");
            Assert.AreEqual(0, _records.Calls, "no personal best / day best");
        }

        [Test]
        public void PracticeFinish_ThenRestart_StartsANewRunWithoutTheGate()
        {
            RaceFlowLogic flow = Start(Practice);
            _session.Finish(Outcome(Practice));
            int requests = _session.Requests.Count;

            flow.Restart();

            Assert.AreEqual(requests + 1, _session.Requests.Count, "Restart starts a new run");
            Assert.AreEqual(Practice, _session.Requests[_session.Requests.Count - 1].EventId);
            Assert.AreEqual(0, _gate.EnterCalls, "Practice is entered free; the gate is not asked again");
            Assert.IsFalse(flow.PracticeFinished, "the new run can finish again");
        }

        [Test]
        public void PracticeRunsEndedTwice_RecordNothingEither()
        {
            RaceFlowLogic flow = Start(Practice);
            _session.Finish(Outcome(Practice));
            flow.Restart();
            _session.Finish(Outcome(Practice));

            Assert.AreEqual(0, _results.PresentCount);
            Assert.AreEqual(0, _records.Calls);
        }

        [Test]
        public void NormalEventFinish_StillOpensResults_PaysAndRecords()
        {
            RaceFlowLogic flow = Start(Normal);

            _session.Finish(Outcome(Normal));

            Assert.AreEqual(1, _results.PresentCount);
            Assert.IsTrue(flow.ResultsOpen);
            Assert.IsFalse(flow.PracticeFinished);
            Assert.AreSame(_results, _navigator.CurrentScreen);
            Assert.AreEqual(1, _rewards.Calls);
            Assert.AreEqual(1, _records.Calls);
        }

        [Test]
        public void WithoutAPracticeRule_EveryEventEndsWithResults()
        {
            RaceFlowLogic flow = Start(Practice, withPracticeRule: false);

            _session.Finish(Outcome(Practice));

            Assert.AreEqual(1, _results.PresentCount);
            Assert.IsTrue(flow.ResultsOpen);
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
        }

        private sealed class FakeGate : IRaceEntryGate
        {
            public RaceEventId Pending;
            public int EnterCalls;
            public bool HasPendingEntry { get; set; }
            public EntryResult Enter(RaceEventId id)
            {
                EnterCalls++;
                return EntryResult.EnteredFree;
            }

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
            public int Calls;
            public int Resolve(RaceOutcome outcome, string payoutId)
            {
                Calls++;
                return 0;
            }
        }

        private sealed class FakeRecords : IPersonalBests
        {
            public int Calls;
            public bool TryGetBest(RaceEventId eventId, out double seconds)
            {
                seconds = 0d;
                return false;
            }

            public PersonalBestUpdate Record(RaceEventId eventId, double seconds, string raceId)
            {
                Calls++;
                return new PersonalBestUpdate(true, true, seconds);
            }
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public bool Load(string sceneName, Action onLoaded) => true;
        }

        private sealed class FakeResults : IResultsView
        {
            public int PresentCount;
            public event Action RaceAgainRequested { add { } remove { } }
            public event Action MainMenuRequested { add { } remove { } }
            public void Present(ResultsData data) => PresentCount++;
            public void NotifyDenied() { }
            public void Show() { }
            public void Hide() { }
        }
    }
}
