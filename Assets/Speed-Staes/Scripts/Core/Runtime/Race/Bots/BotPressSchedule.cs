using System;
using System.Collections.Generic;

namespace SpeedStars.Core.Race.Bots
{
    /// <summary>One key event of a bot, measured from GO.</summary>
    public readonly struct BotPressEvent
    {
        /// <summary>Seconds after GO.</summary>
        public readonly double Time;
        /// <summary>0 or 1; the legs take turns.</summary>
        public readonly int Leg;
        /// <summary>True = key goes down, false = key is released.</summary>
        public readonly bool IsPress;

        public BotPressEvent(double time, int leg, bool isPress) { Time = time; Leg = leg; IsPress = isPress; }
    }

    /// <summary>
    /// A bot's whole key timeline: the legs press in turn every press period, each key is held for the hold time. The first press waits
    /// at least the reaction time; every press moves by a bounded jitter, and now and then (mistake chance) by a larger early or late
    /// shift. Times never go backwards: a press never starts before the previous release. Pure; the same plan and seed give the same events.
    /// </summary>
    public sealed class BotPressSchedule
    {
        /// <summary>Smallest gap kept between a release and the next press (seconds); only keeps the order strict.</summary>
        private const double MinGapSeconds = 0.001;

        private readonly List<BotPressEvent> _events;

        /// <summary>Press and release events, sorted by time, starting with a press.</summary>
        public IReadOnlyList<BotPressEvent> Events => _events;

        /// <summary>Time of the last event (seconds after GO).</summary>
        public double EndSeconds => _events.Count == 0 ? 0.0 : _events[_events.Count - 1].Time;

        /// <param name="horizonSeconds">Presses are made while their time is below this (e.g. the target time plus slack).</param>
        public BotPressSchedule(BotPlan plan, Random random, double horizonSeconds)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (random == null) throw new ArgumentNullException(nameof(random));

            _events = new List<BotPressEvent>();
            double period = Math.Max(plan.PressPeriodSeconds, 0.01);
            double hold = Math.Max(plan.HoldSeconds, 0.001);
            double reaction = Math.Max(plan.ReactionSeconds, 0.0);
            double lastRelease = 0.0;

            for (int k = 0; ; k++)
            {
                double press = reaction + k * period + Offset(plan, period, random);
                if (press < reaction) press = reaction;                          // never before the reaction
                if (k > 0 && press < lastRelease + MinGapSeconds) press = lastRelease + MinGapSeconds;
                if (press >= horizonSeconds) break;

                int leg = k % 2;
                double release = press + hold;
                _events.Add(new BotPressEvent(press, leg, true));
                _events.Add(new BotPressEvent(release, leg, false));
                lastRelease = release;
            }
        }

        private static double Offset(BotPlan plan, double period, Random random)
        {
            double jitter = (random.NextDouble() * 2.0 - 1.0) * plan.JitterSeconds;
            if (random.NextDouble() >= plan.MistakeChance) return jitter;
            // Mistake: early or late by up to the shift ratio of a period.
            double shift = random.NextDouble() * plan.MistakeShiftRatio * period;
            return jitter + (random.NextDouble() < 0.5 ? -shift : shift);
        }
    }
}
