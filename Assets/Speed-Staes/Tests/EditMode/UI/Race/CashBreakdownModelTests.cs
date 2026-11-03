using NUnit.Framework;
using SpeedStars.Meta.Contracts;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>The Cash breakdown of the Results screen (economy v2). Amounts here are test inputs, not game balance.</summary>
    public sealed class CashBreakdownModelTests
    {
        private static RaceCashReward Reward(int finish, int difficulty, int opponents, int passed = 0, int total = 0) =>
            new RaceCashReward(finish, difficulty, opponents, passed, total);

        [Test]
        public void EmptyReward_HidesTheWholeBlock()
        {
            CashBreakdown breakdown = CashBreakdown.Build(RaceCashReward.None);

            Assert.IsFalse(breakdown.Visible);
            Assert.AreEqual(0, breakdown.VisibleLines);
            Assert.AreEqual(string.Empty, breakdown.TotalText);
        }

        [Test]
        public void AllThreeLines_AreShownInOrder_WithTheTotalOnTop()
        {
            CashBreakdown breakdown = CashBreakdown.Build(Reward(4, 5, 6, 2, 3));

            Assert.IsTrue(breakdown.Visible);
            Assert.AreEqual("+15", breakdown.TotalText);
            Assert.AreEqual(15, breakdown.Total);
            Assert.AreEqual(3, breakdown.VisibleLines);
            Assert.AreEqual("Finished race +4", breakdown.Line(0).Text);
            Assert.AreEqual("Difficulty bonus +5", breakdown.Line(1).Text);
            Assert.AreEqual("Rivals beaten +6", breakdown.Line(2).Text);
        }

        [Test]
        public void ALineThatPaidNothing_IsHidden_AndTheOthersStay()
        {
            CashBreakdown breakdown = CashBreakdown.Build(Reward(7, 0, 0, 0, 5));

            Assert.IsTrue(breakdown.Visible);
            Assert.IsTrue(breakdown.Line(0).Visible);
            Assert.IsFalse(breakdown.Line(1).Visible);
            Assert.IsFalse(breakdown.Line(2).Visible);
            Assert.AreEqual(1, breakdown.VisibleLines);
            Assert.AreEqual("+7", breakdown.TotalText);
        }

        [Test]
        public void TheTotalIsTheSumOfTheLines()
        {
            RaceCashReward reward = Reward(3, 9, 11, 1, 2);
            CashBreakdown breakdown = CashBreakdown.Build(reward);

            Assert.AreEqual(reward.Total, breakdown.Total);
            Assert.AreEqual(reward.Finish + reward.Difficulty + reward.Opponents, breakdown.Total);
        }

        [Test]
        public void LineIndexOutOfRange_IsAnInvisibleEmptyLine()
        {
            CashBreakdown breakdown = CashBreakdown.Build(Reward(1, 1, 1));

            Assert.IsFalse(breakdown.Line(3).Visible);
            Assert.IsFalse(breakdown.Line(-1).Visible);
        }

        [Test]
        public void CounterStart_IsTheWalletBeforeTheRace_NeverNegative()
        {
            Assert.AreEqual(70, CashBreakdown.CounterStart(100, 30));
            Assert.AreEqual(0, CashBreakdown.CounterStart(20, 30), "a wallet smaller than the payout (state mismatch) never counts below 0");
            Assert.AreEqual(50, CashBreakdown.CounterStart(50, 0));
            Assert.AreEqual(0, CashBreakdown.CounterStart(-5, 3));
        }

        [Test]
        public void ShownTexts_UseOnlyPlainAscii()
        {
            string all = CashBreakdownTexts.FinishedRace + CashBreakdownTexts.DifficultyBonus + CashBreakdownTexts.RivalsBeaten + CashBreakdownTexts.Plus(12);
            foreach (char c in all) Assert.Less(c, 128, "the legacy UI font must have every glyph");
        }
    }
}
