using System;
using NUnit.Framework;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>The economy side of the entry gate model (Timed Ticket, full inventory, Cash, ad). Numbers here are test inputs, not game balance.</summary>
    public sealed class EntryGateEconomyTests
    {
        private static EntryGateState State(int tickets = 1, int cap = 3, bool timedActive = false, int timedStock = 0,
            double remainingSeconds = 0d, int cashPrice = 0, int cash = 0, bool ad = false)
        {
            return new EntryGateState(tickets, cap, timedActive, timedStock, TimeSpan.FromSeconds(remainingSeconds), cashPrice, cash, ad);
        }

        [Test]
        public void TimedTicketInStock_NoWindow_OffersTheTimedButton()
        {
            EntryGateModel model = EntryGateModel.Build(State(timedStock: 2));

            Assert.IsTrue(model.ShowUseTimedButton);
            Assert.AreEqual("2", model.TimedValueText);
        }

        [Test]
        public void NoTimedTicketInStock_HidesTheTimedButton()
        {
            Assert.IsFalse(EntryGateModel.Build(State(timedStock: 0)).ShowUseTimedButton);
        }

        [Test]
        public void WindowRunning_HidesTimedAndEntryButtons_AndShowsTheCountdown()
        {
            EntryGateModel model = EntryGateModel.Build(State(timedActive: true, timedStock: 5, remainingSeconds: 59 * 60 + 12));

            Assert.IsFalse(model.ShowUseTimedButton, "time does not stack");
            Assert.IsFalse(model.ShowUseTicketButton);
            Assert.AreEqual("59:12", model.TimedValueText);
            StringAssert.Contains("active", model.TimedStatusText);
        }

        [Test]
        public void FullInventory_ClosesCashAndAd_AndSaysSo()
        {
            EntryGateModel model = EntryGateModel.Build(State(tickets: 3, cap: 3, cashPrice: 10, cash: 1000, ad: true));

            Assert.IsTrue(model.TicketsFull);
            Assert.IsFalse(model.BuyWithCashInteractable);
            Assert.IsFalse(model.WatchAdInteractable);
            Assert.AreEqual(HomeTexts.EntryTicketsFull, model.OffersCaption);
        }

        [Test]
        public void NotFull_WithPriceAndEnoughCash_OpensCash()
        {
            EntryGateModel model = EntryGateModel.Build(State(tickets: 1, cap: 3, cashPrice: 10, cash: 10));

            Assert.IsFalse(model.TicketsFull);
            Assert.IsTrue(model.BuyWithCashInteractable);
            Assert.AreEqual("10", model.CashPriceText);
            Assert.AreEqual(HomeTexts.GetTickets, model.OffersCaption);
        }

        [Test]
        public void Cash_ClosedWithoutPrice_OrWithoutEnoughCash()
        {
            Assert.IsFalse(EntryGateModel.Build(State(cashPrice: 0, cash: 1000)).BuyWithCashInteractable);
            Assert.AreEqual(string.Empty, EntryGateModel.Build(State(cashPrice: 0)).CashPriceText);
            Assert.IsFalse(EntryGateModel.Build(State(cashPrice: 10, cash: 9)).BuyWithCashInteractable);
        }

        [Test]
        public void Ad_FollowsAvailability_WhenNotFull()
        {
            Assert.IsFalse(EntryGateModel.Build(State(ad: false)).WatchAdInteractable);
            Assert.IsTrue(EntryGateModel.Build(State(ad: true)).WatchAdInteractable);
        }

        [Test]
        public void ThreeArgumentBuild_KeepsEverythingExtraClosed()
        {
            EntryGateModel model = EntryGateModel.Build(1, 3, false);

            Assert.IsFalse(model.ShowUseTimedButton);
            Assert.IsFalse(model.BuyWithCashInteractable);
            Assert.IsFalse(model.WatchAdInteractable);
        }

        [Test]
        public void ZeroCap_IsNeverCountedAsFull()
        {
            Assert.IsFalse(EntryGateModel.Build(State(tickets: 0, cap: 0)).TicketsFull);
        }
    }
}
