using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SpeedStars.Physical;
using SpeedStars.Runner;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static SpeedStars.Tools.Builders.SceneBuilderUtil;

namespace SpeedStars.Tools.Builders
{
    /// <summary>
    /// Live character for the Home scene (T-019): a prefab INSTANCE of Malik's runner (the link stays, the prefab file is
    /// never written), a pedestal, a framing camera and a light. The runner's input / debug tools are switched off as
    /// scene overrides and SpeedStars.Bridge.MenuCharacter (T-017) is added and pointed at the runner root.
    /// MenuCharacter is looked up by type name, so this file compiles before T-017 lands; a missing type or field is a
    /// clear error instead of a silently empty slot.
    /// </summary>
    internal static class SceneHomeCharacter
    {
        private const string Source = nameof(SceneHomeCharacter);

        public const string RunnerPrefabPath = "Assets/Speed-Staes/Prefabs/Character/HumanCharacterDummy_F.prefab";
        private const string PedestalMaterialPath = "Assets/Speed-Staes/Scenes/Menu/Materials/HomePedestal.mat";
        private const string MenuCharacterType = "SpeedStars.Bridge.MenuCharacter";
        private const string ScreenshotPath = "Temp/shots/home.png";

        // Prefab forward is +Z (RunnerBody advances along transform.forward); the camera looks along +Z, so turn around.
        private const float CharacterYaw = 180f;
        private const float PedestalRadius = 0.7f;
        private const float PedestalHeight = 0.2f;

        // Camera: vertical FOV and distance frame ~2.4 m of height; the look-at point sits a little above the waist so
        // the figure reads centred with the pedestal under it.
        private const float CameraFov = 30f;
        private const float CameraDistance = 5f;
        private const float CameraLookHeight = 1.0f;
        private const float RunnerHeightGuess = 1.9f;

        private static readonly Color PedestalColor = new Color(0.93f, 0.95f, 0.98f);

        public static void Build(Scene scene, Camera camera)
        {
            var prefab = LoadRequired<GameObject>(RunnerPrefabPath, "Malik's character prefab");
            Type menuCharacter = FindType(MenuCharacterType)
                ?? throw new InvalidOperationException($"{MenuCharacterType} not found. It comes from T-017 (gameplay-agent); build after it compiles.");

            // Pedestal: its top face is y = 0, the runner stands there.
            var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            UnityEngine.Object.DestroyImmediate(pedestal.GetComponent<Collider>());
            pedestal.transform.position = new Vector3(0f, -PedestalHeight * 0.5f, 0f);
            pedestal.transform.localScale = new Vector3(PedestalRadius * 2f, PedestalHeight * 0.5f, PedestalRadius * 2f); // cylinder is 2 units tall
            pedestal.GetComponent<Renderer>().sharedMaterial = PedestalMaterial();
            SceneManager.MoveGameObjectToScene(pedestal, scene);

            var runner = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (runner == null) throw new InvalidOperationException($"Could not instantiate '{RunnerPrefabPath}'.");
            runner.name = prefab.name;
            runner.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, CharacterYaw, 0f));

            SwitchOff(runner);

            var component = runner.GetComponent(menuCharacter) ?? runner.AddComponent(menuCharacter);
            if (component == null) throw new InvalidOperationException($"Could not add {MenuCharacterType} to '{runner.name}'.");
            var so = new SerializedObject(component);
            SerializedProperty target = FindTargetField(so, menuCharacter);
            if (target.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException($"{MenuCharacterType}.{target.name} is not an object reference.");
            target.objectReferenceValue = ResolveTarget(menuCharacter, target.name, runner);
            Apply(so);

            CreateLight(scene);
            FrameCamera(camera);
            Log(Source, $"Home: '{runner.name}' prefab instance on '{pedestal.name}', {menuCharacter.Name}.{target.name} -> runner root.");
        }

        /// <summary>Optional: a 2400x1080 PNG from the Home camera (3D only, Overlay canvases are not drawn). Skipped without a GPU.</summary>
        public static void TryCaptureShot(Camera camera)
        {
            if (camera == null) return;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Log(Source, "No graphics device (-nographics); Home screenshot skipped.");
                return;
            }
            RenderTexture rt = null;
            var previous = RenderTexture.active;
            try
            {
                rt = new RenderTexture(2400, 1080, 24);
                camera.targetTexture = rt;
                camera.aspect = 2400f / 1080f;
                camera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(2400, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 2400, 1080), 0, 0);
                tex.Apply();
                string full = FullPath(ScreenshotPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Log(Source, $"Wrote {ScreenshotPath}.");
            }
            catch (Exception e)
            {
                Warn(Source, $"Home screenshot failed: {e.Message}");
            }
            finally
            {
                camera.targetTexture = null;
                camera.ResetAspect();
                RenderTexture.active = previous;
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            }
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
            // RunnerBody / RunnerPoseDriver are left as the prefab has them: MenuCharacter (T-017) decides what to do with them.
        }

        private static void FrameCamera(Camera camera)
        {
            camera.orthographic = false;
            camera.fieldOfView = CameraFov;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 60f;
            camera.transform.position = new Vector3(0f, CameraLookHeight, -CameraDistance);
            camera.transform.rotation = Quaternion.identity; // looks along +Z at the runner's centre
            float visible = 2f * CameraDistance * Mathf.Tan(CameraFov * 0.5f * Mathf.Deg2Rad);
            if (visible < RunnerHeightGuess * 1.15f)
                Warn(Source, "Home camera frames less than the runner's height; raise CameraDistance.");
        }

        private static void CreateLight(Scene scene)
        {
            var go = SceneUiLayout.CreateRoot(scene, "Directional Light", typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(40f, 150f, 0f); // from camera side, slightly above
        }

        private static Material PedestalMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(PedestalMaterialPath);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Standard") ?? throw new InvalidOperationException("Shader 'Standard' not found.");
            EnsureParentFolder(PedestalMaterialPath);
            var material = new Material(shader) { name = "HomePedestal", color = PedestalColor };
            material.SetFloat("_Glossiness", 0.25f);
            AssetDatabase.CreateAsset(material, PedestalMaterialPath);
            return material;
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
