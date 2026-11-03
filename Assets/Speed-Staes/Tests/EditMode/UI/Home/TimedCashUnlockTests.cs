using System;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>The one-tap "spend Cash, unlock the Timed Ticket window" of the entry gate, and the model of its button. Prices are test inputs.</summary>
    public sealed class TimedCashUnlockTests
    {
        private sealed class FakeShop : ITimedTicketShop
        {
            public TimedTicketPurchaseResult Result = TimedTicketPurchaseResult.Bought;
            public int Calls;
            public string LastId;

            public TimedTicketPurchaseResult TryBuyWithCash(string transactionId)
            {
                Calls++;
                LastId = transactionId;
                return Result;
            }
        }

        private sealed class FakeAccess : ITimedAccess
        {
            public TimedAccessResult Result = TimedAccessResult.Activated;
            public int Calls;

            public TimedAccessResult TryActivate()
            {
                Calls++;
                return Result;
            }
        }

        [Test]
        public void Bought_ThenActivated_IsUnlocked()
        {
            var shop = new FakeShop();
            var access = new FakeAccess();
            var unlock = new TimedCashUnlock(shop, access, () => false, () => "id-1");

            Assert.AreEqual(TimedCashUnlockResult.Unlocked, unlock.TryUnlock());
            Assert.AreEqual(1, shop.Calls);
            Assert.AreEqual("id-1", shop.LastId);
            Assert.AreEqual(1, access.Calls);
        }

        [Test]
        public void NotEnoughCash_StartsNothing()
        {
            var shop = new FakeShop { Result = TimedTicketPurchaseResult.NotEnough };
            var access = new FakeAccess();

            Assert.AreEqual(TimedCashUnlockResult.NotEnough, new TimedCashUnlock(shop, access, () => false).TryUnlock());
            Assert.AreEqual(0, access.Calls);
        }

        [Test]
        public void NotForSale_StartsNothing()
        {
            var shop = new FakeShop { Result = TimedTicketPurchaseResult.NotForSale };
            var access = new FakeAccess();

            Assert.AreEqual(TimedCashUnlockResult.NotForSale, new TimedCashUnlock(shop, access, () => false).TryUnlock());
            Assert.AreEqual(0, access.Calls);
        }

        [Test]
        public void WindowAlreadyRunning_SpendsNothing()
        {
            var shop = new FakeShop();
            var access = new FakeAccess();

            Assert.AreEqual(TimedCashUnlockResult.AlreadyActive, new TimedCashUnlock(shop, access, () => true).TryUnlock());
            Assert.AreEqual(0, shop.Calls, "time does not stack: no Cash is charged");
            Assert.AreEqual(0, access.Calls);
        }

        [Test]
        public void BoughtButNotStarted_IsReported_SoTheTicketIsNotLostSilently()
        {
            var shop = new FakeShop();
            var access = new FakeAccess { Result = TimedAccessResult.NotAvailable };

            Assert.AreEqual(TimedCashUnlockResult.BoughtNotStarted, new TimedCashUnlock(shop, access, () => false).TryUnlock());
        }

        [Test]
        public void MissingServices_DoNothing()
        {
            Assert.AreEqual(TimedCashUnlockResult.Unavailable, new TimedCashUnlock(null, new FakeAccess(), () => false).TryUnlock());
            Assert.AreEqual(TimedCashUnlockResult.Unavailable, new TimedCashUnlock(new FakeShop(), null, () => false).TryUnlock());
        }

        [Test]
        public void EachTapUsesAFreshTransactionId()
        {
            var shop = new FakeShop();
            var unlock = new TimedCashUnlock(shop, new FakeAccess(), () => false);

            unlock.TryUnlock();
            string first = shop.LastId;
            unlock.TryUnlock();

            Assert.IsNotEmpty(first);
            Assert.AreNotEqual(first, shop.LastId);
        }

        // ---- the button the gate shows for it ----

        private static EntryGateState Gate(int timedCashPrice, int cash, bool timedActive = false, double minutes = 60d) =>
            new EntryGateState(0, 3, timedActive, 0, TimeSpan.Zero, 0, cash, false, timedCashPrice, TimeSpan.FromMinutes(minutes));

        [Test]
        public void Button_IsShownWithPrice_AndGreyWithoutTheCash()
        {
            EntryGateModel poor = EntryGateModel.Build(Gate(timedCashPrice: 40, cash: 39));
            EntryGateModel rich = EntryGateModel.Build(Gate(timedCashPrice: 40, cash: 40));

            Assert.IsTrue(poor.ShowTimedCashButton);
            Assert.IsFalse(poor.TimedCashInteractable);
            Assert.IsTrue(rich.TimedCashInteractable);
        }

        [Test]
        public void Button_TextCarriesPriceAndWindowLengthFromTheInputs()
        {
            Assert.AreEqual("SPEND 40 - UNLOCK 1 HOUR", EntryGateModel.Build(Gate(40, 99, minutes: 60)).TimedCashLabel);
            Assert.AreEqual("SPEND 25 - UNLOCK 2 HOURS", EntryGateModel.Build(Gate(25, 99, minutes: 120)).TimedCashLabel);
            Assert.AreEqual("SPEND 25 - UNLOCK 90 MIN", EntryGateModel.Build(Gate(25, 99, minutes: 90)).TimedCashLabel);
        }

        [Test]
        public void Button_IsHidden_WhileAWindowRuns_WithoutAPrice_OrWithoutAWindowLength()
        {
            Assert.IsFalse(EntryGateModel.Build(Gate(40, 99, timedActive: true)).ShowTimedCashButton, "time does not stack");
            Assert.IsFalse(EntryGateModel.Build(Gate(0, 99)).ShowTimedCashButton, "not for sale");
            Assert.IsFalse(EntryGateModel.Build(Gate(40, 99, minutes: 0)).ShowTimedCashButton, "no window length configured");
        }

        [Test]
        public void OldStateWithoutTheNewValues_ShowsNoCashButton()
        {
            var state = new EntryGateState(1, 3, false, 0, TimeSpan.Zero, 0, 100, false);

            Assert.IsFalse(EntryGateModel.Build(state).ShowTimedCashButton);
        }
    }
}
