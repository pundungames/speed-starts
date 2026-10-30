using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.Core.Race;

namespace SpeedStars.Tests.RaceStart
{
    /// <summary>
    /// The race start timing (RaceStartSequence): order Ready, Set, Go; the random SET wait stays inside its range; restart and cancel leave
    /// no old Go behind; an early or huge frame skips nothing. Test inputs are local numbers, no balance value of the game is asserted.
    /// </summary>
    public sealed class RaceStartSequenceTests
    {
        private const float Ready = 1f;
        private const float SetMin = 2f;
        private const float SetMax = 5f;

        private List<RaceStartPhase> _events;

        private RaceStartSequence Make(int seed = 1, float ready = Ready, float setMin = SetMin, float setMax = SetMax)
        {
            _events = new List<RaceStartPhase>();
            var sequence = new RaceStartSequence(ready, setMin, setMax, new Random(seed));
            sequence.PhaseChanged += _events.Add;
            return sequence;
        }

        [Test]
        public void New_IsIdle_AndTickDoesNothing()
        {
            RaceStartSequence sequence = Make();
            sequence.Tick(100f);

            Assert.AreEqual(RaceStartPhase.Idle, sequence.Phase);
            Assert.IsFalse(sequence.IsCounting);
            Assert.IsEmpty(_events);
        }

        [Test]
        public void Begin_RaisesReady_AndStartsCounting()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();

            Assert.AreEqual(RaceStartPhase.Ready, sequence.Phase);
            Assert.IsTrue(sequence.IsCounting);
            Assert.AreEqual(new[] { RaceStartPhase.Ready }, _events);
        }

        [Test]
        public void Ticks_GoThroughReadySetGo_InOrder_AndGoIsRaisedOnce()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            for (int i = 0; i < 200; i++) sequence.Tick(0.1f);

