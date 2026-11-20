using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SpeedStars.Themes
{
    /// <summary>
    /// Puts a theme's <see cref="ThemeWeather"/> on the scene while that theme's world is open: sky backdrop, sun, ambient,
    /// the air over the whole world and the airborne layers. One component for every theme (the desert air used to be a
    /// script of its own); what a theme looks like is the profile's data. It lives on the world's root, so the menu's
    /// SetActive and the race's world switch turn it on and off by themselves.
    /// Play mode only, like the desert script it replaces: ambient and sky are scene settings, and writing them in the
    /// editor would dirty Home.unity on every theme the user clicks through.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThemeWeatherView : MonoBehaviour
    {
        [Tooltip("The theme's weather. Empty = the weather of the ThemedLoopWay definition on this object, so a generated race world needs no wiring.")]
        [SerializeField] private ThemeWeather weather;

        // The untouched scene look: captured once by whichever view runs first, restored by the one that holds it. Themes
        // enable and disable in any order, so a per-instance capture would hand the next theme the previous theme's values.
        private static ThemeWeatherView _holder;
        private static Camera _camera;
        private static CameraClearFlags _savedFlags;
        private static Color _savedBackground;
        private static AmbientMode _savedAmbientMode;
        private static Color _savedAmbientLight, _savedSky, _savedEquator, _savedGround;
        private static Light _sun;
        private static Color _savedSunColor;
        private static float _savedSunIntensity;

        private struct Layer
        {
            public Transform root;
            public Vector3 follow;
        }

        private readonly List<Layer> _layers = new List<Layer>();
        private GameObject _sky;

        /// <summary>The profile that is applied: the field, else the one on this world's theme definition.</summary>
        public ThemeWeather Weather
        {
            get
            {
                if (weather != null) return weather;
                ThemedLoopWay world = GetComponent<ThemedLoopWay>();
                return world != null && world.theme != null ? world.theme.weather : null;
            }
        }

        private void OnEnable() => Apply();

        private void LateUpdate()
        {
            if (_holder == null) Apply(); // Camera.main was not there yet when the world opened
            Follow();
        }

        private void OnDisable() => Restore();

        private void OnDestroy() => Restore();

        private void Apply()
        {
            ThemeWeather profile = Weather;
            if (!Application.isPlaying || profile == null || _holder == this) return;
            if (gameObject.scene != SceneManager.GetActiveScene()) return; // a world in another loaded scene is not the shown one
            Camera camera = Camera.main;
            if (camera == null) return;

            if (_holder == null) Capture(camera);
            _holder = this;

            if (_sun != null)
            {
                _sun.color = profile.sunColor;
                _sun.intensity = _savedSunIntensity * profile.sunIntensityScale;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = profile.ambientSky;
            RenderSettings.ambientEquatorColor = profile.ambientEquator;
            RenderSettings.ambientGroundColor = profile.ambientGround;

            if (profile.skyMaterial != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = profile.horizonFill;
                BuildSky(profile.skyMaterial);
            }

            BuildLayers(profile);

            // The generators set a haze per object as they build; the theme's air pulls all of them the same way.
            ThemeAtmosphere.SetAir(this, profile.Air);
            foreach (ThemeAtmosphere atmosphere in GetComponentsInChildren<ThemeAtmosphere>(true)) atmosphere.Apply();

            Follow();
        }

        private void Capture(Camera camera)
        {
            _camera = camera;
            _savedFlags = camera.clearFlags;
            _savedBackground = camera.backgroundColor;
            _savedAmbientMode = RenderSettings.ambientMode;
            _savedAmbientLight = RenderSettings.ambientLight;
            _savedSky = RenderSettings.ambientSkyColor;
            _savedEquator = RenderSettings.ambientEquatorColor;
            _savedGround = RenderSettings.ambientGroundColor;
            _sun = null;
            foreach (Light light in FindObjectsOfType<Light>())
            {
                if (light.type != LightType.Directional || light.gameObject.scene != gameObject.scene) continue;
                _sun = light;
                _savedSunColor = light.color;
                _savedSunIntensity = light.intensity;
                break;
            }
        }

        /// <summary>Full-screen quad at the far plane, so the sky needs no skybox material and no second camera.</summary>
        private void BuildSky(Material material)
        {
            if (_sky != null) return;
            _sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _sky.name = "Theme Sky Backdrop";
            _sky.hideFlags = HideFlags.HideAndDontSave;
            _sky.transform.SetParent(transform, false);
            Collider collider = _sky.GetComponent<Collider>();
            if (collider != null) Discard(collider);
            MeshRenderer renderer = _sky.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void BuildLayers(ThemeWeather profile)
        {
            if (_layers.Count > 0 || profile.layers == null) return;
            foreach (WeatherLayer layer in profile.layers)
            {
                if (layer == null || layer.material == null) continue;
                var go = new GameObject(string.IsNullOrEmpty(layer.name) ? "Weather Layer" : layer.name);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.transform.SetParent(transform, false);

                var particles = go.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particles.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(layer.lifetime.x, layer.lifetime.y);
                main.startSpeed = new ParticleSystem.MinMaxCurve(layer.speed.x, layer.speed.y);
                main.startSize = new ParticleSystem.MinMaxCurve(layer.size.x, layer.size.y);
                main.startColor = layer.tint;
                main.maxParticles = Mathf.Max(1, layer.maxParticles);
                main.gravityModifier = layer.gravity;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = true;

                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = layer.rate;

                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = layer.boxSize;
                Vector3 direction = layer.direction;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    Vector3 up = Mathf.Abs(direction.y) > 0.99f ? Vector3.forward : Vector3.up;
                    shape.rotation = Quaternion.LookRotation(direction, up).eulerAngles;
                }

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = layer.material;
                bool streak = layer.shape == WeatherParticleShape.Streak;
                renderer.renderMode = streak ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
                if (streak)
                {
                    renderer.lengthScale = layer.streakLength;
                    renderer.velocityScale = 0f;
                }
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                particles.Play();

                _layers.Add(new Layer { root = go.transform, follow = layer.follow });
            }
        }

        /// <summary>Sky and layers ride with the camera: x and y are world values, z keeps its distance to the camera.</summary>
        private void Follow()
        {
            if (_camera == null) return;
            Transform view = _camera.transform;
            if (_sky != null)
            {
                float distance = _camera.farClipPlane * .94f;
                float height = 2 * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * .5f) * distance;
                _sky.transform.SetPositionAndRotation(view.position + view.forward * distance, view.rotation);
                _sky.transform.localScale = new Vector3(height * _camera.aspect * 1.02f, height * 1.02f, 1);
            }
            foreach (Layer layer in _layers)
                if (layer.root != null)
                    layer.root.position = new Vector3(layer.follow.x, layer.follow.y, view.position.z + layer.follow.z);
        }

        private void Restore()
        {
            if (_holder == this)
            {
                if (_camera != null)
                {
                    _camera.clearFlags = _savedFlags;
                    _camera.backgroundColor = _savedBackground;
                }
                if (_sun != null)
                {
                    _sun.color = _savedSunColor;
                    _sun.intensity = _savedSunIntensity;
                }
                RenderSettings.ambientMode = _savedAmbientMode;
                RenderSettings.ambientLight = _savedAmbientLight;
                RenderSettings.ambientSkyColor = _savedSky;
                RenderSettings.ambientEquatorColor = _savedEquator;
                RenderSettings.ambientGroundColor = _savedGround;
                _holder = null;
                _camera = null;
                _sun = null;
            }
            ThemeAtmosphere.ClearAir(this);

            foreach (Layer layer in _layers)
                if (layer.root != null) Discard(layer.root.gameObject);
            _layers.Clear();
            if (_sky != null) Discard(_sky);
            _sky = null;
        }

        private static void Discard(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
