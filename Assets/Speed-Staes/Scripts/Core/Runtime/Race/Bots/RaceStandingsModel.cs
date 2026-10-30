using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>
    /// Live order of the race (player and bots). Each frame the race scene hands in every runner (always in the same order, one entry per
    /// runner); the model sorts them: finished runners by time, then runners on the track by distance. <see cref="Changed"/> is raised
    /// only when the order or the set of finished runners changes, not on every frame. <see cref="Reset"/> returns to the empty start.
    /// </summary>
    public sealed class RaceStandingsModel : IRaceStandings
    {
        private readonly List<RaceEntry> _entries = new List<RaceEntry>();
        private int[] _order = new int[0];
        private int[] _lastOrder = new int[0];
        private bool[] _lastFinished = new bool[0];
        private int _playerRank;

        public IReadOnlyList<RaceEntry> Entries => _entries;
        public int PlayerRank => _playerRank;
        public int RunnerCount => _entries.Count;
        public event Action Changed;

        /// <summary>Feeds this frame's runners. Keep the input order the same between frames.</summary>
        public void Update(IReadOnlyList<RaceEntry> runners)
        {
            if (runners == null) throw new ArgumentNullException(nameof(runners));
            int n = runners.Count;
            if (_order.Length != n) _order = new int[n];
            for (int i = 0; i < n; i++) _order[i] = i;

            // Insertion sort by (input index) stable rule; no allocation per frame.
            for (int i = 1; i < n; i++)
            {
                int cur = _order[i];
                int j = i - 1;
                while (j >= 0 && Before(runners[cur], runners[_order[j]])) { _order[j + 1] = _order[j]; j--; }
                _order[j + 1] = cur;
            }

            _entries.Clear();
            _playerRank = 0;
            for (int i = 0; i < n; i++)
            {
                RaceEntry e = runners[_order[i]];
                _entries.Add(e);
                if (e.IsPlayer && _playerRank == 0) _playerRank = i + 1;
            }

            bool changed = _lastOrder.Length != n;
            if (!changed)
            {
                for (int i = 0; i < n && !changed; i++)
                    changed = _lastOrder[i] != _order[i] || _lastFinished[i] != _entries[i].Finished;
            }
            if (_lastOrder.Length != n) { _lastOrder = new int[n]; _lastFinished = new bool[n]; }
            for (int i = 0; i < n; i++) { _lastOrder[i] = _order[i]; _lastFinished[i] = _entries[i].Finished; }

            if (changed) Changed?.Invoke();
        }

        /// <summary>Back to the start line: nobody listed, no rank. Raises nothing.</summary>
        public void Reset()
        {
            _entries.Clear();
            _playerRank = 0;
            _order = new int[0];
            _lastOrder = new int[0];
            _lastFinished = new bool[0];
        }

        /// <summary>True when <paramref name="a"/> must stand ahead of <paramref name="b"/>. Equal = false, so ties keep input order.</summary>
        private static bool Before(RaceEntry a, RaceEntry b)
        {
            if (a.Finished != b.Finished) return a.Finished;
            if (a.Finished) return a.TimeSeconds < b.TimeSeconds;
            return a.DistanceMeters > b.DistanceMeters;
        }
    }
}
