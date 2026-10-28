using System.Collections.Generic;
using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// Every race event, in the order the event select screen lists them (list order = catalog order). Code never
    /// enumerates events itself: adding an event means adding a definition here.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Race Event Catalog", fileName = "RaceEventCatalog")]
    public sealed class RaceEventCatalog : ScriptableObject
    {
        [Tooltip("Events in display order. Ids must be unique; empty slots are ignored.")]
        [SerializeField] private RaceEventDefinition[] events = new RaceEventDefinition[0];

        /// <summary>Events in catalog order, empty slots skipped. A new list each call.</summary>
        public IReadOnlyList<RaceEventDefinition> GetEvents()
        {
            var result = new List<RaceEventDefinition>();
            if (events == null) return result;
            foreach (var definition in events)
                if (definition != null) result.Add(definition);
            return result;
        }

        /// <summary>The first event with this id; false for an empty or unknown id.</summary>
        public bool TryGet(RaceEventId id, out RaceEventDefinition definition)
        {
            if (!id.IsEmpty && events != null)
            {
                foreach (var candidate in events)
                {
                    if (candidate != null && candidate.Id == id)
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }
            definition = null;
            return false;
        }

        /// <summary>Ids used by more than one event (only the first of each can be looked up).</summary>
        public IReadOnlyList<string> FindDuplicateIds() => ConfigIdCheck.FindDuplicates(Ids());

        private IEnumerable<string> Ids()
        {
            if (events == null) yield break;
            foreach (var definition in events)
                if (definition != null) yield return definition.Id.Value;
        }

        private void OnValidate() => ConfigIdCheck.Warn(this, "race event", Ids());
    }
}
