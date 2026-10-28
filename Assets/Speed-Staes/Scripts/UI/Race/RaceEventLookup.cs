using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;

namespace SpeedStars.UI.Race
{
    /// <summary>What the race flow needs to know about an event; read from the catalog asset, never hard-coded.</summary>
    public readonly struct RaceEventInfo
    {
        public readonly RaceEventId Id;
        public readonly string DisplayName;
        public readonly float DistanceMeters;

        public RaceEventInfo(RaceEventId id, string displayName, float distanceMeters)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            DistanceMeters = distanceMeters;
        }
    }

    /// <summary>Event facts by id. A seam over RaceEventCatalog so RaceFlowLogic is tested without ScriptableObjects.</summary>
    public interface IRaceEventLookup
    {
        bool TryGet(RaceEventId id, out RaceEventInfo info);
    }

    /// <summary>The real lookup: reads RaceEventCatalog.</summary>
    public sealed class CatalogRaceEventLookup : IRaceEventLookup
    {
        private readonly RaceEventCatalog _catalog;

        public CatalogRaceEventLookup(RaceEventCatalog catalog)
        {
            _catalog = catalog ?? throw new System.ArgumentNullException(nameof(catalog));
        }

        public bool TryGet(RaceEventId id, out RaceEventInfo info)
        {
            if (_catalog.TryGet(id, out RaceEventDefinition definition))
            {
                info = new RaceEventInfo(definition.Id, definition.DisplayName, definition.DistanceMeters);
                return true;
            }
            info = default;
            return false;
        }
    }
}
