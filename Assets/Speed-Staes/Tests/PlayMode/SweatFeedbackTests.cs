using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpeedStars.PlayTests
{
    public sealed class SweatFeedbackTests
    {
        GameObject _player, _npc;
        static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("SpeedStars.Physical." + name)).First(t => t != null);

        [UnityTest]
        public IEnumerator EnergyThresholdsResetAndPlayerOnly()
        {
            var bodyType = RuntimeType("RunnerBody");
            var sweatType = RuntimeType("SweatFeedback");
            _npc = new GameObject("Sweat Test NPC");
            _npc.AddComponent(bodyType);
            Assert.IsNull(_npc.GetComponent(sweatType));
            _player = new GameObject("Sweat Test Player");
            var body = (Behaviour)_player.AddComponent(bodyType);
            bodyType.GetMethod("EnablePlayerFeedback").Invoke(body, null);
            var sweat = (Behaviour)_player.GetComponent(sweatType);
            Assert.IsNotNull(sweat);
            var sim = bodyType.GetProperty("Sim").GetValue(body);
            body.enabled = false; // Freeze only physics so threshold samples remain exact.
            var energy = sim.GetType().GetProperty("Energy");
            float max = (float)sim.GetType().GetProperty("EnergyMax").GetValue(sim);
            yield return null;
            foreach (float sample in new[] { 0.30f, 0.29f, 0.15f, 0.14f, 0.31f })
            {
                energy.SetValue(sim, max * sample);
                yield return null;
                yield return null;
                float expected = sample < 0.15f ? 2f : sample < 0.30f ? 1f : 0f;
                Assert.AreEqual(expected, sweatType.GetProperty("SpeedMultiplier").GetValue(sweat));
                int count = (int)sweatType.GetProperty("ActiveDropCount").GetValue(sweat);
                if (expected == 0f) Assert.AreEqual(0, count);
                else Assert.Greater(count, 0, "Low energy must start emitting individual sweat particles.");
            }
            energy.SetValue(sim, max * 0.1f);
            bodyType.GetField("acceptInput").SetValue(body, false);
            yield return null;
            Assert.Greater((int)sweatType.GetProperty("ActiveDropCount").GetValue(sweat), 0,
                "Low energy sweat must remain visible when race input is gated.");
            var particles = sweat.GetComponentInChildren<ParticleSystem>(true);
            Assert.IsNotNull(particles);
            Assert.AreEqual(2f, particles.main.simulationSpeed);
            yield return new WaitForSeconds(0.2f);
            var sampleParticles = new ParticleSystem.Particle[12];
            int alive = particles.GetParticles(sampleParticles);
            Assert.IsTrue(sampleParticles.Take(alive).Any(p => p.velocity.y < 0f),
                "After the initial hop, gravity must pull the drops down.");
            Assert.IsTrue(sampleParticles.Take(alive).Any(p => p.velocity.y > 0f),
                "New drops must hop upward before falling.");
            sweat.enabled = false;
            Assert.AreEqual(0, sweatType.GetProperty("ActiveDropCount").GetValue(sweat));
            sweat.enabled = true;
            yield return null;
            Assert.Greater((int)sweatType.GetProperty("ActiveDropCount").GetValue(sweat), 0);
            bodyType.GetMethod("ResetRun").Invoke(body, null);
            Assert.AreEqual(0, sweatType.GetProperty("ActiveDropCount").GetValue(sweat));
        }

        [TearDown]
        public void Cleanup()
        {
            if (_player) UnityEngine.Object.DestroyImmediate(_player);
            if (_npc) UnityEngine.Object.DestroyImmediate(_npc);
        }
    }
}
