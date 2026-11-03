using System;
using SpeedStars.Core.Services;
using UnityEngine;

namespace SpeedStars.Meta.Practice
{
    /// <summary>What Practice remembers between runs (its own save key, so the player progress file is not touched).</summary>
    [Serializable]
    public sealed class PracticeSelection
    {
        public float distanceMeters;
        public bool hurdles;
        public bool lockEnergy;
        public bool standingStart;
    }

    /// <summary>
    /// The player's Practice choices (T-090): start distance, hurdles, lock energy, standing start. Pure model: distances are
    /// snapped to the configured list, every change is saved at once and raised once. A store of null keeps the choices in memory only.
    /// </summary>
    public sealed class PracticeOptions
    {
        public const string SaveKey = "practice";

        private readonly PracticeConfig _config;
        private readonly ISaveStore _store;
        private readonly PracticeSelection _selection;

        public PracticeOptions(PracticeConfig config, ISaveStore store)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _store = store;
            _selection = Load(store);
            _selection.distanceMeters = Snap(_selection.distanceMeters);
        }

        public event Action Changed;

        /// <summary>The distances of the dropdown, in display order.</summary>
        public System.Collections.Generic.IReadOnlyList<float> DistanceChoices => _config.Distances;

        public float DistanceMeters => _selection.distanceMeters;
        public bool Hurdles => _selection.hurdles;
        public bool LockEnergy => _selection.lockEnergy;
        public bool StandingStart => _selection.standingStart;

        /// <summary>Index of the chosen distance in <see cref="PracticeConfig.Distances"/>.</summary>
        public int DistanceIndex
        {
            get
            {
                for (int i = 0; i < _config.Distances.Count; i++)
                    if (Mathf.Approximately(_config.Distances[i], _selection.distanceMeters)) return i;
                return 0;
            }
        }

        /// <summary>Takes the configured distance nearest to the value. True when the choice changed.</summary>
        public bool SetDistance(float meters)
        {
            float snapped = Snap(meters);
            if (Mathf.Approximately(snapped, _selection.distanceMeters)) return false;
            _selection.distanceMeters = snapped;
            Commit();
            return true;
        }

        public bool SetDistanceIndex(int index)
        {
            if (index < 0 || index >= _config.Distances.Count) return false;
            return SetDistance(_config.Distances[index]);
        }

        public bool SetHurdles(bool on) => Set(ref _selection.hurdles, on);
        public bool SetLockEnergy(bool on) => Set(ref _selection.lockEnergy, on);
        public bool SetStandingStart(bool on) => Set(ref _selection.standingStart, on);

        /// <summary>The hurdle layout of the chosen distance, or false when hurdles are off (or the config has none).</summary>
        public bool TryGetHurdles(out PracticeConfig.HurdleLayout layout)
        {
            layout = null;
            return _selection.hurdles && _config.TryGetHurdles(_selection.distanceMeters, out layout);
        }

        private bool Set(ref bool field, bool value)
        {
            if (field == value) return false;
            field = value;
            Commit();
            return true;
        }

        private void Commit()
        {
            if (_store != null) _store.Write(SaveKey, JsonUtility.ToJson(_selection));
            Changed?.Invoke();
        }

        private float Snap(float meters)
        {
            float best = _config.DefaultDistance;
            float bestGap = float.MaxValue;
            foreach (float candidate in _config.Distances)
            {
                float gap = Mathf.Abs(candidate - meters);
                if (gap >= bestGap) continue;
                bestGap = gap;
                best = candidate;
            }
            return best;
        }

        private PracticeSelection Load(ISaveStore store)
        {
            var fresh = new PracticeSelection { distanceMeters = _config.DefaultDistance };
            if (store == null || !store.TryRead(SaveKey, out string json) || string.IsNullOrEmpty(json)) return fresh;
            try
            {
                return JsonUtility.FromJson<PracticeSelection>(json) ?? fresh;
            }
            catch (ArgumentException)
            {
                return fresh; // a broken file reads as the defaults; the next change writes a clean one
            }
        }
    }
}
