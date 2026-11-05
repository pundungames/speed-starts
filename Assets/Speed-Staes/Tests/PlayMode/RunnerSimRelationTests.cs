using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// What the runner sim's press quality does (T-108), as relations, no balance number: the head angle tells good and bad running
    /// apart (mashing > pressing early > on the rhythm), a press on time throws the head less than the old flat kick, Form is higher for
    /// a good rhythm than for mashing and fades, and two keys together never beat the best single-key run. <c>RunnerSim</c> lives in
    /// Assembly-CSharp, which this assembly cannot see, so everything goes through reflection. The runner's values are read from the
    /// character prefab (the game runs with them, not with the code defaults); without the Editor the code defaults are used.
    /// </summary>
    public sealed class RunnerSimRelationTests
    {
        private const string PrefabPath = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const float Step = 1f / 240f;
        private const float MaxRaceSeconds = 30f;
        private const float RaceMeters = 100f;
        private const float KeyHold = 0.07f;
        /// <summary>Ignore the start when averaging the head (s), as the probe does.</summary>
        private const float HeadSettleSeconds = 2f;

        private static Type SimType => Resolve("SpeedStars.Physical.RunnerSim");
        private static Type TuningType => Resolve("SpeedStars.Physical.RunnerTuning");

        private static Type Resolve(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).First(t => t != null);

        /// <summary>The prefab's tuning, copied: tests change their copy, not the asset.</summary>
        private static object NewTuning(Action<object> change = null)
        {
            object tuning = Activator.CreateInstance(TuningType);
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, PrefabPath);
            Component body = prefab.GetComponent(Resolve("SpeedStars.Physical.RunnerBody"));
            object source = body.GetType().GetField("tuning").GetValue(body);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), tuning);
