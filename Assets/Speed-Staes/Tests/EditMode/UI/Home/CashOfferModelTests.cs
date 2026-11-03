using NUnit.Framework;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>The Cash offer window (Cash "+" button): amount, ads-today counter, button state and the reason line. Numbers are test inputs.</summary>
    public sealed class CashOfferModelTests
    {
        private static CashOfferState State(int amount = 100, int limit = 4, int remaining = 4, bool busy = false, bool available = true) =>
            new CashOfferState(amount, limit, remaining, busy, available);

        [Test]
        public void OpenOffer_ShowsAmountAndZeroUsedCounter_AndAWorkingButton()
        {
            CashOfferModel model = CashOfferModel.Build(State(amount: 100, limit: 4, remaining: 4));

            Assert.AreEqual("+100", model.AmountText);
            Assert.AreEqual("0/4", model.CounterText);
            Assert.IsTrue(model.WatchInteractable);
            Assert.IsFalse(model.ShowSpinner);
            Assert.AreEqual(string.Empty, model.NoteText);
        }

        [Test]
        public void CounterCountsWatchedAdsOverTheLimit()
        {
            Assert.AreEqual("1/4", CashOfferModel.Build(State(limit: 4, remaining: 3)).CounterText);
            Assert.AreEqual("4/4", CashOfferModel.Build(State(limit: 4, remaining: 0, available: false)).CounterText);
        }

        [Test]
        public void LimitReached_ClosesTheButton_AndSaysWhy()
        {
            CashOfferModel model = CashOfferModel.Build(State(remaining: 0, available: false));

            Assert.IsFalse(model.WatchInteractable);
            Assert.AreEqual(CashOfferTexts.LimitReached, model.NoteText);
        }

        [Test]
        public void NoAdReady_WithRightsLeft_ClosesTheButton_AndSaysSo()
        {
            CashOfferModel model = CashOfferModel.Build(State(remaining: 2, available: false));

            Assert.IsFalse(model.WatchInteractable);
            Assert.AreEqual(CashOfferTexts.NoAd, model.NoteText);
        }

        [Test]
        public void OfferOffInTheConfig_ClosesTheButton_WithItsOwnLine()
        {
            Assert.AreEqual(CashOfferTexts.NotOffered, CashOfferModel.Build(State(amount: 0, available: false)).NoteText);
            Assert.AreEqual(CashOfferTexts.NotOffered, CashOfferModel.Build(State(limit: 0, remaining: 0, available: false)).NoteText);
        }

        [Test]
        public void AdRunning_ShowsTheSpinner_AndTheButtonCannotBePressedAgain()
        {
            CashOfferModel model = CashOfferModel.Build(State(busy: true, available: false));

            Assert.IsTrue(model.ShowSpinner);
            Assert.IsFalse(model.WatchInteractable);
            Assert.AreEqual(CashOfferTexts.Loading, model.NoteText);
        }

        [Test]
        public void AvailableButBusy_IsStillNotPressable()
        {
            Assert.IsFalse(CashOfferModel.Build(State(busy: true, available: true)).WatchInteractable);
        }

        [Test]
        public void OddInputs_NeverGoNegative_AndRemainingNeverExceedsTheLimit()
        {
            CashOfferModel model = CashOfferModel.Build(State(amount: -5, limit: 3, remaining: 9, available: false));

            Assert.AreEqual("+0", model.AmountText);
            Assert.AreEqual("0/3", model.CounterText);
        }

        [Test]
        public void ShownTexts_UseOnlyPlainAscii()
        {
            string all = CashOfferTexts.Title + CashOfferTexts.Caption + CashOfferTexts.WatchAd + CashOfferTexts.YourCash + CashOfferTexts.LimitReached
                + CashOfferTexts.NoAd + CashOfferTexts.NotOffered + CashOfferTexts.Loading;
            foreach (char c in all) Assert.Less(c, 128, "the legacy UI font must have every glyph");
        }
    }
}
