using NUnit.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>The player's split recording: step, interpolation, start and finish points, samples that must not count.</summary>
    public sealed class RaceSplitRecorderTests
    {
        private const float Target = 100f;
        private const float Step = 10f;
        private const float Tolerance = 1f;

        private static RaceSplitRecorder Started()
        {
            var recorder = new RaceSplitRecorder(Step, Tolerance);
            recorder.Begin(Target);
            return recorder;
        }

        [Test]
        public void Begin_StartsAtZeroZero()
        {
            RaceSplitRecorder recorder = Started();
            SplitSeries series = recorder.Finish(0, 0d);

            Assert.AreEqual(1, series.Points.Count);
            Assert.AreEqual(0f, series.Points[0].Distance);
            Assert.AreEqual(0d, series.Points[0].Seconds);
        }

        [Test]
        public void Sample_KeepsOnePointPerStep()
        {
            RaceSplitRecorder recorder = Started();
            for (int i = 1; i <= 35; i++) recorder.Sample(i, i * 0.1d); // 1 m per sample

            SplitSeries series = recorder.Finish(0, 0d);
            Assert.AreEqual(4, series.Points.Count); // 0, 10, 20, 30
            Assert.AreEqual(30f, series.Points[3].Distance, 1e-4f);
            Assert.AreEqual(3.0d, series.Points[3].Seconds, 1e-6d);
        }

        [Test]
        public void Sample_FillsSkippedStepsByInterpolation()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(25f, 5d); // one big frame over two steps

            SplitSeries series = recorder.Finish(0, 0d);
            Assert.AreEqual(3, series.Points.Count);
            Assert.AreEqual(10f, series.Points[1].Distance, 1e-4f);
            Assert.AreEqual(2d, series.Points[1].Seconds, 1e-6d);
            Assert.AreEqual(20f, series.Points[2].Distance, 1e-4f);
            Assert.AreEqual(4d, series.Points[2].Seconds, 1e-6d);
        }

        [Test]
        public void Sample_IgnoresBackwardsRepeatedAndBadSamples()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(12f, 2d);
            recorder.Sample(5f, 3d);                       // distance went back (a reset)
            recorder.Sample(12f, 4d);                      // no progress
            recorder.Sample(20f, 1d);                      // time went back
            recorder.Sample(float.NaN, 5d);
            recorder.Sample(30f, double.PositiveInfinity);
            recorder.Sample(21f, 4d);

            SplitSeries series = recorder.Finish(0, 0d);
            Assert.AreEqual(3, series.Points.Count); // 0, 10, 20
            Assert.AreEqual(20f, series.Points[2].Distance, 1e-4f);
            for (int i = 1; i < series.Points.Count; i++)
            {
                Assert.Greater(series.Points[i].Distance, series.Points[i - 1].Distance);
                Assert.GreaterOrEqual(series.Points[i].Seconds, series.Points[i - 1].Seconds);
            }
        }

        [Test]
        public void Finish_ReachingTheLine_EndsExactlyOnEventDistanceAndFinishTime()
        {
            RaceSplitRecorder recorder = Started();
            for (int i = 1; i <= 995; i++) recorder.Sample(i * 0.1f, i * 0.01d); // last real sample 99.5 m
            SplitSeries series = recorder.Finish(0, 10.42d);

            Assert.IsTrue(series.Finished);
            Assert.AreEqual(10.42d, series.FinishSeconds);
            SplitPoint last = series.Points[series.Points.Count - 1];
            Assert.AreEqual(Target, last.Distance);
            Assert.AreEqual(10.42d, last.Seconds);
            Assert.IsTrue(series.HasLine);
        }

        [Test]
        public void Finish_SamplePastTheLine_DoesNotLeaveTwoPointsAtTheEnd()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(100.3f, 10d); // passes the line inside the frame; the 100 m boundary point is made by interpolation
            SplitSeries series = recorder.Finish(0, 9.97d);

            int atEnd = 0;
            foreach (SplitPoint p in series.Points) if (p.Distance >= Target - 1e-3f) atEnd++;
            Assert.AreEqual(1, atEnd);
            Assert.AreEqual(9.97d, series.Last.Seconds);
        }

        [Test]
        public void Finish_DropsPointsLaterThanTheResultTime()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(100f, 10.5d); // the runner's own clock ran a bit ahead of the result time
            SplitSeries series = recorder.Finish(0, 10.0d);

            foreach (SplitPoint p in series.Points) Assert.LessOrEqual(p.Seconds, 10.0d);
            Assert.AreEqual(10.0d, series.Last.Seconds);
        }

        [Test]
        public void Finish_ShortOfTheLine_IsNotFinishedAndHasNoInventedTime()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(40f, 5d);
            SplitSeries series = recorder.Finish(0, 8d);

            Assert.IsFalse(series.Finished);
            Assert.AreEqual(0d, series.FinishSeconds);
            Assert.AreEqual(40f, series.Last.Distance, 1e-4f); // ends on the last real point, not at the line
        }

        [Test]
        public void Finish_ZeroTimeEvenAtTheLine_IsNotFinished()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(100f, 10d);
            SplitSeries series = recorder.Finish(0, 0d);

            Assert.IsFalse(series.Finished);
        }

        [Test]
        public void Finish_ForceFinishAtStart_GivesAnEmptyChartNotACrash()
        {
            RaceSplitRecorder recorder = Started();
            SplitSeries series = recorder.Finish(0, 0.02d); // a dev finish at ~0 s / 0 m

            Assert.IsFalse(series.Finished);
            Assert.IsFalse(series.HasLine);
        }

        [Test]
        public void Finish_WithoutBegin_IsEmptyAndNotFinished()
        {
            var recorder = new RaceSplitRecorder(Step, Tolerance);
            recorder.Sample(50f, 5d); // ignored: no run began
            SplitSeries series = recorder.Finish(0, 9d);

            Assert.IsFalse(series.Finished);
            Assert.AreEqual(0, series.Points.Count);
        }

        [Test]
        public void Begin_DropsThePreviousRun()
        {
            RaceSplitRecorder recorder = Started();
            recorder.Sample(60f, 6d);
            recorder.Begin(110f);

            SplitSeries series = recorder.Finish(0, 0d);
            Assert.AreEqual(1, series.Points.Count);
        }

        [Test]
        public void Step_BelowMinimum_IsClampedNotAnEndlessLoop()
        {
            var recorder = new RaceSplitRecorder(0f, 1f);
            recorder.Begin(2f);
            recorder.Sample(1f, 1d);

            SplitSeries series = recorder.Finish(0, 0d);
            Assert.Less(series.Points.Count, 500);
        }
    }
}
