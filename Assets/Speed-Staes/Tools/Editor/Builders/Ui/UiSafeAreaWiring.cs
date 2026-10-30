using SpeedStars.UI.Framework;
using UnityEditor;
using UnityEngine;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Adds a SafeAreaFitter to a screen / popup / widget instance placed in a scene (T-051). It lives on the scene's prefab
    /// instance as an added component, so the screen prefabs themselves stay untouched and the user can still move or remove
    /// the fitter in the Inspector. A stretched root fits its children (backgrounds and dims stay full-bleed); any other root
    /// is an edge widget and fits itself.
    /// </summary>
    internal static class UiSafeAreaWiring
    {
        /// <summary>Flying / pooled widgets that are positioned in world space by code and must not be re-anchored.</summary>
        private static readonly string[] Excluded = { "RewardFlight", "RedDot" };

        public static void Attach(GameObject instance, GameObject prefab)
        {
            if (instance == null || !(instance.transform is RectTransform rect)) return;
            if (instance.GetComponent<SafeAreaFitter>() != null) return;
            if (IsExcluded(prefab != null ? prefab.name : instance.name)) return;

            SafeAreaMode mode = SafeAreaMath.IsFullStretch(rect.anchorMin, rect.anchorMax) ? SafeAreaMode.Children : SafeAreaMode.Self;
            var fitter = AddRequired<SafeAreaFitter>(instance);
            var so = new SerializedObject(fitter);
            Prop(so, "mode").enumValueIndex = (int)mode;
            Prop(so, "includeVertical").boolValue = false;
            Apply(so);
        }

        private static bool IsExcluded(string name)
        {
            foreach (string excluded in Excluded)
                if (name == excluded) return true;
            return false;
        }
    }
}
