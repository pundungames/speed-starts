using NUnit.Framework;
using SpeedStars.Meta.Relay;
using SpeedStars.UI.Home;

namespace SpeedStars.Tests.Meta.Relay
{
    public sealed class RelayRulesTests
    {
        [Test]
        public void DefaultPreset_WrapsOverThePresets_AndSurvivesNoPresets()
        {
            Assert.AreEqual(0, RelayTeamRules.DefaultPreset(0, 3));
            Assert.AreEqual(1, RelayTeamRules.DefaultPreset(4, 3));
            Assert.AreEqual(0, RelayTeamRules.DefaultPreset(2, 0));
        }

        [Test]
        public void ResolvePreset_KeepsAValidChoice_ElseTheLegDefault()
        {
            Assert.AreEqual(2, RelayTeamRules.ResolvePreset(2, 0, 3));
            Assert.AreEqual(RelayTeamRules.DefaultPreset(1, 3), RelayTeamRules.ResolvePreset(-1, 1, 3));
            Assert.AreEqual(RelayTeamRules.DefaultPreset(1, 2), RelayTeamRules.ResolvePreset(5, 1, 2));
            Assert.AreEqual(0, RelayTeamRules.ResolvePreset(1, 1, 0));
        }

        [TestCase(4, 0)]
        [TestCase(4, 1)]
        [TestCase(4, 3)]
        [TestCase(4, 9)]
        public void StageLegs_AreTheOtherLegs_InOrder_AndNeverTheSelectedOne(int legs, int selected)
        {
            int[] stage = RelayTeamRules.StageLegs(legs, selected);

            Assert.AreEqual(legs - 1, stage.Length);
            int shown = selected >= 0 && selected < legs ? selected : 0;
            Assert.IsFalse(System.Array.IndexOf(stage, shown) >= 0);
            for (int i = 1; i < stage.Length; i++) Assert.Greater(stage[i], stage[i - 1]);
        }

        [Test]
        public void StageLegs_NoTeam_IsEmpty()
        {
            Assert.IsEmpty(RelayTeamRules.StageLegs(1, 0));
            Assert.IsEmpty(RelayTeamRules.StageLegs(0, 0));
        }

        [Test]
        public void TeamLegCount_IsTheLongestRelay_FlatEventsDoNotCount()
        {
            Assert.AreEqual(4, RelayTeamRules.TeamLegCount(new[] { 0, 0, 4, 0 }));
            Assert.AreEqual(0, RelayTeamRules.TeamLegCount(new[] { 0, 1 }));
            Assert.AreEqual(0, RelayTeamRules.TeamLegCount(null));
        }

        [Test]
        public void CardDecision_EnterOnce_StayWhileRelay_LeaveOnce()
        {
            Assert.AreEqual(RelayCardAction.EnterRelay, RelayCardRules.Decide(true, false));
            Assert.AreEqual(RelayCardAction.None, RelayCardRules.Decide(true, true));
            Assert.AreEqual(RelayCardAction.LeaveRelay, RelayCardRules.Decide(false, true));
            Assert.AreEqual(RelayCardAction.None, RelayCardRules.Decide(false, false));
        }

        [Test]
        public void ShowsTeam_OnlyForARelayThatIsNotTheDaysRace()
        {
            Assert.IsTrue(RelayCardRules.ShowsTeam(true, false, 4));
            Assert.IsFalse(RelayCardRules.ShowsTeam(true, true, 4));
            Assert.IsFalse(RelayCardRules.ShowsTeam(false, false, 4));
            Assert.IsFalse(RelayCardRules.ShowsTeam(true, false, 1));
        }

        [Test]
        public void LegArrows_WrapAroundTheTeam()
        {
            Assert.AreEqual(3, PresetStepper.Step(0, 4, -1));
            Assert.AreEqual(0, PresetStepper.Step(3, 4, 1));
        }

        [TestCase(0, "1st leg")]
        [TestCase(1, "2nd leg")]
        [TestCase(2, "3rd leg")]
        [TestCase(3, "4th leg")]
        [TestCase(10, "11th leg")]
        [TestCase(20, "21st leg")]
        public void LegLabel_IsEnglishOrdinal(int leg, string expected)
        {
            Assert.AreEqual(expected, HomeTexts.RelayLeg(leg));
        }
    }
}
