using System;
using System.Collections.Generic;

namespace SpeedStars.UI.Race
{
    /// <summary>One runner of the Analysis screen: chart line, ranking row.</summary>
    public readonly struct AnalysisRunner
    {
        /// <summary>Stable slot of the runner (the player is 0); the line colour follows it, never the place.</summary>
        public readonly int RunnerId;
        public readonly string Name;

        /// <summary>Finishing place, 0 when the runner did not finish.</summary>
        public readonly int Rank;
        public readonly string TimeText;
        public readonly bool IsPlayer;
        public readonly SplitSeries Series;

        public AnalysisRunner(int runnerId, string name, int rank, string timeText, bool isPlayer, SplitSeries series)
        {
            RunnerId = runnerId;
            Name = name ?? string.Empty;
            Rank = rank;
            TimeText = timeText ?? string.Empty;
            IsPlayer = isPlayer;
            Series = series;
        }

        public bool Finished => Series != null && Series.Finished;
    }

    /// <summary>Another runner's recorded series (the mechanics hand these over later; none exist today).</summary>
    public readonly struct AnalysisRival
    {
        public readonly string Name;
        public readonly SplitSeries Series;

        public AnalysisRival(string name, SplitSeries series)
        {
            Name = name ?? string.Empty;
            Series = series;
        }
    }

    /// <summary>Everything the Analysis screen shows for the last race, ready to draw.</summary>
    public sealed class AnalysisData
    {
        private static readonly AnalysisRunner[] NoRunners = new AnalysisRunner[0];

        public AnalysisData(string eventTitle, string cashText, float eventDistance, IReadOnlyList<AnalysisRunner> runners)
        {
            EventTitle = eventTitle ?? string.Empty;
            CashText = cashText ?? string.Empty;
            EventDistance = eventDistance;
            Runners = runners ?? NoRunners;
        }

        public string EventTitle { get; }
        public string CashText { get; }
        public float EventDistance { get; }

        /// <summary>Ranking order: finishers by time, then the ones that did not finish.</summary>
        public IReadOnlyList<AnalysisRunner> Runners { get; }

        /// <summary>False when no runner has a line to draw (e.g. a race ended at 0 s / 0 m): the chart shows its empty state.</summary>
        public bool HasChartData
        {
            get
            {
                foreach (AnalysisRunner runner in Runners)
                    if (runner.Series != null && runner.Series.HasLine) return true;
                return false;
            }
        }

        /// <summary>Index of the local player in <see cref="Runners"/>, or -1.</summary>
        public int PlayerIndex
        {
            get
            {
                for (int i = 0; i < Runners.Count; i++)
                    if (Runners[i].IsPlayer) return i;
                return -1;
            }
        }
    }

    /// <summary>
    /// Pure rules that turn recorded series into the Analysis screen data. Order is the real finishing order (by time);
    /// the player is wherever the times put them. A runner that did not finish gets no time ("DNF") and no place.
    /// </summary>
    public static class AnalysisModel
    {
        public const int PlayerRunnerId = 0;
        public const string DidNotFinish = "DNF";

        public static AnalysisData Build(string eventTitle, int cash, float eventDistance, string playerName, int playerRank,
            SplitSeries playerSeries, IReadOnlyList<AnalysisRival> rivals)
        {
            string name = string.IsNullOrWhiteSpace(playerName) ? ResultsTexts.DefaultPlayerName : playerName.Trim();

            var entries = new List<Entry>();
            entries.Add(new Entry(PlayerRunnerId, name, true, playerSeries, entries.Count));
            if (rivals != null)
            {
                for (int i = 0; i < rivals.Count; i++)
                    entries.Add(new Entry(PlayerRunnerId + 1 + i, rivals[i].Name, false, rivals[i].Series, entries.Count));
            }

            // Finishers by time (ties keep the input order, the player first); the others after them.
            entries.Sort((a, b) =>
            {
                bool fa = a.Series != null && a.Series.Finished;
                bool fb = b.Series != null && b.Series.Finished;
                if (fa != fb) return fa ? -1 : 1;
                if (fa)
                {
                    int byTime = a.Series.FinishSeconds.CompareTo(b.Series.FinishSeconds);
                    if (byTime != 0) return byTime;
                }
                return a.Order.CompareTo(b.Order);
            });

            var runners = new List<AnalysisRunner>(entries.Count);
            bool alone = entries.Count == 1;
            int place = 0;
            foreach (Entry e in entries)
            {
                bool finished = e.Series != null && e.Series.Finished;
                int rank = 0;
                string time = DidNotFinish;
                if (finished)
                {
                    place++;
                    rank = alone && playerRank >= 1 ? playerRank : place; // alone, the race's own rank is the truth
                    time = ResultsTexts.Time(e.Series.FinishSeconds);
                }
                runners.Add(new AnalysisRunner(e.RunnerId, e.Name, rank, time, e.IsPlayer, e.Series));
            }

            return new AnalysisData(eventTitle, Math.Max(0, cash).ToString(System.Globalization.CultureInfo.InvariantCulture), eventDistance, runners);
        }

        private readonly struct Entry
        {
            public readonly int RunnerId;
            public readonly string Name;
            public readonly bool IsPlayer;
            public readonly SplitSeries Series;
            public readonly int Order;

            public Entry(int runnerId, string name, bool isPlayer, SplitSeries series, int order)
            {
                RunnerId = runnerId;
                Name = name;
                IsPlayer = isPlayer;
                Series = series;
                Order = order;
            }
        }
    }

    /// <summary>
    /// What the Results screen offers the flow when it can open the Analysis screen: the recording numbers, and a place to
    /// hand the data of the finished race. The flow does not know the Analysis view.
    /// </summary>
    public interface IAnalysisSink
    {
        float SplitStepMeters { get; }
        float FinishToleranceMeters { get; }

        /// <summary>The data of the race that just finished (kept until the next one).</summary>
        void PresentAnalysis(AnalysisData data);
    }
}
