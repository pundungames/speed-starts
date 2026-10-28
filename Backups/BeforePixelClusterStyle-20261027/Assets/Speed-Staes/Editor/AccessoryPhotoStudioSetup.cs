using System.IO;
using System.Linq;
using System.Text;
using SpeedStars.Runner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.EditorTools
{
    public static class AccessoryPhotoStudioSetup
    {
        const string ScenePath = "Assets/Speed-Staes/Scenes/Test/AccessoryPhotoStudio.unity";

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Sync Accessories From Model")]
        public static void SyncAccessoriesFromModel()
        {
            if (EditorApplication.isPlaying) return;
            const string modelPath = "Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!source || !studio) return;
            var sources = source.GetComponentsInChildren<Renderer>(true).ToDictionary(r => r.name);
            int updated = 0;
            foreach (var target in studio.character.Find("Accessories").GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!sources.TryGetValue(target.name, out var renderer)) continue;
                var asset = target.sharedMesh;
                if (!asset || AssetDatabase.GetAssetPath(asset) != "Assets/Speed-Staes/Art/Character/RunnerAccessoryMeshes.asset") continue;
                var skinned = renderer as SkinnedMeshRenderer;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = skinned ? skinned.sharedMesh : filter ? filter.sharedMesh : null;
                if (!mesh) continue;
                var generated = Object.Instantiate(mesh);
                generated.name = asset.name;
                var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var vertices = generated.vertices;
                var normals = generated.normals;
                var tangents = generated.tangents;
                var normalMatrix = matrix.inverse.transpose;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
                for (int i = 0; i < tangents.Length; i++)
                {
                    var xyz = matrix.MultiplyVector(new Vector3(tangents[i].x, tangents[i].y, tangents[i].z)).normalized;
                    tangents[i] = new Vector4(xyz.x, xyz.y, xyz.z, tangents[i].w * (matrix.determinant < 0 ? -1 : 1));
                }
                generated.vertices = vertices;
                generated.normals = normals;
                generated.tangents = tangents;
                var bones = target.bones;
                var poses = asset.bindposes;
                var weights = generated.boneWeights;
                if (skinned)
                {
                    var remap = skinned.bones.Select(b => System.Array.FindIndex(bones, t => t && t.name == b.name)).ToArray();
                    if (remap.Any(i => i < 0)) throw new System.InvalidOperationException("Bone mismatch: " + target.name);
                    poses = new Matrix4x4[bones.Length];
                    for (int i = 0; i < remap.Length; i++) poses[remap[i]] = mesh.bindposes[i] * matrix.inverse;
                    for (int i = 0; i < weights.Length; i++)
                    {
                        var weight = weights[i];
                        weight.boneIndex0 = remap[weight.boneIndex0];
                        weight.boneIndex1 = remap[weight.boneIndex1];
                        weight.boneIndex2 = remap[weight.boneIndex2];
                        weight.boneIndex3 = remap[weight.boneIndex3];
                        weights[i] = weight;
                    }
                }
                else
                {
                    // Unrigged glasses exported from Blender follow the existing head bone.
                    int head = System.Array.FindIndex(bones, b => b && b.name == "B-head");
                    if (head < 0) throw new System.InvalidOperationException("Head bone missing.");
                    var sourceBody = source.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "LowPoly Male");
                    poses = new Matrix4x4[bones.Length];
                    var bodyMatrix = source.transform.worldToLocalMatrix * sourceBody.transform.localToWorldMatrix;
                    for (int b = 0; b < bones.Length; b++)
                    {
                        int sourceIndex = System.Array.FindIndex(sourceBody.bones, bone => bone && bone.name == bones[b].name);
                        poses[b] = sourceIndex >= 0 ? sourceBody.sharedMesh.bindposes[sourceIndex] * bodyMatrix.inverse : Matrix4x4.identity;
                    }
                    weights = new BoneWeight[vertices.Length];
                    for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = head, weight0 = 1 };
                }
                generated.bindposes = poses;
                generated.boneWeights = weights;
                generated.RecalculateBounds();
                EditorUtility.CopySerialized(generated, asset);
                asset.UploadMeshData(false);
                EditorUtility.SetDirty(asset);
                Object.DestroyImmediate(generated);
                // Material slot order can change on export; use the model's remapped materials.
                target.sharedMaterials = renderer.sharedMaterials;
                target.sharedMesh = null;
                target.sharedMesh = asset;
                target.localBounds = asset.bounds;
                updated++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);
            EditorSceneManager.SaveScene(studio.gameObject.scene);
            Debug.Log($"Updated {updated} accessory meshes from current FBX; existing mesh IDs preserved for all prefab/scene references.");
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Inspect Model Update")]
        public static void InspectModelUpdate()
        {
            var path = "Assets/Kevin Iglesias/Human Character Dummy/Models/HumanCharacterDummy_F.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var report = new StringBuilder();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"SOURCE {renderer.name}: {renderer.sharedMesh?.name} vertices={renderer.sharedMesh?.vertexCount} bones={renderer.bones.Length} root={renderer.rootBone?.name}");
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                report.AppendLine($"DETAIL {renderer.name}: type={renderer.GetType().Name} pos={renderer.transform.localPosition} scale={renderer.transform.localScale} rot={renderer.transform.localEulerAngles} mesh={(renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh.name : "skinned")}");
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            foreach (var renderer in studio.character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine($"STUDIO {renderer.name}: {renderer.sharedMesh?.name} vertices={renderer.sharedMesh?.vertexCount} path={AssetDatabase.GetAssetPath(renderer.sharedMesh)}");
            foreach (var renderer in studio.character.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.name == "G01" || r.name == "T01"))
                report.AppendLine($"DEST {renderer.name}: pos={renderer.transform.localPosition} scale={renderer.transform.localScale} rot={renderer.transform.localEulerAngles} bind0={renderer.sharedMesh.bindposes[0]} bone0={renderer.bones[0].name}");
            Directory.CreateDirectory(studio.CaptureDirectory);
            File.WriteAllText(Path.Combine(studio.CaptureDirectory, "ModelUpdateInspection.txt"), report.ToString());
            Debug.Log("Model inspection written to " + studio.CaptureDirectory);
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Open Studio")]
        public static void OpenStudio()
        {
            if (EditorApplication.isPlaying) return;
            if (File.Exists(ScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            var source = Object.FindObjectsOfType<Transform>(true)
                .FirstOrDefault(t => t.parent == null && t.Find("Accessories"));
            if (!source) { Debug.LogError("Open the character scene first."); return; }
            var original = SceneManager.GetActiveScene();
            // Preserve the user's existing scene edits before opening the separate studio.
            if (original.isDirty && !EditorSceneManager.SaveScene(original)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var model = Object.Instantiate(source.gameObject);
            model.name = "Photo Character";
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            if (PrefabUtility.IsPartOfPrefabInstance(model))
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var behaviour in model.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
            foreach (string name in new[] { "Glove", "Helmet", "Leggings", "Shoes", "Short", "Tshirt" })
            {
                var part = model.transform.Find(name);
                if (part) part.gameObject.SetActive(false);
            }
            var torso = model.transform.Find("LowPoly Male");
            if (torso) torso.gameObject.SetActive(true);
            foreach (var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) mesh.updateWhenOffscreen = true;
            RelaxArm(model.transform, "L", 1);
            RelaxArm(model.transform, "R", -1);

            var camera = new GameObject("Photo Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.84f, 0.89f, 0.94f);
            camera.orthographic = true;
            camera.orthographicSize = 1.15f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50;
            camera.transform.position = new Vector3(0, 0.95f, 4);
            camera.transform.LookAt(new Vector3(0, 0.95f, 0));
            AddLight("Key Light", new Vector3(35, 145, 0), 1.1f, Color.white);
            AddLight("Fill Light", new Vector3(20, -35, 0), 0.55f, new Color(0.78f, 0.86f, 1));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.72f, 0.76f);
            RenderSettings.skybox = null;
            var studio = new GameObject("Accessory Photo Studio").AddComponent<AccessoryPhotoStudio>();
            studio.character = model.transform;
            studio.photoCamera = camera;
            studio.SelectSet(1);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(original, true);
            Selection.activeGameObject = studio.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 1, 0), new Vector3(2, 2, 2)), false);
            ValidateSets();
            Debug.Log("Studio ready. Play, click Game view, 1-9 / arrows to change outfits; V saves PNG.");
        }

        static void AddLight(string name, Vector3 angles, float intensity, Color color)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.eulerAngles = angles;
            light.intensity = intensity;
            light.color = color;
        }

        static void RelaxArm(Transform model, string side, float sign)
        {
            var bones = model.GetComponentsInChildren<Transform>(true);
            var upper = bones.FirstOrDefault(t => t.name == "B-upperArm." + side);
            var forearm = bones.FirstOrDefault(t => t.name == "B-forearm." + side);
            var hand = bones.FirstOrDefault(t => t.name == "B-hand." + side);
            if (!upper || !forearm || !hand) return;
            sign = Mathf.Sign(upper.position.x - model.position.x);
            Vector3 direction = new Vector3(sign * 0.35f, -1, 0.08f).normalized;
            upper.rotation = Quaternion.FromToRotation(forearm.position - upper.position, direction) * upper.rotation;
            forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position,
                new Vector3(sign * 0.15f, -1, 0.2f).normalized) * forearm.rotation;
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Validate All Sets")]
        public static void ValidateSets()
        {
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) return;
            int selected = studio.selectedSet;
            var report = new StringBuilder();
            for (int number = 1; number <= 9; number++)
            {
                studio.SelectSet(number);
                foreach (string category in AccessoryPhotoStudio.Categories)
                {
                    var root = studio.character.Find("Accessories/" + category);
                    var active = root.Cast<Transform>().Where(t => t.gameObject.activeSelf).Select(t => t.name).ToArray();
                    if (active.Length == 0) throw new System.InvalidOperationException($"Set {number}: {category} missing");
                    report.AppendLine($"{number:00} / {category}: {string.Join(", ", active)}");
                }
            }
            studio.SelectSet(selected);
            Directory.CreateDirectory(studio.CaptureDirectory);
            File.WriteAllText(Path.Combine(studio.CaptureDirectory, "SetMapping.txt"), report.ToString());
            Debug.Log("All 9 sets / 8 categories validated. Mapping: " + studio.CaptureDirectory);
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Capture Current Set")]
        public static void CaptureCurrent() => Object.FindObjectOfType<AccessoryPhotoStudio>()?.Capture();

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Refine Pose And Lighting")]
        public static void RefinePose()
        {
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) return;
            RelaxArm(studio.character, "L", 1);
            RelaxArm(studio.character, "R", -1);
            GameObject.Find("Key Light").transform.eulerAngles = new Vector3(35, 145, 0);
            GameObject.Find("Fill Light").transform.eulerAngles = new Vector3(20, -35, 0);
            EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);
            EditorSceneManager.SaveScene(studio.gameObject.scene);
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Capture All 9 Sets")]
        public static void CaptureAll()
        {
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) return;
            int selected = studio.selectedSet;
            for (int i = 1; i <= 9; i++) { studio.SelectSet(i); studio.Capture(); }
            studio.SelectSet(selected);
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Verify Runtime Colors And Base")]
        public static void VerifyRuntimeColorsAndBase()
        {
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio || !Application.isPlaying) throw new System.InvalidOperationException("Enter Play mode first.");
            int selected = studio.selectedSet;
            var renderers = studio.character.GetComponentsInChildren<Renderer>(true);
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
            var originals = materials.Select(m => m.HasProperty("_Color") ? m.GetColor("_Color") : Color.clear).ToArray();
            studio.SelectSet(1);
            var top = studio.character.Find("Accessories/Tops/T01").GetComponent<Renderer>();
            var shorts = studio.character.Find("Accessories/Shorts/S01").GetComponent<Renderer>();
            var topBlock = new MaterialPropertyBlock();
            var shortsBlock = new MaterialPropertyBlock();
            foreach (string channel in new[] { "Color1_Toon", "Color2_Toon", "Color3_Toon" })
            {
                int topIndex = System.Array.FindIndex(top.sharedMaterials, m => m && m.name == channel);
                int shortsIndex = System.Array.FindIndex(shorts.sharedMaterials, m => m && m.name == channel);
                if (topIndex < 0 || shortsIndex < 0) continue;
                top.GetPropertyBlock(topBlock, topIndex);
                shorts.GetPropertyBlock(shortsBlock, shortsIndex);
                if (topBlock.GetColor("_Color") == shortsBlock.GetColor("_Color"))
                    throw new System.InvalidOperationException("Top and shorts share a color: " + channel);
            }
            Debug.Log("PASS: top and shorts color channels are independent.");
            var renderer = renderers.First(r => r.sharedMaterials.Any(m => m && m.name == "Color2_Toon"));
            int index = System.Array.FindIndex(renderer.sharedMaterials, m => m && m.name == "Color2_Toon");
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block, index);
            var first = block.GetColor("_Color");
            studio.SelectSet(1);
            renderer.GetPropertyBlock(block, index);
            if (first == block.GetColor("_Color")) throw new System.InvalidOperationException("Repeated set did not reroll colors.");
            var skinRenderer = renderers.First(r => r.sharedMaterials.Any(m => m && m.name == "ColorSkin_Toon"));
            int skinIndex = System.Array.FindIndex(skinRenderer.sharedMaterials, m => m && m.name == "ColorSkin_Toon");
            skinRenderer.GetPropertyBlock(block, skinIndex);
            Color firstSkin = block.GetColor("_Color");
            studio.SelectSet(1);
            skinRenderer.GetPropertyBlock(block, skinIndex);
            if (firstSkin == block.GetColor("_Color")) throw new System.InvalidOperationException("Skin did not reroll.");
            studio.SelectSet(2);
            studio.SelectSet(1);
            renderer.GetPropertyBlock(block, index);
            if (first == block.GetColor("_Color")) throw new System.InvalidOperationException("Returning to set did not reroll colors.");
            studio.Capture();
            studio.SelectSet(0);
            var accessories = studio.character.Find("Accessories");
            var active = accessories.Cast<Transform>().SelectMany(t => t.Cast<Transform>())
                .Where(t => t.gameObject.activeSelf).Select(t => t.name).OrderBy(n => n).ToArray();
            if (active.Length != 2 || !active.Contains("S01") || !active.Contains("SH01"))
                throw new System.InvalidOperationException("Base must contain only S01 and SH01.");
            studio.Capture();
            studio.PreviousSet();
            if (studio.selectedSet != 9) throw new System.InvalidOperationException("Previous wrap failed.");
            studio.NextSet();
            if (studio.selectedSet != 0) throw new System.InvalidOperationException("Next wrap failed.");
            for (int i = 0; i < materials.Length; i++)
                if (materials[i].HasProperty("_Color") && materials[i].GetColor("_Color") != originals[i])
                    throw new System.InvalidOperationException("Shared material was changed: " + materials[i].name);
            studio.SelectSet(selected);
            Debug.Log("PASS: outfit and skin reroll, returning rerolls, base has S01/SH01 only, 0-9 wraps, shared materials unchanged.");
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Restore Original Environment")]
        public static void RestoreEnvironment()
        {
            if (EditorApplication.isPlaying) return;
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) return;
            var destination = studio.gameObject.scene;
            if (destination.GetRootGameObjects().Any(go => go.name == "Original Environment")) return;
            var source = EditorSceneManager.OpenScene("Assets/Speed-Staes/Scenes/Test/SerhatSiri.unity", OpenSceneMode.Additive);
            SceneManager.SetActiveScene(source);
            var skybox = RenderSettings.skybox;
            var ambientMode = RenderSettings.ambientMode;
            var ambientLight = RenderSettings.ambientLight;
            var ambientSky = RenderSettings.ambientSkyColor;
            var ambientEquator = RenderSettings.ambientEquatorColor;
            var ambientGround = RenderSettings.ambientGroundColor;
            var ambientIntensity = RenderSettings.ambientIntensity;
            bool fog = RenderSettings.fog;
            var fogColor = RenderSettings.fogColor;
            var fogMode = RenderSettings.fogMode;
            float fogDensity = RenderSettings.fogDensity;
            float fogStart = RenderSettings.fogStartDistance, fogEnd = RenderSettings.fogEndDistance;
            var sourceCharacter = source.GetRootGameObjects().First(go => go.transform.Find("Accessories"));
            var position = sourceCharacter.transform.position;
            SceneManager.SetActiveScene(destination);
            var environment = new GameObject("Original Environment");
            foreach (var root in source.GetRootGameObjects())
            {
                if (root == sourceCharacter || root.GetComponent<Camera>()) continue;
                var clone = Object.Instantiate(root, environment.transform);
                clone.name = root.name;
                foreach (var shadow in clone.GetComponentsInChildren<RunnerContactShadow>(true))
                    shadow.target = studio.character;
            }
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            var sun = environment.GetComponentsInChildren<Light>().FirstOrDefault(l => l.type == LightType.Directional);
            RenderSettings.sun = sun;
            studio.photoCamera.transform.position += position - studio.character.position;
            studio.character.position = position;
            studio.photoCamera.clearFlags = skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            foreach (var root in destination.GetRootGameObjects())
                if (root.name == "Key Light" || root.name == "Fill Light") root.SetActive(false);
            EditorSceneManager.CloseScene(source, true);
            FrameEnvironment();
            EditorSceneManager.MarkSceneDirty(destination);
            EditorSceneManager.SaveScene(destination);
            Debug.Log("Original environment restored to photo studio.");
        }

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Frame Environment")]
        public static void FrameEnvironment()
        {
            var studio = Object.FindObjectOfType<AccessoryPhotoStudio>();
            if (!studio) return;
            studio.character.rotation = Quaternion.Euler(0, 90, 0);
            var camera = studio.photoCamera;
            camera.transform.position = studio.character.position + new Vector3(4.5f, 1.3f, 0);
            camera.transform.LookAt(studio.character.position + Vector3.up * 0.95f);
            camera.orthographic = false;
            camera.fieldOfView = 29;
            camera.farClipPlane = 500;
            EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);
            EditorSceneManager.SaveScene(studio.gameObject.scene);
        }
    }
}
