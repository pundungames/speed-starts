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
        [UnityTest]
        public IEnumerator CaptureActualRaceFeedback()
        {
            yield return PlaySmokeSupport.LoadScene("Race");
            yield return new WaitForSeconds(4f);
            var body = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().First(b => b.GetType().Name == "RunnerBody");
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
            while (airborne() && Time.time < deadline) yield return null;
            tap(1, true);
            yield return null;
            tap(1, false);
            yield return new WaitForEndOfFrame();
            Capture("contact");
            foreach (var line in UnityEngine.Object.FindObjectsOfType<LineRenderer>())
                if (line.name == "Contact Ring") Debug.Log($"VFX CONTACT position={line.transform.position} width={line.widthMultiplier} color={line.startColor} shader={line.sharedMaterial.shader.name} supported={line.sharedMaterial.shader.isSupported}");
            var tuning = type.GetField("tuning").GetValue(body);
            tuning.GetType().GetField("pushGain").SetValue(tuning, 8f);
            type.GetMethod("ResetRun").Invoke(body, null);
            tap(0, true);
            yield return null;
            tap(0, false);
            yield return new WaitForSeconds(0.3f);
            yield return new WaitForEndOfFrame();
            Capture("wind");
            foreach (var line in UnityEngine.Object.FindObjectsOfType<LineRenderer>())
                if (line.name == "Momentum Wind Ribbon") Debug.Log($"VFX WIND width={line.widthMultiplier} alpha={line.colorGradient.Evaluate(0).a} start={line.GetPosition(0)} end={line.GetPosition(line.positionCount-1)} shader={line.sharedMaterial.shader.name} supported={line.sharedMaterial.shader.isSupported} enabled={line.enabled} bounds={line.bounds} camera={Camera.main.transform.position}");
            Assert.IsNotNull(Camera.main);
        }
        static void Capture(string name)
        {
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
