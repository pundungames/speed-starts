using System;
using NUnit.Framework;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Tickets;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>
    /// The rewarded-ad rules of the entry gate (EntryGateAdFlow) over fake ad and ticket services. Ticket amounts are read
    /// from the configs built here, never from shipped assets.
    /// </summary>
    public sealed class EntryGateAdFlowTests
    {
        private TicketTestKit _kit;

        [SetUp]
        public void SetUp()
        {
            _kit = new TicketTestKit(0);
            SetAd(enabled: true, tickets: 1);
        }

        [TearDown]
        public void TearDown()
        {
            _kit.Dispose();
        }

        private void SetAd(bool enabled, int tickets)
        {
            ConfigFixture.SetBool(_kit.Economy, "rewardedAdEnabled", enabled);
            ConfigFixture.SetInt(_kit.Economy, "rewardedAdEntryTickets", tickets);
        }

        private EntryGateAdFlow Flow(IRewardedAds ads, IEntryTicketSource source, Action granted = null)
        {
            return new EntryGateAdFlow(ads, source, _kit.Economy, granted);
        }

        // ---- the offer ----

        [Test]
        public void NoAdService_OfferStaysClosed_AndNothingStarts()
        {
            EntryGateAdFlow flow = Flow(null, new FakeSource());

            Assert.IsFalse(flow.IsOffered);
            Assert.IsFalse(flow.TryStart(_ => Assert.Fail("no ad service: no result")));
        }

        [Test]
        public void AdNotReady_OfferClosed()
        {
            var ads = new FakeAds { IsReady = false };
            EntryGateAdFlow flow = Flow(ads, new FakeSource());

            Assert.IsFalse(flow.IsOffered);
            Assert.IsFalse(flow.TryStart(null));
            Assert.AreEqual(0, ads.Shows);
        }

        [Test]
        public void ConfigClosed_OfferClosed_EvenWithAReadyAd()
        {
            var ads = new FakeAds();
            foreach (bool enabled in new[] { false, true })
            {
                SetAd(enabled, enabled ? 0 : 1); // disabled with a ticket count, and enabled with none
                EntryGateAdFlow flow = Flow(ads, new FakeSource());

                Assert.IsFalse(flow.IsOffered, $"enabled={enabled}");
                Assert.IsFalse(flow.TryStart(null));
            }
            Assert.AreEqual(0, ads.Shows);
        }

        [Test]
        public void ConfiguredAndReady_OfferOpen()
        {
            Assert.IsTrue(Flow(new FakeAds(), new FakeSource()).IsOffered);
        }

        // ---- results ----

        [Test]
        public void Completed_AsksForTheTicketOnce_ThenReportsGranted()
        {
            var ads = new FakeAds();
            var source = new FakeSource();
            int granted = 0;
            EntryGateAdFlow flow = Flow(ads, source, () => granted++);
            AdWatchOutcome? outcome = null;

            Assert.IsTrue(flow.TryStart(o => outcome = o));
            Assert.IsTrue(flow.IsBusy);
            Assert.IsNull(outcome, "nothing is paid before the ad ends");
            Assert.AreEqual(0, source.Grants);

            ads.Finish(AdResult.Completed);

            Assert.AreEqual(AdWatchOutcome.Granted, outcome);
            Assert.AreEqual(1, source.Grants);
            Assert.AreEqual(TicketGrantSource.RewardedAd, source.LastSource);
            Assert.AreEqual(1, granted);
            Assert.IsFalse(flow.IsBusy);
        }

        [Test]
        public void Completed_WithAFullInventory_PaysNothing()
        {
            var ads = new FakeAds();
            var source = new FakeSource { Next = TicketGrantResult.Full };
            int granted = 0;
            EntryGateAdFlow flow = Flow(ads, source, () => granted++);
            AdWatchOutcome? outcome = null;

            flow.TryStart(o => outcome = o);
            ads.Finish(AdResult.Completed);

            Assert.AreEqual(AdWatchOutcome.Full, outcome);
            Assert.AreEqual(0, granted);
            Assert.IsFalse(flow.IsBusy);
        }

        [Test]
        public void Completed_WhenTheSourceGivesNothing_ReportsUnavailable()
        {
            var ads = new FakeAds();
            var source = new FakeSource { Next = TicketGrantResult.NotAvailable };
            AdWatchOutcome? outcome = null;
            EntryGateAdFlow flow = Flow(ads, source);

            flow.TryStart(o => outcome = o);
            ads.Finish(AdResult.Completed);

            Assert.AreEqual(AdWatchOutcome.Unavailable, outcome);
        }

        [TestCase(AdResult.Skipped, AdWatchOutcome.Skipped)]
        [TestCase(AdResult.Failed, AdWatchOutcome.Failed)]
        public void SkippedOrFailed_NeverAsksForATicket(AdResult result, AdWatchOutcome expected)
        {
            var ads = new FakeAds();
            var source = new FakeSource();
            int granted = 0;
            EntryGateAdFlow flow = Flow(ads, source, () => granted++);
            AdWatchOutcome? outcome = null;

            flow.TryStart(o => outcome = o);
            ads.Finish(result);

            Assert.AreEqual(expected, outcome);
            Assert.AreEqual(0, source.Grants);
            Assert.AreEqual(0, granted);
            Assert.IsFalse(flow.IsBusy, "a refused ad frees the button again");
        }

        // ---- one ad at a time ----

        [Test]
        public void DoubleStart_OpensOneAd_AndThePlayerCanWatchAgainAfterwards()
        {
            var ads = new FakeAds();
            EntryGateAdFlow flow = Flow(ads, new FakeSource());

            Assert.IsTrue(flow.TryStart(null));
            Assert.IsFalse(flow.IsAvailable);
            Assert.IsFalse(flow.TryStart(null), "second tap while the first ad runs");
            Assert.AreEqual(1, ads.Shows);

            ads.Finish(AdResult.Completed);

            Assert.IsTrue(flow.TryStart(null));
            Assert.AreEqual(2, ads.Shows);
        }

        [Test]
        public void ARepeatedAnswerOfTheSameAd_PaysOnce()
        {
            var ads = new FakeAds();
            var source = new FakeSource();
            EntryGateAdFlow flow = Flow(ads, source);
            int answers = 0;

            flow.TryStart(_ => answers++);
            Action<AdResult> answer = ads.Pending;
            answer(AdResult.Completed);
            answer(AdResult.Completed);

            Assert.AreEqual(1, source.Grants);
            Assert.AreEqual(1, answers);
        }

        [Test]
        public void AnAnswerThatComesAtOnce_IsHandledBeforeTryStartReturns()
        {
            var ads = new FakeAds { AnswerAtOnce = AdResult.Completed };
            var source = new FakeSource();
            EntryGateAdFlow flow = Flow(ads, source);
            AdWatchOutcome? outcome = null;

            Assert.IsTrue(flow.TryStart(o => outcome = o));

            Assert.AreEqual(AdWatchOutcome.Granted, outcome);
            Assert.AreEqual(1, source.Grants);
            Assert.IsFalse(flow.IsBusy);
        }

        [Test]
        public void ResultStillPaysOut_WhenNobodyListensAnymore()
        {
            // The window closed while the ad ran: the view is gone, the callback is a no-op, the ticket still arrives.
            var ads = new FakeAds();
            var source = new FakeSource();
            EntryGateAdFlow flow = Flow(ads, source);

            flow.TryStart(null);
            ads.Finish(AdResult.Completed);

            Assert.AreEqual(1, source.Grants);
        }

        [Test]
        public void AdServiceThatThrows_DoesNotLeaveTheFlowBusy()
        {
            var ads = new FakeAds { ThrowOnShow = true };
            EntryGateAdFlow flow = Flow(ads, new FakeSource());

            Assert.Throws<InvalidOperationException>(() => flow.TryStart(null));
            Assert.IsFalse(flow.IsBusy);
        }

        // ---- with the real ticket service ----

        [Test]
        public void RealTicketService_Completed_RaisesTheCountByTheConfiguredAmount()
        {
            SetAd(enabled: true, tickets: 2);
            var service = new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
            var ads = new FakeAds();
            int before = service.Count;
            Assert.IsTrue(_kit.Economy.TryGetRewardedAdEntryTickets(out int amount));
            AdWatchOutcome? outcome = null;
            EntryGateAdFlow flow = Flow(ads, service);

            flow.TryStart(o => outcome = o);
            ads.Finish(AdResult.Completed);

            Assert.AreEqual(AdWatchOutcome.Granted, outcome);
            Assert.AreEqual(Math.Min(service.Cap, before + amount), service.Count);
        }

        [Test]
        public void RealTicketService_FullInventory_RefusesTheTicket()
        {
            _kit.Dispose();
            _kit = new TicketTestKit(TicketTestKit.Cap);
            SetAd(enabled: true, tickets: 1);
            var service = new EntryTicketService(_kit.Repository, _kit.Clock, _kit.Economy);
            var ads = new FakeAds();
            AdWatchOutcome? outcome = null;
            EntryGateAdFlow flow = Flow(ads, service);
            int before = service.Count;

            flow.TryStart(o => outcome = o);
            ads.Finish(AdResult.Completed);

            Assert.AreEqual(AdWatchOutcome.Full, outcome);
            Assert.AreEqual(before, service.Count);
        }

        // ---- fakes ----

        private sealed class FakeAds : IRewardedAds
        {
            public bool IsReady { get; set; } = true;
            public int Shows { get; private set; }
            public Action<AdResult> Pending { get; private set; }
            public AdResult? AnswerAtOnce { get; set; }
            public bool ThrowOnShow { get; set; }

            public void Show(Action<AdResult> done)
            {
                if (ThrowOnShow) throw new InvalidOperationException("ad service failed");
                Shows++;
                Pending = done;
                if (AnswerAtOnce.HasValue) done(AnswerAtOnce.Value);
            }

            public void Finish(AdResult result) => Pending(result);
        }

        private sealed class FakeSource : IEntryTicketSource
        {
            public TicketGrantResult Next { get; set; } = TicketGrantResult.Granted;
            public int Grants { get; private set; }
            public TicketGrantSource? LastSourceOrNull { get; private set; }
            public TicketGrantSource LastSource => LastSourceOrNull.GetValueOrDefault();

            public TicketGrantResult TryGrant(TicketGrantSource source)
            {
                Grants++;
                LastSourceOrNull = source;
                return Next;
            }
        }
    }
}
