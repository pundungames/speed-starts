using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>
    /// One race event (PDF §1.3, MD §12). The asset is meant to be the single source of the race distance: the race
    /// adapter writes it into the scene's race length, the scene value is not read (TEMEL-SOZLESME §9 #8).
    /// Whether a ticket is needed and whether Purple Gems are paid are per-event flags; the gem amounts themselves
    /// live in the one shared <c>GemRewardConfig</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Race Event", fileName = "RaceEvent")]
    public sealed class RaceEventDefinition : ScriptableObject
    {
        [Tooltip("Stable id written to saves and best times. Never change it once players have saves.")]
        [SerializeField] private string eventId;

        [Tooltip("Name shown on the event select screen.")]
        [SerializeField] private string displayName;

        [Tooltip("Race length in meters.")]
        [SerializeField, Min(0f)] private float distanceMeters;

        [Tooltip("Entry needs a ticket (the event was paid in the original game). Free events stay free.")]
        [SerializeField] private bool requiresTicket;

        [Tooltip("Pays Purple Gems by the single-race rank, from the shared Gem Reward table.")]
        [SerializeField] private bool awardsPurpleGems;

        [Tooltip("Off = listed but not playable yet (its race rules are not built).")]
        [SerializeField] private bool isPlayable;

        [Tooltip("Off = the player runs alone, with no opponent bots (Practice).")]
        [SerializeField] private bool hasOpponents = true;

        [Tooltip("Hurdles on the player's lane. 0 = a flat race.")]
        [SerializeField, Min(0)] private int hurdleCount;

        [Tooltip("Distance of the first hurdle from the start (m).")]
        [SerializeField, Min(0f)] private float firstHurdleMeters;

        [Tooltip("Distance between hurdles (m).")]
        [SerializeField, Min(0f)] private float hurdleSpacingMeters;

        [Tooltip("Relay legs: the distance is split into this many equal legs, each run by another runner of the team, with a baton exchange between them. 0 = not a relay.")]
        [SerializeField, Min(0)] private int relayLegCount;

        [Tooltip("Relay only: material of the baton (a thin cylinder in the runner's hand). Empty = the default material.")]
        [SerializeField] private Material batonMaterial;

        public RaceEventId Id => new RaceEventId(eventId);
        public string DisplayName => displayName ?? string.Empty;
        public float DistanceMeters => distanceMeters;
        public bool RequiresTicket => requiresTicket;
        public bool AwardsPurpleGems => awardsPurpleGems;
        public bool IsPlayable => isPlayable;
        public bool HasOpponents => hasOpponents;
        public int HurdleCount => hurdleCount;
        public float FirstHurdleMeters => firstHurdleMeters;
        public float HurdleSpacingMeters => hurdleSpacingMeters;
        public int RelayLegCount => relayLegCount;
        public bool IsRelay => relayLegCount > 1;
        public Material BatonMaterial => batonMaterial;
    }
}
