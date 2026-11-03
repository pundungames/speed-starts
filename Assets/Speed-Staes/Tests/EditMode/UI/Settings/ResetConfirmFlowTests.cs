using NUnit.Framework;
using SpeedStars.UI.Settings;

namespace SpeedStars.Tests.UI.Settings
{
    public sealed class ResetConfirmFlowTests
    {
        [Test]
        public void NewFlow_IsClosed_AndConfirmDoesNothing()
        {
            var flow = new ResetConfirmFlow();

            Assert.IsFalse(flow.IsOpen);
            Assert.AreEqual(ResetKind.None, flow.Confirm());
        }

        [Test]
        public void PersonalBests_NeedsOneYes()
        {
            var flow = new ResetConfirmFlow();
            flow.Begin(ResetKind.PersonalBests);

            Assert.IsTrue(flow.IsFinalStep);
            Assert.AreEqual(ResetKind.PersonalBests, flow.Confirm());
            Assert.IsFalse(flow.IsOpen);
        }

        [Test]
        public void AllProgress_NeedsTwoYes_AndOnlyTheSecondRuns()
        {
            var flow = new ResetConfirmFlow();
            flow.Begin(ResetKind.AllProgress);

            Assert.IsFalse(flow.IsFinalStep);
            Assert.AreEqual(ResetKind.None, flow.Confirm());
            Assert.AreEqual(ResetStep.Second, flow.Step);
            Assert.IsTrue(flow.IsFinalStep);
            Assert.AreEqual(ResetKind.AllProgress, flow.Confirm());
            Assert.IsFalse(flow.IsOpen);
        }

        [Test]
        public void Cancel_AtAnyStep_RunsNothing_AndRestartsFromTheFirstQuestion()
        {
            var flow = new ResetConfirmFlow();
            flow.Begin(ResetKind.AllProgress);
            flow.Confirm();
            flow.Cancel();

            Assert.IsFalse(flow.IsOpen);
            Assert.AreEqual(ResetKind.None, flow.Confirm());

            flow.Begin(ResetKind.AllProgress);
            Assert.AreEqual(ResetStep.First, flow.Step);
        }

        [Test]
        public void TheNewResets_NeedOneYes_AndRunWhatWasAsked()
        {
            foreach (ResetKind kind in new[] { ResetKind.OnePersonalBest, ResetKind.CustomRacers })
            {
                var flow = new ResetConfirmFlow();
                flow.Begin(kind);

                Assert.IsTrue(flow.IsFinalStep, kind.ToString());
                Assert.AreEqual(kind, flow.Confirm());
                Assert.IsFalse(flow.IsOpen);
            }
        }

        [Test]
        public void TheNewQuestions_HaveWords_AndTheBodyNamesTheEvent()
        {
            foreach (ResetKind kind in new[] { ResetKind.OnePersonalBest, ResetKind.CustomRacers })
            {
                Assert.IsNotEmpty(SettingsTexts.ConfirmTitle(kind, ResetStep.First));
                Assert.IsNotEmpty(SettingsTexts.ConfirmBody(kind, ResetStep.First, "Test Event"));
            }
            StringAssert.Contains("Test Event", SettingsTexts.ConfirmBody(ResetKind.OnePersonalBest, ResetStep.First, "Test Event"));
        }

        [Test]
        public void LaneOptions_AreAutoThenOneToCount()
        {
            string[] options = SettingsTexts.LaneOptions(4);

            Assert.AreEqual(5, options.Length);
            Assert.AreEqual(SettingsTexts.LaneAuto, options[0]);
            Assert.AreEqual("1", options[1]);
            Assert.AreEqual("4", options[4]);
        }

        [Test]
        public void SliderSnap_RoundsToTheStep_AndStaysInsideTheRange()
        {
            Assert.AreEqual(0.5f, SliderSnap.Snap(0.52f, 0.1f, 0f, 1f), 1e-5f);
            Assert.AreEqual(1f, SliderSnap.Snap(3f, 0.1f, 0f, 1f), 1e-5f);
            Assert.AreEqual(0f, SliderSnap.Snap(-3f, 0.1f, 0f, 1f), 1e-5f);
        }

        [Test]
        public void BeginNone_OpensNothing()
        {
            var flow = new ResetConfirmFlow();
            flow.Begin(ResetKind.None);

            Assert.IsFalse(flow.IsOpen);
        }

        [Test]
        public void EveryQuestion_HasWords_AndOnlyTheLastSaysReset()
        {
            foreach (ResetKind kind in new[] { ResetKind.PersonalBests, ResetKind.AllProgress })
            foreach (ResetStep step in new[] { ResetStep.First, ResetStep.Second })
            {
                Assert.IsNotEmpty(SettingsTexts.ConfirmTitle(kind, step));
                Assert.IsNotEmpty(SettingsTexts.ConfirmBody(kind, step));
            }
            Assert.AreEqual(SettingsTexts.ResetButton, SettingsTexts.ConfirmButton(true));
            Assert.AreNotEqual(SettingsTexts.ResetButton, SettingsTexts.ConfirmButton(false));
        }
    }
}
