using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.PlayTests
{
    public sealed class StepFeedbackVisualTests
    {
        int _previousCaptureRate;
        [SetUp]
        public void StableCaptureTiming()
        {
            _previousCaptureRate = Time.captureFramerate;
            Time.captureFramerate = 60;
        }
        [TearDown]
        public void RestoreCaptureTiming() => Time.captureFramerate = _previousCaptureRate;
        [UnityTest]
        public IEnumerator CaptureActualRaceFeedback()
        {
            yield return PlaySmokeSupport.LoadScene("Race");
            yield return new WaitForSeconds(4f);
            var adapter = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().First(b => b.GetType().Name == "RaceSessionAdapter");
            var body = (MonoBehaviour)adapter.GetType().GetField("body",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(adapter);
            Assert.IsNotNull(body, "Capture must use the player's runner, not a background runner.");
            foreach (var other in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().Where(b => b.GetType().Name == "RunnerBody" && b != body))
                Assert.IsFalse(other.GetComponents<MonoBehaviour>().Any(c => c.GetType().Name == "PerfectStepFeedback"
                    || c.GetType().Name == "EarlyStepFeedback" || c.GetType().Name == "LateStepFeedback"
                    || c.GetType().Name == "MomentumWindFeedback"), "NPCs must not have player feedback components.");
            var type = body.GetType();
            var input = type.GetField("input").GetValue(body);
            var inject = input.GetType().GetMethod("Inject");
            Action<int, bool> tap = (leg, down) => inject.Invoke(input, new object[] { leg, down, Time.timeAsDouble });
            Func<object> sim = () => type.GetProperty("Sim").GetValue(body);
            Func<bool> airborne = () => (bool)sim().GetType().GetProperty("Airborne").GetValue(sim());
            type.GetField("acceptInput").SetValue(body, true);
            type.GetMethod("ResetRun").Invoke(body, null);
            tap(0, true);
            yield return null;
            tap(0, false);
            float deadline = Time.time + 2;
            while (!airborne() && Time.time < deadline) yield return null;
            Assert.IsTrue(airborne(), "Initial stride must enter flight before the next press.");
            while (airborne() && Time.time < deadline) yield return null;
            tap(1, true);
            yield return null;
            tap(1, false);
            yield return null;
            yield return null;
            // Tell a rhythm/environment miss from a feedback bug: the press must be a good, efficient one before a PERFECT label is expected.
            object contactSim = sim();
            Assert.AreEqual("Good", contactSim.GetType().GetProperty("LastResult").GetValue(contactSim).ToString(),
                "The second press after flight must count as Good; otherwise the PERFECT label is rightly absent.");
            Assert.GreaterOrEqual((float)contactSim.GetType().GetProperty("LastEfficiency").GetValue(contactSim), 0.7f,
                "A good press below the efficiency bar shows no PERFECT label.");
            Capture("contact");
            AssertLabelBehind(body.transform, "PERFECT");
            foreach (var line in UnityEngine.Object.FindObjectsOfType<LineRenderer>())
                if (line.name == "Contact Ring") Debug.Log($"VFX CONTACT position={line.transform.position} width={line.widthMultiplier} color={line.startColor} shader={line.sharedMaterial.shader.name} supported={line.sharedMaterial.shader.isSupported}");
            var tuning = type.GetField("tuning").GetValue(body);
            tuning.GetType().GetField("pushGain").SetValue(tuning, 8f);
            float oldKick = (float)tuning.GetType().GetField("headKick").GetValue(tuning);
            float oldNod = (float)tuning.GetType().GetField("headNod").GetValue(tuning);
            tuning.GetType().GetField("headKick").SetValue(tuning, 0f);
            tuning.GetType().GetField("headNod").SetValue(tuning, 0f);
            type.GetMethod("ResetRun").Invoke(body, null);
            tap(0, true);
            yield return null;
            tap(0, false);
            for (int step = 0; step < 3; step++)
            {
                deadline = Time.time + 2;
                while (!airborne() && Time.time < deadline) yield return null;
                Assert.IsTrue(airborne());
                while (airborne() && Time.time < deadline) yield return null;
                tap((step + 1) % 2, true);
                yield return null;
                tap((step + 1) % 2, false);
                yield return null;
            }
            yield return null;
            Capture("wind");
            Assert.AreEqual(3, body.GetComponentsInChildren<LineRenderer>().Count(l => l.name == "Momentum Air Stroke"));
            foreach (var line in UnityEngine.Object.FindObjectsOfType<LineRenderer>())
                if (line.name == "Momentum Wind Ribbon") Debug.Log($"VFX WIND width={line.widthMultiplier} alpha={line.colorGradient.Evaluate(0).a} start={line.GetPosition(0)} end={line.GetPosition(line.positionCount-1)} shader={line.sharedMaterial.shader.name} supported={line.sharedMaterial.shader.isSupported} enabled={line.enabled} bounds={line.bounds} camera={Camera.main.transform.position}");
            Assert.IsNotNull(Camera.main);
            tuning.GetType().GetField("pushGain").SetValue(tuning, 1.5f);
            tuning.GetType().GetField("headKick").SetValue(tuning, oldKick);
            tuning.GetType().GetField("headNod").SetValue(tuning, oldNod);
            tuning.GetType().GetField("lateSpeed").SetValue(tuning, 0.3f);
            type.GetMethod("ResetRun").Invoke(body, null);
            tap(0, true);
            yield return null;
            tap(0, false);
            yield return new WaitForSeconds(0.48f);
            tap(1, true);
            yield return null;
            tap(1, false);
            yield return null;
            Capture("late");
            AssertLabelBehind(body.transform, "LATE");
            deadline = Time.time + 2;
            while (!airborne() && Time.time < deadline) yield return null;
            Assert.IsTrue(airborne());
            while (airborne() && Time.time < deadline) yield return null;
            tap(0, true);
            yield return null;
            tap(0, false);
            yield return null;
            Capture("recovery");
            AssertLabelBehind(body.transform, "RECOVER!");
            Assert.IsFalse(UnityEngine.Object.FindObjectsOfType<TextMesh>().Any(t => t.text == "LATE"),
                "Recovery must replace the late warning without overlapping text.");
        }
        static void AssertLabelBehind(Transform runner, string text)
        {
            var label = UnityEngine.Object.FindObjectsOfType<TextMesh>().FirstOrDefault(t => t.text == text);
            Assert.IsNotNull(label, text + " label must be visible.");
            Vector3 head = runner.GetComponentsInChildren<Transform>(true).First(t => t.name == "B-head").position;
            Vector3 right = Camera.main.transform.right;
            float side = Vector3.Dot(runner.forward, right) < 0 ? -1 : 1;
            Assert.Less(Vector3.Dot(label.transform.position - head, right) * side, -0.5f);
            Assert.Greater(label.transform.position.y, head.y - 0.05f,
                "Text must clear the chest-height wind ribbon.");
        }
        static void Capture(string name)
        {
            // Batchmode has no end-of-frame screenshot; the logic asserts around the call still run.
            if (Application.isBatchMode) return;
            Directory.CreateDirectory("Temp/FeedbackReview");
            var camera = Camera.main;
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            var rt = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                File.WriteAllBytes("Temp/FeedbackReview/" + name + ".png", texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                UnityEngine.Object.Destroy(rt);
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
