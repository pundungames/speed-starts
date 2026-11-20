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
    /// a theme is data in <c>Weather.asset</c> instead of a script per theme. Egypt is the first theme: its numbers are the
    /// ones the old desert script carried, with the dust read stronger and a second, ground-level sand layer (the
    /// "yellow, dusty, hot" feedback). An existing Weather.asset is the user's tuning and is never overwritten; only the
    /// references are rewired. Run it again after a theme's prefab was rebuilt.
    /// </summary>
    public static class ThemeWeatherSetup
    {
        private const string EgyptFolder = "Assets/Speed-Staes/Prefabs/Themes/Egypt/";
        private const string EgyptWeather = EgyptFolder + "Weather.asset";
        private const string EgyptDefinition = EgyptFolder + "Definition.asset";
        private const string EgyptMap = "Assets/Speed-Staes/Prefabs/Themes/LoopWay2.prefab";

        [MenuItem("Speed Stars/Themes/Set Up Egypt Weather")]
        public static void Egypt()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Stop Play mode before writing prefabs.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Save and close Prefab Mode before writing the map assets.");

            ThemeWeather weather = AssetDatabase.LoadAssetAtPath<ThemeWeather>(EgyptWeather);
            bool created = weather == null;
            if (created)
            {
                weather = ScriptableObject.CreateInstance<ThemeWeather>();
                FillEgypt(weather);
                AssetDatabase.CreateAsset(weather, EgyptWeather);
            }

            var definition = AssetDatabase.LoadAssetAtPath<LoopThemeDefinition>(EgyptDefinition)
                ?? throw new InvalidOperationException("Missing " + EgyptDefinition);
            if (definition.weather != weather)
            {
                var so = new SerializedObject(definition);
                so.FindProperty("weather").objectReferenceValue = weather;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }

            int removed = AttachView(EgyptMap);
            AssetDatabase.SaveAssets();
            Debug.Log($"ThemeWeatherSetup: Egypt weather {(created ? "created" : "kept")} at {EgyptWeather}; " +
                      $"LoopWay2 carries ThemeWeatherView ({removed} old desert script(s) removed).");
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
        /// dust layers - slow golden motes hanging in the light and a faster sand sweep just over the ground.
        /// </summary>
        private static void FillEgypt(ThemeWeather weather)
        {
            const string materials = EgyptFolder + "Materials/";
            Material sky = Require<Material>(materials + "Desert Sun Sky.mat");
            Material dust = Require<Material>(materials + "Desert Airborne Dust.mat");

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
                    shape = WeatherParticleShape.Mote,
                    tint = new Color(1f, 0.72f, 0.34f, 0.10f),
                    size = new Vector2(1.0f, 3.4f),
                    lifetime = new Vector2(8f, 13f),
                    speed = new Vector2(0.15f, 0.45f),
                    direction = new Vector3(0f, 0f, 1f),
                    rate = 12f,
                    maxParticles = 150,
                    boxSize = new Vector3(75f, 3.4f, 125f),
                    follow = new Vector3(-30f, 1.5f, 22f)
                },
                new WeatherLayer
                {
                    name = "Sand Sweeping Over The Ground",
                    material = dust,
                    shape = WeatherParticleShape.Mote,
                    tint = new Color(1f, 0.80f, 0.46f, 0.11f),
                    size = new Vector2(0.5f, 1.5f),
                    lifetime = new Vector2(2.5f, 4.5f),
                    speed = new Vector2(2.4f, 4.0f),
                    direction = new Vector3(0f, 0.06f, 1f),
                    rate = 26f,
                    maxParticles = 120,
                    boxSize = new Vector3(60f, 1.1f, 90f),
                    follow = new Vector3(-26f, 0.55f, 18f)
                }
            };
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing " + path);
        }
    }
}
#endif
