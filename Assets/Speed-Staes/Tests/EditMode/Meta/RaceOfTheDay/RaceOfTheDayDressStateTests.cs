using NUnit.Framework;
using SpeedStars.Meta.DailyRace;

namespace SpeedStars.Tests.Meta.RaceOfTheDay
{
    /// <summary>When the day's outfit goes on: after READY, after the opponents were dressed, once per round (T-087).</summary>
    public sealed class RaceOfTheDayDressStateTests
    {
        [Test]
        public void NothingHappens_BeforeAnyRoundStarted()
        {
            var state = new RaceOfTheDayDressState();

            Assert.IsFalse(state.Pending);
            Assert.IsFalse(state.TakeApply(true, true));
        }

        [Test]
        public void ARound_AppliesTheOutfitOnce()
        {
            var state = new RaceOfTheDayDressState();
            state.RoundStarted();

            Assert.IsTrue(state.TakeApply(true, true));
            Assert.IsFalse(state.TakeApply(true, true), "one application per round");
            Assert.IsFalse(state.Pending);
        }

        [Test]
        public void RunnersNotBuiltYet_KeepsTheRoundPending()
        {
            var state = new RaceOfTheDayDressState();
            state.RoundStarted();

            Assert.IsFalse(state.TakeApply(true, false));
            Assert.IsTrue(state.Pending);
            Assert.IsTrue(state.TakeApply(true, true));
        }

        [Test]
        public void AnotherEvent_ClearsTheRoundWithoutApplying()
        {
            var state = new RaceOfTheDayDressState();
            state.RoundStarted();

            Assert.IsFalse(state.TakeApply(false, true));
            Assert.IsFalse(state.Pending);
        }

        [Test]
        public void RaceAgain_DressesAgain()
        {
            var state = new RaceOfTheDayDressState();
            state.RoundStarted();
            state.TakeApply(true, true);

            state.RoundStarted();

            Assert.IsTrue(state.TakeApply(true, true));
        }
    }
}
