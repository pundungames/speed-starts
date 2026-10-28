using System;
using UnityEngine;

namespace SpeedStars.Meta.Config
{
    /// <summary>What a 7-day check-in day gives. Serialized as int: append new values, never reorder.</summary>
    public enum SevenDayRewardKind
    {
        /// <summary>Empty day: claiming it advances the calendar but gives nothing (PDF has no day 6).</summary>
        None = 0,
        Cash = 1,
        EntryTicket = 2,
        TimedTicket = 3,
        Cosmetic = 4
    }

    /// <summary>What happens after the last day is claimed. Serialized as int: append only.</summary>
    public enum SevenDayCyclePolicy
    {
        /// <summary>The next claim is day 1 again (working assumption, OPEN 2026-10-28 #2).</summary>
        RestartFromDayOne = 0,

        /// <summary>No more check-in rewards after the last day.</summary>
        EndAfterLastDay = 1
    }

    /// <summary>One day's reward: a kind plus an amount, or a cosmetic for the Cosmetic kind.</summary>
    [Serializable]
    public struct SevenDayReward
    {
        [Tooltip("What the day gives. None = empty day.")]
        [SerializeField] private SevenDayRewardKind kind;

        [Tooltip("How much Cash / how many tickets. Not used by Cosmetic.")]
        [SerializeField, Min(0)] private int amount;

        [Tooltip("The item given by a Cosmetic day. Not used by the other kinds.")]
        [SerializeField] private CosmeticDefinition cosmetic;

        public SevenDayRewardKind Kind => kind;
        public int Amount => amount;
        public CosmeticDefinition Cosmetic => cosmetic;

        /// <summary>True when the day gives nothing: kind None, an amount kind with no amount, or a Cosmetic day without an item.</summary>
        public bool IsEmpty
        {
            get
            {
                switch (kind)
                {
                    case SevenDayRewardKind.Cash:
                    case SevenDayRewardKind.EntryTicket:
                    case SevenDayRewardKind.TimedTicket:
                        return amount <= 0;
                    case SevenDayRewardKind.Cosmetic:
                        return cosmetic == null;
                    case SevenDayRewardKind.None:
                    default:
                        return true;
                }
            }
        }
    }

    /// <summary>
    /// The 7-day check-in rewards (PDF §2.4, marked provisional there). Days are 1-based like the PDF. Empty days are
    /// normal data, not errors: a missing or None day gives nothing, so an undecided day stays empty instead of
    /// being invented.
    /// </summary>
    [CreateAssetMenu(menuName = "Speed Stars/Config/Seven Day Reward", fileName = "SevenDayRewardConfig")]
    public sealed class SevenDayRewardConfig : ScriptableObject
    {
        /// <summary>Length of the check-in calendar; the feature is defined as seven days.</summary>
        public const int DayCount = 7;

        [Tooltip("Element 0 = day 1 ... element 6 = day 7. Kind None = empty day.")]
        [SerializeField] private SevenDayReward[] days = new SevenDayReward[DayCount];

        [Tooltip("What happens after the last day is claimed.")]
        [SerializeField] private SevenDayCyclePolicy cyclePolicy;

        public SevenDayCyclePolicy CyclePolicy => cyclePolicy;

        /// <summary>The reward of a 1-based day; an empty reward for days outside 1..DayCount or missing from the list.</summary>
        public SevenDayReward GetDay(int dayNumber)
        {
            if (days == null || dayNumber < 1 || dayNumber > DayCount || dayNumber > days.Length) return default;
            return days[dayNumber - 1];
        }

        private void OnValidate()
        {
            if (days != null && days.Length != DayCount)
                UnityEngine.Debug.LogWarning($"{name}: {days.Length} days listed, the check-in has {DayCount}; missing days are empty, extra days are ignored.", this);
        }
    }
}
