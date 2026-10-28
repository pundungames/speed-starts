using System.Collections.Generic;
using SpeedStars.Core.Race;

namespace SpeedStars.UI.Home
{
    /// <summary>One selectable race of the event select screen: its id and whether the game can run it today.</summary>
    public readonly struct EventSelectEntry
    {
        public readonly RaceEventId Id;
        public readonly bool IsPlayable;

        public EventSelectEntry(RaceEventId id, bool isPlayable)
        {
            Id = id;
            IsPlayable = isPlayable;
        }

        /// <summary>No race behind this slot (id missing from the config or the catalog).</summary>
        public bool IsEmpty => Id.IsEmpty;
    }

    /// <summary>What the player has picked: a category card (with a distance), or one of the two bottom buttons.</summary>
    public enum EventSelectTarget
    {
        Category,
        RaceOfTheDay,
        Practice
    }

    /// <summary>
    /// The state of the event select screen as pure C# (no Unity types: EditMode tests run it directly). Exactly one thing
    /// is selected at a time. Picking a card picks its first distance; picking a distance only works on the active card.
    /// A race that is not playable can be selected, but <see cref="CanContinue"/> stays false for it.
    /// </summary>
    public sealed class EventSelectModel
    {
        private readonly List<List<EventSelectEntry>> _categories = new List<List<EventSelectEntry>>();
        private readonly EventSelectEntry _raceOfTheDay;
        private readonly EventSelectEntry _practice;

        public EventSelectModel(IReadOnlyList<IReadOnlyList<EventSelectEntry>> categories, EventSelectEntry raceOfTheDay, EventSelectEntry practice)
        {
            if (categories != null)
            {
                foreach (IReadOnlyList<EventSelectEntry> category in categories)
                    _categories.Add(category != null ? new List<EventSelectEntry>(category) : new List<EventSelectEntry>());
            }
            _raceOfTheDay = raceOfTheDay;
            _practice = practice;
        }

        public EventSelectTarget Target { get; private set; } = EventSelectTarget.Category;
        public int CategoryIndex { get; private set; }
        public int DistanceIndex { get; private set; }

        public int CategoryCount => _categories.Count;

        public int DistanceCount(int categoryIndex) => IsCategory(categoryIndex) ? _categories[categoryIndex].Count : 0;

        public EventSelectEntry EntryAt(int categoryIndex, int distanceIndex)
        {
            if (!IsCategory(categoryIndex) || distanceIndex < 0 || distanceIndex >= _categories[categoryIndex].Count)
                return default;
            return _categories[categoryIndex][distanceIndex];
        }

        /// <summary>The selected race; false when the slot behind the selection is empty.</summary>
        public bool TryGetCurrent(out EventSelectEntry entry)
        {
            switch (Target)
            {
                case EventSelectTarget.RaceOfTheDay: entry = _raceOfTheDay; break;
                case EventSelectTarget.Practice: entry = _practice; break;
                default: entry = EntryAt(CategoryIndex, DistanceIndex); break;
            }
            return !entry.IsEmpty;
        }

        /// <summary>Continue is accepted only for an existing, playable race.</summary>
        public bool CanContinue => TryGetCurrent(out EventSelectEntry entry) && entry.IsPlayable;

        /// <summary>Selects what the game currently has selected (opening state). An id this screen does not list changes nothing.</summary>
        public void Restore(RaceEventId id)
        {
            if (id.IsEmpty) return;

            for (int c = 0; c < _categories.Count; c++)
            {
                for (int d = 0; d < _categories[c].Count; d++)
                {
                    if (_categories[c][d].Id != id) continue;
                    Target = EventSelectTarget.Category;
                    CategoryIndex = c;
                    DistanceIndex = d;
                    return;
                }
            }

            if (_raceOfTheDay.Id == id) Target = EventSelectTarget.RaceOfTheDay;
            else if (_practice.Id == id) Target = EventSelectTarget.Practice;
        }

        /// <returns>True when the selection changed.</returns>
        public bool SelectCategory(int categoryIndex)
        {
            if (!IsCategory(categoryIndex)) return false;
            if (Target == EventSelectTarget.Category && CategoryIndex == categoryIndex) return false;
            Target = EventSelectTarget.Category;
            CategoryIndex = categoryIndex;
            DistanceIndex = 0;
            return true;
        }

        /// <returns>True when the selection changed; false for a distance of a card that is not the active one.</returns>
        public bool SelectDistance(int categoryIndex, int distanceIndex)
        {
            if (Target != EventSelectTarget.Category || CategoryIndex != categoryIndex) return false;
            if (distanceIndex < 0 || distanceIndex >= DistanceCount(categoryIndex)) return false;
            if (DistanceIndex == distanceIndex) return false;
            DistanceIndex = distanceIndex;
            return true;
        }

        public bool SelectRaceOfTheDay() => SelectSpecial(EventSelectTarget.RaceOfTheDay);

        public bool SelectPractice() => SelectSpecial(EventSelectTarget.Practice);

        private bool SelectSpecial(EventSelectTarget target)
        {
            if (Target == target) return false;
            Target = target;
            return true;
        }

        private bool IsCategory(int index) => index >= 0 && index < _categories.Count;
    }
}
