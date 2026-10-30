using System.Collections.Generic;
using NUnit.Framework;
using SpeedStars.UI.Race;

namespace SpeedStars.Tests.UI.Race
{
    /// <summary>Axes, ticks, normalising, nearest sample and ranking order of the Analysis screen. The multipliers are the config's shipped list.</summary>
    public sealed class AnalysisChartModelTests
    {
        private static readonly float[] Multipliers = { 1f, 2f, 2.5f, 5f, 10f };
        private const int Ticks = 5;

        private static SplitSeries Finished(int id, double seconds, float distance = 100f)
        {
            var points = new List<SplitPoint> { new SplitPoint(0f, 0d), new SplitPoint(distance * 0.5f, seconds * 0.45d), new SplitPoint(distance, seconds) };
            return new SplitSeries(id, points, true, seconds);
        }

        private static SplitSeries NotFinished(int id, float distance, double seconds)
        {
            var points = new List<SplitPoint> { new SplitPoint(0f, 0d), new SplitPoint(distance, seconds) };
            return new SplitSeries(id, points, false, 0d);
        }

        // ---- X axis ----

        [TestCase(60f)]
        [TestCase(100f)]
        [TestCase(110f)]
        [TestCase(200f)]
        [TestCase(400f)]
        public void DistanceAxis_EndsOnTheEventDistance(float eventDistance)
        {
            ChartAxis axis = AnalysisChartModel.DistanceAxis(eventDistance, new List<SplitSeries>(), Ticks, Multipliers);
            Assert.AreEqual(eventDistance, axis.Max, 1e-4d);
        }

        [Test]
        public void DistanceAxis_WithoutAnEventDistance_UsesTheFarthestPoint()
        {
            var series = new List<SplitSeries> { NotFinished(0, 42f, 5d), NotFinished(1, 77f, 9d) };
            Assert.AreEqual(77d, AnalysisChartModel.DistanceAxis(0f, series, Ticks, Multipliers).Max, 1e-4d);
        }

        [Test]
        public void DistanceAxis_WithNothingAtAll_IsStillUsable()
        {
            ChartAxis axis = AnalysisChartModel.DistanceAxis(0f, null, Ticks, Multipliers);
            Assert.Greater(axis.Max, 0d);
            Assert.Greater(axis.Step, 0d);
        }

        [Test]
        public void Ticks_Of100m_AreEveryTwentyAndEndOn100()
        {
            ChartAxis axis = AnalysisChartModel.DistanceAxis(100f, null, Ticks, Multipliers);
            List<double> ticks = AnalysisChartModel.Ticks(axis);

            CollectionAssert.AreEqual(new double[] { 0d, 20d, 40d, 60d, 80d, 100d }, ticks);
        }

        [Test]
        public void Ticks_Of110m_EndOn110WithoutACrampedTickBeforeIt()
        {
            ChartAxis axis = AnalysisChartModel.DistanceAxis(110f, null, Ticks, Multipliers);
            List<double> ticks = AnalysisChartModel.Ticks(axis);

            Assert.AreEqual(0d, ticks[0]);
            Assert.AreEqual(110d, ticks[ticks.Count - 1], 1e-6d);
            for (int i = 1; i < ticks.Count; i++)
                Assert.GreaterOrEqual(ticks[i] - ticks[i - 1], axis.Step * 0.5d - 1e-6d, "two ticks closer than half a step");
        }

        [Test]
        public void Ticks_AreStrictlyIncreasing_ForAnyEventDistance()
        {
            foreach (float distance in new[] { 60f, 100f, 110f, 200f, 400f, 7f, 1234f })
            {
                List<double> ticks = AnalysisChartModel.Ticks(AnalysisChartModel.DistanceAxis(distance, null, Ticks, Multipliers));
                for (int i = 1; i < ticks.Count; i++) Assert.Greater(ticks[i], ticks[i - 1], $"{distance} m");
            }
        }

        // ---- Y axis ----

        [Test]
        public void TimeAxis_CoversEveryFinisher_AndIsAMultipleOfTheStep()
        {
            var series = new List<SplitSeries> { Finished(0, 10.42d), Finished(1, 21.5d), Finished(2, 12.9d) };
            ChartAxis axis = AnalysisChartModel.TimeAxis(series, Ticks, Multipliers);

            Assert.GreaterOrEqual(axis.Max, 21.5d);
            Assert.AreEqual(0d, axis.Max % axis.Step, 1e-9d);
            Assert.Less(axis.Max, 21.5d + axis.Step); // round up to the next tick, not further
        }

