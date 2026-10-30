using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Rewards;

namespace SpeedStars.Tests.UI.Rewards
{
    /// <summary>
    /// The decisions of the daily gift popup, the 7-day panel and the auto-open rule (pure model, no scene). Amounts and counts are
    /// test inputs, not game balance; what is asserted is the relation between the state and what the player sees.
    /// </summary>
    public sealed class RewardPanelsModelTests
    {
        // ---- daily gift popup ----

        [Test]
        public void Daily_Available_ShowsPlusAmount_EntryTicket_HeldCount_AndAWorkingClaim()
        {
            DailyGiftLayout layout = RewardPanelsModel.Daily(DailyGiftState.Available, 2, 1, 5);

            Assert.IsTrue(layout.ShowQuantity);
            Assert.AreEqual("+2", layout.Quantity);
            Assert.AreEqual(RewardTexts.EntryTicket, layout.State);
            Assert.AreEqual(RewardTexts.Capacity(1, 5), layout.Capacity);
            StringAssert.Contains("1", layout.Capacity);
            StringAssert.Contains("5", layout.Capacity);
            Assert.AreEqual(RewardTexts.Claim, layout.Button);
            Assert.IsFalse(layout.ButtonIsOk);
            Assert.IsTrue(layout.ButtonInteractable);
        }

        [Test]
        public void Daily_Full_ShowsTheFullNotice_WithOk_AndNoQuantity()
        {
            DailyGiftLayout layout = RewardPanelsModel.Daily(DailyGiftState.Full, 1, 4, 4);

            Assert.IsFalse(layout.ShowQuantity, "no reward is offered when the inventory is full");
            Assert.AreEqual(RewardTexts.TicketsFull, layout.State);
            Assert.AreEqual(RewardTexts.Capacity(4, 4), layout.Capacity);
            Assert.AreEqual(RewardTexts.Ok, layout.Button);
            Assert.IsTrue(layout.ButtonIsOk);
            Assert.IsTrue(layout.ButtonInteractable);
        }

        [Test]
        public void Daily_NotDue_KeepsTheClaimLookButTheButtonIsDead()
        {
            DailyGiftLayout layout = RewardPanelsModel.Daily(DailyGiftState.NotDue, 1, 0, 3);

            Assert.IsFalse(layout.ButtonIsOk);
            Assert.IsFalse(layout.ButtonInteractable, "nothing to claim: the button must not work");
        }

        [Test]
        public void ClaimOutcome_OnlyAGrantedTicketFlies()
        {
            Assert.AreEqual(DailyClaimOutcome.Fly, RewardPanelsModel.OutcomeOf(DailyGiftResult.Granted));
            Assert.AreEqual(DailyClaimOutcome.ShowFull, RewardPanelsModel.OutcomeOf(DailyGiftResult.Full));
            Assert.AreEqual(DailyClaimOutcome.Close, RewardPanelsModel.OutcomeOf(DailyGiftResult.AlreadyClaimed));
            Assert.AreEqual(DailyClaimOutcome.Close, RewardPanelsModel.OutcomeOf(DailyGiftResult.NotDue));
        }

        // ---- opening priority ----

        [TestCase(DailyGiftState.Available, true)]
        [TestCase(DailyGiftState.Full, true)]
        [TestCase(DailyGiftState.NotDue, false)]
        public void AutoOpen_OnHome_FollowsTheState(DailyGiftState state, bool expected)
        {
            Assert.AreEqual(expected, RewardPanelsModel.ShouldAutoOpenDailyGift(state, homeIsTopScreen: true, anotherPopupOpen: false));
        }

        [Test]
        public void AutoOpen_NeverOverAnotherPopup_NorOverAnotherScreen()
        {
            Assert.IsFalse(RewardPanelsModel.ShouldAutoOpenDailyGift(DailyGiftState.Available, homeIsTopScreen: true, anotherPopupOpen: true),
                "two modals do not stack");
            Assert.IsFalse(RewardPanelsModel.ShouldAutoOpenDailyGift(DailyGiftState.Available, homeIsTopScreen: false, anotherPopupOpen: false));
        }

        // ---- 7 day cards ----

