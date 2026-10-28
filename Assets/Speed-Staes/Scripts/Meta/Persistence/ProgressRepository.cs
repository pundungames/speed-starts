using System;
using SpeedStars.Core.Services;

namespace SpeedStars.Meta.Persistence
{
    /// <summary>How the last <c>ProgressRepository.Load</c> went.</summary>
    public enum ProgressLoadStatus
    {
        NotLoaded,
        /// <summary>No save existed; a new progress was made from the starting balances.</summary>
        Created,
        Loaded,
        /// <summary>The save comes from a newer build. Running on a new progress; saving is refused so it survives.</summary>
        NewerSchema,
        /// <summary>The save could not be parsed or has no version. Running on a new progress; saving is refused.</summary>
        Unreadable
    }

    /// <summary>
    /// What a brand-new player starts with. Supplied from outside (the economy config asset) so no balance number
    /// lives in code.
    /// </summary>
    public readonly struct StartingBalances
    {
        public readonly int Cash;
        public readonly int PurpleGems;
        public readonly int EntryTickets;
        public readonly int TimedTickets;

        public StartingBalances(int cash, int purpleGems, int entryTickets, int timedTickets)
        {
            Cash = cash;
            PurpleGems = purpleGems;
            EntryTickets = entryTickets;
            TimedTickets = timedTickets;
        }
    }

    /// <summary>
    /// Loads and saves the one <c>PlayerProgress</c> through <c>ISaveStore</c> as JSON. Owns the schema version:
    /// a save from a newer build, or one that cannot be read, is never overwritten — the game runs on a fresh
    /// progress and <c>Save</c> returns false, so an update (or a person) can still recover the file.
    /// </summary>
    public sealed class ProgressRepository
    {
        public const string SaveKey = "progress";

        /// <summary>Bump when the saved shape changes, and add the migration step in <c>Load</c>.</summary>
        public const int CurrentSchemaVersion = 1;

        private readonly ISaveStore _store;
        private readonly Func<StartingBalances> _startingBalances;

        public ProgressRepository(ISaveStore store, Func<StartingBalances> startingBalances)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _startingBalances = startingBalances ?? throw new ArgumentNullException(nameof(startingBalances));
        }

        /// <summary>The live progress; null until <c>Load</c>. Services change it in place, then call <c>Save</c>.</summary>
        public PlayerProgress Current { get; private set; }

        public ProgressLoadStatus Status { get; private set; } = ProgressLoadStatus.NotLoaded;

        /// <summary>True when <c>Save</c> will write (the loaded save, if any, is ours to replace).</summary>
        public bool CanSave => Status == ProgressLoadStatus.Created || Status == ProgressLoadStatus.Loaded;

        public PlayerProgress Load()
        {
            if (!_store.TryRead(SaveKey, out string json))
                return Use(CreateNew(), ProgressLoadStatus.Created);

            var progress = new PlayerProgress();
            try
            {
                UnityEngine.JsonUtility.FromJsonOverwrite(json, progress);
            }
            catch (Exception e)
            {
                // JsonUtility throws ArgumentException on malformed text; any failure here means "cannot read".
                return Reject(ProgressLoadStatus.Unreadable, "cannot parse (" + e.Message + ")");
            }

            if (progress.SchemaVersion <= 0)
                return Reject(ProgressLoadStatus.Unreadable, "no schema version");
            if (progress.SchemaVersion > CurrentSchemaVersion)
                return Reject(ProgressLoadStatus.NewerSchema,
                    "schema " + progress.SchemaVersion + " is newer than " + CurrentSchemaVersion);

            // Older schemas are migrated here, one version step at a time, once a version 2 exists.
            progress.EnsureValid();
            return Use(progress, ProgressLoadStatus.Loaded);
        }

        /// <summary>
        /// Writes <c>Current</c>. Returns false (and writes nothing) when the stored save must be protected, see
        /// <c>CanSave</c>. Disk errors are thrown by the store; the previous save stays readable.
        /// </summary>
        public bool Save()
        {
            if (Current == null) throw new InvalidOperationException("Call Load() before Save().");
            if (!CanSave) return false;

            Current.SchemaVersion = CurrentSchemaVersion;
            _store.Write(SaveKey, UnityEngine.JsonUtility.ToJson(Current));
            return true;
        }

        private PlayerProgress CreateNew()
        {
            StartingBalances start = _startingBalances();
            return new PlayerProgress
            {
                SchemaVersion = CurrentSchemaVersion,
                Cash = start.Cash,
                PurpleGems = start.PurpleGems,
                EntryTickets = start.EntryTickets,
                TimedTickets = start.TimedTickets
            };
        }

        private PlayerProgress Reject(ProgressLoadStatus status, string reason)
        {
            UnityEngine.Debug.LogWarning("[ProgressRepository] Save '" + SaveKey + "' not loaded: " + reason
                                         + ". Playing on a new progress; the file is left untouched.");
            return Use(CreateNew(), status);
        }

        private PlayerProgress Use(PlayerProgress progress, ProgressLoadStatus status)
        {
            Current = progress;
            Status = status;
            return progress;
        }
    }
}
