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