#endif
            change?.Invoke(tuning);
            return tuning;
        }

        private static void Set(object tuning, string field, float value) => TuningType.GetField(field).SetValue(tuning, value);

        private sealed class Sim
        {
            private readonly object _sim;
            private readonly MethodInfo _press, _release, _advance;
            private readonly PropertyInfo _distance, _speed, _airborne, _head, _form, _time, _lastPress, _lastResult, _lastEff, _presses, _hopBias;

            public Sim(object tuning)
            {
                _sim = Activator.CreateInstance(SimType, tuning);
                _press = SimType.GetMethod("Press");
                _release = SimType.GetMethod("Release");
                _advance = SimType.GetMethod("Advance");
                _distance = SimType.GetProperty("Distance");
                _speed = SimType.GetProperty("Speed");
                _airborne = SimType.GetProperty("Airborne");
                _head = SimType.GetProperty("HeadAngle");
                _form = SimType.GetProperty("Form");
                _time = SimType.GetProperty("Time");
                _lastPress = SimType.GetProperty("LastPressTime");
                _lastResult = SimType.GetProperty("LastResult");
                _lastEff = SimType.GetProperty("LastEfficiency");
                _presses = SimType.GetProperty("Presses");
                _hopBias = SimType.GetProperty("HopBias");
            }

            public void Press(int leg) => _press.Invoke(_sim, new object[] { leg });
            public void Release(int leg) => _release.Invoke(_sim, new object[] { leg });
            public void Advance(float dt) => _advance.Invoke(_sim, new object[] { dt });
            public float Distance => (float)_distance.GetValue(_sim);
            public float Speed => (float)_speed.GetValue(_sim);
            public bool Airborne => (bool)_airborne.GetValue(_sim);
            public float Head => (float)_head.GetValue(_sim);
            public float Form => (float)_form.GetValue(_sim);
            public double Time => (double)_time.GetValue(_sim);
            public double LastPressTime => (double)_lastPress.GetValue(_sim);
            public string LastResult => _lastResult.GetValue(_sim).ToString();
            public float LastEfficiency => (float)_lastEff.GetValue(_sim);
            public int Presses => (int)_presses.GetValue(_sim);
            public float HopBias => (float)_hopBias.GetValue(_sim);
        }

        private struct Result
        {
            public float Seconds;     // time to RaceMeters, MaxRaceSeconds if not there
            public float HeadAverage; // after the start
            public float HeadPeak;
            public float Form;        // at the end
        }

        /// <summary>Presses by a fixed gap (s); alternating legs, or both keys at once. Runs to the finish or the time limit.</summary>
        private static Result Rhythm(object tuning, float gap, bool both = false, float hold = KeyHold)
        {
            var s = new Sim(tuning);
            var r = new Result();
            double nextPress = 0.2, release = -1, headSum = 0;
            int leg = 0, headCount = 0, releaseA = 0, releaseB = -1;
            double t = 0;
            while (t < MaxRaceSeconds && s.Distance < RaceMeters)
            {
                if (t >= nextPress)
                {
                    s.Press(leg);
                    releaseA = leg;
                    releaseB = -1;
                    if (both) { s.Press(1 - leg); releaseB = 1 - leg; }
                    release = t + hold;
                    leg = both ? leg : 1 - leg;
                    nextPress += gap;
                }
                if (release >= 0 && t >= release)
                {
                    s.Release(releaseA);
                    if (releaseB >= 0) s.Release(releaseB);
                    release = -1;
                }
                s.Advance(Step);
                t += Step;
                r.HeadPeak = Mathf.Max(r.HeadPeak, s.Head);
                if (t > HeadSettleSeconds) { headSum += s.Head; headCount++; }
            }
            r.Seconds = s.Distance >= RaceMeters ? (float)t : MaxRaceSeconds;
            r.HeadAverage = headCount > 0 ? (float)(headSum / headCount) : 0f;
            r.Form = s.Form;
            return r;
        }

        /// <summary>The best player: presses the moment the body is back on the ground.</summary>
        private static Result OnTime(object tuning)
        {
            var s = new Sim(tuning);
            var r = new Result();
            double t = 0, release = -1;
            int leg = 0, held = 0;
            while (t < MaxRaceSeconds && s.Distance < RaceMeters)
            {
                bool landed = !s.Airborne && s.Time - s.LastPressTime > 0.09 && t > 0.2;
                if (landed)
                {
                    s.Press(leg);
                    held = leg;
                    release = t + KeyHold;
                    leg = 1 - leg;
                }
                if (release >= 0 && t >= release) { s.Release(held); release = -1; }
                s.Advance(Step);
                t += Step;
                r.HeadPeak = Mathf.Max(r.HeadPeak, s.Head);
            }
            r.Seconds = s.Distance >= RaceMeters ? (float)t : MaxRaceSeconds;
            r.Form = s.Form;
            return r;
        }

        private static readonly float[] SingleGaps = { 0.22f, 0.24f, 0.26f, 0.28f, 0.30f, 0.34f, 0.40f, 0.50f };

        [Test]
        public void TwoKeysTogetherNeverBeatTheBestSingleKeyRun()
        {
            object tuning = NewTuning();
            float best = SingleGaps.Select(g => Rhythm(tuning, g).Seconds).Concat(new[] { OnTime(tuning).Seconds }).Min();
            foreach (float gap in new[] { 0.20f, 0.22f, 0.24f, 0.26f, 0.30f, 0.34f, 0.40f, 0.50f })
            {
                float pair = Rhythm(tuning, gap, both: true).Seconds;
                Assert.GreaterOrEqual(pair, best - 0.01f, $"both keys every {gap:0.00} s ran faster than any single-key run");
            }
        }

        [Test]
        public void SecondKeyOfAPairAddsNothingUnlessTogetherPushAllowsIt()
        {
            float Speed(float share, bool pair)
            {
                var s = new Sim(NewTuning(t => Set(t, "togetherPush", share)));
                s.Advance(0.2f);
                s.Press(0);
                if (pair) { s.Advance(0.02f); s.Press(1); }
                // A tap, not a hold: the hold thrust (per held foot) is a different thing.
                s.Advance(KeyHold);
                s.Release(0);
                if (pair) s.Release(1);
                s.Advance(0.5f);
                return s.Speed;
            }

            float single = Speed(0f, pair: false);
            Assert.AreEqual(single, Speed(0f, pair: true), single * 0.02f, "a pair is one push");
            Assert.Greater(Speed(1f, pair: true), single * 1.2f, "togetherPush = 1 gives the second key its push");
        }

        [Test]
        public void PairKeepsTheFirstKeysFeedback()
        {
            var s = new Sim(NewTuning());
            s.Advance(0.2f);
            s.Press(0);
            string result = s.LastResult;
            float efficiency = s.LastEfficiency;
            s.Advance(0.02f);
            s.Press(1);
            Assert.AreEqual(2, s.Presses);
            Assert.AreEqual(result, s.LastResult);
            Assert.AreEqual(efficiency, s.LastEfficiency, 1e-4f);
        }

        /// <summary>Taps in the order given (leg numbers; -1 = both keys together), one every gap seconds.</summary>
        private static Sim Taps(object tuning, float gap, params int[] legs)
        {
            var s = new Sim(tuning);
            s.Advance(0.2f);
            foreach (int leg in legs)
            {
                if (leg < 0) { s.Press(0); s.Press(1); } else s.Press(leg);
                s.Advance(KeyHold);
                if (leg < 0) { s.Release(0); s.Release(1); } else s.Release(leg);
                s.Advance(gap - KeyHold);
            }
            return s;
        }

        [Test]
        public void TheSameLegAgainRaisesHopBiasAndTheOtherLegLowersIt()
        {
            object tuning = NewTuning();
            float last = -1f;
            for (int n = 1; n <= 5; n++)
            {
                float bias = Taps(tuning, 0.3f, Enumerable.Repeat(0, n).ToArray()).HopBias;
                Assert.Greater(bias, last, $"{n} taps on the same leg: the bias must keep rising");
                last = bias;
            }
            float other = Taps(tuning, 0.3f, 0, 0, 0, 0, 1).HopBias;
            Assert.Less(other, Taps(tuning, 0.3f, 0, 0, 0, 0, 0).HopBias, "the other leg's press must lower it");
        }

        [Test]
        public void KeysInTurnAndKeysTogetherLeaveHopBiasBelowOneKeyAgain()
        {
            object tuning = NewTuning();
            float single = Taps(tuning, 0.3f, 0, 0, 0, 0, 0, 0, 0, 0).HopBias;
            float turns = Taps(tuning, 0.3f, 0, 1, 0, 1, 0, 1, 0, 1).HopBias;
            float together = Taps(tuning, 0.3f, -1, -1, -1, -1, -1, -1, -1, -1).HopBias;
            Assert.Less(turns, single * 0.5f);
            Assert.Less(together, single * 0.5f);
        }

        [Test]
        public void HopBiasFadesWithoutPresses()
        {
            var s = Taps(NewTuning(), 0.3f, 0, 0, 0, 0, 0, 0);
            float bias = s.HopBias;
            s.Advance(5f);
            Assert.Less(s.HopBias, bias * 0.5f);
        }

        [Test]
        public void HopBiasChangesNoSpeed()
        {
            float Speed(float follow)
            {
                var s = Taps(NewTuning(t => Set(t, "hopBiasFollow", follow)), 0.3f, 0, 0, 0, 0, 0, 0);
                return s.Speed;
            }

            Assert.AreEqual(Speed(0f), Speed(1f), 1e-4f);
        }

        [Test]
        public void LightFastMashIsSlowerThanTheRhythm()
        {
            object tuning = NewTuning();
            Assert.Greater(Rhythm(tuning, 0.14f, hold: 0.04f).Seconds, Rhythm(tuning, 0.26f).Seconds,
                "mashing with light taps must not pay");
        }

        [Test]
        public void MashingShowsMoreHeadThanPressingEarlyThanARhythm()
        {
            object tuning = NewTuning();
            float mash = Rhythm(tuning, 0.14f).HeadAverage;
            float early = Rhythm(tuning, 0.20f).HeadAverage;
            float rhythm = Rhythm(tuning, 0.26f).HeadAverage;
            Assert.Greater(mash, early, "mashing must show the head further forward than pressing early");
            Assert.Greater(early, rhythm, "pressing early must show the head further forward than the rhythm");
        }

        [Test]
        public void PressesOnTimeThrowTheHeadLessThanTheFlatKick()
        {
            // The old behaviour: every press takes the whole kick, whatever its timing.
            object flat = NewTuning(t =>
            {
                Set(t, "goodKickShare", 1f);
                Set(t, "wasteKickShare", 1f);
            });
            float old = Rhythm(flat, 0.26f).HeadPeak;
            float now = Rhythm(NewTuning(), 0.26f).HeadPeak;
            Assert.Less(now, old, "a press on time must not throw the head as hard as a wasted one");
        }

        [Test]
        public void GoodRhythmHasMoreFormThanMashingAndItFadesWithoutPresses()
        {
            object tuning = NewTuning();
            float good = Rhythm(tuning, 0.26f).Form;
            float mash = Rhythm(tuning, 0.14f).Form;
            Assert.Greater(good, mash);

            var s = new Sim(tuning);
            s.Advance(0.2f);
            for (int i = 0; i < 12; i++)
            {
                s.Press(i % 2);
                s.Advance(KeyHold);
                s.Release(i % 2);
                s.Advance(0.26f - KeyHold);
            }
            float running = s.Form;
            s.Advance(4f);
            Assert.Greater(running, 0.5f, "a good rhythm builds Form");
            Assert.Less(s.Form, running * 0.5f, "Form fades with no presses");
        }

        /// <summary>Speed (m/s) after seconds of tapping one key by a fixed gap.</summary>
        private static float OneKeySpeed(object tuning, float gap, float seconds = 8f)
        {
            var s = new Sim(tuning);
            double next = 0.2, release = -1, t = 0;
            while (t < seconds)
            {
                if (t >= next) { s.Press(1); release = t + KeyHold; next += gap; }
                if (release >= 0 && t >= release) { s.Release(1); release = -1; }
                s.Advance(Step);
                t += Step;
            }
            return s.Speed;
        }

        [Test]
        public void RepeatingOneKeyDoesNotStopTheRunnerWhereAnAlmostSlowerGapMovesIt()
        {
            object tuning = NewTuning();
            // The pressed leg of a hop never left the body, and a press in flight has no stride to be early for: no step to
            // standstill between neighbouring gaps.
            for (float gap = 0.16f; gap < 0.34f; gap += 0.01f)
            {
                float here = OneKeySpeed(tuning, gap), next = OneKeySpeed(tuning, gap + 0.01f);
                if (next >= 1f) Assert.Greater(here, 0.3f * next, $"gap {gap:0.00} stops where {gap + 0.01f:0.00} runs");
                if (here >= 1f) Assert.Greater(next, 0.3f * here, $"gap {gap + 0.01f:0.00} stops where {gap:0.00} runs");
            }
        }

        [Test]
        public void TappingOneKeyFasterThanHopFullGapIsNotFasterThanAtThatGap()
        {
            object tuning = NewTuning();
            float full = (float)TuningType.GetField("hopFullGap").GetValue(tuning);
            float atFull = OneKeySpeed(tuning, full);
            for (float gap = full - 0.01f; gap >= 0.14f; gap -= 0.02f)
                Assert.LessOrEqual(OneKeySpeed(tuning, gap), atFull + 0.2f, $"gap {gap:0.00} beats hopFullGap");
        }

        /// <summary>Speed (m/s) after seconds of alternating taps by a fixed gap and hold.</summary>
        private static float AlternatingSpeed(object tuning, float gap, float hold, float seconds)
        {
            var s = new Sim(tuning);
            double next = 0.2, release = -1, t = 0;
            int leg = 0, relLeg = 0;
            while (t < seconds)
            {
                if (t >= next) { s.Press(leg); relLeg = leg; release = t + hold; leg = 1 - leg; next += gap; }
                if (release >= 0 && t >= release) { s.Release(relLeg); release = -1; }
                s.Advance(Step);
                t += Step;
            }
            return s.Speed;
        }

        private static readonly float[] MashGaps = { 0.08f, 0.09f, 0.10f, 0.11f, 0.12f, 0.13f, 0.14f, 0.15f, 0.16f };
        private static readonly float[] MashHolds = { 0.04f, 0.07f };

        [Test]
        public void SustainedMashingKeepsMovingAndIsSlowerThanASlowRhythm()
        {
            object tuning = NewTuning();
            float slow = AlternatingSpeed(tuning, 0.5f, KeyHold, 10f);
            foreach (float hold in MashHolds)
                foreach (float gap in MashGaps)
                {
                    float mash = AlternatingSpeed(tuning, gap, hold, 10f);
                    Assert.Greater(mash, 0.5f, $"mashing at {gap:0.00} (key {hold:0.00}) must not leave the runner standing");
                    Assert.Less(mash, slow, $"mashing at {gap:0.00} (key {hold:0.00}) must not beat a slow rhythm");
                }
        }

        [Test]
        public void MashingCoversTheRaceNoFasterThanASlowRhythmWhateverTheKeyTime()
        {
            // Every other mashed press lands on the beat by chance and is a perfect one: its relief must not wipe out what the
            // wasted ones built, or mashing runs as fast as a good rhythm (T-114).
            object tuning = NewTuning();
            float slow = Rhythm(tuning, 0.50f).Seconds;
            foreach (float hold in MashHolds)
                foreach (float gap in MashGaps)
                    Assert.Greater(Rhythm(tuning, gap, hold: hold).Seconds, slow, $"mashing at {gap:0.00} (key {hold:0.00}) is faster than a 0.50 rhythm");
        }

        [Test]
        public void AClearlyTooFastCadenceIsSlowerThanTheFastestRhythm()
        {
            object tuning = NewTuning();
            float fast = Rhythm(tuning, 0.30f).Seconds;
            foreach (float hold in MashHolds)
                for (int i = 18; i <= 22; i++)
                    Assert.Greater(Rhythm(tuning, i / 100f, hold: hold).Seconds, fast, $"{i / 100f:0.00} (key {hold:0.00}) is faster than a 0.30 rhythm");
        }

        [Test]
        public void NeighbouringMashGapsDoNotJumpInSpeed()
        {
            // Gaps 0.01 apart from mashing up to a fast rhythm: the slope between them is continuous, no step to a crawl.
            object tuning = NewTuning();
            foreach (float hold in MashHolds)
            {
                float prev = AlternatingSpeed(tuning, 0.08f, hold, 10f);
                for (int i = 9; i <= 24; i++)
                {
                    float now = AlternatingSpeed(tuning, i / 100f, hold, 10f);
                    Assert.LessOrEqual(Mathf.Max(now, prev) / Mathf.Max(0.05f, Mathf.Min(now, prev)), 1.6f, $"speed jumps between {(i - 1) / 100f:0.00} and {i / 100f:0.00} (key {hold:0.00})");
                    prev = now;
                }
            }
        }

        /// <summary>Seconds until the head first passes each angle under alternating taps (-1 = never within 15 s).</summary>
        private static float[] HeadTimes(object tuning, float gap, float hold, params float[] angles)
        {
            var s = new Sim(tuning);
            var times = Enumerable.Repeat(-1f, angles.Length).ToArray();
            double next = 0.2, release = -1, t = 0;
            int leg = 0, relLeg = 0;
            while (t < 15.0)
            {
                if (t >= next) { s.Press(leg); relLeg = leg; release = t + hold; leg = 1 - leg; next += gap; }
                if (release >= 0 && t >= release) { s.Release(relLeg); release = -1; }
                s.Advance(Step);
                t += Step;
                for (int i = 0; i < angles.Length; i++)
                    if (times[i] < 0f && s.Head >= angles[i]) times[i] = (float)t;
            }
            return times;
        }

        [Test]
        public void MashingLeansTheHeadSlowlyAndInOrder()
        {
            // The lean builds over seconds: 30 degrees takes a couple of seconds, 60 a few more, 80 later still; nothing lies down within a second.
            object tuning = NewTuning();
            foreach (float hold in MashHolds)
                foreach (float gap in new[] { 0.12f, 0.13f, 0.14f })
            {
                float[] t = HeadTimes(tuning, gap, hold, 30f, 60f, 79f);
                Assert.Greater(t[0], 1f, $"{gap:0.00}: the head is at 30 degrees within a second");
                Assert.Greater(t[1], t[0] + 1f, $"{gap:0.00}: 60 degrees comes too soon after 30");
                Assert.Greater(t[2], t[1] + 0.5f, $"{gap:0.00}: 80 degrees comes too soon after 60");
                Assert.Less(t[2], 10f, $"{gap:0.00}: the head never gets all the way forward");
            }
        }

        [Test]
        public void BackOnTheRhythmTheHeadComesUpWithinSeconds()
        {
            object tuning = NewTuning();
            var s = new Sim(tuning);
            double next = 0.2, release = -1, t = 0;
            int leg = 0, relLeg = 0;
            float headAtSwitch = 0f;
            double backAt = -1;
            const double SwitchAt = 9.0;
            while (t < 16.0)
            {
                float gap = t < SwitchAt ? 0.13f : 0.28f, hold = t < SwitchAt ? 0.04f : KeyHold;
                if (t >= next) { s.Press(leg); relLeg = leg; release = t + hold; leg = 1 - leg; next += gap; }
                if (release >= 0 && t >= release) { s.Release(relLeg); release = -1; }
                s.Advance(Step);
                t += Step;
                if (headAtSwitch == 0f && t >= SwitchAt) headAtSwitch = s.Head;
                if (backAt < 0 && t >= SwitchAt && s.Head < 15f) backAt = t - SwitchAt;
            }
            Assert.Greater(headAtSwitch, 60f, "the mashing leaned the head forward");
            Assert.That(backAt, Is.InRange(0.0, 3.5), "the rhythm must bring the head up within a few seconds");
        }

        [Test]
        public void MashingBuildsLeanOverSecondsNotInOneSecond()
        {
            object tuning = NewTuning();
            foreach (float hold in MashHolds)
                foreach (float gap in new[] { 0.12f, 0.13f, 0.14f })
                {
                    var s = new Sim(tuning);
                    double next = 0.2, release = -1, t = 0;
                    int leg = 0, relLeg = 0;
                    float headAt1 = 0f;
                    while (t < 6f)
                    {
                        if (t >= next) { s.Press(leg); relLeg = leg; release = t + hold; leg = 1 - leg; next += gap; }
                        if (release >= 0 && t >= release) { s.Release(relLeg); release = -1; }
                        s.Advance(Step);
                        t += Step;
                        if (t < 1f) headAt1 = s.Head;
                    }
                    Assert.Less(headAt1, 0.5f * s.Head, $"the head must go forward gradually under sustained mashing ({gap:0.00}, key {hold:0.00})");
                }
        }

        [Test]
        public void TheRhythmRowsKeepTheHeadLowAndTheirOrder()
        {
            // The cost of mashing must not reach a rhythm: slower rhythms are slower, the head stays up.
            object tuning = NewTuning();
            float mashPeak = Rhythm(tuning, 0.14f, hold: 0.04f).HeadPeak;
            float previous = 0f;
            foreach (float gap in new[] { 0.26f, 0.28f, 0.30f, 0.40f, 0.50f })
            {
                Result r = Rhythm(tuning, gap);
                Assert.Less(r.HeadPeak, 0.5f * mashPeak, $"rhythm {gap:0.00} leans the head");
                Assert.Greater(r.Seconds, previous, $"rhythm {gap:0.00} is not slower than the faster one");
                previous = r.Seconds;
            }
        }
    }
}
