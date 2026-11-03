using NUnit.Framework;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.UI.Home
{
    /// <summary>The Home "Racer of the day" card: when the day's outfit goes on the menu character and when it comes off (T-087).</summary>
    public sealed class RacerCardRulesTests
    {
        [Test]
        public void DailySelected_ShowsTheDaysOutfit_EvenWhenAlreadyShown()
        {
            Assert.AreEqual(RacerCardAction.ShowDailyOutfit, RacerCardRules.Decide(true, false));
            Assert.AreEqual(RacerCardAction.ShowDailyOutfit, RacerCardRules.Decide(true, true), "re-showing is harmless; the preview ignores an equal outfit");
        }

        [Test]
        public void OtherEvent_AfterTheDailyOutfit_RestoresTheOwnOutfit()
        {
            Assert.AreEqual(RacerCardAction.RestoreOwnOutfit, RacerCardRules.Decide(false, true));
        }

        [Test]
        public void OtherEvent_WithoutAnyDailyOutfit_TouchesNothing()
        {
            Assert.AreEqual(RacerCardAction.None, RacerCardRules.Decide(false, false), "never releases a preview the costume screen put there");
        }
    }
}
