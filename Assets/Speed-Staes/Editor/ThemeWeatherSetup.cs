#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SpeedStars.Themes;

namespace SpeedStars.Editor
{
    /// <summary>
    /// Writes a theme's weather profile and hangs the one <see cref="ThemeWeatherView"/> on its map, so the atmosphere of
    /// a theme is data in <c>Weather.asset</c> instead of a script per theme. An existing Weather.asset is the user's
    /// tuning and is never overwritten; only the missing pieces (materials, references, the view) are made. Run a theme
    /// again after its map prefab was rebuilt.
    /// </summary>
    public static class ThemeWeatherSetup
    {
        private const string Themes = "Assets/Speed-Staes/Prefabs/Themes/";

        // The standard city is Serhat's own map: a LoopWayGenerator with no theme definition, so its weather is named
        // on the view itself instead of coming from a definition.
        private const string StandardCityMap = "Assets/Kevin Iglesias/Human Character Dummy/Models/LoopWay.prefab";

        [MenuItem("Speed Stars/Themes/Set Up Egypt Weather")]
        public static void Egypt() => Configure("Egypt", Themes + "LoopWay2.prefab", FillEgypt);

        [MenuItem("Speed Stars/Themes/Set Up Pirate Sea Weather")]
        public static void PirateSea() => Configure("PirateSea", Themes + "LoopWay4.prefab", FillPirateSea);

        [MenuItem("Speed Stars/Themes/Set Up Rome Weather")]
        public static void Rome() => Configure("RomeByzantium", Themes + "LoopWay3.prefab", FillRome);

        [MenuItem("Speed Stars/Themes/Set Up Floating Sky Weather")]
        public static void FloatingSky() => Configure("FloatingSky", Themes + "LoopWay5.prefab", FillFloatingSky);

        [MenuItem("Speed Stars/Themes/Set Up Standard City Weather")]
        public static void StandardCity() => Configure("StandardCity", StandardCityMap, FillStandardCity, hasDefinition: false);

        /// <summary>Makes (or keeps) the theme's profile, points its definition at it and puts the view on its map.</summary>
        private static void Configure(string theme, string mapPath, Action<ThemeWeather, string> fill, bool hasDefinition = true)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before writing prefabs.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Save and close Prefab Mode before writing the map assets.");

            string folder = Themes + theme + "/";
            if (!AssetDatabase.IsValidFolder(folder.TrimEnd('/'))) AssetDatabase.CreateFolder(Themes.TrimEnd('/'), theme);
            if (!AssetDatabase.IsValidFolder(folder + "Materials")) AssetDatabase.CreateFolder(folder.TrimEnd('/'), "Materials");
            string weatherPath = folder + "Weather.asset";
            ThemeWeather weather = AssetDatabase.LoadAssetAtPath<ThemeWeather>(weatherPath);
            bool created = weather == null;
            if (created)
            {
                weather = ScriptableObject.CreateInstance<ThemeWeather>();
                fill(weather, folder);
                AssetDatabase.CreateAsset(weather, weatherPath);
            }

            if (hasDefinition)
            {
                var definition = AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(folder + "Definition.asset")
                    ?? throw new InvalidOperationException("Missing " + folder + "Definition.asset");
                if (definition.weather != weather)
                {
                    var so = new SerializedObject(definition);
                    so.FindProperty("weather").objectReferenceValue = weather;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(definition);
                }
            }

            int removed = AttachView(mapPath, hasDefinition ? null : weather);
            AssetDatabase.SaveAssets();
            Debug.Log($"ThemeWeatherSetup: {theme} weather {(created ? "created" : "kept")} at {weatherPath}; " +
                      $"{System.IO.Path.GetFileNameWithoutExtension(mapPath)} carries ThemeWeatherView ({removed} old desert script(s) removed).");
        }

