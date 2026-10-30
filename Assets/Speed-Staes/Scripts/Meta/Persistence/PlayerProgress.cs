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
        [SerializeField] private List<CosmeticPreset> cosmeticPresets = new List<CosmeticPreset>();
        [SerializeField] private int selectedPresetIndex;
        [SerializeField] private List<EventBestTime> bestTimes = new List<EventBestTime>();
        [SerializeField] private List<string> processedTransactionIds = new List<string>();
        [SerializeField] private long lastSeenUtcTicks;
        [SerializeField] private string selectedThemeId = string.Empty;
        [SerializeField] private List<string> ownedThemeIds = new List<string>();
        [SerializeField] private SettingChoice musicChoice;
        [SerializeField] private SettingChoice sfxChoice;
        [SerializeField] private string languageCode = string.Empty;
        [SerializeField] private string flagCode = string.Empty;
        [SerializeField] private string playerName = string.Empty;
        [SerializeField] private List<DayBestEntry> dayBests = new List<DayBestEntry>();

        /// <summary>Best finished race per (event, UTC day), capped by the leaderboard config. Added with the leaderboard (T-037), no schema bump: older saves open with an empty list.</summary>
        public List<DayBestEntry> DayBests => dayBests;

        /// <summary>Music switch the player set; Default = never touched (the settings config decides). Added with the settings window, no schema bump: older saves open as Default.</summary>
        public SettingChoice MusicChoice { get => musicChoice; set => musicChoice = value; }

        /// <summary>Sound effects switch the player set; Default = never touched.</summary>
        public SettingChoice SfxChoice { get => sfxChoice; set => sfxChoice = value; }

        /// <summary>Language code the player chose. Empty = never chosen (config default).</summary>
        public string LanguageCode { get => languageCode; set => languageCode = value ?? string.Empty; }

        /// <summary>Flag code the player chose. Empty = never chosen (config default).</summary>
        public string FlagCode { get => flagCode; set => flagCode = value ?? string.Empty; }

        /// <summary>Saved player name. Empty = never set (config default).</summary>
        public string PlayerName { get => playerName; set => playerName = value ?? string.Empty; }

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

        /// <summary>
        /// Schema 1 only: the single outfit of old saves. Moved into the first preset on load (<c>MigrateLegacyCosmetics</c>);
        /// empty afterwards. New code uses <c>CosmeticPresets</c>.
        /// </summary>
        public List<SelectedCosmetic> SelectedCosmetics => selectedCosmetics;

        /// <summary>Racer presets (Custom 1..N), in order. Filled up to the configured count by the cosmetic service.</summary>
        public List<CosmeticPreset> CosmeticPresets => cosmeticPresets;

        /// <summary>Index into <c>CosmeticPresets</c> of the preset worn in menu and race.</summary>
        public int SelectedPresetIndex { get => selectedPresetIndex; set => selectedPresetIndex = value; }
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
        /// Schema 1 to 2: a saved single outfit becomes the first preset (only when there are no presets yet), and the
        /// legacy list is emptied so it is never migrated twice.
        /// </summary>
        internal void MigrateLegacyCosmetics()
        {
            EnsureValid();
            if (cosmeticPresets.Count == 0 && selectedCosmetics.Count > 0)
            {
                var preset = new CosmeticPreset();
                preset.Slots.AddRange(selectedCosmetics);
                cosmeticPresets.Add(preset);
            }
            selectedCosmetics.Clear();
        }

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
            for (int i = selectedCosmetics.Count - 1; i >= 0; i--)
                if (selectedCosmetics[i] == null) selectedCosmetics.RemoveAt(i);
            foreach (SelectedCosmetic legacy in selectedCosmetics) legacy.EnsureValid();
            if (cosmeticPresets == null) cosmeticPresets = new List<CosmeticPreset>();
            for (int i = cosmeticPresets.Count - 1; i >= 0; i--)
                if (cosmeticPresets[i] == null) cosmeticPresets.RemoveAt(i);
            foreach (CosmeticPreset preset in cosmeticPresets) preset.EnsureValid();
            if (bestTimes == null) bestTimes = new List<EventBestTime>();
            if (processedTransactionIds == null) processedTransactionIds = new List<string>();
            if (selectedThemeId == null) selectedThemeId = string.Empty;
            if (ownedThemeIds == null) ownedThemeIds = new List<string>();
            if (languageCode == null) languageCode = string.Empty;
            if (flagCode == null) flagCode = string.Empty;
            if (playerName == null) playerName = string.Empty;
            if (dayBests == null) dayBests = new List<DayBestEntry>();
            for (int i = dayBests.Count - 1; i >= 0; i--)
                if (dayBests[i] == null) dayBests.RemoveAt(i);
        }
    }

    /// <summary>A two-state setting that remembers "never touched": Default = use the config value. Serialized as int, append only.</summary>
    public enum SettingChoice
    {
        Default = 0,
        On = 1,
        Off = 2
    }

    /// <summary>
    /// One racer preset: the item and colour worn per category. A category without an entry is worn empty. Slots reuse
    /// <c>SelectedCosmetic</c> (category stored by enum name).
    /// </summary>
    [System.Serializable]
    public sealed class CosmeticPreset
    {
        [SerializeField] private List<SelectedCosmetic> slots = new List<SelectedCosmetic>();

        public List<SelectedCosmetic> Slots => slots;

        internal void EnsureValid()
        {
            if (slots == null) slots = new List<SelectedCosmetic>();
            for (int i = slots.Count - 1; i >= 0; i--)
                if (slots[i] == null) slots.RemoveAt(i);
            foreach (SelectedCosmetic slot in slots) slot.EnsureValid();
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
    /// The cosmetic worn in one category, plus the colour of each of its colour channels. The category is stored by enum
    /// NAME so reordering the category enum cannot move a saved choice into another slot. Colours go by the item's channel
    /// order: <c>colorHex</c> is the primary channel (the only colour saves had before items had several channels, so old
    /// saves need no migration), <c>moreColorHexes</c> holds the second and third channel (absent in old saves).
    /// </summary>
    [System.Serializable]
    public sealed class SelectedCosmetic
    {
        [SerializeField] private string category = string.Empty;
        [SerializeField] private string cosmeticId = string.Empty;
        [SerializeField] private string colorHex = string.Empty;
        [SerializeField] private List<string> moreColorHexes = new List<string>();

        public SelectedCosmetic() { }

        public SelectedCosmetic(string category, string cosmeticId, string colorHex)
            : this(category, cosmeticId, colorHex, string.Empty, string.Empty) { }

        public SelectedCosmetic(string category, string cosmeticId, string colorHex, string colorHex2, string colorHex3)
        {
            Category = category;
            CosmeticId = cosmeticId;
            ColorHex = colorHex;
            SetMoreColors(colorHex2, colorHex3);
        }

        public string Category { get => category; set => category = value ?? string.Empty; }
        public string CosmeticId { get => cosmeticId; set => cosmeticId = value ?? string.Empty; }

        /// <summary>"RRGGBB" of the primary channel. Empty = the item's own default color.</summary>
        public string ColorHex { get => colorHex; set => colorHex = value ?? string.Empty; }

        /// <summary>"RRGGBB" of the second (index 1) or third (index 2) channel; empty when unset. Other indexes: empty.</summary>
        public string MoreColorAt(int index)
        {
            if (moreColorHexes == null || index < 1) return string.Empty;
            return index - 1 < moreColorHexes.Count ? moreColorHexes[index - 1] ?? string.Empty : string.Empty;
        }

        /// <summary>Replaces the second and third channel colours; trailing empty ones are not stored.</summary>
        public void SetMoreColors(string colorHex2, string colorHex3)
        {
            EnsureValid();
            moreColorHexes.Clear();
            colorHex2 = colorHex2 ?? string.Empty;
            colorHex3 = colorHex3 ?? string.Empty;
            if (colorHex2.Length > 0 || colorHex3.Length > 0) moreColorHexes.Add(colorHex2);
            if (colorHex3.Length > 0) moreColorHexes.Add(colorHex3);
        }

        internal void EnsureValid()
        {
            if (category == null) category = string.Empty;
            if (cosmeticId == null) cosmeticId = string.Empty;
            if (colorHex == null) colorHex = string.Empty;
            if (moreColorHexes == null) moreColorHexes = new List<string>();
        }
    }

    /// <summary>The player's best finished race of one event on one UTC day (leaderboard source). Time in whole milliseconds, moments as UTC ticks.</summary>
    [System.Serializable]
    public sealed class DayBestEntry
    {
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private long dayUtcTicks;
        [SerializeField] private long elapsedMilliseconds;
        [SerializeField] private long achievedUtcTicks;

        public DayBestEntry() { }

        public DayBestEntry(RaceEventId eventId, long dayUtcTicks, long elapsedMilliseconds, long achievedUtcTicks)
        {
            this.eventId = eventId.Value ?? string.Empty;
            this.dayUtcTicks = dayUtcTicks;
            this.elapsedMilliseconds = elapsedMilliseconds;
            this.achievedUtcTicks = achievedUtcTicks;
        }

        public RaceEventId EventId => new RaceEventId(eventId);

        /// <summary>UTC ticks of 00:00 of the day this race was run.</summary>
        public long DayUtcTicks => dayUtcTicks;

        public long ElapsedMilliseconds { get => elapsedMilliseconds; set => elapsedMilliseconds = value; }
        public long AchievedUtcTicks { get => achievedUtcTicks; set => achievedUtcTicks = value; }
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
