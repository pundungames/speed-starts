using System;
using UnityEngine;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>
    /// One emote: an id, a name, a Cash price (0 = free) and the NAME of the baked animation it plays (v2, T-089). The animation
    /// itself is Malik's <c>RunnerEmoteData</c> asset; it lives in the runner prefab's <c>RunnerEmote.clips</c> and the Bridge
    /// finds it there by this name, so this assembly never references runner code. An empty clip name is the "None" emote: it
    /// plays nothing. No code lists emotes; the catalog does.
    /// (The file keeps its old name so its .meta stays; the keyframe types of T-040 are gone.)
    /// </summary>
    [Serializable]
    public sealed class EmoteDefinition
    {
        [Tooltip("Stable id written to saves (owned and equipped emotes). Never change it once players have saves.")]
        [SerializeField] private string emoteId;

        [Tooltip("Name shown to the player (English, generic).")]
        [SerializeField] private string displayName;

        [Tooltip("Price in Cash. 0 = free: every player owns the emote.")]
        [SerializeField, Min(0)] private int cashPrice;

        [Tooltip("Name of the baked animation (RunnerEmoteData asset name, see Animations/Baked) in the runner prefab's RunnerEmote clips. Empty = the None emote: nothing plays.")]
        [SerializeField] private string clipName;

        public string Id => emoteId ?? string.Empty;
        public string DisplayName => displayName ?? string.Empty;
        public int CashPrice => cashPrice;
        public string ClipName => clipName ?? string.Empty;

        /// <summary>False for the None emote.</summary>
        public bool HasClip => !string.IsNullOrEmpty(clipName);
    }
}
