using System;
using System.Collections.Generic;
using SpeedStars.Core.Services;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using UnityEngine;

namespace SpeedStars.Meta.Relay
{
    /// <summary>What the relay team remembers between runs. <c>presets</c>: one entry per leg, -1 = never chosen (the leg's default).</summary>
    [Serializable]
    public sealed class RelayTeamSave
    {
        public int selectedLeg;
        public int[] presets = Array.Empty<int>();
    }

    /// <summary>
    /// <see cref="IRelayTeam"/> over the save store, under its own key (like Practice) so the player progress file keeps its format.
    /// Choices are stored as preset indexes and checked against the preset count on every read, so a catalog with fewer presets
    /// never breaks the team; the stored value comes back if the count grows again. A broken or missing file reads as the defaults.
    /// </summary>
    public sealed class RelayTeamService : IRelayTeam
    {
        public const string SaveKey = "relay";

        private readonly ISaveStore _store;
        private readonly ICosmeticService _cosmetics;
        private readonly int _legCount;
        private readonly int[] _presets;
        private int _selectedLeg;

        /// <summary>The game's team: as long as the longest relay event of the catalog (the installer calls this).</summary>
        public static RelayTeamService Create(ISaveStore store, ICosmeticService cosmetics, RaceEventCatalog events) =>
            new RelayTeamService(store, cosmetics, LegCountOf(events));

        public RelayTeamService(ISaveStore store, ICosmeticService cosmetics, int legCount)
        {
            _store = store;
            _cosmetics = cosmetics ?? throw new ArgumentNullException(nameof(cosmetics));
            _legCount = Math.Max(0, legCount);
            _presets = new int[_legCount];
            for (int i = 0; i < _presets.Length; i++) _presets[i] = -1;
            Load();
        }

        public event Action Changed;

        public int LegCount => _legCount;

        public int SelectedLeg => _selectedLeg;

        public bool SelectLeg(int leg)
        {
            if (leg < 0 || leg >= _legCount) return false;
            if (leg == _selectedLeg) return true;
            _selectedLeg = leg;
            Commit();
            return true;
        }

        public int GetPreset(int leg)
        {
            if (leg < 0 || leg >= _legCount) return 0;
            return RelayTeamRules.ResolvePreset(_presets[leg], leg, _cosmetics.PresetCount);
        }

        public bool SetPreset(int leg, int preset)
        {
            if (leg < 0 || leg >= _legCount || preset < 0 || preset >= _cosmetics.PresetCount) return false;
            if (GetPreset(leg) == preset && _presets[leg] == preset) return true;
            _presets[leg] = preset;
            Commit();
            return true;
        }

        private static int LegCountOf(RaceEventCatalog events)
        {
            if (events == null) return 0;
            var counts = new List<int>();
            foreach (RaceEventDefinition definition in events.GetEvents())
                if (definition != null) counts.Add(definition.RelayLegCount);
            return RelayTeamRules.TeamLegCount(counts);
        }

        private void Commit()
        {
            if (_store != null)
            {
                var save = new RelayTeamSave { selectedLeg = _selectedLeg, presets = (int[])_presets.Clone() };
                _store.Write(SaveKey, JsonUtility.ToJson(save));
            }
            Changed?.Invoke();
        }

        private void Load()
        {
            if (_store == null || !_store.TryRead(SaveKey, out string json) || string.IsNullOrEmpty(json)) return;
            RelayTeamSave save;
            try
            {
                save = JsonUtility.FromJson<RelayTeamSave>(json);
            }
            catch (ArgumentException)
            {
                return; // a broken file reads as the defaults; the next change writes a clean one
            }
            if (save == null) return;

            if (save.selectedLeg >= 0 && save.selectedLeg < _legCount) _selectedLeg = save.selectedLeg;
            int[] stored = save.presets ?? Array.Empty<int>();
            for (int leg = 0; leg < _legCount && leg < stored.Length; leg++)
                _presets[leg] = stored[leg] >= 0 ? stored[leg] : -1;
        }
    }
}
