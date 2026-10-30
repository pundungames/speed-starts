using System;
using SpeedStars.Meta.Contracts;

namespace SpeedStars.Meta.Emotes
{
    /// <summary>Plain event relay between the emote screen and the menu character.</summary>
    public sealed class EmotePreview : IEmotePreview
    {
        public event Action<string> PlayRequested;
        public event Action StopRequested;

        public void Play(string id)
        {
            if (!string.IsNullOrEmpty(id)) PlayRequested?.Invoke(id);
        }

        public void Stop() => StopRequested?.Invoke();
    }
}
