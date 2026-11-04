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
            private readonly PropertyInfo _distance, _speed, _airborne, _head, _form, _time, _lastPress, _lastResult, _lastEff, _presses;

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
    }
}
