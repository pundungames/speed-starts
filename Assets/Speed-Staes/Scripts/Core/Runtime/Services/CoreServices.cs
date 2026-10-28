using System;

namespace SpeedStars.Core.Services
{
    /// <summary>
    /// Wall-clock source for everything that runs on real time (ticket regen, timed access, daily gift).
    /// Abstracted so tests can move time and so a rolled-back device clock can be caught in one place.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>
    /// Raw text storage for save data. Write must be atomic: a crash mid-write keeps the previous content.
    /// Serialization and schema live on the meta side; this layer only stores strings by key.
    /// </summary>
    public interface ISaveStore
    {
        bool TryRead(string key, out string contents);
        void Write(string key, string contents);
        void Delete(string key);
    }
}
