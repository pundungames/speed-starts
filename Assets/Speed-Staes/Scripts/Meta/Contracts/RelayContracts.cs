using System;

namespace SpeedStars.Meta.Contracts
{
    /// <summary>
    /// The player's relay team: one racer preset per leg and the leg being edited. A leg without a choice wears the preset
    /// with its own number (wrapping), so a fresh save already shows a team. The same preset may serve several legs.
    /// Presets are the ones of <see cref="ICosmeticService"/>; this only remembers which leg wears which.
    /// </summary>
    public interface IRelayTeam
    {
        /// <summary>Raised after the selected leg or a leg's preset changed.</summary>
        event Action Changed;

        /// <summary>Legs of the team (the relay events' leg count); 0 when no event is a relay.</summary>
        int LegCount { get; }

        /// <summary>The leg the card edits (0-based).</summary>
        int SelectedLeg { get; }

        /// <summary>False for a leg outside 0..LegCount-1. Saved at once.</summary>
        bool SelectLeg(int leg);

        /// <summary>The preset this leg wears; always a valid preset index (0 for a bad leg or no presets).</summary>
        int GetPreset(int leg);

        /// <summary>False for a leg or preset out of range. Saved at once.</summary>
        bool SetPreset(int leg, int preset);
    }
}
