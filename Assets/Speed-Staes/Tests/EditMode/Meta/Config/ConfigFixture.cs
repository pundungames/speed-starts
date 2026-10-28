using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tests.Meta.Config
{
    /// <summary>
    /// Creates in-memory config instances and writes their private serialized fields through SerializedObject, the same
    /// path the Inspector uses. Writing by serialized name also proves the field names the assets will store.
    /// Each test gives its own values; nothing here mirrors the real asset numbers.
    /// </summary>
    internal sealed class ConfigFixture
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        public T Create<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            created.Add(instance);
            return instance;
        }

        public void DestroyAll()
        {
            foreach (var instance in created)
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            created.Clear();
        }

        public static void SetString(UnityEngine.Object target, string path, string value) =>
            Edit(target, so => Find(so, path).stringValue = value);

        public static void SetInt(UnityEngine.Object target, string path, int value) =>
            Edit(target, so => Find(so, path).intValue = value);

        public static void SetFloat(UnityEngine.Object target, string path, float value) =>
            Edit(target, so => Find(so, path).floatValue = value);

        public static void SetBool(UnityEngine.Object target, string path, bool value) =>
            Edit(target, so => Find(so, path).boolValue = value);

        public static void SetInts(UnityEngine.Object target, string path, params int[] values) =>
            Edit(target, so =>
            {
                var array = Find(so, path);
                array.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    array.GetArrayElementAtIndex(i).intValue = values[i];
            });

        public static void SetStrings(UnityEngine.Object target, string path, params string[] values) =>
            Edit(target, so =>
            {
                var array = Find(so, path);
                array.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    array.GetArrayElementAtIndex(i).stringValue = values[i];
            });

        /// <summary>Fills an object-reference array; null entries become empty slots.</summary>
        public static void SetObjects(UnityEngine.Object target, string path, params UnityEngine.Object[] values) =>
            Edit(target, so =>
            {
                var array = Find(so, path);
                array.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });

        public static void Edit(UnityEngine.Object target, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static SerializedProperty Find(SerializedObject so, string path)
        {
            var property = so.FindProperty(path);
            Assert.IsNotNull(property, $"Serialized field '{path}' not found on {so.targetObject.GetType().Name}.");
            return property;
        }
    }
}
