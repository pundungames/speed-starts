using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.Tests.Meta.Config;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class TimedTicketShopTests
    {
        private const int Price = 90; // arbitrary test input

        private EconomyTestKit _k;

        [SetUp]
        public void SetUp()
        {
            _k = new EconomyTestKit();
            ConfigFixture.SetInt(_k.Kit.Economy, "timedTicketCashPrice", Price);
        }

        [TearDown]
        public void TearDown() => _k.Dispose();

        [Test]
        public void Buy_ChargesThePrice_AndAddsOneToTheStock()
        {
            _k.Progress.Cash = Price + 7;

            Assert.AreEqual(TimedTicketPurchaseResult.Bought, _k.TimedShop.TryBuyWithCash("buy-1"));

            Assert.AreEqual(7, _k.Progress.Cash);
            Assert.AreEqual(1, _k.Progress.TimedTickets);
        }

        [Test]
        public void NotEnoughCash_ChangesNothing_AndTheIdIsNotSpent()
        {
            _k.Progress.Cash = Price - 1;

            Assert.AreEqual(TimedTicketPurchaseResult.NotEnough, _k.TimedShop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(Price - 1, _k.Progress.Cash);
            Assert.AreEqual(0, _k.Progress.TimedTickets);

            _k.Progress.Cash = Price;
            Assert.AreEqual(TimedTicketPurchaseResult.Bought, _k.TimedShop.TryBuyWithCash("buy-1"));
        }

        [Test]
        public void NoPrice_IsNotForSale()
        {
            ConfigFixture.SetInt(_k.Kit.Economy, "timedTicketCashPrice", 0);
            _k.Progress.Cash = 1000;

            Assert.AreEqual(TimedTicketPurchaseResult.NotForSale, _k.TimedShop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(1000, _k.Progress.Cash);
        }

        [Test]
        public void SamePurchaseIdTwice_ChargesOnce()
        {
            _k.Progress.Cash = Price * 3;

            _k.TimedShop.TryBuyWithCash("buy-1");

            Assert.AreEqual(TimedTicketPurchaseResult.AlreadyProcessed, _k.TimedShop.TryBuyWithCash("buy-1"));
            Assert.AreEqual(Price * 2, _k.Progress.Cash);
            Assert.AreEqual(1, _k.Progress.TimedTickets);
        }

        [Test]
        public void BoughtTicket_CanBeActivated_AndTheStockSurvivesARestart()
        {
            _k.Progress.Cash = Price;
            _k.TimedShop.TryBuyWithCash("buy-1");

            var reloaded = _k.Restart();
            Assert.AreEqual(1, reloaded.Current.TimedTickets);
            Assert.AreEqual(TimedAccessResult.Activated, _k.Timed.TryActivate());
        }
    }
}
