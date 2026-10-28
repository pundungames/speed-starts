namespace SpeedStars.UI.Home
{
    /// <summary>
    /// Previous / next arrows of the character card: the preset index one step away, wrapping around at both ends.
    /// Pure, so the wrap rule is tested without a scene.
    /// </summary>
    public static class PresetStepper
    {
        /// <param name="direction">-1 previous, +1 next; any other value is treated by its sign, 0 stays.</param>
        /// <returns>The new index; 0 when there are no presets.</returns>
        public static int Step(int current, int count, int direction)
        {
            if (count <= 0) return 0;
            int step = direction > 0 ? 1 : direction < 0 ? -1 : 0;
            int next = (current + step) % count;
            return next < 0 ? next + count : next;
        }
    }
}