            Assert.AreEqual(new[] { RaceStartPhase.Ready, RaceStartPhase.Set, RaceStartPhase.Go }, _events);
            Assert.AreEqual(RaceStartPhase.Go, sequence.Phase);
            Assert.IsFalse(sequence.IsCounting, "after Go nothing counts any more");
        }

        [Test]
        public void StaysReady_UntilItsTimeIsUp()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(Ready - 0.01f);

            Assert.AreEqual(RaceStartPhase.Ready, sequence.Phase);
            sequence.Tick(0.02f);
            Assert.AreEqual(RaceStartPhase.Set, sequence.Phase);
        }

        [Test]
        public void SetWait_IsRandomButAlwaysInsideItsRange()
        {
            var seen = new HashSet<float>();
            for (int seed = 0; seed < 200; seed++)
            {
                RaceStartSequence sequence = Make(seed);
                sequence.Begin();
                sequence.Tick(Ready + 0.001f);

                Assert.GreaterOrEqual(sequence.SetWaitSeconds, SetMin);
                Assert.LessOrEqual(sequence.SetWaitSeconds, SetMax);
                seen.Add(sequence.SetWaitSeconds);
            }
            Assert.Greater(seen.Count, 20, "the wait must vary between starts, or GO could be timed");
        }

        [Test]
        public void Go_NeverComesBeforeTheShortestWait_AndAlwaysByTheLongest()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                RaceStartSequence sequence = Make(seed);
                sequence.Begin();
                sequence.Tick(Ready + SetMin - 0.01f);
                Assert.AreNotEqual(RaceStartPhase.Go, sequence.Phase, $"seed {seed}: Go before the shortest wait");

                sequence.Tick(SetMax - SetMin + 0.02f);
                Assert.AreEqual(RaceStartPhase.Go, sequence.Phase, $"seed {seed}: no Go after the longest wait");
            }
        }

        [Test]
        public void OneHugeFrame_RaisesEveryPhaseInOrder_NothingSkipped_NothingDelayed()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(1000f);

            Assert.AreEqual(new[] { RaceStartPhase.Ready, RaceStartPhase.Set, RaceStartPhase.Go }, _events);
        }

        [Test]
        public void Cancel_GoesIdle_AndOldTimeCannotRaiseGo()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(Ready + 0.5f);
            sequence.Cancel();
            sequence.Tick(1000f);

            Assert.AreEqual(RaceStartPhase.Idle, sequence.Phase);
            Assert.AreEqual(new[] { RaceStartPhase.Ready, RaceStartPhase.Set, RaceStartPhase.Idle }, _events);
        }

        [Test]
        public void Cancel_WhenAlreadyIdle_RaisesNothing()
        {
            RaceStartSequence sequence = Make();
            sequence.Cancel();
            sequence.Begin();
            sequence.Cancel();
            sequence.Cancel();

            Assert.AreEqual(new[] { RaceStartPhase.Ready, RaceStartPhase.Idle }, _events);
        }

        [Test]
        public void RestartDuringSet_StartsOver_WithOneGoOnly()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(Ready + 0.5f); // in Set
            _events.Clear();

            sequence.Begin();
            Assert.AreEqual(new[] { RaceStartPhase.Ready }, _events, "a restart shows Ready again");

            sequence.Tick(Ready - 0.01f);
            Assert.AreEqual(RaceStartPhase.Ready, sequence.Phase, "the old Set time must not carry over");

            sequence.Tick(1000f);
            int goCount = 0;
            foreach (RaceStartPhase phase in _events) if (phase == RaceStartPhase.Go) goCount++;
            Assert.AreEqual(1, goCount);
        }

        [Test]
        public void RestartAfterGo_RunsTheWholeCountdownAgain()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(1000f);
            _events.Clear();

            sequence.Begin();
            sequence.Tick(1000f);

            Assert.AreEqual(new[] { RaceStartPhase.Ready, RaceStartPhase.Set, RaceStartPhase.Go }, _events);
        }

        [Test]
        public void HandlerThatCancelsOnSet_StopsTheTickAtOnce()
        {
            RaceStartSequence sequence = Make();
            sequence.PhaseChanged += phase => { if (phase == RaceStartPhase.Set) sequence.Cancel(); };
            sequence.Begin();
            sequence.Tick(1000f);

            Assert.AreEqual(RaceStartPhase.Idle, sequence.Phase);
            CollectionAssert.DoesNotContain(_events, RaceStartPhase.Go);
        }

        [Test]
        public void HandlerThatRestartsOnSet_DoesNotLeakAGoFromTheOldRun()
        {
            RaceStartSequence sequence = Make();
            bool restarted = false;
            sequence.PhaseChanged += phase =>
            {
                if (phase == RaceStartPhase.Set && !restarted)
                {
                    restarted = true;
                    sequence.Begin();
                }
            };
            sequence.Begin();
            sequence.Tick(Ready + 0.01f);

            Assert.AreEqual(RaceStartPhase.Ready, sequence.Phase, "the restart inside the handler wins");
            CollectionAssert.DoesNotContain(_events, RaceStartPhase.Go);
        }

        [Test]
        public void NegativeOrZeroTime_IsIgnored()
        {
            RaceStartSequence sequence = Make();
            sequence.Begin();
            sequence.Tick(-5f);
            sequence.Tick(0f);
            sequence.Tick(Ready - 0.01f);

            Assert.AreEqual(RaceStartPhase.Ready, sequence.Phase);
        }

        [Test]
        public void SwappedOrNegativeRange_IsNormalised()
        {
            RaceStartSequence sequence = Make(1, Ready, SetMax, SetMin); // min and max swapped
            sequence.Begin();
            sequence.Tick(Ready + 0.001f);
            Assert.GreaterOrEqual(sequence.SetWaitSeconds, SetMin);
            Assert.LessOrEqual(sequence.SetWaitSeconds, SetMax);

            RaceStartSequence negative = Make(1, -3f, -1f, -2f);
            negative.Begin();
            negative.Tick(0.001f);
            Assert.AreEqual(RaceStartPhase.Go, negative.Phase, "negative times count as zero: straight to Go");
        }

        [Test]
        public void NullRandom_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new RaceStartSequence(1f, 1f, 2f, null));
        }
    }
}
