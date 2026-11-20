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

        [MenuItem("Speed Stars/Themes/Set Up Egypt Weather")]
        public static void Egypt() => Configure("Egypt", Themes + "LoopWay2.prefab", FillEgypt);

        [MenuItem("Speed Stars/Themes/Set Up Pirate Sea Weather")]
        public static void PirateSea() => Configure("PirateSea", Themes + "LoopWay4.prefab", FillPirateSea);

        /// <summary>Makes (or keeps) the theme's profile, points its definition at it and puts the view on its map.</summary>
        private static void Configure(string theme, string mapPath, Action<ThemeWeather, string> fill)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before writing prefabs.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Save and close Prefab Mode before writing the map assets.");

            string folder = Themes + theme + "/";
            string weatherPath = folder + "Weather.asset";
            ThemeWeather weather = AssetDatabase.LoadAssetAtPath<ThemeWeather>(weatherPath);
            bool created = weather == null;
            if (created)
            {
                weather = ScriptableObject.CreateInstance<ThemeWeather>();
                fill(weather, folder);
                AssetDatabase.CreateAsset(weather, weatherPath);
            }

            var definition = AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(folder + "Definition.asset")
                ?? throw new InvalidOperationException("Missing " + folder + "Definition.asset");
            if (definition.weather != weather)
            {
                var so = new SerializedObject(definition);
                so.FindProperty("weather").objectReferenceValue = weather;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }

            int removed = AttachView(mapPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"ThemeWeatherSetup: {theme} weather {(created ? "created" : "kept")} at {weatherPath}; " +
                      $"{System.IO.Path.GetFileNameWithoutExtension(mapPath)} carries ThemeWeatherView ({removed} old desert script(s) removed).");
        }

        /// <summary>Puts the single weather view on a map prefab's root and takes the per-theme desert script off it.</summary>
        private static int AttachView(string prefabPath)
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
                if (root.GetComponent<ThemeWeatherView>() == null) root.AddComponent<ThemeWeatherView>();
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
