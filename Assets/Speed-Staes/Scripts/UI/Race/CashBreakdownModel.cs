using System.Globalization;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.UI.Race
{
    /// <summary>The fixed sentences of the Cash breakdown under the Cash counter of the Results screen (plain ASCII).</summary>
    public static class CashBreakdownTexts
    {
        public const string FinishedRace = "Finished race";
        public const string DifficultyBonus = "Difficulty bonus";
        public const string RivalsBeaten = "Rivals beaten";

        /// <summary>"+30": the total line.</summary>
        public static string Plus(int amount) => "+" + amount.ToString(CultureInfo.InvariantCulture);

        /// <summary>"Finished race +10": one breakdown line.</summary>
        public static string Line(string label, int amount) => label + " " + Plus(amount);
    }

    /// <summary>One line of the breakdown. <see cref="Visible"/> is false for a line that paid nothing.</summary>
    public readonly struct CashBreakdownLine
    {
        public CashBreakdownLine(bool visible, string text)
        {
            Visible = visible;
            Text = text ?? string.Empty;
        }

        public bool Visible { get; }

        public string Text { get; }
    }

    /// <summary>
    /// What the Results screen shows for the Cash a race paid (economy v2): the total and up to three lines (finish,
    /// difficulty, rivals beaten). A line that paid 0 is hidden; an empty reward (free event, nothing paid) hides the
    /// whole block. Pure values; the view only writes them.
    /// </summary>
    public readonly struct CashBreakdown
    {
        public const int LineCount = 3;

        private readonly CashBreakdownLine _finish;
        private readonly CashBreakdownLine _difficulty;
        private readonly CashBreakdownLine _rivals;

        private CashBreakdown(bool visible, int total, CashBreakdownLine finish, CashBreakdownLine difficulty, CashBreakdownLine rivals)
        {
            Visible = visible;
            Total = total;
            TotalText = visible ? CashBreakdownTexts.Plus(total) : string.Empty;
            _finish = finish;
            _difficulty = difficulty;
            _rivals = rivals;
        }

        /// <summary>The block shows at all.</summary>
        public bool Visible { get; }

        /// <summary>Cash that reached the wallet.</summary>
        public int Total { get; }

        public string TotalText { get; }

        /// <summary>Lines in screen order: 0 finish, 1 difficulty, 2 rivals beaten.</summary>
        public CashBreakdownLine Line(int index)
        {
            switch (index)
            {
                case 0: return _finish;
                case 1: return _difficulty;
                case 2: return _rivals;
                default: return default;
            }
        }

        /// <summary>How many lines are visible (the reveal staggers over them).</summary>
        public int VisibleLines
        {
            get
            {
                int count = 0;
                for (int i = 0; i < LineCount; i++)
                    if (Line(i).Visible) count++;
                return count;
            }
        }

        public static CashBreakdown Build(RaceCashReward reward)
        {
            if (reward.IsEmpty) return new CashBreakdown(false, 0, default, default, default);

            return new CashBreakdown(
                true,
                reward.Total,
                LineOf(CashBreakdownTexts.FinishedRace, reward.Finish),
                LineOf(CashBreakdownTexts.DifficultyBonus, reward.Difficulty),
                LineOf(CashBreakdownTexts.RivalsBeaten, reward.Opponents));
        }

        /// <summary>
        /// The Cash counter value an animated count-up starts from: what the wallet held before the race paid
        /// (never below 0 and never above <paramref name="finalCash"/>).
        /// </summary>
        public static int CounterStart(int finalCash, int paid)
        {
            if (finalCash < 0) finalCash = 0;
            if (paid < 0) paid = 0;
            return finalCash - paid < 0 ? 0 : finalCash - paid;
        }

        private static CashBreakdownLine LineOf(string label, int amount)
        {
            return amount > 0 ? new CashBreakdownLine(true, CashBreakdownTexts.Line(label, amount)) : default;
        }
    }
}
