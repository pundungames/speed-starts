using System;
using SpeedStars.Bridge;
using SpeedStars.Runner;
using SpeedStars.Themes;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// The theme side of the Home scene, which is a copy of Serhat's showcase scene (T-023): his camera, light and sky
    /// are kept; only his OnGUI picker is switched off and our <see cref="MenuThemeView"/> is added next to his
    /// <see cref="MainMenuThemeShowcase"/>. Edits are scene overrides on the copy; his scene file is never written.
    /// </summary>
    internal static class SceneHomeTheme
    {
        private const string Source = nameof(SceneHomeTheme);

        /// <summary>Scene-wide lookup by object name (inactive objects too); null when absent.</summary>
        public static Transform FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            }
            return null;
        }

        public static MainMenuThemeShowcase RequireShowcase(Scene scene)
        {
            MainMenuThemeShowcase found = null;
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var showcase in root.GetComponentsInChildren<MainMenuThemeShowcase>(true))
                {
                    if (found == null) found = showcase;
                    count++;
                }
            }
            if (found == null)
                throw new InvalidOperationException($"No MainMenuThemeShowcase in '{scene.path}'; the theme template changed.");
            if (count > 1) Warn(Source, $"{count} MainMenuThemeShowcase components in '{scene.path}'; the first is used.");
            return found;
        }

        /// <summary>The camera the Home scene is looked at with: the showcase's own preview camera, else the one tagged MainCamera.</summary>
        public static Camera HomeCamera(Scene scene)
        {
            Camera camera = RequireShowcase(scene).previewCamera;
            if (camera != null) return camera;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Camera c in root.GetComponentsInChildren<Camera>(true))
                    if (c.CompareTag("MainCamera")) return c;
            }
            throw new InvalidOperationException($"No camera in '{scene.path}'.");
        }

        /// <summary>
        /// Removes every <see cref="PastelBuildingRow"/> from the copy. Its OnValidate/delayCall rebuilds "Generated Buildings"
        /// with fresh fileIDs on each editor load, which dirties Home.unity with a ~140-object diff. The generated buildings
        /// stay; on prefab instances this is a removed-component override (Undo.DestroyObjectImmediate is the supported path).
        /// </summary>
        public static int RemoveBuildingGenerators(Scene scene)
        {
            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (var row in root.GetComponentsInChildren<PastelBuildingRow>(true))
                {
                    Undo.DestroyObjectImmediate(row);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Hides Serhat's IMGUI panel and adds the MenuThemeView that drives the showcase from the saved theme.</summary>
        public static MenuThemeView Prepare(Scene scene)
        {
            MainMenuThemeShowcase showcase = RequireShowcase(scene);
            if (showcase.themeDisplays == null || showcase.themeDisplays.Length == 0)
                throw new InvalidOperationException("MainMenuThemeShowcase has no theme displays; nothing to switch between.");

            var showcaseSo = new SerializedObject(showcase);
            Prop(showcaseSo, "showSelection").boolValue = false; // our environment button switches themes, not his panel
            Apply(showcaseSo);

            var view = showcase.GetComponent<MenuThemeView>() ?? AddRequired<MenuThemeView>(showcase.gameObject);
            var viewSo = new SerializedObject(view);
            Prop(viewSo, "showcase").objectReferenceValue = showcase;
            Transform platform = FindByName(scene, SceneHomeCharacter.PlatformName)
                ?? throw new InvalidOperationException($"'{SceneHomeCharacter.PlatformName}' not found in '{scene.path}'.");
            Prop(viewSo, "characterPlatform").objectReferenceValue = platform.gameObject;
            Apply(viewSo);
            int rows = RemoveBuildingGenerators(scene);
            Log(Source, $"Home: removed {rows} PastelBuildingRow generator(s) (editor-load rebuild churn).");
            Log(Source, $"Home: showcase panel off, MenuThemeView on '{showcase.name}' ({showcase.themeDisplays.Length} theme displays).");
            return view;
        }
    }
}
