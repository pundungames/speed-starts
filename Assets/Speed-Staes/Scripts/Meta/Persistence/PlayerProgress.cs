using System.Collections.Generic;
using SpeedStars.Core.Race;
using UnityEngine;

namespace SpeedStars.Meta.Persistence
{
    /// <summary>
    /// Everything about the player that must survive an app restart (MD §14). Plain data, written as JSON by
    /// <c>ProgressRepository</c> through JsonUtility: serialized fields only, lists instead of dictionaries, and times as
    /// UTC ticks so deadlines keep running while the app is closed. The private field names ARE the save format's keys:
    /// renaming a field silently drops that value from existing saves, so rename the property instead.
    /// Rules (ticket cap, regen, rewards, prices) live in the services that use this data, not here.
    /// </summary>
    [System.Serializable]
    public sealed class PlayerProgress
    {
        [SerializeField] private int schemaVersion;
        [SerializeField] private int cash;
        [SerializeField] private int purpleGems;
        [SerializeField] private int entryTickets;
        [SerializeField] private int timedTickets;
        [SerializeField] private long timedAccessExpiresUtcTicks;
        [SerializeField] private long entryRegenDueUtcTicks;
        [SerializeField] private string dailyGiftDayKey = string.Empty;
        [SerializeField] private SevenDayCheckInState sevenDay = new SevenDayCheckInState();
        [SerializeField] private List<string> ownedCosmeticIds = new List<string>();
        [SerializeField] private List<SelectedCosmetic> selectedCosmetics = new List<SelectedCosmetic>();
        [SerializeField] private List<EventBestTime> bestTimes = new List<EventBestTime>();
        [SerializeField] private List<string> processedTransactionIds = new List<string>();
        [SerializeField] private long lastSeenUtcTicks;
        [SerializeField] private string selectedThemeId = string.Empty;
        [SerializeField] private List<string> ownedThemeIds = new List<string>();

        /// <summary>Format version of this save; stamped by the repository on every save.</summary>
        public int SchemaVersion { get => schemaVersion; set => schemaVersion = value; }

        public int Cash { get => cash; set => cash = value; }
        public int PurpleGems { get => purpleGems; set => purpleGems = value; }
        public int EntryTickets { get => entryTickets; set => entryTickets = value; }

        /// <summary>Unused Timed Tickets in stock (not the running access window).</summary>
        public int TimedTickets { get => timedTickets; set => timedTickets = value; }

        /// <summary>When the running timed access ends. 0 = never activated (always in the past, so inactive).</summary>
        public long TimedAccessExpiresUtcTicks { get => timedAccessExpiresUtcTicks; set => timedAccessExpiresUtcTicks = value; }

        /// <summary>When the next Entry Ticket regenerates. 0 = no regen timer running.</summary>
        public long EntryRegenDueUtcTicks { get => entryRegenDueUtcTicks; set => entryRegenDueUtcTicks = value; }

        /// <summary>Day key of the last claimed daily Entry Gift. Empty = never claimed.</summary>
        public string DailyGiftDayKey { get => dailyGiftDayKey; set => dailyGiftDayKey = value ?? string.Empty; }

        public SevenDayCheckInState SevenDay => sevenDay;
        public List<string> OwnedCosmeticIds => ownedCosmeticIds;
        public List<SelectedCosmetic> SelectedCosmetics => selectedCosmetics;
        public List<EventBestTime> BestTimes => bestTimes;

        /// <summary>Ids of economy transactions already applied; a repeated id must not pay or charge twice.</summary>
        public List<string> ProcessedTransactionIds => processedTransactionIds;

        /// <summary>Latest UTC the clock has handed out; lets <c>ClockService</c> notice a rolled-back device clock.</summary>
        public long LastSeenUtcTicks { get => lastSeenUtcTicks; set => lastSeenUtcTicks = value; }

        /// <summary>Id of the chosen world theme. Empty = never chosen (the catalog's default applies).</summary>
        public string SelectedThemeId { get => selectedThemeId; set => selectedThemeId = value ?? string.Empty; }

        /// <summary>Ids of bought themes (the default theme is owned without being listed). Added with the theme feature; older saves open with an empty list.</summary>
        public List<string> OwnedThemeIds => ownedThemeIds;

        /// <summary>
        /// JSON written by an older build may lack newer fields; JsonUtility can then leave them null.
        /// Called after every load so callers never null-check.
        /// </summary>
        internal void EnsureValid()
        {
            if (dailyGiftDayKey == null) dailyGiftDayKey = string.Empty;
            if (sevenDay == null) sevenDay = new SevenDayCheckInState();
            sevenDay.EnsureValid();
            if (ownedCosmeticIds == null) ownedCosmeticIds = new List<string>();
            if (selectedCosmetics == null) selectedCosmetics = new List<SelectedCosmetic>();
            if (bestTimes == null) bestTimes = new List<EventBestTime>();
            if (processedTransactionIds == null) processedTransactionIds = new List<string>();
            if (selectedThemeId == null) selectedThemeId = string.Empty;
            if (ownedThemeIds == null) ownedThemeIds = new List<string>();
        }
    }

    /// <summary>Progress of the 7-day check-in calendar. What a day gives lives in its config, not here.</summary>
    [System.Serializable]
    public sealed class SevenDayCheckInState
    {
        [SerializeField] private int claimedCount;
        [SerializeField] private string lastClaimDayKey = string.Empty;

        /// <summary>How many calendar days have been claimed so far.</summary>
        public int ClaimedCount { get => claimedCount; set => claimedCount = value; }

        /// <summary>Day key of the last claim. Empty = never claimed.</summary>
        public string LastClaimDayKey { get => lastClaimDayKey; set => lastClaimDayKey = value ?? string.Empty; }

        internal void EnsureValid()
        {
            if (lastClaimDayKey == null) lastClaimDayKey = string.Empty;
        }
    }

    /// <summary>
    /// The cosmetic worn in one category, plus its color. The category is stored by enum NAME so reordering the
    /// category enum cannot move a saved choice into another slot.
    /// </summary>
    [System.Serializable]
    public sealed class SelectedCosmetic
    {
        [SerializeField] private string category = string.Empty;
        [SerializeField] private string cosmeticId = string.Empty;
        [SerializeField] private string colorHex = string.Empty;

        public SelectedCosmetic() { }

        public SelectedCosmetic(string category, string cosmeticId, string colorHex)
        {
            Category = category;
            CosmeticId = cosmeticId;
            ColorHex = colorHex;
        }

        public string Category { get => category; set => category = value ?? string.Empty; }
        public string CosmeticId { get => cosmeticId; set => cosmeticId = value ?? string.Empty; }

        /// <summary>"RRGGBB". Empty = the item's own default color.</summary>
        public string ColorHex { get => colorHex; set => colorHex = value ?? string.Empty; }
    }

    /// <summary>Local best time of one race event.</summary>
    [System.Serializable]
    public sealed class EventBestTime
    {
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private double bestSeconds;

        public EventBestTime() { }

        public EventBestTime(RaceEventId eventId, double bestSeconds)
        {
            EventId = eventId;
            BestSeconds = bestSeconds;
        }

        public RaceEventId EventId { get => new RaceEventId(eventId); set => eventId = value.Value ?? string.Empty; }
        public double BestSeconds { get => bestSeconds; set => bestSeconds = value; }
    }
}
