using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Tests.Meta.Config;
using SpeedStars.Tests.Meta.Tickets;

namespace SpeedStars.Tests.Meta.Economy
{
    /// <summary>GDD §6.5 table as relations: cap, regen, timer restart, timed access vs. tickets, no refund, no double charge.</summary>
    public sealed class EconomyEdgeCaseTests
    {
        private const int Price = 13;
        private EconomyTestKit _k;

        [TearDown]
        public void TearDown() => _k?.Dispose();

        private void Setup(int entryTickets)
        {
            _k = new EconomyTestKit(entryTickets);
            ConfigFixture.SetInt(_k.Kit.Economy, "dailyGiftEntryTickets", 2);
            ConfigFixture.SetInt(_k.Kit.Economy, "entryTicketCashPrice", Price);
        }

        // --- cap and acquisition ---

        [Test]
        public void FullInventory_RefusesEverySource_AndChangesNothing()
        {
            Setup(TicketTestKit.Cap);
            ConfigFixture.SetBool(_k.Kit.Economy, "rewardedAdEnabled", true);
            ConfigFixture.SetInt(_k.Kit.Economy, "rewardedAdEntryTickets", 1);

            foreach (TicketGrantSource source in Enum.GetValues(typeof(TicketGrantSource)))
                Assert.AreEqual(TicketGrantResult.Full, _k.Tickets.TryGrant(source), source.ToString());

            Assert.AreEqual(TicketTestKit.Cap, _k.Progress.EntryTickets);
        }

        [Test]
        public void Grant_NeverPassesTheCap()
        {
            Setup(TicketTestKit.Cap - 1); // the daily gift is 2

            Assert.AreEqual(TicketGrantResult.Granted, _k.Tickets.TryGrant(TicketGrantSource.DailyGift));

            Assert.AreEqual(TicketTestKit.Cap, _k.Progress.EntryTickets);
            Assert.IsNull(_k.Tickets.NextTicketUtc); // at the cap the timer stops
        }

        [Test]
        public void SourceWithNothingConfigured_IsNotAvailable()
        {
            Setup(0); // rewarded ad is off by default

            Assert.AreEqual(TicketGrantResult.NotAvailable, _k.Tickets.TryGrant(TicketGrantSource.RewardedAd));
            Assert.AreEqual(0, _k.Progress.EntryTickets);
        }

        [Test]
        public void CashShop_NoPrice_IsNotForSale()
        {
            Setup(0);
            ConfigFixture.SetInt(_k.Kit.Economy, "entryTicketCashPrice", 0);
            _k.Progress.Cash = 1000;

            Assert.AreEqual(TicketPurchaseResult.NotForSale, _k.Shop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(1000, _k.Progress.Cash);
        }

        [Test]
        public void CashShop_Buys_ChargesPriceOnce_EvenWhenRetriedWithTheSameId()
        {
            Setup(0);
            _k.Progress.Cash = Price * 3;

            Assert.AreEqual(TicketPurchaseResult.Bought, _k.Shop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(TicketPurchaseResult.AlreadyProcessed, _k.Shop.TryBuyWithCash("buy-1"));

            Assert.AreEqual(Price * 2, _k.Progress.Cash);
            Assert.AreEqual(1, _k.Progress.EntryTickets);
        }

        [Test]
        public void CashShop_FullInventory_IsNotCharged()
        {
            Setup(TicketTestKit.Cap);
            _k.Progress.Cash = Price * 2;

            Assert.AreEqual(TicketPurchaseResult.Full, _k.Shop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(Price * 2, _k.Progress.Cash);
            CollectionAssert.IsEmpty(_k.Progress.ProcessedTransactionIds);
        }

        [Test]
        public void CashShop_TooPoor_ChangesNothing()
        {
            Setup(0);
            _k.Progress.Cash = Price - 1;

            Assert.AreEqual(TicketPurchaseResult.NotEnough, _k.Shop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(Price - 1, _k.Progress.Cash);
            Assert.AreEqual(0, _k.Progress.EntryTickets);
        }

        // --- regen ---

        [Test]
        public void Regen_OneTicketPerInterval_AndNeverPastTheCap()
        {
            Setup(0);
            _k.Tickets.Refresh();

            _k.Kit.Clock.Advance(TicketTestKit.Interval);
            Assert.AreEqual(1, _k.Tickets.Count);

            _k.Kit.Clock.Advance(TimeSpan.FromTicks(TicketTestKit.Interval.Ticks * (TicketTestKit.Cap * 3)));
            Assert.AreEqual(TicketTestKit.Cap, _k.Tickets.Count);
        }

        [Test]
        public void SpendingFromTheCap_StartsAFreshFullInterval()
        {
            Setup(TicketTestKit.Cap);
            _k.Kit.Clock.Advance(TimeSpan.FromHours(5)); // idle at the cap: no timer ran

            Assert.IsTrue(_k.Tickets.TrySpend());

            Assert.AreEqual(_k.Kit.Clock.UtcNow + TicketTestKit.Interval, _k.Tickets.NextTicketUtc);
            _k.Kit.Clock.Advance(TicketTestKit.Interval - TimeSpan.FromSeconds(1));
            Assert.AreEqual(TicketTestKit.Cap - 1, _k.Tickets.Count);
            _k.Kit.Clock.Advance(TimeSpan.FromSeconds(1));
            Assert.AreEqual(TicketTestKit.Cap, _k.Tickets.Count);
        }

        // --- entry vs. timed access, no refund ---

        [Test]
        public void ActiveTimedAccess_NeverSpendsEntryOrTimedStock()
        {
            Setup(2);
            _k.Progress.TimedTickets = 1;
            _k.Timed.TryActivate();

            for (int i = 0; i < 3; i++)
            {
                _k.Gate.Enter(EconomyTestKit.PaidEvent);
                _k.Gate.TryTakePending(out _);
            }

            Assert.AreEqual(2, _k.Progress.EntryTickets);
            Assert.AreEqual(0, _k.Progress.TimedTickets);
        }

        [Test]
        public void SpentEntryTicket_IsNotRefunded_WhenTheRaceIsAbandoned()
        {
            Setup(2);
            _k.Gate.Enter(EconomyTestKit.PaidEvent);
            _k.Gate.TryTakePending(out _);

            var reloaded = _k.Restart(); // quit mid-race

            Assert.AreEqual(1, reloaded.Current.EntryTickets);
        }

        // --- premium purchase through the ledger ---

        [Test]
        public void PremiumPrice_ExactGemsBuy_OneShortDoesNot_AndNoDoubleCharge()
        {
            Setup(0);
            const int premium = 17;
            _k.Progress.PurpleGems = premium - 1;
            Assert.AreEqual(TransactionResult.NotEnough, _k.Ledger.TrySpend(EconomyCurrency.PurpleGem, premium, "cosmetic:x"));

            _k.Progress.PurpleGems = premium;
            Assert.AreEqual(TransactionResult.Done, _k.Ledger.TrySpend(EconomyCurrency.PurpleGem, premium, "cosmetic:x"));
            Assert.AreEqual(TransactionResult.AlreadyProcessed, _k.Ledger.TrySpend(EconomyCurrency.PurpleGem, premium, "cosmetic:x"));
            Assert.AreEqual(0, _k.Progress.PurpleGems);
        }
    }
}
