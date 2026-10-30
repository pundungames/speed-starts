using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.PlayTests
{
    public sealed class PerfectStepFeedbackTests
    {
        GameObject _runner;
        Component _body, _input;
        object Sim => _body.GetType().GetProperty("Sim").GetValue(_body);
        T Read<T>(string property) => (T)Sim.GetType().GetProperty(property).GetValue(Sim);
        static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).First(t => t != null);
        void Inject(int leg, bool pressed) => _input.GetType().GetMethod("Inject")
            .Invoke(_input, new object[] { leg, pressed, Time.timeAsDouble });
        int VisibleBursts() => UnityEngine.Object.FindObjectsOfType<LineRenderer>()
            .Count(l => l.gameObject.name == "Contact Ring" && l.gameObject.activeInHierarchy);
        int VisibleEarlyMarks() => UnityEngine.Object.FindObjectsOfType<LineRenderer>()
            .Count(l => l.gameObject.name == "Early Step Arc" && l.gameObject.activeInHierarchy);
        TextMesh LateLabel() => UnityEngine.Object.FindObjectsOfType<TextMesh>()
            .FirstOrDefault(t => t.name == "Late Step Label" && t.gameObject.activeInHierarchy);

        [UnityTest]
        public IEnumerator CorrectRhythmEmitsOnceAndResetClearsContact()
        {
            _runner = new GameObject("Perfect Step Test Runner");
            _input = _runner.AddComponent(RuntimeType("SpeedStars.Runner.RunnerInput"));
            _body = _runner.AddComponent(RuntimeType("SpeedStars.Physical.RunnerBody"));
            yield return null;
            Inject(0, true);
            yield return null;
            yield return null;
            Assert.AreEqual("Start", Read<object>("LastResult").ToString());
            Assert.AreEqual(0, VisibleBursts(), "Standing start must not report perfect rhythm.");
            Inject(0, false);
            float deadline = Time.time + 2;
            while (!Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Assert.IsTrue(Read<bool>("Airborne"));
            while (Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Inject(1, true);
            yield return null;
            yield return null;
            Assert.AreEqual("Good", Read<object>("LastResult").ToString());
            Assert.GreaterOrEqual(Read<float>("LastEfficiency"), 0.7f);
            Assert.AreEqual(1, VisibleBursts());
            Assert.AreEqual(0, VisibleEarlyMarks(), "Good input must not show the early-step cue.");
            Assert.IsNull(LateLabel());
            var ring = UnityEngine.Object.FindObjectsOfType<LineRenderer>().First(l => l.name == "Contact Ring");
            Vector3 contact = ring.transform.position;
            Inject(1, false);
            yield return null;
            yield return null;
            Assert.AreEqual(contact, ring.transform.position, "Contact must stay on the ground as runner moves.");
            _body.GetType().GetMethod("ResetRun").Invoke(_body, null);
            Assert.AreEqual(0, VisibleBursts());
        }

        [UnityTest]
        public IEnumerator EarlyAndLatePressesStaySilent()
        {
            _runner = new GameObject("Perfect Step Test Runner");
            _input = _runner.AddComponent(RuntimeType("SpeedStars.Runner.RunnerInput"));
            _body = _runner.AddComponent(RuntimeType("SpeedStars.Physical.RunnerBody"));
            yield return null;
            Inject(0, true);
            yield return null;
            Inject(0, false);
            float deadline = Time.time + 2;
            while (!Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Inject(1, true);
            yield return null;
            yield return null;
            Assert.AreEqual("Early", Read<object>("LastResult").ToString());
            Assert.AreEqual(0, VisibleBursts());
            Assert.AreEqual(2, VisibleEarlyMarks(), "Early input shows exactly two quiet arcs.");
            Assert.IsNull(LateLabel());
            Inject(1, false);
            yield return new WaitForSeconds(0.45f);
            Assert.AreEqual(0, VisibleEarlyMarks(), "Early cue must fade without lingering.");
            Inject(0, true);
            yield return null;
            yield return null;
            Assert.AreEqual("Late", Read<object>("LastResult").ToString());
            Assert.AreEqual(0, VisibleBursts());
            Assert.AreEqual(0, VisibleEarlyMarks(), "Late input must not show the early-step cue.");
            var late = LateLabel();
            Assert.IsNotNull(late);
            Assert.AreEqual("LATE", late.text);
            Assert.Greater(late.color.r, late.color.g * 3);
            Assert.Greater(late.transform.position.y - _runner.transform.position.y, 1f,
                "Late feedback must be at chest height, away from the feet.");
            yield return new WaitForSeconds(0.65f);
            Assert.IsNull(LateLabel(), "Late text must fade without lingering.");
            Inject(0, false);
            yield return null;
            yield return new WaitForSeconds(0.3f);
            Inject(1, true);
            yield return null;
            yield return null;
            Assert.AreEqual("Late", Read<object>("LastResult").ToString());
            Assert.IsNull(LateLabel(), "Repeated late steps inside 1.5 seconds must stay quiet.");
            _body.GetType().GetMethod("ResetRun").Invoke(_body, null);
            Assert.IsNull(LateLabel(), "Reset clears late text immediately.");
            Inject(0, true);
            Inject(0, false);
            yield return null;
            deadline = Time.time + 2;
            while (!Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Inject(1, true);
            yield return null;
            yield return null;
            Assert.AreEqual(2, VisibleEarlyMarks());
            _body.GetType().GetMethod("ResetRun").Invoke(_body, null);
            Assert.AreEqual(0, VisibleEarlyMarks(), "Reset must clear the cue immediately.");
        }

        [UnityTest]
        public IEnumerator MomentumWindFollowsSpeedAndClearsOnReset()
        {
            _runner = new GameObject("Momentum Wind Test Runner");
            _input = _runner.AddComponent(RuntimeType("SpeedStars.Runner.RunnerInput"));
            _body = _runner.AddComponent(RuntimeType("SpeedStars.Physical.RunnerBody"));
            var tuning = _body.GetType().GetField("tuning").GetValue(_body);
            tuning.GetType().GetField("pushGain").SetValue(tuning, 8f);
            yield return null;
            Assert.IsFalse(UnityEngine.Object.FindObjectsOfType<LineRenderer>()
                .Any(l => l.name == "Momentum Wind Ribbon"));
            Inject(0, true);
            Inject(0, false);
            yield return new WaitForSeconds(0.3f);
            var ribbon = UnityEngine.Object.FindObjectsOfType<LineRenderer>()
                .FirstOrDefault(l => l.name == "Momentum Wind Ribbon");
            Assert.IsNotNull(ribbon, "Running at speed must show back airflow.");
            Assert.Greater(ribbon.widthMultiplier, 0);
            Vector3 head = ribbon.GetPosition(0), tail = ribbon.GetPosition(ribbon.positionCount - 1);
            Assert.Less(Vector3.Dot(tail - head, _runner.transform.forward), -0.2f,
                "Airflow must trail behind the back.");
            Assert.Greater(head.y - _runner.transform.position.y, 1f);
            _body.GetType().GetMethod("ResetRun").Invoke(_body, null);
            Assert.IsFalse(ribbon.gameObject.activeInHierarchy, "Reset clears airflow immediately.");
            Inject(0, true);
            Inject(0, false);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(ribbon.gameObject.activeInHierarchy);
            yield return new WaitForSeconds(4f);
            Assert.IsFalse(ribbon.gameObject.activeInHierarchy, "Wind must fade as the runner slows to a stop.");
        }

        [UnityTest]
        public IEnumerator RecoveryShowsRingAndTextAfterLateStep()
        {
            _runner = new GameObject("Recovery Test Runner");
            _input = _runner.AddComponent(RuntimeType("SpeedStars.Runner.RunnerInput"));
            _body = _runner.AddComponent(RuntimeType("SpeedStars.Physical.RunnerBody"));
            var tuning = _body.GetType().GetField("tuning").GetValue(_body);
            tuning.GetType().GetField("lateSpeed").SetValue(tuning, 0.3f);
            yield return null;
            Inject(0, true);
            yield return null;
            Inject(0, false);
            yield return new WaitForSeconds(0.48f);
            Inject(1, true);
            yield return null;
            Inject(1, false);
            yield return null;
            yield return null;
            Assert.AreEqual("Late", Read<object>("LastResult").ToString());
            float deadline = Time.time + 2;
            while (!Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Assert.IsTrue(Read<bool>("Airborne"));
            while (Read<bool>("Airborne") && Time.time < deadline) yield return null;
            Inject(0, true);
            Inject(0, false);
            yield return null;
            yield return null;
            Assert.AreEqual("Good", Read<object>("LastResult").ToString());
            var label = UnityEngine.Object.FindObjectsOfType<TextMesh>().FirstOrDefault(t => t.text == "RECOVER!");
            Assert.IsNotNull(label);
            Assert.Greater(label.transform.position.y - _runner.transform.position.y, 1);
            Assert.AreEqual(1, VisibleBursts());
            Assert.IsFalse(UnityEngine.Object.FindObjectsOfType<LineRenderer>().Any(l => l.name == "Push Streak"));
            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(label.gameObject.activeInHierarchy);
            _body.GetType().GetMethod("ResetRun").Invoke(_body, null);
            Assert.AreEqual(0, VisibleBursts());
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_runner) UnityEngine.Object.Destroy(_runner);
            yield return null;
        }
    }
}
