namespace SpeedStars.UI.Framework
{
    /// <summary>
    /// Stagger spacing rule (agent-kit/05 §4): a list never takes longer than the cap, however many items it has,
    /// so the interval shrinks for long lists: interval = min(target, cap / count).
    /// </summary>
    public static class UiStagger
    {
        /// <param name="targetInterval">Wanted delay between two items (s).</param>
        /// <param name="maxTotal">Cap for the whole list (s); 0 or less = no cap.</param>
        /// <param name="count">Number of items that will reveal.</param>
        public static float Interval(float targetInterval, float maxTotal, int count)
        {
            float target = targetInterval > 0f ? targetInterval : 0f;
            if (count <= 0 || maxTotal <= 0f) return target;

            float capped = maxTotal / count;
            return capped < target ? capped : target;
        }
    }
}
