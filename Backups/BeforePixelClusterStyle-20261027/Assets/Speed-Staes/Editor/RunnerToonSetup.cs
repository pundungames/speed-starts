using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpeedStars.Editor
{
    public static class RunnerToonSetup
    {
        const string ShaderName = "Speed Stars/Runner Toon";
        const string Folder = "Assets/Speed-Staes/Materials/RunnerToon";

        [MenuItem("Speed Stars/Toon/Refine Runner Look")]
        public static void Refine()
        {
            if (Application.isPlaying) { Debug.LogError("Exit Play Mode before refining the runner."); return; }
            var root = GameObject.Find("HumanCharacterDummy_F");
            var shader = Shader.Find(ShaderName);
            var contactShader = Shader.Find("Speed Stars/Runner Contact Shadow");
            if (!root || !shader || !contactShader || ShaderUtil.ShaderHasError(shader) || ShaderUtil.ShaderHasError(contactShader))
            { Debug.LogError("Runner or compiled toon/contact shaders unavailable."); return; }
            var materials = new HashSet<Material>();
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    if (material && material.shader == shader) materials.Add(material);
            foreach (var material in materials)
            {
                Undo.RecordObject(material, "Refine Toon Material");
                material.SetFloat("_OutlineWidth", 1.15f);
                material.SetFloat("_ShadowThreshold", 0.32f);
                material.SetFloat("_ShadowStrength", material.name.Contains("Skin") ? 0.38f : 0.34f);
                material.SetFloat("_NoiseStrength", 0.045f);
                material.SetFloat("_NoiseScale", 100);
                EditorUtility.SetDirty(material);
            }
            var lightObject = GameObject.Find("Directional Light");
            if (lightObject && lightObject.TryGetComponent<Light>(out var light))
            {
                Undo.RecordObjects(new Object[] { light, light.transform }, "Refine Toon Light");
                light.transform.rotation = Quaternion.Euler(62, 235, 0);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.38f;
            }
            const string contactPath = Folder + "/ContactShadow.mat";
            var contact = AssetDatabase.LoadAssetAtPath<Material>(contactPath);
            if (!contact)
            {
                contact = new Material(contactShader);
                AssetDatabase.CreateAsset(contact, contactPath);
            }
            var shadow = GameObject.Find("Runner Contact Shadow");
            if (!shadow)
            {
                shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shadow.name = "Runner Contact Shadow";
                Undo.RegisterCreatedObjectUndo(shadow, "Create Contact Shadow");
                Object.DestroyImmediate(shadow.GetComponent<Collider>());
            }
            var shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = contact;
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
            var follow = shadow.GetComponent<SpeedStars.Runner.RunnerContactShadow>();
            if (!follow) follow = Undo.AddComponent<SpeedStars.Runner.RunnerContactShadow>(shadow);
            Undo.RecordObject(follow, "Configure Contact Shadow");
            follow.target = root.transform;
            follow.groundHeight = root.transform.position.y + 0.012f;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Runner look refined: {materials.Count} materials; softer directional shadow and contact oval configured.");
        }

        [MenuItem("Speed Stars/Toon/Apply to Runner")]
        public static void Apply()
        {
            var shader = Shader.Find(ShaderName);
            if (!shader || ShaderUtil.ShaderHasError(shader))
            {
                Debug.LogError("Runner Toon shader is missing or has compilation errors.");
                return;
            }
            var root = GameObject.Find("HumanCharacterDummy_F");
            if (!root) { Debug.LogError("Runner not found in the open scene."); return; }
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Speed-Staes/Materials", "RunnerToon");
            var converted = new Dictionary<Material, Material>();
            int count = 0;
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source || source.shader == shader) continue;
                    if (!converted.TryGetValue(source, out var toon))
                    {
                        toon = new Material(shader) { name = source.name + "_Toon" };
                        if (source.HasProperty("_Color")) toon.SetColor("_Color", source.GetColor("_Color"));
                        if (source.HasProperty("_MainTex"))
                        {
                            toon.SetTexture("_MainTex", source.GetTexture("_MainTex"));
                            toon.SetTextureScale("_MainTex", source.GetTextureScale("_MainTex"));
                            toon.SetTextureOffset("_MainTex", source.GetTextureOffset("_MainTex"));
                        }
                        AssetDatabase.CreateAsset(toon, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + toon.name + ".mat"));
                        converted.Add(source, toon);
                    }
                    materials[i] = toon;
                }
                Undo.RecordObject(renderer, "Apply Runner Toon");
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                count++;
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"Runner Toon applied: {count} renderers, {converted.Count} new materials. Original FBX materials preserved.");
        }
    }
}
