using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Flow;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    public sealed class CountdownFormatterTests
    {
        [Test]
        public void UnderAnHour_IsMinutesAndSeconds()
        {
            Assert.AreEqual("05:09", CountdownFormatter.Format(TimeSpan.FromSeconds(309)));
        }

        [Test]
        public void WithHours_ShowsHoursWithoutPadding()
        {
            Assert.AreEqual("1:02:03", CountdownFormatter.Format(new TimeSpan(1, 2, 3)));
        }

        [Test]
        public void PartialSecond_RoundsUp_SoItNeverShowsZeroWithTimeLeft()
        {
            Assert.AreEqual("00:01", CountdownFormatter.Format(TimeSpan.FromMilliseconds(200)));
        }

        [Test]
        public void ZeroOrNegative_IsZero()
        {
            Assert.AreEqual("00:00", CountdownFormatter.Format(TimeSpan.Zero));
            Assert.AreEqual("00:00", CountdownFormatter.Format(TimeSpan.FromSeconds(-5)));
        }
    }

    public sealed class HomeEntryRulesTests
    {
        [Test]
        public void Play_FreeEvent_EntersDirectly()
        {
            Assert.AreEqual(PlayStep.EnterNow, HomeEntryRules.DecidePlay(true, true, false, false));
        }

        [Test]
        public void Play_TicketedEvent_OpensTheGate()
        {
            Assert.AreEqual(PlayStep.OpenGate, HomeEntryRules.DecidePlay(true, true, true, false));
        }

        [Test]
        public void Play_TicketedEventWithTimedAccess_EntersDirectly()
        {
            Assert.AreEqual(PlayStep.EnterNow, HomeEntryRules.DecidePlay(true, true, true, true));
        }

        [Test]
        public void Play_NotPlayableOrUnknown_DoesNothing()
        {
            Assert.AreEqual(PlayStep.None, HomeEntryRules.DecidePlay(true, false, false, false));
            Assert.AreEqual(PlayStep.None, HomeEntryRules.DecidePlay(false, false, false, false));
        }

        [Test]
        public void Resolve_EveryEntryResultIsCovered()
        {
            Assert.AreEqual(EntryOutcome.StartRace, HomeEntryRules.Resolve(EntryResult.EnteredFree));
            Assert.AreEqual(EntryOutcome.StartRace, HomeEntryRules.Resolve(EntryResult.EnteredWithTicket));
            Assert.AreEqual(EntryOutcome.StartRace, HomeEntryRules.Resolve(EntryResult.EnteredWithTimedAccess));
            Assert.AreEqual(EntryOutcome.NoTicket, HomeEntryRules.Resolve(EntryResult.NoTicket));
            Assert.AreEqual(EntryOutcome.NotPlayable, HomeEntryRules.Resolve(EntryResult.NotPlayable));
            Assert.AreEqual(EntryOutcome.UnknownEvent, HomeEntryRules.Resolve(EntryResult.UnknownEvent));
        }

        [Test]
        public void Warning_ExistsForEveryRefusal_AndIsEmptyForSuccess()
        {
            Assert.IsNotEmpty(HomeEntryRules.WarningFor(EntryOutcome.NoTicket));
            Assert.IsNotEmpty(HomeEntryRules.WarningFor(EntryOutcome.NotPlayable));
            Assert.IsNotEmpty(HomeEntryRules.WarningFor(EntryOutcome.UnknownEvent));
            Assert.IsEmpty(HomeEntryRules.WarningFor(EntryOutcome.StartRace));
        }
    }

    public sealed class EntryGateModelTests
    {
        [Test]
        public void WithTickets_ShowsCountAndAllowsUse()
        {
            EntryGateModel model = EntryGateModel.Build(2, 3, false);

            Assert.AreEqual("2/3", model.TicketCountText);
            Assert.IsTrue(model.ShowUseTicketButton);
            Assert.IsTrue(model.UseTicketInteractable);
            Assert.IsFalse(model.TimedAccessActive);
        }

        [Test]
        public void WithoutTickets_UseButtonIsShownButNotInteractable()
        {
            EntryGateModel model = EntryGateModel.Build(0, 3, false);

            Assert.IsTrue(model.ShowUseTicketButton);
            Assert.IsFalse(model.UseTicketInteractable);
        }

        [Test]
        public void TimedAccessActive_HidesTheTicketButton_AndSaysSo()
        {
            EntryGateModel model = EntryGateModel.Build(2, 3, true);

            Assert.IsFalse(model.ShowUseTicketButton);
            Assert.IsTrue(model.TimedAccessActive);
            StringAssert.Contains("active", model.TimedStatusText);
        }
    }

    public sealed class HomeEntryFlowTests
    {
        private FakeGate _gate;
        private FakeLoader _loader;
        private HomeEntryFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _gate = new FakeGate();
            _loader = new FakeLoader();
            _flow = new HomeEntryFlow(_gate, new SceneFlow(_loader, new SceneFlowScenes("TestHome", "TestRace")));
        }

        [Test]
        public void SuccessfulEntry_MovesToTheRaceScene()
        {
            _gate.Result = EntryResult.EnteredWithTicket;

            EntryOutcome outcome = _flow.TryEnter(new RaceEventId("100m"));

            Assert.AreEqual(EntryOutcome.StartRace, outcome);
            CollectionAssert.AreEqual(new[] { "TestRace" }, _loader.Requested);
        }

        [Test]
        public void RefusedEntry_StaysOnHome()
        {
            _gate.Result = EntryResult.NoTicket;

            EntryOutcome outcome = _flow.TryEnter(new RaceEventId("100m"));

            Assert.AreEqual(EntryOutcome.NoTicket, outcome);
            Assert.IsEmpty(_loader.Requested);
        }

        [Test]
        public void DoubleTap_LoadsOnce()
        {
            _gate.Result = EntryResult.EnteredFree;

            _flow.TryEnter(new RaceEventId("100m"));
            _flow.TryEnter(new RaceEventId("100m"));

            Assert.AreEqual(1, _loader.Requested.Count);
        }

        private sealed class FakeGate : IRaceEntryGate
        {
            public EntryResult Result;
            public bool HasPendingEntry => false;
            public EntryResult Enter(RaceEventId id) => Result;

            public bool TryTakePending(out RaceEventId id)
            {
                id = default;
                return false;
            }
        }

        private sealed class FakeLoader : ISceneLoader
        {
            public readonly List<string> Requested = new List<string>();

            public bool Load(string sceneName, Action onLoaded)
            {
                Requested.Add(sceneName);
                return true;
            }
        }
    }
}
