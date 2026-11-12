using UnityEngine;
using UnityEngine.Rendering;

namespace SpeedStars.Physical
{
    /// <summary>Random voxel sweat particles jump from the crown, then fall under gravity. Player only.</summary>
    [DefaultExecutionOrder(1260)]
    [RequireComponent(typeof(RunnerBody))]
    public sealed class SweatFeedback : MonoBehaviour
    {
        RunnerBody _body;
        Transform _head;
        Camera _camera;
        GameObject _root;
        Texture2D _texture;
        Material _material;
        ParticleSystem _particles;
        bool _flowing, _hasPrevious;
        Vector3 _previous;
        public float SpeedMultiplier { get; private set; }
        public int ActiveDropCount { get; private set; }

        public static float MultiplierForEnergy(float energy01)
            => energy01 < 0.15f ? 2f : energy01 < 0.30f ? 1f : 0f;

        void Awake() => _body = GetComponent<RunnerBody>();
        void OnEnable() => _body.RunReset += Clear;
        void Start()
        {
            var material = Resources.Load<Material>("Sweat");
            if (!material) { enabled = false; return; }
            foreach (var bone in GetComponentsInChildren<Transform>(true))
                if (bone.name == "B-head") { _head = bone; break; }
            _texture = BuildDropTexture();
            _material = new Material(material);
            _material.mainTexture = _texture;
            _root = new GameObject("Sweat Droplets");
            _root.layer = gameObject.layer;
            _root.transform.SetParent(transform, false);
            _particles = _root.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.maxParticles = 12;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.62f, 0.78f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.088f, 0.12f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);
            main.gravityModifier = 0.36f;
            // Follow the running player's translation, but leave emitted drops independent of head animation.
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = transform;
            var emission = _particles.emission;
            emission.rateOverTime = 5.5f;
            var shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.055f;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.85f), new Keyframe(0.12f, 1f),
                new Keyframe(0.85f, 1f), new Keyframe(1f, 0.65f)));
            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0.0072f;
            renderer.maxParticleSize = 0.028f;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Clear();
        }

        void LateUpdate()
        {
            if (!_particles || _body.Sim == null) return;
            float multiplier = MultiplierForEnergy(_body.Sim.Energy01);
            Vector3 position = transform.position;
            bool teleport = _hasPrevious && Vector3.Distance(position, _previous) > Mathf.Max(2f, 30f * Time.deltaTime);
            _previous = position;
            _hasPrevious = true;
            if (teleport || multiplier == 0f) { Clear(); return; }
            SpeedMultiplier = multiplier;
            if (!_camera) _camera = Camera.main;
            Vector3 towardsCamera = _camera ? -_camera.transform.forward : -transform.forward;
            // The crown is the emitter, not four fixed positions beside the body.
            _root.transform.position = (_head ? _head.position : position + Vector3.up * 1.65f)
                + Vector3.up * 0.2f + towardsCamera * 0.3f;
            _root.transform.rotation = Quaternion.identity;
            var main = _particles.main;
            main.simulationSpeed = multiplier;
            if (!_flowing)
            {
                _flowing = true;
                _particles.Play();
                _particles.Emit(1);
            }
            ActiveDropCount = _particles.particleCount;
        }

        static Texture2D BuildDropTexture()
        {
            string[] rows = { "   bb   ", "   acb  ", "  bacb  ", " bacccb ",
                "bahccccb", "bahccccb", "bccccccb", " bddddb ", "  bbbb  " };
            var texture = new Texture2D(8, 9, TextureFormat.RGBA32, false);
            texture.name = "Single Voxel Sweat Drop";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 8; x++)
                {
                    char c = rows[y][x];
                    Color color = c == ' ' ? Color.clear :
                        c == 'b' ? new Color(0.015f, 0.2f, 0.28f) :
                        c == 'h' ? new Color(0.86f, 1f, 1f) :
                        c == 'a' ? new Color(0.4f, 0.9f, 1f) :
                        c == 'd' ? new Color(0f, 0.48f, 0.63f) : new Color(0f, 0.82f, 0.96f);
                    texture.SetPixel(x, 8 - y, color);
                }
            texture.Apply();
            return texture;
        }
        void Clear()
        {
            SpeedMultiplier = 0f; ActiveDropCount = 0;
            _flowing = false; _hasPrevious = false;
            if (_particles) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDisable() { _body.RunReset -= Clear; Clear(); }
        void OnDestroy()
        {
            if (_root) Destroy(_root);
            if (_material) Destroy(_material);
            if (_texture) Destroy(_texture);
        }
    }
}
