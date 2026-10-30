using System;
using System.Collections.Generic;
using System.Linq;
using SpeedStars.Meta.Config;
using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// Every emote of the menu character with its price and its procedural key frames, and which one a new player has
    /// equipped. Code never lists emotes: adding one means adding an entry here.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Emote Catalog", fileName = "EmoteCatalog")]
    public sealed class EmoteCatalog : ScriptableObject
    {
        [Tooltip("Emotes in display order. Ids must be unique.")]
        [SerializeField] private EmoteDefinition[] emotes = new EmoteDefinition[0];

        [Tooltip("Id of the emote equipped until the player chooses. Empty or unknown = the first free emote.")]
        [SerializeField] private string defaultEmoteId;

        /// <summary>Emotes in list order, empty slots skipped. A new list each call.</summary>
        public IReadOnlyList<EmoteDefinition> GetEmotes()
        {
            if (emotes == null) return new List<EmoteDefinition>();
            return emotes.Where(e => e != null).ToList();
        }

        /// <summary>The first emote with this id; false for an empty or unknown id.</summary>
        public bool TryGet(string id, out EmoteDefinition emote)
        {
            if (!string.IsNullOrEmpty(id) && emotes != null)
            {
                foreach (EmoteDefinition candidate in emotes)
                {
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        emote = candidate;
                        return true;
                    }
                }
            }
            emote = null;
            return false;
        }

        /// <summary>The start emote: the configured one, else the first free one, else the first; null only when empty.</summary>
        public EmoteDefinition DefaultEmote
        {
            get
            {
                if (TryGet(defaultEmoteId, out EmoteDefinition configured)) return configured;
                IReadOnlyList<EmoteDefinition> all = GetEmotes();
                foreach (EmoteDefinition emote in all)
                    if (emote.CashPrice <= 0) return emote;
                return all.Count > 0 ? all[0] : null;
            }
        }

        public IReadOnlyList<string> FindDuplicateIds() => ConfigIdCheck.FindDuplicates(Ids());

        private IEnumerable<string> Ids()
        {
            if (emotes == null) yield break;
            foreach (EmoteDefinition entry in emotes)
                if (entry != null) yield return entry.Id;
        }

        private void OnValidate() => ConfigIdCheck.Warn(this, "emote", Ids());
    }
}