        [Test]
        public void TimeAxis_AlsoCoversARunnerThatDidNotFinish()
        {
            var series = new List<SplitSeries> { Finished(0, 10d), NotFinished(1, 60f, 18d) };
            Assert.GreaterOrEqual(AnalysisChartModel.TimeAxis(series, Ticks, Multipliers).Max, 18d);
        }

        [Test]
        public void TimeAxis_ForAnEmptyRace_IsStillUsable()
        {
            ChartAxis axis = AnalysisChartModel.TimeAxis(new List<SplitSeries>(), Ticks, Multipliers);
            Assert.Greater(axis.Max, 0d);
            Assert.Greater(axis.Step, 0d);
        }

        [TestCase(0d)]
        [TestCase(-5d)]
        [TestCase(double.NaN)]
        public void NiceStep_NeverReturnsZeroOrNegative(double raw)
        {
            Assert.Greater(AnalysisChartModel.NiceStep(raw, Multipliers), 0d);
        }

        [Test]
        public void NiceStep_IsAtLeastTheRawValue()
        {
            foreach (double raw in new[] { 0.3d, 1d, 2.08d, 4.3d, 22d, 80d, 999d })
                Assert.GreaterOrEqual(AnalysisChartModel.NiceStep(raw, Multipliers), raw - 1e-9d, $"raw {raw}");
        }

        [TestCase(12d, "12")]
        [TestCase(0d, "0")]
        [TestCase(2.5d, "2.5")]
        [TestCase(7.50000001d, "7.5")]
        public void TickText_WholeNumbersWithoutDecimals(double value, string expected)
        {
            Assert.AreEqual(expected, AnalysisChartModel.TickText(value));
        }

        // ---- normalise ----

        [TestCase(0d, 100d, 0f)]
        [TestCase(50d, 100d, 0.5f)]
        [TestCase(100d, 100d, 1f)]
        [TestCase(130d, 100d, 1f)]
        [TestCase(-4d, 100d, 0f)]
        [TestCase(10d, 0d, 0f)]
        [TestCase(double.NaN, 100d, 0f)]
        public void Fraction_IsClampedToThePlot(double value, double max, float expected)
        {
            Assert.AreEqual(expected, AnalysisChartModel.Fraction(value, max), 1e-6f);
        }

        [Test]
        public void Fraction_AFasterRunnerIsLowerOnTheChart()
        {
            double max = 25d;
            Assert.Less(AnalysisChartModel.Fraction(8d, max), AnalysisChartModel.Fraction(11d, max));
        }

        // ---- nearest point ----

        [Test]
        public void NearestIndex_PicksTheClosestDistance()
        {
            var points = new List<SplitPoint>
            {
                new SplitPoint(0f, 0d), new SplitPoint(20f, 2d), new SplitPoint(40f, 4.5d), new SplitPoint(60f, 7d), new SplitPoint(100f, 12d)
            };

            Assert.AreEqual(0, AnalysisChartModel.NearestIndex(points, -10f));
            Assert.AreEqual(1, AnalysisChartModel.NearestIndex(points, 29f));
            Assert.AreEqual(2, AnalysisChartModel.NearestIndex(points, 31f));
            Assert.AreEqual(3, AnalysisChartModel.NearestIndex(points, 60f));
            Assert.AreEqual(4, AnalysisChartModel.NearestIndex(points, 500f));
        }

        [Test]
        public void NearestIndex_WithNoPoints_IsMinusOne()
        {
            Assert.AreEqual(-1, AnalysisChartModel.NearestIndex(new List<SplitPoint>(), 10f));
            Assert.AreEqual(-1, AnalysisChartModel.NearestIndex(null, 10f));
        }

        [Test]
        public void CursorText_ReadsDistanceSlashTime()
        {
            Assert.AreEqual("60 m / 10.42 s", AnalysisChartModel.CursorText(60f, 10.42d));
            Assert.AreEqual("12.5 m / 1:02.05 s", AnalysisChartModel.CursorText(12.5f, 62.05d));
        }

        // ---- ranking order ----

        [Test]
        public void Build_AlonePlayer_KeepsTheRaceRank_AndTheFinishTimeOfTheTable()
        {
            AnalysisData data = AnalysisModel.Build("100m", 7, 100f, "Ada", 1, Finished(0, 10.42d), null);

            Assert.AreEqual(1, data.Runners.Count);
            Assert.AreEqual(1, data.Runners[0].Rank);
            Assert.AreEqual(ResultsTexts.Time(10.42d), data.Runners[0].TimeText); // same text as the results table row
            Assert.IsTrue(data.Runners[0].IsPlayer);
            Assert.AreEqual("Ada", data.Runners[0].Name);
            Assert.AreEqual("7", data.CashText);
            Assert.IsTrue(data.HasChartData);
        }

