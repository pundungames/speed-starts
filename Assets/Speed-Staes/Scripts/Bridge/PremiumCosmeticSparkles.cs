using System.Collections.Generic;
using SpeedStars.Meta.Config;
using SpeedStars.Meta.Contracts;
using SpeedStars.Meta.Cosmetics;
using UnityEngine;

namespace SpeedStars.Bridge
{
    /// <summary>Short, locally simulated glints on equipped premium garments. Shoes keep their own ribbons.</summary>
    public sealed class PremiumCosmeticSparkles : MonoBehaviour
    {
        readonly Dictionary<SkinnedMeshRenderer, ParticleSystem> _emitters = new Dictionary<SkinnedMeshRenderer, ParticleSystem>();
        readonly HashSet<ParticleSystem> _selected = new HashSet<ParticleSystem>();

        public void Refresh(CosmeticOutfit outfit, CosmeticCatalog catalog, Transform accessories)
        {
            Clear();
            foreach (CosmeticCategory category in System.Enum.GetValues(typeof(CosmeticCategory)))
            {
                if (category == CosmeticCategory.Shoes) continue;
                CosmeticDefinition item;
                if (!catalog.TryGet(outfit.Get(category).CosmeticId, out item) || !item.IsPremium) continue;
                Transform slot = accessories.Find(category.ToString());
                if (!slot) continue;
                // Each category and variant gets a stable, distinct hue and one of three silhouettes.
                int variant = Mathf.Max(0, item.SortOrder - 1);
                int style = (int)category * 2 + variant;
                Color tint = Color.HSVToRGB(Mathf.Repeat(style * 0.13750776f + 0.06f, 1f), 0.62f, 1f);
                string hex = outfit.Get(category).ColorHex;
                Color garment;
                if (CosmeticColor.TryParse(hex, out garment))
                    tint = Color.Lerp(tint, garment, 0.35f);
                foreach (string name in item.PartNames)
                {
                    Transform part = slot.Find(name);
                    if (!part || !part.gameObject.activeSelf) continue;
                    foreach (var mesh in part.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        ParticleSystem particles;
                        if (!_emitters.TryGetValue(mesh, out particles) || !particles)
                        {
                            particles = PremiumSparkleFactory.Create(mesh.transform, "Premium Sparkles", style % 3, tint);
                            if (!particles) continue;
                            var shape = particles.shape;
                            shape.enabled = true;
                            shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                            shape.skinnedMeshRenderer = mesh;
                            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
                            shape.normalOffset = 0.025f;
                            _emitters[mesh] = particles;
                        }
                        var main = particles.main;
                        main.startColor = tint;
                        _selected.Add(particles);
                        particles.Play();
                    }
                }
            }
        }

        public void Clear()
        {
            _selected.Clear();
            StopAll();
        }

        void StopAll()
        {
            foreach (var particles in _emitters.Values)
                if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnDisable() => StopAll();

        void OnEnable()
        {
            foreach (var particles in _selected)
                if (particles) particles.Play();
        }

        void OnDestroy()
        {
            foreach (var particles in _emitters.Values)
                if (particles) Destroy(particles.gameObject);
        }
    }

    /// <summary>Shared lightweight glint setup. No trails, lights, collisions or textures.</summary>
    public static class PremiumSparkleFactory
    {
        public static ParticleSystem Create(Transform parent, string name, int style, Color tint)
        {
            var material = Resources.Load<Material>("PremiumSparkle" + style);
            if (!material) return null;
            var go = new GameObject(name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = tint;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 16;
            var emission = particles.emission;
            emission.rateOverTime = 7f;
            var shape = particles.shape;
            shape.enabled = false;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }
    }
}
