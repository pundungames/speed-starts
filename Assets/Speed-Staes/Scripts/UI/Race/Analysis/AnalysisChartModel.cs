using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpeedStars.UI.Race
{
    /// <summary>An axis: runs 0..Max, ticks every Step (plus the end when it falls between ticks).</summary>
    public readonly struct ChartAxis
    {
        public readonly double Max;
        public readonly double Step;

        public ChartAxis(double max, double step)
        {
            Max = max;
            Step = step;
        }
    }

    /// <summary>
    /// Pure maths of the Analysis chart (no Unity types): axis limits and ticks computed at run time from the data
    /// (the art grid is only a template), normalising points to the plot rectangle, finding the sample nearest to a
    /// pointer, and the balloon text.
    /// X = distance / event distance; Y = cumulative time / Y limit (a lower line means the same distance in less time).
    /// </summary>
    public static class AnalysisChartModel
    {
        private const double MinYData = 1d; // an empty or tiny run still gets a readable 0..N axis

        /// <summary>X limit: the event distance (100 m race ends at 100, 110 m hurdles at 110); without one, the farthest point drawn.</summary>
        public static ChartAxis DistanceAxis(float eventDistance, IReadOnlyList<SplitSeries> series, int targetTicks, IReadOnlyList<float> multipliers)
        {
            double max = eventDistance;
            if (max <= 0d)
            {
                max = 0d;
                if (series != null)
                    foreach (SplitSeries s in series)
                        if (s != null && s.Points.Count > 0 && s.Last.Distance > max) max = s.Last.Distance;
                if (max <= 0d) max = 100d;
            }
            return new ChartAxis(max, NiceStep(max / Math.Max(2, targetTicks), multipliers));
        }

        /// <summary>
        /// Y limit: a round value that covers every drawn series (finish times of finishers, the last real sample of the
        /// others), a multiple of the tick step.
        /// </summary>
        public static ChartAxis TimeAxis(IReadOnlyList<SplitSeries> series, int targetTicks, IReadOnlyList<float> multipliers)
        {
            double data = 0d;
            if (series != null)
            {
                foreach (SplitSeries s in series)
                {
                    if (s == null || s.Points.Count == 0) continue;
                    double top = s.Finished ? s.FinishSeconds : s.Last.Seconds;
                    if (top > data) data = top;
                }
            }
            if (data < MinYData) data = MinYData;

            double step = NiceStep(data / Math.Max(2, targetTicks), multipliers);
            double max = Math.Ceiling(data / step - 1e-9) * step;
            return new ChartAxis(max, step);
        }

        /// <summary>
        /// Smallest "nice" step (multiplier x power of ten) that is at least <paramref name="raw"/>. Without multipliers the
        /// classic 1 / 2 / 5 / 10 is used. Never zero or negative.
        /// </summary>
        public static double NiceStep(double raw, IReadOnlyList<float> multipliers)
        {
            if (double.IsNaN(raw) || double.IsInfinity(raw) || raw <= 0d) return 1d;

            double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(raw)));
            IReadOnlyList<float> list = multipliers != null && multipliers.Count > 0 ? multipliers : DefaultMultipliers;
            foreach (float m in list)
            {
                if (m <= 0f) continue;
                double candidate = m * magnitude;
                if (candidate >= raw - raw * 1e-9) return candidate;
            }
            return 10d * magnitude;
        }

        private static readonly float[] DefaultMultipliers = { 1f, 2f, 5f, 10f };

        /// <summary>
        /// Tick values 0, step, 2 step ... up to the axis end; the end itself is always a tick, and the multiple just
        /// before it is dropped when it would sit closer than half a step (110 m with a 25 step: 0, 25, 50, 75, 110).
        /// </summary>
        public static List<double> Ticks(ChartAxis axis)
        {
            var ticks = new List<double>();
            if (axis.Step <= 0d || axis.Max <= 0d)
            {
                ticks.Add(0d);
                return ticks;
            }

            int count = (int)Math.Floor(axis.Max / axis.Step + 1e-9);
            for (int i = 0; i <= count; i++) ticks.Add(i * axis.Step);

            double last = ticks[ticks.Count - 1];
            if (axis.Max - last > 1e-6)
            {
                if (axis.Max - last < axis.Step * 0.5d && ticks.Count > 1) ticks.RemoveAt(ticks.Count - 1);
                ticks.Add(axis.Max);
            }
            return ticks;
        }

        /// <summary>Tick label: whole numbers when the value is whole, one decimal otherwise ("12", "2.5").</summary>
        public static string TickText(double value)
        {
            if (Math.Abs(value - Math.Round(value)) < 1e-6) return Math.Round(value).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }

        /// <summary>Position of a value on an axis as 0..1 (clamped); a bad axis gives 0.</summary>
        public static float Fraction(double value, double axisMax)
        {
            if (axisMax <= 0d || double.IsNaN(value)) return 0f;
            double f = value / axisMax;
            if (f < 0d) return 0f;
            if (f > 1d) return 1f;
            return (float)f;
        }

        /// <summary>Index of the point whose distance is closest to <paramref name="distance"/>; -1 when there are no points.</summary>
        public static int NearestIndex(IReadOnlyList<SplitPoint> points, float distance)
        {
            if (points == null || points.Count == 0) return -1;

            int best = 0;
            float bestGap = Math.Abs(points[0].Distance - distance);
            for (int i = 1; i < points.Count; i++)
            {
                float gap = Math.Abs(points[i].Distance - distance);
                if (gap < bestGap)
                {
                    best = i;
                    bestGap = gap;
                }
            }
            return best;
        }

        /// <summary>Balloon text "60 m / 10.42 s" (a whole distance without decimals, otherwise one).</summary>
        public static string CursorText(float distance, double seconds)
        {
            string meters = Math.Abs(distance - Math.Round(distance)) < 0.05f
                ? ((long)Math.Round(distance)).ToString(CultureInfo.InvariantCulture)
                : distance.ToString("0.#", CultureInfo.InvariantCulture);
            return meters + " m / " + RaceDisplayFormat.Time(seconds) + " s";
        }
    }
}
