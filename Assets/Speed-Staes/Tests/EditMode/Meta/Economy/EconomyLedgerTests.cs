using NUnit.Framework;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Tests.Meta.Economy
{
    public sealed class EconomyLedgerTests
    {
        private EconomyTestKit _k;

        [SetUp]
        public void SetUp() => _k = new EconomyTestKit();

        [TearDown]
        public void TearDown() => _k.Dispose();

        [Test]
        public void Spend_Enough_ChangesOnlyThatBalance()
        {
            _k.Progress.Cash = 50;
            _k.Progress.PurpleGems = 7;

            Assert.AreEqual(TransactionResult.Done, _k.Ledger.TrySpend(EconomyCurrency.Cash, 20, "t1"));

            Assert.AreEqual(30, _k.Progress.Cash);
            Assert.AreEqual(7, _k.Progress.PurpleGems);
        }

        [Test]
        public void Spend_SameIdTwice_ChargesOnce()
        {
            _k.Progress.Cash = 100;
            _k.Ledger.TrySpend(EconomyCurrency.Cash, 20, "t1");

            Assert.AreEqual(TransactionResult.AlreadyProcessed, _k.Ledger.TrySpend(EconomyCurrency.Cash, 20, "t1"));

            Assert.AreEqual(80, _k.Progress.Cash);
        }

        [Test]
        public void Spend_NotEnough_ChangesNothing_AndDoesNotBurnTheId()
        {
            _k.Progress.PurpleGems = 4;

            Assert.AreEqual(TransactionResult.NotEnough, _k.Ledger.TrySpend(EconomyCurrency.PurpleGem, 5, "t1"));
            Assert.AreEqual(4, _k.Progress.PurpleGems);
            CollectionAssert.IsEmpty(_k.Progress.ProcessedTransactionIds);

            // The same purchase can succeed later once the player can afford it.
            _k.Progress.PurpleGems = 5;
            Assert.AreEqual(TransactionResult.Done, _k.Ledger.TrySpend(EconomyCurrency.PurpleGem, 5, "t1"));
            Assert.AreEqual(0, _k.Progress.PurpleGems);
        }

        [Test]
        public void ExactBalance_CanBeSpent_OneLessCannot()
        {
            _k.Progress.Cash = 10;
            Assert.AreEqual(TransactionResult.NotEnough, _k.Ledger.TrySpend(EconomyCurrency.Cash, 11, "a"));
            Assert.AreEqual(TransactionResult.Done, _k.Ledger.TrySpend(EconomyCurrency.Cash, 10, "b"));
            Assert.AreEqual(0, _k.Progress.Cash);
        }

        [Test]
        public void InvalidAmountOrId_ChangesNothing()
        {
            _k.Progress.Cash = 10;

            Assert.AreEqual(TransactionResult.InvalidAmount, _k.Ledger.TrySpend(EconomyCurrency.Cash, 0, "t"));
            Assert.AreEqual(TransactionResult.InvalidAmount, _k.Ledger.TrySpend(EconomyCurrency.Cash, -3, "t"));
            Assert.AreEqual(TransactionResult.InvalidAmount, _k.Ledger.Grant(EconomyCurrency.Cash, -3, "t"));
            Assert.AreEqual(TransactionResult.InvalidAmount, _k.Ledger.Grant(EconomyCurrency.Cash, 5, ""));
            Assert.AreEqual(TransactionResult.InvalidAmount, _k.Ledger.Grant(EconomyCurrency.Cash, 5, null));

            Assert.AreEqual(10, _k.Progress.Cash);
            CollectionAssert.IsEmpty(_k.Progress.ProcessedTransactionIds);
        }

        [Test]
        public void Grant_AddsOnce_ForEveryCurrency()
        {
            _k.Ledger.Grant(EconomyCurrency.Cash, 5, "c");
            _k.Ledger.Grant(EconomyCurrency.PurpleGem, 6, "g");
            _k.Ledger.Grant(EconomyCurrency.TimedTicket, 2, "tt");
            Assert.AreEqual(TransactionResult.AlreadyProcessed, _k.Ledger.Grant(EconomyCurrency.Cash, 5, "c"));

            Assert.AreEqual(5, _k.Progress.Cash);
            Assert.AreEqual(6, _k.Progress.PurpleGems);
            Assert.AreEqual(2, _k.Progress.TimedTickets);
        }

        [Test]
        public void Grant_NearIntMax_Clamps()
        {
            _k.Progress.Cash = int.MaxValue - 1;
            _k.Ledger.Grant(EconomyCurrency.Cash, 50, "big");
            Assert.AreEqual(int.MaxValue, _k.Progress.Cash);
        }

        [Test]
        public void WalletChanged_FiresOnSuccess_NotOnFailure()
        {
            int fired = 0;
            _k.Wallet.Changed += () => fired++;
            _k.Progress.Cash = 5;

            _k.Ledger.TrySpend(EconomyCurrency.Cash, 9, "no");
            _k.Ledger.TrySpend(EconomyCurrency.Cash, 0, "bad");
            Assert.AreEqual(0, fired);

            _k.Ledger.TrySpend(EconomyCurrency.Cash, 5, "yes");
            _k.Ledger.TrySpend(EconomyCurrency.Cash, 5, "yes");
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ProcessedList_IsCapped_OldestDroppedFirst()
        {
            for (int i = 0; i < EconomyTestKit.TransactionCap + 2; i++)
                _k.Ledger.Grant(EconomyCurrency.Cash, 1, "id" + i);

            var ids = _k.Progress.ProcessedTransactionIds;
            Assert.AreEqual(EconomyTestKit.TransactionCap, ids.Count);
            Assert.IsFalse(ids.Contains("id0"));
            Assert.IsTrue(ids.Contains("id" + (EconomyTestKit.TransactionCap + 1)));
        }

        [Test]
        public void ProcessedIds_SurviveRestart()
        {
            _k.Progress.Cash = 30;
            _k.Ledger.TrySpend(EconomyCurrency.Cash, 10, "once");
            _k.Kit.Repository.Save();

            var reloaded = _k.Restart();

            Assert.AreEqual(TransactionResult.AlreadyProcessed, _k.Ledger.TrySpend(EconomyCurrency.Cash, 10, "once"));
            Assert.AreEqual(20, reloaded.Current.Cash);
        }
    }
}
