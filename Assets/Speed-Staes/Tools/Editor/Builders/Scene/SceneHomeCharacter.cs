using System;
using System.Linq;
using System.Reflection;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Live character of the Home scene: a prefab INSTANCE of Malik's runner (the link stays, the prefab file is never
    /// written) standing where Serhat's template scene has its unpacked copy of the model, on the character platform.
    /// Camera, light, skybox and plinth are Serhat's and stay as they are. The runner's input / debug tools are
    /// switched off as scene overrides and SpeedStars.Bridge.MenuCharacter is added and pointed at the runner root.
    /// MenuCharacter is looked up by type name, so a missing type or field is a clear error instead of an empty slot.
    /// </summary>
    internal static class SceneHomeCharacter
    {
        private const string Source = nameof(SceneHomeCharacter);

        public const string RunnerPrefabPath = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const string MenuCharacterType = "SpeedStars.Bridge.MenuCharacter";
        private const string MenuIdlePoseType = "SpeedStars.Bridge.MenuIdlePose";

        // Names in Serhat's template scene (Anamenu Themalari). The builder stops with a message when one is gone.
        public const string PlatformName = "Foreground Character Platform";
        public const string TemplateCharacterName = "Main Menu Character - Existing Model";

        /// <summary>
        /// Replaces the template's unpacked model with a runner prefab instance: same parent, same sibling slot, same
        /// local position / rotation / scale. Returns the new runner root.
        /// </summary>
        public static GameObject ReplaceTemplateCharacter(Scene scene)
        {
            var prefab = LoadRequired<GameObject>(RunnerPrefabPath, "Malik's character prefab");
            Type menuCharacter = FindType(MenuCharacterType)
                ?? throw new InvalidOperationException($"{MenuCharacterType} not found (Scripts/Bridge/MenuCharacter.cs).");

            Transform platform = SceneHomeTheme.FindByName(scene, PlatformName)
                ?? throw new InvalidOperationException($"'{PlatformName}' not found in '{scene.path}'; the theme template changed. Update SceneHomeCharacter.");
            Transform old = platform.Find(TemplateCharacterName)
                ?? throw new InvalidOperationException($"'{TemplateCharacterName}' not found under '{PlatformName}' in '{scene.path}'; the theme template changed. Update SceneHomeCharacter.");
            if (PrefabUtility.IsPartOfPrefabInstance(old.gameObject))
                throw new InvalidOperationException($"'{TemplateCharacterName}' is a prefab instance in the template; it is expected to be unpacked.");

            Vector3 localPosition = old.localPosition;
            Quaternion localRotation = old.localRotation;
            Vector3 localScale = old.localScale;
            int siblingIndex = old.GetSiblingIndex();
            UnityEngine.Object.DestroyImmediate(old.gameObject);

            var runner = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (runner == null) throw new InvalidOperationException($"Could not instantiate '{RunnerPrefabPath}'.");
            runner.name = prefab.name;
            runner.transform.SetParent(platform, false);
            runner.transform.localPosition = localPosition;
            runner.transform.localRotation = localRotation;
            runner.transform.localScale = localScale;
            runner.transform.SetSiblingIndex(siblingIndex);

            SwitchOff(runner);

            var component = runner.GetComponent(menuCharacter) ?? runner.AddComponent(menuCharacter);
            if (component == null) throw new InvalidOperationException($"Could not add {MenuCharacterType} to '{runner.name}'.");
            var so = new SerializedObject(component);
            SerializedProperty target = FindTargetField(so, menuCharacter);
            if (target.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException($"{MenuCharacterType}.{target.name} is not an object reference.");
            target.objectReferenceValue = ResolveTarget(menuCharacter, target.name, runner);
            Apply(so);

            // The idle pose is added here, not at runtime, so its angles are editable in the Inspector before Play.
            Type idlePose = FindType(MenuIdlePoseType);
            if (idlePose != null && runner.GetComponent(idlePose) == null) runner.AddComponent(idlePose);

            Log(Source, $"Home: '{runner.name}' prefab instance under '{PlatformName}' (local {localPosition}, replaces '{TemplateCharacterName}'), {menuCharacter.Name}.{target.name} -> runner root.");
            return runner;
        }

        /// <summary>Input and debug tools off as scene overrides; the prefab file is untouched.</summary>
        private static void SwitchOff(GameObject runner)
        {
            var tools = new System.Collections.Generic.List<Behaviour>();
            tools.AddRange(runner.GetComponentsInChildren<RunnerInput>(true));
            tools.AddRange(runner.GetComponentsInChildren<RunnerBodyHUD>(true));
            tools.AddRange(runner.GetComponentsInChildren<RunnerReplay>(true));
            if (tools.Count == 0) Warn(Source, "No RunnerInput / RunnerBodyHUD / RunnerReplay on the runner prefab; nothing to switch off.");
            foreach (Behaviour tool in tools)
            {
                var so = new SerializedObject(tool);
                Prop(so, "m_Enabled").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                Log(Source, $"Home: {tool.GetType().Name} on '{tool.gameObject.name}' switched off (scene override).");
            }
            // RunnerBody / RunnerPoseDriver are left as the prefab has them: MenuCharacter decides what to do with them.
        }

        private static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

        /// <summary>
        /// The "runner root" field of MenuCharacter: a few likely names first, then the only serialized GameObject /
        /// Component reference it has. Anything else is an error that names the fields found.
        /// </summary>
        private static SerializedProperty FindTargetField(SerializedObject so, Type type)
        {
            foreach (string name in new[] { "target", "runner", "runnerRoot", "character", "root" })
            {
                var p = so.FindProperty(name);
                if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) return p;
            }
            var candidates = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                            && (typeof(GameObject).IsAssignableFrom(f.FieldType) || typeof(Component).IsAssignableFrom(f.FieldType)))
                .ToList();
            if (candidates.Count == 1) return Prop(so, candidates[0].Name);
            throw new InvalidOperationException(
                $"{type.FullName}: cannot tell which field takes the runner root. Object fields: " +
                (candidates.Count == 0 ? "none" : string.Join(", ", candidates.Select(f => f.Name))) + ". Update SceneHomeCharacter.FindTargetField.");
        }

        /// <summary>The runner root as the field's own type: GameObject, Transform or a component on the root (e.g. RunnerBody).</summary>
        private static UnityEngine.Object ResolveTarget(Type owner, string field, GameObject runner)
        {
            FieldInfo f = owner.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Type t = f?.FieldType ?? typeof(GameObject);
            if (t == typeof(GameObject)) return runner;
            if (t == typeof(Transform)) return runner.transform;
            var c = runner.GetComponent(t);
            if (c == null) throw new InvalidOperationException($"{owner.Name}.{field} wants a {t.Name}, but the runner root has none.");
            return c;
        }
    }
}
