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
    /// When a bot asks for its hurdle jump (T-086). Pure, shared by the race (RaceBots) and the calibration tool, so the measured times
    /// include the same jumps. The jump is asked so that the middle of the flight lands over the next hurdle, from the speed now; each
    /// hurdle gets one timing error: the plan's jitter, or with the mistake chance a whole flight early or late, which knocks the hurdle.
    /// </summary>
    public sealed class BotJumpDriver
    {
        private const float Asked = float.MaxValue; // this hurdle's jump is already on its way

        private readonly Random _random;
        private int _planned = -1;
        private float _error;

        public BotJumpDriver(Random random) => _random = random ?? throw new ArgumentNullException(nameof(random));

        /// <summary>A new race: no hurdle planned yet.</summary>
        public void Reset()
        {
            _planned = -1;
            _error = 0f;
        }

        /// <summary>True = ask for the jump now. Call every frame while a hurdle is ahead; <paramref name="hurdleIndex"/> tells hurdles apart.</summary>
        public bool ShouldJump(int hurdleIndex, float hurdleMeters, float distance, float speed, bool inJump,
            float windupSeconds, float airSeconds, float jitterSeconds, float mistakeChance)
        {
            if (_planned < hurdleIndex)
            {
                _planned = hurdleIndex;
                float sign = _random.Next(2) == 0 ? -1f : 1f;
                _error = sign * (_random.NextDouble() < mistakeChance ? airSeconds : (float)_random.NextDouble() * jitterSeconds);
            }
            else if (_error == Asked) return false;

            if (inJump || speed <= 0.1f) return false;
            if ((hurdleMeters - distance) / speed > windupSeconds + airSeconds * 0.5f + _error) return false;
            _error = Asked;
            return true;
        }
    }

    /// <summary>
    /// A bot's whole key timeline: the legs press in turn every press period, each key is held for the hold time. The first press waits
    /// at least the reaction time; every press moves by a bounded jitter, and now and then (mistake chance) by a larger early or late
    /// shift. On top come the plan's <see cref="BotQuirks"/>: the bot's own style (faster or slower tapping, more or fewer slips),
    /// stretches of faster and slower tempo, the same foot twice, and rushes (a burst of fast presses, then slower ones until the
    /// rhythm is back), each rush a little different. Times never go backwards: a press never starts before the previous release.
    /// Pure; the same plan and seed give the same events.
    /// </summary>
    public sealed class BotPressSchedule
    {
        /// <summary>Smallest gap kept between a release and the next press (seconds); only keeps the order strict.</summary>
        private const double MinGapSeconds = 0.001;
        /// <summary>No rush or wrong foot in the first presses: the start stays clean.</summary>
        private const int CleanStartPresses = 4;
        /// <summary>A stretch of one tempo lasts this many presses (min, max).</summary>
        private const int PhraseMin = 6, PhraseMax = 16;
        /// <summary>Share of the way to the next stretch's tempo covered per press.</summary>
        private const double PhraseGlide = 0.25;
        /// <summary>Presses in one stumble (min, max), each anywhere from this much to that much of a period apart.</summary>
        private const int StumbleMin = 2, StumbleMax = 4;
        private const double StumbleRatioMin = 0.5, StumbleRatioMax = 1.9;
        /// <summary>A bot takes at least this share of its style's tempo and hold change (the rest drawn).</summary>
        private const double StyleStrengthMin = 0.6;
        /// <summary>The pause after a stumble, in periods (min, max).</summary>
        private const double PauseMin = 1.5, PauseMax = 2.3;

        private readonly List<BotPressEvent> _events;

        /// <summary>Press and release events, sorted by time, starting with a press.</summary>
        public IReadOnlyList<BotPressEvent> Events => _events;

        /// <summary>Time of the last event (seconds after GO).</summary>
        public double EndSeconds => _events.Count == 0 ? 0.0 : _events[_events.Count - 1].Time;

        /// <summary>The style this bot drew; empty without styles.</summary>
        public string StyleName { get; }

        /// <param name="horizonSeconds">Presses are made while their time is below this (e.g. the target time plus slack).</param>
        public BotPressSchedule(BotPlan plan, Random random, double horizonSeconds)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (random == null) throw new ArgumentNullException(nameof(random));

            _events = new List<BotPressEvent>();
            BotQuirks q = plan.Quirks;
            // This bot's style, drawn once: its tempo and hold, how much its tempo moves, how prone it is to each slip.
            bool styled = TryPickStyle(q.Styles, random, out BotStyle style);
            StyleName = styled ? style.name ?? string.Empty : string.Empty;
            double share = styled ? StyleStrengthMin + (1.0 - StyleStrengthMin) * random.NextDouble() : 0.0; // no two bots of a style alike
            double period = Math.Max(plan.PressPeriodSeconds * (styled ? 1.0 + (style.periodScale - 1.0) * share : 1.0), 0.01);
            double hold = Math.Max(plan.HoldSeconds * (styled ? 1.0 + (style.holdScale - 1.0) * share : 1.0), 0.001);
            double driftRange = q.TempoDrift * (styled ? style.driftScale : 1f);
            double wrongFootChance = q.WrongFootChance * (styled ? style.wrongFootScale : 1f);
            double rushChance = q.RushChance * (styled ? style.rushScale : 1f);
            double stumbleChance = q.StumbleChance * (styled ? style.stumbleScale : 1f);
            double reaction = Math.Max(plan.ReactionSeconds, 0.0);
            double lastRelease = 0.0;
            double nominal = reaction, drift = 0.0, driftTarget = 0.0, rushRatio = 1.0, recoverRatio = 1.0;
            int leg = 1, rushLeft = 0, recoverLeft = 0, phraseLeft = 0, stumbleLeft = 0;
            bool pauseNext = false;

            for (int k = 0; ; k++)
            {
                bool stumbling = false;
                if (k > 0)
                {
                    double ratio = 1.0 + drift;
                    if (rushLeft > 0)
                    {
                        ratio = rushRatio;
                        if (--rushLeft == 0) StartRecovery(q, random, ref recoverLeft, ref recoverRatio);
                    }
                    else if (stumbleLeft > 0)
                    {
                        ratio = StumbleRatioMin + (StumbleRatioMax - StumbleRatioMin) * random.NextDouble();
                        stumbling = true;
                        if (--stumbleLeft == 0) pauseNext = true;
                    }
                    else if (pauseNext)
                    {
                        ratio = PauseMin + (PauseMax - PauseMin) * random.NextDouble();
                        pauseNext = false;
                        StartRecovery(q, random, ref recoverLeft, ref recoverRatio);
                    }
                    else if (recoverLeft > 0) { ratio = recoverRatio; recoverLeft--; }
                    nominal += period * ratio;
                }
                if (driftRange > 0.0)
                {
                    if (--phraseLeft <= 0)
                    {
                        driftTarget = Math.Max(-0.5, (random.NextDouble() * 2.0 - 1.0) * driftRange);
                        phraseLeft = random.Next(PhraseMin, PhraseMax + 1);
                    }
                    drift += (driftTarget - drift) * PhraseGlide;
                }

                double press = nominal + Offset(plan, period, random);
                if (press < reaction) press = reaction;                          // never before the reaction
                if (k > 0 && press < lastRelease + MinGapSeconds) press = lastRelease + MinGapSeconds;
                if (press >= horizonSeconds) break;

                bool settled = k >= CleanStartPresses && rushLeft == 0 && recoverLeft == 0 && stumbleLeft == 0 && !pauseNext && !stumbling;
                bool sameFoot = stumbling ? random.NextDouble() < 0.5
                    : settled && wrongFootChance > 0.0 && random.NextDouble() < wrongFootChance;
                if (!sameFoot) leg = 1 - leg;
                if (settled && rushChance > 0.0 && random.NextDouble() < rushChance)
                {
                    rushLeft = random.Next(q.RushPressesMin, q.RushPressesMax + 1);
                    rushRatio = q.RushPeriodRatio * (0.8 + 0.4 * random.NextDouble());
                }
                else if (settled && stumbleChance > 0.0 && random.NextDouble() < stumbleChance)
                {
                    stumbleLeft = random.Next(StumbleMin, StumbleMax + 1);
                }

                double keyHold = rushLeft > 0 ? Math.Min(hold, period * rushRatio * 0.5)
                    : stumbling ? hold * (0.6 + 1.2 * random.NextDouble())
                    : hold;
                double release = press + keyHold;
                _events.Add(new BotPressEvent(press, leg, true));
                _events.Add(new BotPressEvent(release, leg, false));
                lastRelease = release;
            }
        }

        private static bool TryPickStyle(IReadOnlyList<BotStyle> styles, Random random, out BotStyle style)
        {
            style = default;
            double total = 0.0;
            for (int i = 0; i < styles.Count; i++) total += Math.Max(0f, styles[i].weight);
            if (total <= 0.0) return false;
            double draw = random.NextDouble() * total;
            for (int i = 0; i < styles.Count; i++)
            {
                style = styles[i];
                draw -= Math.Max(0f, style.weight);
                if (draw < 0.0) return true;
            }
            return true;
        }

        private static void StartRecovery(BotQuirks q, Random random, ref int recoverLeft, ref double recoverRatio)
        {
            recoverLeft = random.Next(Math.Max(1, q.RecoverPresses - 1), q.RecoverPresses + 2);
            recoverRatio = 1.0 + (q.RecoverPeriodRatio - 1.0) * (0.6 + 0.8 * random.NextDouble());
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
