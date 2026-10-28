using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Editor helpers shared by the config, ProjectContext and scene builders (T-006). Every lookup that can miss
    /// throws with the path or field name in the message: the builders run unattended in batchmode, where a silent
    /// null would only show up later as an empty Inspector slot.
    /// </summary>
    internal static class SceneBuilderUtil
    {
        /// <summary>Absolute disk path of a project-relative asset path (never resolved against the process CWD).</summary>
        public static string FullPath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, assetPath));
        }

        public static bool FileExists(string assetPath) => File.Exists(FullPath(assetPath));

        /// <summary>Creates every missing folder of "Assets/A/B" through the AssetDatabase, so each one gets its .meta.</summary>
        public static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            int slash = folder.LastIndexOf('/');
            if (slash <= 0)
                throw new InvalidOperationException($"Cannot create folder '{folder}': it must be inside Assets/.");

            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException($"Could not create folder '{folder}'.");
        }

        public static void EnsureParentFolder(string assetPath)
        {
            string parent = Path.GetDirectoryName(assetPath.Replace('\\', '/'));
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException($"'{assetPath}' has no parent folder.");
            EnsureFolder(parent.Replace('\\', '/'));
        }

        /// <summary>
        /// The asset to fill, or null when it already exists and <paramref name="overwrite"/> is off: an existing asset
        /// belongs to the user and is left exactly as it is. With overwrite the same asset (same GUID, so every
        /// reference to it survives) is reset to the code defaults first; a new one is created with the code defaults.
        /// </summary>
        public static T CreateOrReset<T>(string path, bool overwrite) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null && FileExists(path))
                throw new InvalidOperationException($"'{path}' exists but is not a {typeof(T).Name}; move it away or fix its type.");
            if (existing != null && !overwrite) return null;

            string assetName = Path.GetFileNameWithoutExtension(path);
            var fresh = ScriptableObject.CreateInstance<T>();
            if (existing == null)
            {
                EnsureParentFolder(path);
                fresh.name = assetName;
                AssetDatabase.CreateAsset(fresh, path);
                return fresh;
            }

            EditorUtility.CopySerialized(fresh, existing);
            existing.name = assetName; // CopySerialized also copies the (empty) name of the fresh instance
            UnityEngine.Object.DestroyImmediate(fresh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Loads an asset another step produces; throws naming that step when it is missing.</summary>
        public static T LoadRequired<T>(string path, string producedBy) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"{typeof(T).Name} not found at '{path}'. It comes from {producedBy}; run that step first.");
            return asset;
        }

        /// <summary>A serialized field by name (or path like "scenes.homeScene"); throws when the code renamed it.</summary>
        public static SerializedProperty Prop(SerializedObject so, string path)
        {
            var property = so.FindProperty(path);
            if (property == null)
            {
                var target = so.targetObject;
                throw new InvalidOperationException(
                    $"Serialized field '{path}' not found on {target.GetType().FullName} ('{target.name}'). It was renamed or removed; update the builder.");
            }
            return property;
        }

        /// <summary>A field of a struct/class element (array item); throws when it is missing.</summary>
        public static SerializedProperty Rel(SerializedProperty parent, string name)
        {
            var property = parent.FindPropertyRelative(name);
            if (property == null)
                throw new InvalidOperationException($"Serialized field '{name}' not found under '{parent.propertyPath}' ({parent.serializedObject.targetObject.GetType().FullName}).");
            return property;
        }

        public static void SetObjectList(SerializedProperty list, UnityEngine.Object[] items)
        {
            list.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        public static void Apply(SerializedObject so)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so.targetObject);
        }

        /// <summary>AddComponent that throws instead of returning null (script missing, not compiled, or disallowed).</summary>
        public static T AddRequired<T>(GameObject go) where T : Component
        {
            var component = go.AddComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Could not add {typeof(T).FullName} to '{go.name}' (script missing or not compiled?).");
            return component;
        }

        public static void Log(string source, string message) => UnityEngine.Debug.Log($"[{source}] {message}");

        public static void Warn(string source, string message) => UnityEngine.Debug.LogWarning($"[{source}] {message}");

        public static void Skipped(string source, string path) => Log(source, $"'{path}' exists; left as it is (overwrite off).");
    }
}
