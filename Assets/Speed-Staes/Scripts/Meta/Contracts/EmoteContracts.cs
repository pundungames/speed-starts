using System;
using System.Collections.Generic;

namespace SpeedStars.Meta.Contracts
{
    public enum EmotePurchaseResult
    {
        /// <summary>Cash was spent once and the emote is owned now.</summary>
        Purchased,
        /// <summary>Already owned (free emotes always are): nothing was charged.</summary>
        AlreadyOwned,
        /// <summary>Not enough Cash; nothing changed.</summary>
        NotEnoughCash,
        UnknownEmote
    }

    /// <summary>One emote as the UI lists it. A price of 0 means the emote is free (always owned).</summary>
    public readonly struct EmoteInfo
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly int CashPrice;

        public EmoteInfo(string id, string displayName, int cashPrice)
        {
            Id = id;
            DisplayName = displayName;
            CashPrice = cashPrice;
        }
    }

    /// <summary>Emote ownership, Cash purchases and the equipped emote (saved with the progress).</summary>
    public interface IEmoteService
    {
        /// <summary>All emotes in catalog order.</summary>
        IReadOnlyList<EmoteInfo> Emotes { get; }

        /// <summary>The saved emote; a saved id that is gone or not owned falls back to the catalog default.</summary>
        string EquippedId { get; }

        /// <summary>Raised when ownership or the equipped emote changed.</summary>
        event Action Changed;

        bool TryGet(string id, out EmoteInfo info);
        bool IsOwned(string id);

        /// <summary>Buys an emote with Cash: once, and nothing changes on failure. Does not equip it.</summary>
        EmotePurchaseResult TryPurchase(string id);

        /// <summary>Equips an owned emote. False for an unknown or unowned id.</summary>
        bool Equip(string id);
    }

    /// <summary>
    /// The player's race emote switches (T-088 holds them in the settings; this is the seam the emote code reads, so it never
    /// depends on the settings). The default implementation says yes to everything until an adapter over the settings is bound.
    /// </summary>
    public interface IEmoteRaceOptions
    {
        /// <summary>The emote button is on the race HUD.</summary>
        bool ShowEmoteButton { get; }

        /// <summary>The player's runner plays the equipped emote by itself when the race is finished.</summary>
        bool AutoEmoteSelf { get; }

        /// <summary>The opponents play an emote when they cross the line.</summary>
        bool AutoEmoteCompetitors { get; }

        /// <summary>Raised after any of the switches changed.</summary>
        event Action Changed;
    }

    /// <summary>All three switches on; never changes.</summary>
    public sealed class DefaultEmoteRaceOptions : IEmoteRaceOptions
    {
        public bool ShowEmoteButton => true;
        public bool AutoEmoteSelf => true;
        public bool AutoEmoteCompetitors => true;
        public event Action Changed { add { } remove { } }
    }

    /// <summary>
    /// What the race HUD's emote button asks of the race scene (implemented in Bridge over the runner's emote player). The HUD
    /// only shows the button, asks <see cref="RequestEmote"/> and listens; it never sees runner code.
    /// </summary>
    public interface IRaceEmoteControl
    {
        /// <summary>The equipped emote has an animation (it is not None).</summary>
        bool HasEmote { get; }

        /// <summary>Play is accepted right now: an animation is equipped, the race has left the start, and the last one is over.</summary>
        bool CanEmote { get; }

        /// <summary>Display name of the equipped emote; empty for None.</summary>
        string EmoteName { get; }

        /// <summary>Raised when HasEmote, CanEmote or the name may have changed.</summary>
        event Action Changed;

        /// <summary>Plays the equipped emote on the player's runner. False when it was not accepted.</summary>
        bool RequestEmote();
    }

    /// <summary>
    /// What the menu character plays right now. The emote screen asks, the character (Bridge) listens; a locked emote can be
    /// previewed too.
    /// </summary>
    public interface IEmotePreview
    {
        /// <summary>An emote should play (emote id).</summary>
        event Action<string> PlayRequested;

        /// <summary>The playing emote should end now (the screen closed).</summary>
        event Action StopRequested;

        void Play(string id);
        void Stop();
    }
}