        [Test]
        public void Build_OrdersByFinishTime_AndTheYouLineFollowsThePlayer()
        {
            var rivals = new List<AnalysisRival>
            {
                new AnalysisRival("A", Finished(1, 9.9d)),
                new AnalysisRival("B", Finished(2, 11.2d)),
            };
            AnalysisData data = AnalysisModel.Build("100m", 0, 100f, "", 1, Finished(0, 10.5d), rivals);

            Assert.AreEqual(1, data.Runners[0].RunnerId);
            Assert.AreEqual(0, data.Runners[1].RunnerId);
            Assert.AreEqual(2, data.Runners[2].RunnerId);
            Assert.AreEqual(1, data.PlayerIndex);
            Assert.AreEqual(2, data.Runners[1].Rank);
            Assert.IsTrue(data.Runners[1].IsPlayer);
            Assert.IsFalse(data.Runners[0].IsPlayer);
            Assert.AreEqual(ResultsTexts.DefaultPlayerName, data.Runners[1].Name);
        }

        [Test]
        public void Build_ColourSlotIsTheRunnerId_NotThePlace()
        {
            var rivals = new List<AnalysisRival> { new AnalysisRival("A", Finished(1, 9d)) };
            AnalysisData data = AnalysisModel.Build("100m", 0, 100f, "P", 1, Finished(0, 10d), rivals);

            foreach (AnalysisRunner runner in data.Runners)
                Assert.AreEqual(runner.RunnerId, runner.Series.RunnerId);
            Assert.AreEqual(AnalysisModel.PlayerRunnerId, data.Runners[data.PlayerIndex].RunnerId);
        }

        [Test]
        public void Build_RunnerThatDidNotFinish_IsLastWithDnfAndNoPlace()
        {
            var rivals = new List<AnalysisRival>
            {
                new AnalysisRival("Slow", NotFinished(1, 70f, 12d)),
                new AnalysisRival("Fast", Finished(2, 9.5d)),
            };
            AnalysisData data = AnalysisModel.Build("100m", 0, 100f, "P", 1, Finished(0, 10d), rivals);

            AnalysisRunner last = data.Runners[data.Runners.Count - 1];
            Assert.AreEqual("Slow", last.Name);
            Assert.AreEqual(AnalysisModel.DidNotFinish, last.TimeText);
            Assert.AreEqual(0, last.Rank);
            Assert.AreEqual(1, data.Runners[0].Rank);
        }

        [Test]
        public void Build_PlayerThatDidNotFinish_ShowsDnfAndNoTime()
        {
            AnalysisData data = AnalysisModel.Build("100m", 0, 100f, "P", 1, NotFinished(0, 40f, 6d), null);

            Assert.AreEqual(AnalysisModel.DidNotFinish, data.Runners[0].TimeText);
            Assert.AreEqual(0, data.Runners[0].Rank);
            Assert.IsTrue(data.HasChartData); // the partial line is real data and is drawn
        }

        [Test]
        public void Build_RaceEndedAtZero_HasNoChartData_AndDoesNotThrow()
        {
            var recorder = new RaceSplitRecorder(1f, 1f);
            recorder.Begin(100f);
            SplitSeries empty = recorder.Finish(0, 0d);

            AnalysisData data = null;
            Assert.DoesNotThrow(() => data = AnalysisModel.Build("100m", 0, 100f, "P", 1, empty, null));
            Assert.IsFalse(data.HasChartData);
            Assert.AreEqual(AnalysisModel.DidNotFinish, data.Runners[0].TimeText);

            ChartAxis x = AnalysisChartModel.DistanceAxis(data.EventDistance, new List<SplitSeries>(), Ticks, Multipliers);
            ChartAxis y = AnalysisChartModel.TimeAxis(new List<SplitSeries>(), Ticks, Multipliers);
            Assert.Greater(x.Max, 0d);
            Assert.Greater(y.Max, 0d);
        }

        [Test]
        public void Build_NegativeCash_ShowsZero()
        {
            Assert.AreEqual("0", AnalysisModel.Build("t", -5, 100f, "P", 1, Finished(0, 10d), null).CashText);
        }
    }
}
