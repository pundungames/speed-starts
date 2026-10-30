using System;
using System.Collections.Generic;
using SpeedStars.Core.Race;

namespace SpeedStars.UI.Leaderboard
{
    /// <summary>One distance button of the event modal: the race and its label.</summary>
    public readonly struct LeaderboardEventOption
    {
        public readonly RaceEventId Id;
        public readonly string Label;

        public LeaderboardEventOption(RaceEventId id, string label)
        {
            Id = id;
            Label = label ?? string.Empty;
        }
    }

    /// <summary>One category tab (SPRINTS / HURDLES / RELAYS) and the races it offers; none = "NO EVENTS AVAILABLE".</summary>
    public sealed class LeaderboardEventGroup
    {
        public readonly string Title;
        public readonly IReadOnlyList<LeaderboardEventOption> Options;

        public LeaderboardEventGroup(string title, IReadOnlyList<LeaderboardEventOption> options)
        {
            Title = title ?? string.Empty;
            Options = options ?? Array.Empty<LeaderboardEventOption>();
        }
    }

    /// <summary>The event select layout's category entry in plain form (title and race ids).</summary>
    public readonly struct LeaderboardEventLayoutEntry
    {
        public readonly string Title;
        public readonly IReadOnlyList<string> Ids;

        public LeaderboardEventLayoutEntry(string title, IReadOnlyList<string> ids)
        {
            Title = title ?? string.Empty;
            Ids = ids ?? Array.Empty<string>();
        }
    }

    /// <summary>Pure rules around the category / distance list, no Unity types.</summary>
    public static class LeaderboardEventGroups
    {
        /// <summary>
        /// Groups in layout order. <paramref name="labelOf"/> returns the button label of a race id, or null/empty when the
        /// race does not exist or is not playable (it is left out: no event is made up).
        /// </summary>
        public static List<LeaderboardEventGroup> Build(IEnumerable<LeaderboardEventLayoutEntry> layout, Func<string, string> labelOf)
        {
            var groups = new List<LeaderboardEventGroup>();
            if (layout == null) return groups;
            foreach (LeaderboardEventLayoutEntry entry in layout)
            {
                var options = new List<LeaderboardEventOption>();
                foreach (string id in entry.Ids)
                {
                    string label = labelOf != null ? labelOf(id) : null;
                    if (!string.IsNullOrEmpty(label)) options.Add(new LeaderboardEventOption(new RaceEventId(id), label));
                }
                groups.Add(new LeaderboardEventGroup(entry.Title, options));
            }
            return groups;
        }

        public static bool Find(IReadOnlyList<LeaderboardEventGroup> groups, RaceEventId id, out int groupIndex, out LeaderboardEventOption option)
        {
            if (groups != null && !id.IsEmpty)
            {
                for (int g = 0; g < groups.Count; g++)
                {
                    foreach (LeaderboardEventOption candidate in groups[g].Options)
                    {
                        if (candidate.Id != id) continue;
                        groupIndex = g;
                        option = candidate;
                        return true;
                    }
                }
            }
            groupIndex = -1;
            option = default;
            return false;
        }

        /// <summary>The race to open with: <paramref name="preferred"/> when it is on offer, else the first race of the first group that has one, else empty.</summary>
        public static RaceEventId Resolve(IReadOnlyList<LeaderboardEventGroup> groups, RaceEventId preferred)
        {
            if (Find(groups, preferred, out _, out _)) return preferred;
            if (groups != null)
            {
                foreach (LeaderboardEventGroup group in groups)
                    if (group.Options.Count > 0) return group.Options[0].Id;
            }
            return default;
        }

        /// <summary>"SPRINTS · 100m" for the filter field; empty when the race is not on offer.</summary>
        public static string FilterLabel(IReadOnlyList<LeaderboardEventGroup> groups, RaceEventId id)
        {
            if (!Find(groups, id, out int g, out LeaderboardEventOption option)) return string.Empty;
            string title = groups[g].Title;
            return string.IsNullOrEmpty(title) ? option.Label : title + LeaderboardTexts.EventSeparator + option.Label;
        }
    }

    /// <summary>
    /// State of the event modal while it is open: the shown category and the chosen race. Changing the category does not
    /// carry the old distance over; APPLY is allowed only with a race of the shown category chosen. Cancel / X simply
    /// discard the picker (the screen's active filter was never touched).
    /// </summary>
    public sealed class LeaderboardEventPicker
    {
        private readonly IReadOnlyList<LeaderboardEventGroup> _groups;
        private RaceEventId _chosen;

        public LeaderboardEventPicker(IReadOnlyList<LeaderboardEventGroup> groups)
        {
            _groups = groups ?? Array.Empty<LeaderboardEventGroup>();
        }

        public int Category { get; private set; }
        public IReadOnlyList<LeaderboardEventGroup> Groups => _groups;

        public IReadOnlyList<LeaderboardEventOption> Options =>
            Category >= 0 && Category < _groups.Count ? _groups[Category].Options : Array.Empty<LeaderboardEventOption>();

        /// <summary>The chosen race while it belongs to the shown category; empty otherwise.</summary>
        public RaceEventId Chosen
        {
            get
            {
                foreach (LeaderboardEventOption option in Options)
                    if (option.Id == _chosen) return _chosen;
                return default;
            }
        }

        public bool CanApply => !Chosen.IsEmpty;

        /// <summary>Opens on the category of the active race with that race chosen (first category when it is not on offer).</summary>
        public void Begin(RaceEventId active)
        {
            _chosen = default;
            Category = 0;
            if (LeaderboardEventGroups.Find(_groups, active, out int group, out _))
            {
                Category = group;
                _chosen = active;
            }
        }

        public void SelectCategory(int index)
        {
            if (index < 0 || index >= _groups.Count) return;
            Category = index;
        }

        /// <summary>False when the race is not in the shown category.</summary>
        public bool Choose(RaceEventId id)
        {
            foreach (LeaderboardEventOption option in Options)
            {
                if (option.Id != id) continue;
                _chosen = id;
                return true;
            }
            return false;
        }
    }
}
