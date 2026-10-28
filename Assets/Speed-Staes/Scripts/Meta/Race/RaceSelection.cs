using System;
using SpeedStars.Core.Race;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Race
{
    /// <summary>
    /// The race event picked on the Home screen. Starts on the first playable event in catalog order (empty id when
    /// there is none). Lives for the app session only; it is not saved.
    /// </summary>
    public sealed class RaceSelection : IRaceSelection
    {
        public RaceSelection(RaceEventCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            foreach (RaceEventDefinition definition in catalog.GetEvents())
            {
                if (!definition.IsPlayable) continue;
                Selected = definition.Id;
                break;
            }
        }

        public RaceEventId Selected { get; private set; }

        public event Action Changed;

        public void Select(RaceEventId id)
        {
            if (Selected == id) return;
            Selected = id;
            Changed?.Invoke();
        }
    }
}
