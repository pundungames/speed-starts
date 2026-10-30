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
