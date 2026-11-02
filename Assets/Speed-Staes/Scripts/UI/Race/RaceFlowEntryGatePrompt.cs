using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.UI.Home;
using UnityEngine;

namespace SpeedStars.UI.Race
{
    /// <summary>
    /// Race again with no ticket opens the same entry gate modal Home uses; its confirm pays and reloads the Race scene,
    /// so the next race starts the way one from Home does.
    /// </summary>
    public sealed class RaceFlowEntryGatePrompt : IEntryGatePrompt
    {
        private readonly EntryGateModalView _modal;
        private readonly RaceEventCatalog _catalog;

        public RaceFlowEntryGatePrompt(EntryGateModalView modal, RaceEventCatalog catalog)
        {
            _modal = modal;
            _catalog = catalog;
        }

        public void Open(RaceEventId id)
        {
            if (_catalog.TryGet(id, out RaceEventDefinition definition))
                _modal.Open(definition);
            else
                Debug.LogWarning($"[RaceFlow] Event '{id}' is not in the catalog; the entry gate cannot open.");
        }
    }
}
