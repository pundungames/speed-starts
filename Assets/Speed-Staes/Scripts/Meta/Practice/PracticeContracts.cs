using System;

namespace SpeedStars.Meta.Practice
{
    /// <summary>
    /// What the Practice panel sees of the race (T-090). The Bridge implements it over the race session; the panel view only reads
    /// the choices and asks for the two actions. An action the mechanics cannot do yet is reported by its <c>Can...</c> flag, and the
    /// panel greys that row.
    /// </summary>
    public interface IPracticeControl
    {
        /// <summary>The current event is Practice (the panel is shown only then).</summary>
        bool IsPractice { get; }

        PracticeOptions Options { get; }

        /// <summary>Lock energy has a mechanic behind it (the sim keeps the energy full).</summary>
        bool CanLockEnergy { get; }

        /// <summary>Reset energy has a mechanic behind it (the sim refills the energy now).</summary>
        bool CanResetEnergy { get; }

        /// <summary>Standing start has a mechanic behind it (the runner leaves from standing).</summary>
        bool CanStandingStart { get; }

        /// <summary>Refills the energy now. No effect while <see cref="CanResetEnergy"/> is false.</summary>
        void ResetEnergy();

        /// <summary>Practice turned on / off for the race, or a capability appeared.</summary>
        event Action Changed;
    }

    /// <summary>The runner's energy, as Practice needs it. To be implemented over the sim once the mechanics expose it (needs: DECISION, Malik).</summary>
    public interface IPracticeEnergy
    {
        /// <summary>Energy full again, now.</summary>
        void Refill();

        /// <summary>While true the energy never drops.</summary>
        void SetLocked(bool locked);
    }

    /// <summary>Standing start: the runner leaves the line from standing, not crouched. To be implemented once the mechanics have it (needs: DECISION, Malik).</summary>
    public interface IPracticeStandingStart
    {
        void SetStanding(bool standing);
    }
}
