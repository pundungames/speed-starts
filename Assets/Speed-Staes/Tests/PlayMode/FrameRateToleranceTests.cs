using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.PlayTests
{
    /// <summary>
    /// The same press schedule, on the same game clock, played at 30 / 60 / 120 FPS must give the same race within a small
    /// tolerance. Presses go in through RunnerInput.Inject with their own exact timestamps and arrive one frame later than
    /// they happened (inside the frame interval that contains them), the way Input System events do. Time.captureFramerate
    /// fixes the frame length, so the run is deterministic and not bound to wall time. The tolerance is a relation between the
    /// runs, not a balance number. Assembly-CSharp is reached by reflection (RunnerBody.Sim: Time, Distance, Speed).
    /// </summary>
    public sealed class FrameRateToleranceTests
    {
        static readonly int[] FrameRates = { 30, 60, 120 };
        const int ReferenceFps = 60;
        const double SampleTime = 6.0;       // sim seconds at which distance and speed are compared when the race is not finished
        const double FinishDistance = 100.0;
        const double GiveUpTime = 30.0;      // sim seconds; a schedule that has not finished by now is compared at SampleTime only
        const double RelativeTolerance = 0.01;
        const double Lead = 0.25;            // quiet time before the first press

        struct Result
        {
            public int Fps;
            public double Finish;            // sim seconds to 100 m, -1 when not reached
            public double Dist6, Speed6;
        }

        /// <summary>Pressing schedule: press time (s after start), leg, release time.</summary>
        struct Step
        {
            public double Press, Release;
            public int Leg;
        }

        GameObject _runner;
        int _savedVSync, _savedTargetFps;

        [SetUp]
        public void Setup()
        {
            _savedVSync = QualitySettings.vSyncCount;
            _savedTargetFps = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0;
            QualitySettings.vSyncCount = _savedVSync;
            Application.targetFrameRate = _savedTargetFps;
            if (_runner) UnityEngine.Object.Destroy(_runner);
            yield return null;
        }

        /// <summary>Alternating feet on a steady beat (a tap per beat, released shortly after).</summary>
        static List<Step> Schedule(double interval, double hold, int count)
        {
            var list = new List<Step>();
            for (int i = 0; i < count; i++)
            {
                double t = Lead + i * interval;
                list.Add(new Step { Press = t, Release = t + hold, Leg = i % 2 });
            }
            return list;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator SteadyRhythmGivesTheSameRaceAtEveryFrameRate()
        {
            // 0.32 s per press: the rhythm of the measurements in CLAUDE.md.
            var schedule = Schedule(0.32, 0.12, 100);
            var results = new List<Result>();
            foreach (int fps in FrameRates) yield return Run(fps, schedule, results);
            AssertClose("steady", results);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FrequentPressingGivesTheSameRaceAtEveryFrameRate()
        {
            // 0.1 s per press (10 per second) and a very short hold: the "mash" pattern.
            var schedule = Schedule(0.10, 0.04, 320);
            var results = new List<Result>();
            foreach (int fps in FrameRates) yield return Run(fps, schedule, results);
            AssertClose("frequent", results);
        }

        IEnumerator Run(int fps, List<Step> schedule, List<Result> results)
        {
            if (_runner) UnityEngine.Object.Destroy(_runner);
            yield return null;

            Time.captureFramerate = fps;
            yield return null;

            _runner = new GameObject("FPS Tolerance Runner " + fps);
            Component input = _runner.AddComponent(RuntimeType("SpeedStars.Runner.RunnerInput"));
            Component body = _runner.AddComponent(RuntimeType("SpeedStars.Physical.RunnerBody"));
            // Awake of the body (its time origin) ran in AddComponent: this is the same game time.
            double origin = Time.timeAsDouble;
            var inject = input.GetType().GetMethod("Inject");
            var simProperty = body.GetType().GetProperty("Sim");
            object sim = simProperty.GetValue(body);
            var timeProp = sim.GetType().GetProperty("Time");
            var distProp = sim.GetType().GetProperty("Distance");
            var speedProp = sim.GetType().GetProperty("Speed");

            // Events sorted by time; the release of a step is queued with its own time.
            var events = new List<(double time, int leg, bool pressed)>();
            foreach (Step s in schedule)
            {
                events.Add((s.Press, s.Leg, true));
                events.Add((s.Release, s.Leg, false));
            }
            events.Sort((a, b) => a.time.CompareTo(b.time));
            int next = 0;

            double frame = 1.0 / fps;
            var result = new Result { Fps = fps, Finish = -1, Dist6 = -1, Speed6 = -1 };
            double prevTime = 0, prevDist = 0;
            bool sampled = false;

            // Runs after the body's Update of a frame: queue what happens inside the NEXT frame interval, so the body consumes
            // it at that frame's Update (a press older than the sim would land late; this is how a real event arrives).
            while (true)
            {
                double simTime = (double)timeProp.GetValue(sim);
                double dist = (float)distProp.GetValue(sim);
                double speed = (float)speedProp.GetValue(sim);

                if (!sampled && simTime >= SampleTime - 1e-4)
                {
                    double over = simTime - SampleTime;
                    result.Dist6 = dist - speed * over;
                    result.Speed6 = speed;
                    sampled = true;
                }
                if (result.Finish < 0 && dist >= FinishDistance && simTime > prevTime)
                {
                    result.Finish = prevTime + (simTime - prevTime) * (FinishDistance - prevDist) / (dist - prevDist);
                }
                prevTime = simTime;
                prevDist = dist;
                if (result.Finish >= 0 || simTime >= GiveUpTime || next >= events.Count && simTime > schedule[schedule.Count - 1].Release + 3) break;

                double gameNow = Time.timeAsDouble - origin;
                double nextNow = gameNow + frame;
                while (next < events.Count && events[next].time <= nextNow + 1e-4)
                {
                    var e = events[next++];
                    inject.Invoke(input, new object[] { e.leg, e.pressed, origin + e.time });
                }
                yield return null;
            }

            if (!sampled)
            {
                result.Dist6 = prevDist;
                result.Speed6 = (float)speedProp.GetValue(sim);
            }
            results.Add(result);
            UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "FPS_TOLERANCE fps={0} finish={1:0.0000} dist6={2:0.0000} speed6={3:0.0000}",
                result.Fps, result.Finish, result.Dist6, result.Speed6));
            Time.captureFramerate = 0;
        }

        static void AssertClose(string name, List<Result> results)
        {
            Result reference = results.First(r => r.Fps == ReferenceFps);
            foreach (Result r in results)
            {
                if (r.Fps == ReferenceFps) continue;
                string head = string.Format(CultureInfo.InvariantCulture,
                    "{0}: {1} FPS vs {2} FPS", name, r.Fps, ReferenceFps);
                Assert.AreEqual(reference.Dist6, r.Dist6, Math.Max(0.01, reference.Dist6 * RelativeTolerance),
                    head + ": distance at the sample time differs beyond tolerance.");
                Assert.AreEqual(reference.Speed6, r.Speed6, Math.Max(0.01, reference.Speed6 * RelativeTolerance),
                    head + ": speed at the sample time differs beyond tolerance.");
                Assert.AreEqual(reference.Finish >= 0, r.Finish >= 0, head + ": one finished 100 m and the other did not.");
                if (reference.Finish >= 0 && r.Finish >= 0)
                    Assert.AreEqual(reference.Finish, r.Finish, reference.Finish * RelativeTolerance,
                        head + ": 100 m time differs beyond tolerance.");
            }
        }

        static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).First(t => t != null);
    }
}