        [TestCase(SevenDayCardState.Available)]
        [TestCase(SevenDayCardState.Claimed)]
        [TestCase(SevenDayCardState.Locked)]
        public void DayCard_ShowsExactlyOneLook_AndItsMatchingExtras(SevenDayCardState state)
        {
            DayCardLayout card = RewardPanelsModel.DayCard(3, state, RewardIcon.Diamond, 7);

            int looks = (card.ShowAvailableLook ? 1 : 0) + (card.ShowClaimedLook ? 1 : 0) + (card.ShowLockedLook ? 1 : 0);
            Assert.AreEqual(1, looks, "one look layer at a time");
            Assert.AreEqual(state == SevenDayCardState.Locked, card.ShowLock, "only a locked day has the lock");
            Assert.AreEqual(state == SevenDayCardState.Claimed, card.ShowClaimedFooter, "only a claimed day has the tick and CLAIMED bar");
            Assert.AreEqual(state == SevenDayCardState.Available, card.ShowAvailableFooter, "only the available day has the AVAILABLE bar");
        }

        [Test]
        public void DayCard_TextsCarryTheDayNumberAndTheAmountSeparately()
        {
            DayCardLayout cash = RewardPanelsModel.DayCard(2, SevenDayCardState.Locked, RewardIcon.Diamond, 250);
            DayCardLayout timed = RewardPanelsModel.DayCard(5, SevenDayCardState.Locked, RewardIcon.TimedTicket, 2);
            DayCardLayout hat = RewardPanelsModel.DayCard(7, SevenDayCardState.Locked, RewardIcon.Cosmetic, 0);

            Assert.AreEqual(RewardTexts.DayLabel(2), cash.DayText);
            StringAssert.Contains("250", cash.AmountText);
            Assert.AreEqual(RewardIcon.Diamond, cash.Icon);
            StringAssert.Contains("2", timed.AmountText);
            Assert.AreEqual(RewardIcon.TimedTicket, timed.Icon);
            Assert.AreEqual(string.Empty, hat.AmountText, "the hat has a picture and a caption, not an amount");
        }

        [Test]
        public void IconOf_MapsTheRewardKindsByName_AndUnknownKindsHaveNoPicture()
        {
            Assert.AreEqual(RewardIcon.Diamond, RewardPanelsModel.IconOf("Cash"));
            Assert.AreEqual(RewardIcon.TimedTicket, RewardPanelsModel.IconOf("TimedTicket"));
            Assert.AreEqual(RewardIcon.Cosmetic, RewardPanelsModel.IconOf("Cosmetic"));
            Assert.AreEqual(RewardIcon.None, RewardPanelsModel.IconOf("SomethingNew"));
            Assert.AreEqual(RewardIcon.None, RewardPanelsModel.IconOf(null));
        }

        // ---- 7 day CLAIM button ----

        [Test]
        public void SevenDayClaim_WorksOnlyWhileACardIsAvailable()
        {
            var withAvailable = new List<SevenDayCardState>
            {
                SevenDayCardState.Claimed, SevenDayCardState.Available, SevenDayCardState.Locked, SevenDayCardState.Locked
            };
            var none = new List<SevenDayCardState> { SevenDayCardState.Claimed, SevenDayCardState.Locked };

            Assert.AreEqual(1, RewardPanelsModel.AvailableIndex(withAvailable));
            Assert.IsTrue(RewardPanelsModel.SevenDayClaimEnabled(withAvailable, claimRunning: false));
            Assert.AreEqual(-1, RewardPanelsModel.AvailableIndex(none));
            Assert.IsFalse(RewardPanelsModel.SevenDayClaimEnabled(none, claimRunning: false));
        }

        [Test]
        public void SevenDayClaim_IsDeadWhileAClaimRuns_AndForAnEmptyOrMissingList()
        {
            var available = new List<SevenDayCardState> { SevenDayCardState.Available };

            Assert.IsFalse(RewardPanelsModel.SevenDayClaimEnabled(available, claimRunning: true));
            Assert.IsFalse(RewardPanelsModel.SevenDayClaimEnabled(new List<SevenDayCardState>(), claimRunning: false));
            Assert.IsFalse(RewardPanelsModel.SevenDayClaimEnabled(null, claimRunning: false));
        }

        // ---- cosmetic caption ----

        [Test]
        public void CosmeticCaption_ReadsAPremiumHatFromItsOwnRecord()
        {
            Assert.AreEqual("PREMIUM 02 HEADWEAR", RewardPanelsModel.CosmeticCaption("PH02", "Headwear", isPremium: true));
        }

        [Test]
        public void CosmeticCaption_WithoutANumberOrPremium_IsJustTheCategory()
        {
            Assert.AreEqual("HEADWEAR", RewardPanelsModel.CosmeticCaption("Cap", "Headwear", isPremium: false));
            Assert.AreEqual("HEADWEAR", RewardPanelsModel.CosmeticCaption(null, "Headwear", isPremium: false));
        }
    }
}