        /// <summary>Puts the single weather view on a map prefab's root and takes the per-theme desert script off it.</summary>
        private static int AttachView(string prefabPath, ThemeWeather named)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int removed = 0;
                foreach (EgyptDesertAtmosphere old in root.GetComponentsInChildren<EgyptDesertAtmosphere>(true))
                {
                    UnityEngine.Object.DestroyImmediate(old, true);
                    removed++;
                }
                // The view takes the profile from the ThemedLoopWay's definition, so the map keeps one source of truth.
                // A map without a definition (the standard city) carries the profile on the view instead.
                ThemeWeatherView view = root.GetComponent<ThemeWeatherView>() ?? root.AddComponent<ThemeWeatherView>();
                if (named != null)
                {
                    var viewSo = new SerializedObject(view);
                    viewSo.FindProperty("weather").objectReferenceValue = named;
                    viewSo.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return removed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Egypt: the sky, sun and ambient of the old desert script, plus air that closes in sooner and warmer, and two
        /// dust layers - golden motes hanging in the light and a faster sand sweep just over the ground.
        /// </summary>
        private static void FillEgypt(ThemeWeather weather, string folder)
        {
            Material sky = Require<Material>(folder + "Materials/Desert Sun Sky.mat");
            Material dust = Require<Material>(folder + "Materials/Desert Airborne Dust.mat");

            weather.skyMaterial = sky;
            weather.horizonFill = new Color(0.96f, 0.65f, 0.30f);
            weather.sunColor = new Color(1f, 0.82f, 0.56f);
            weather.sunIntensityScale = 0.96f;
            weather.ambientSky = new Color(0.87f, 0.70f, 0.46f);
            weather.ambientEquator = new Color(0.64f, 0.49f, 0.31f);
            weather.ambientGround = new Color(0.36f, 0.27f, 0.18f);
            weather.airTint = new Color(1f, 0.84f, 0.62f);
            weather.airTintBlend = 0.55f;
            weather.worldTint = new Color(1f, 0.88f, 0.70f);
            weather.worldTintBlend = 0.12f;
            weather.airStrengthBoost = 0.12f;
            weather.airStartScale = 0.8f;
            weather.airEndScale = 0.85f;
            weather.layers = new[]
            {
                new WeatherLayer
                {
                    name = "Hanging Golden Desert Dust",
                    material = dust,
                    tint = new Color(1f, 0.74f, 0.38f, 0.16f),
                    size = new Vector2(0.12f, 0.45f),
                    lifetime = new Vector2(8f, 13f),
                    speed = new Vector2(0.15f, 0.45f),
                    direction = new Vector3(0f, 0f, 1f),
                    rate = 90f,
                    maxParticles = 600,
                    boxSize = new Vector3(26f, 5f, 40f),
                    follow = new Vector3(-7f, 2f, 5f)
                },
                new WeatherLayer
                {
                    name = "Sand Sweeping Over The Ground",
                    material = dust,
                    tint = new Color(1f, 0.82f, 0.50f, 0.18f),
                    size = new Vector2(0.10f, 0.35f),
                    lifetime = new Vector2(2.5f, 4.5f),
                    speed = new Vector2(2.4f, 4f),
                    direction = new Vector3(0f, 0.06f, 1f),
                    rate = 120f,
                    maxParticles = 500,
                    boxSize = new Vector3(22f, 1.6f, 34f),
                    follow = new Vector3(-6f, 0.7f, 3f)
                }
            };
        }

        /// <summary>
        /// Pirate Sea: an overcast storm. No sun disk, only a pale break in the cloud where the light sits; a dim cool
        /// sun, cold ambient and air that closes in much sooner, then rain slanting through it with sea mist behind.
        /// </summary>
        private static void FillPirateSea(ThemeWeather weather, string folder)
        {
            string materials = folder + "Materials/";
            Material sky = EnsureMaterial(materials + "Storm Sky.mat", "Speed Stars/Theme Sky", m =>
            {
                m.SetColor("_Horizon", new Color(0.47f, 0.53f, 0.58f));
                m.SetColor("_Zenith", new Color(0.16f, 0.21f, 0.29f));
                m.SetFloat("_Break", 0.05f);
                m.SetColor("_GlowColor", new Color(0.72f, 0.78f, 0.84f));
                m.SetVector("_SunPos", new Vector4(0.72f, 0.80f, 0f, 0f));
                m.SetFloat("_SunSize", 0f);
                m.SetFloat("_GlowStrength", 0.30f);
                m.SetFloat("_GlowSpread", 14f);
            });
            Material rain = EnsureMaterial(materials + "Sea Rain.mat", "Speed Stars/Weather Streak", m =>
            {
                m.SetColor("_Color", new Color(0.80f, 0.88f, 0.96f));
                m.SetFloat("_Softness", 0.45f);
            });
            Material mist = EnsureMaterial(materials + "Sea Mist.mat", "Speed Stars/Desert Dust", m =>
                m.SetColor("_Color", Color.white));

            weather.skyMaterial = sky;
            weather.horizonFill = new Color(0.30f, 0.36f, 0.42f);
            weather.sunColor = new Color(0.62f, 0.70f, 0.80f);
            weather.sunIntensityScale = 0.55f;
            weather.ambientSky = new Color(0.36f, 0.42f, 0.50f);
            weather.ambientEquator = new Color(0.26f, 0.31f, 0.37f);
            weather.ambientGround = new Color(0.15f, 0.18f, 0.22f);
            weather.airTint = new Color(0.48f, 0.56f, 0.62f);
            weather.airTintBlend = 0.70f;
            weather.worldTint = new Color(0.38f, 0.46f, 0.55f);
            weather.worldTintBlend = 0.85f;
            weather.airStrengthBoost = 0.20f;
            weather.airStartScale = 0.60f;
            weather.airEndScale = 0.65f;
            weather.layers = new[]
            {
                new WeatherLayer
                {
                    name = "Rain",
                    material = rain,
                    shape = WeatherParticleShape.Streak,
                    tint = new Color(0.80f, 0.88f, 0.96f, 0.45f),
                    size = new Vector2(0.05f, 0.11f),
                    lifetime = new Vector2(0.7f, 1.2f),
                    speed = new Vector2(14f, 20f),
                    direction = new Vector3(0.14f, -1f, 0.08f),
                    gravity = 0.3f,
                    rate = 420f,
                    maxParticles = 900,
                    boxSize = new Vector3(26f, 9f, 40f),
                    follow = new Vector3(-7f, 5.5f, 5f),
                    streakLength = 3.5f
                },
                new WeatherLayer
                {
                    name = "Sea Mist Off The Water",
                    material = mist,
                    tint = new Color(0.72f, 0.80f, 0.86f, 0.13f),
                    size = new Vector2(0.6f, 2.2f),
                    lifetime = new Vector2(4f, 7f),
                    speed = new Vector2(1.2f, 2.6f),
                    direction = new Vector3(0f, 0.03f, 1f),
                    rate = 40f,
                    maxParticles = 260,
                    boxSize = new Vector3(24f, 4f, 38f),
                    follow = new Vector3(-6f, 1.6f, 4f)
                }
            };
        }

        /// <summary>
        /// Rome: the last hour of light. The sun sits low and large in a gold sky that goes violet overhead, the light
        /// falls warm and long, and the evening air carries leaves tumbling past with fine motes hanging in the glow.
        /// </summary>
        private static void FillRome(ThemeWeather weather, string folder)
        {
            string materials = folder + "Materials/";
            Material sky = EnsureMaterial(materials + "Sunset Sky.mat", "Speed Stars/Theme Sky", m =>
            {
                m.SetColor("_Horizon", new Color(0.99f, 0.74f, 0.45f));
                m.SetColor("_Zenith", new Color(0.33f, 0.33f, 0.53f));
                m.SetFloat("_Break", 0.20f);
                m.SetColor("_SunColor", new Color(1f, 0.94f, 0.73f));
                m.SetColor("_GlowColor", new Color(1f, 0.70f, 0.40f));
                m.SetVector("_SunPos", new Vector4(0.70f, 0.34f, 0f, 0f));
                m.SetFloat("_SunSize", 0.055f);
                m.SetFloat("_GlowStrength", 0.55f);
                m.SetFloat("_GlowSpread", 12f);
            });
            Material leaves = EnsureMaterial(materials + "Autumn Leaves.mat", "Speed Stars/Weather Leaf", m =>
            {
                m.SetColor("_Color", new Color(0.86f, 0.50f, 0.20f));
                m.SetColor("_Vein", new Color(1f, 0.80f, 0.50f));
                m.SetFloat("_VeinStrength", 0.45f);
                m.SetFloat("_Width", 0.55f);
                m.SetFloat("_Edge", 0.12f);
            });
            Material motes = EnsureMaterial(materials + "Evening Motes.mat", "Speed Stars/Desert Dust", m =>
                m.SetColor("_Color", Color.white));

            weather.skyMaterial = sky;
            weather.horizonFill = new Color(0.99f, 0.74f, 0.45f);
            weather.sunColor = new Color(1f, 0.78f, 0.55f);
            weather.sunIntensityScale = 0.85f;
            weather.ambientSky = new Color(0.78f, 0.66f, 0.60f);
            weather.ambientEquator = new Color(0.54f, 0.45f, 0.45f);
            weather.ambientGround = new Color(0.27f, 0.22f, 0.24f);
            weather.airTint = new Color(0.99f, 0.78f, 0.58f);
            weather.airTintBlend = 0.50f;
            weather.worldTint = new Color(1f, 0.78f, 0.56f);
            weather.worldTintBlend = 0.55f;
            weather.airStrengthBoost = 0.08f;
            weather.airStartScale = 0.85f;
            weather.airEndScale = 0.90f;
            weather.layers = new[]
            {
                new WeatherLayer
                {
                    name = "Leaves On The Evening Wind",
                    material = leaves,
                    tint = new Color(0.90f, 0.54f, 0.24f, 0.95f),
                    size = new Vector2(0.14f, 0.34f),
                    lifetime = new Vector2(5f, 9f),
                    speed = new Vector2(1.6f, 3.4f),
                    direction = new Vector3(0.1f, -0.18f, 1f),
                    gravity = 0.04f,
                    rate = 14f,
                    maxParticles = 140,
                    boxSize = new Vector3(26f, 6f, 40f),
                    follow = new Vector3(-7f, 2.6f, 5f),
                    spin = new Vector2(-110f, 110f),
                    turbulence = 0.9f
                },
                new WeatherLayer
                {
                    name = "Motes In The Low Sun",
                    material = motes,
                    tint = new Color(1f, 0.84f, 0.62f, 0.14f),
                    size = new Vector2(0.10f, 0.38f),
                    lifetime = new Vector2(7f, 12f),
                    speed = new Vector2(0.2f, 0.6f),
                    direction = new Vector3(0f, 0.05f, 1f),
                    rate = 70f,
                    maxParticles = 480,
                    boxSize = new Vector3(24f, 4.5f, 38f),
                    follow = new Vector3(-6.5f, 1.8f, 4f)
                }
            };
        }

        /// <summary>
        /// Floating Sky: dawn above the cloud line. Gold at the horizon, a pink band over it, deep lilac overhead, and
        /// the sun low and warm behind the islands. The cloud sea drifts past below the camera, gold dust hangs in the
        /// light and petals turn on the wind. The premium theme, so it is the one that looks least like a day outside.
        /// </summary>
        private static void FillFloatingSky(ThemeWeather weather, string folder)
        {
            string materials = folder + "Materials/";
            Material sky = EnsureMaterial(materials + "Dawn Sky.mat", "Speed Stars/Theme Sky", m =>
            {
                // The clouds and the road take the lower two thirds of the frame, so the gold has to live high to be
                // seen at all: it fills the sky up to the break, pink sits in a band above it and lilac only at the top.
                m.SetColor("_Horizon", new Color(1f, 0.82f, 0.40f));
                m.SetColor("_Mid", new Color(1f, 0.68f, 0.74f));
                m.SetFloat("_MidHeight", 0.90f);
                m.SetFloat("_MidWidth", 0.22f);
                m.SetFloat("_MidBlend", 0.90f);
                m.SetColor("_Zenith", new Color(0.52f, 0.44f, 0.70f));
                m.SetFloat("_Break", 0.78f);
                m.SetColor("_SunColor", new Color(1f, 0.95f, 0.80f));
                m.SetColor("_GlowColor", new Color(1f, 0.82f, 0.45f));
                m.SetVector("_SunPos", new Vector4(0.20f, 0.56f, 0f, 0f));
                m.SetFloat("_SunSize", 0.055f);
                m.SetFloat("_GlowStrength", 0.70f);
                m.SetFloat("_GlowSpread", 6f);
            });
            Material motes = EnsureMaterial(materials + "Sky Motes.mat", "Speed Stars/Desert Dust", m =>
                m.SetColor("_Color", Color.white));
            Material petals = EnsureMaterial(materials + "Sky Petals.mat", "Speed Stars/Weather Leaf", m =>
            {
                m.SetColor("_Color", new Color(1f, 0.72f, 0.80f));
                m.SetColor("_Vein", new Color(1f, 0.90f, 0.84f));
                m.SetFloat("_VeinStrength", 0.35f);
                m.SetFloat("_Width", 0.50f);
                m.SetFloat("_Edge", 0.14f);
            });

            weather.skyMaterial = sky;
            weather.horizonFill = new Color(1f, 0.84f, 0.58f);
            weather.sunColor = new Color(1f, 0.80f, 0.66f);
            weather.sunIntensityScale = 0.78f;
            weather.ambientSky = new Color(0.86f, 0.68f, 0.70f);
            weather.ambientEquator = new Color(0.62f, 0.50f, 0.56f);
            weather.ambientGround = new Color(0.30f, 0.26f, 0.34f);
            weather.airTint = new Color(1f, 0.80f, 0.78f);
            weather.airTintBlend = 0.60f;
            weather.airStrengthBoost = 0.10f;
            weather.airStartScale = 0.75f;
            weather.airEndScale = 0.85f;
            weather.worldTint = new Color(1f, 0.80f, 0.70f);
            weather.worldTintBlend = 0.62f;
            weather.layers = new[]
            {
                new WeatherLayer
                {
                    name = "Cloud Sea Drifting Below",
                    material = motes,
                    tint = new Color(1f, 0.86f, 0.86f, 0.07f),
                    size = new Vector2(3f, 8f),
                    lifetime = new Vector2(12f, 18f),
                    speed = new Vector2(0.3f, 0.8f),
                    direction = new Vector3(0f, 0f, 1f),
                    rate = 10f,
                    maxParticles = 120,
                    boxSize = new Vector3(34f, 2f, 46f),
                    follow = new Vector3(-8f, -1.4f, 6f)
                },
                new WeatherLayer
                {
                    name = "Gold Dust In The Dawn",
                    material = motes,
                    tint = new Color(1f, 0.82f, 0.56f, 0.15f),
                    size = new Vector2(0.10f, 0.40f),
                    lifetime = new Vector2(8f, 13f),
                    speed = new Vector2(0.2f, 0.6f),
                    direction = new Vector3(0f, 0.08f, 1f),
                    rate = 70f,
                    maxParticles = 450,
                    boxSize = new Vector3(26f, 5f, 40f),
                    follow = new Vector3(-7f, 2.2f, 5f)
                },
                new WeatherLayer
                {
                    name = "Petals On The High Wind",
                    material = petals,
                    tint = new Color(1f, 0.74f, 0.82f, 0.95f),
                    size = new Vector2(0.10f, 0.26f),
                    lifetime = new Vector2(5f, 9f),
                    speed = new Vector2(1.4f, 3f),
                    direction = new Vector3(0.08f, -0.1f, 1f),
                    gravity = 0.02f,
                    rate = 24f,
                    maxParticles = 130,
                    boxSize = new Vector3(26f, 6f, 40f),
                    follow = new Vector3(-7f, 2.8f, 5f),
                    spin = new Vector2(-130f, 130f),
                    turbulence = 1.1f
                }
            };
        }

        /// <summary>
        /// Standard City: the clear day the other themes are judged against, so its light and sky are left exactly as
        /// the scene has them - no sky material, the sun and ambient it already had. What it gains is wind: green
        /// leaves torn off the street trees and fine street dust running along the road.
        /// </summary>
        private static void FillStandardCity(ThemeWeather weather, string folder)
        {
            string materials = folder + "Materials/";
            Material leaves = EnsureMaterial(materials + "City Leaves.mat", "Speed Stars/Weather Leaf", m =>
            {
                m.SetColor("_Color", new Color(0.44f, 0.68f, 0.26f));
                m.SetColor("_Vein", new Color(0.72f, 0.86f, 0.42f));
                m.SetFloat("_VeinStrength", 0.40f);
                m.SetFloat("_Width", 0.52f);
                m.SetFloat("_Edge", 0.12f);
            });
            Material motes = EnsureMaterial(materials + "City Motes.mat", "Speed Stars/Desert Dust", m =>
                m.SetColor("_Color", Color.white));

            // No sky material: the camera keeps its own blue sky, which is the look every other theme departs from.
            weather.skyMaterial = null;
            weather.sunColor = new Color(1f, 0.97f, 0.91f);
            weather.sunIntensityScale = 1f;
            weather.ambientSky = new Color(0.52f, 0.56f, 0.62f);
            weather.ambientEquator = new Color(0.40f, 0.43f, 0.48f);
            weather.ambientGround = new Color(0.26f, 0.27f, 0.30f);
            weather.airTint = Color.white;
            weather.airTintBlend = 0f;
            weather.airStrengthBoost = 0f;
            weather.airStartScale = 1f;
            weather.airEndScale = 1f;
            weather.worldTint = Color.white;
            weather.worldTintBlend = 0f;
            weather.layers = new[]
            {
                new WeatherLayer
                {
                    name = "Leaves On The City Wind",
                    material = leaves,
                    tint = new Color(0.50f, 0.72f, 0.28f, 0.95f),
                    size = new Vector2(0.12f, 0.30f),
                    lifetime = new Vector2(4f, 8f),
                    speed = new Vector2(2.4f, 4.6f),
                    direction = new Vector3(0.12f, -0.1f, 1f),
                    gravity = 0.05f,
                    rate = 26f,
                    maxParticles = 180,
                    boxSize = new Vector3(26f, 6f, 40f),
                    follow = new Vector3(-7f, 2.6f, 5f),
                    spin = new Vector2(-150f, 150f),
                    turbulence = 1.4f
                },
                new WeatherLayer
                {
                    name = "Street Dust On The Gust",
                    material = motes,
                    tint = new Color(0.92f, 0.94f, 0.96f, 0.07f),
                    size = new Vector2(0.08f, 0.30f),
                    lifetime = new Vector2(3f, 6f),
                    speed = new Vector2(1.6f, 3.2f),
                    direction = new Vector3(0f, 0.04f, 1f),
                    rate = 40f,
                    maxParticles = 260,
                    boxSize = new Vector3(24f, 2.4f, 38f),
                    follow = new Vector3(-6.5f, 1f, 4f)
                }
            };
        }

        /// <summary>Loads a material, or makes it from a shader the first time. Never rewrites one the user has tuned.</summary>
        private static Material EnsureMaterial(string path, string shaderName, Action<Material> fill)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find(shaderName) ?? throw new InvalidOperationException("Missing shader " + shaderName);
            material = new Material(shader);
            fill(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing " + path);
        }
    }
}
#endif
