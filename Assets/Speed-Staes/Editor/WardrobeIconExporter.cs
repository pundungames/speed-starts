using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpeedStars.Runner;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.EditorTools
{
    public static class WardrobeIconExporter
    {
        const int Width = 600, Height = 500;

        [MenuItem("Tools/Speed Stars/Accessory Photo Studio/Export Transparent Clothing Icons")]
        public static void Export()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode before exporting icons."); return; }
            var originalScene = SceneManager.GetActiveScene();
            const string sourcePath = "Assets/Speed-Staes/Scenes/Test/AccessoryPhotoStudio.unity";
            var scene = SceneManager.GetSceneByPath(sourcePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
            var preview = new PreviewRenderUtility();
            var meshes = new List<Mesh>();
            var folder = "Assets/Speed-Staes/UI/WardrobeIcons_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            folder = AssetDatabase.GenerateUniqueAssetPath(folder);
            Directory.CreateDirectory(folder);
            int count = 0;
            try
            {
                var studio = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<AccessoryPhotoStudio>(true)).First();
                var character = studio.character;
                var accessories = character.Find("Accessories");
                preview.lights[0].intensity = 1.1f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35, 145, 0);
                preview.lights[1].intensity = 0.35f;
                preview.lights[1].transform.rotation = Quaternion.Euler(15, -35, 0);
                preview.ambientColor = new Color(0.5f, 0.53f, 0.57f);
                foreach (string categoryName in AccessoryPhotoStudio.Categories)
                {
                    var category = accessories.Find(categoryName);
                    if (!category) throw new InvalidOperationException("Category missing: " + categoryName);
                    string categoryFolder = folder + "/" + categoryName;
                    Directory.CreateDirectory(categoryFolder);
                    foreach (Transform accessory in category)
                    {
                        var parts = new[] { accessory };
                        string itemName = accessory.name;
                        var item = new GameObject(itemName);
                        preview.AddSingleGO(item);
                        try
                        {
                            foreach (var part in parts)
                                foreach (var renderer in part.GetComponentsInChildren<Renderer>(true))
                                {
                                    var skin = renderer as SkinnedMeshRenderer;
                                    var mesh = new Mesh { name = renderer.name + " Icon Preview", hideFlags = HideFlags.HideAndDontSave };
                                    if (skin) skin.BakeMesh(mesh);
                                    else
                                    {
                                        var filter = renderer.GetComponent<MeshFilter>();
                                        if (!filter || !filter.sharedMesh) { UnityEngine.Object.DestroyImmediate(mesh); continue; }
                                        UnityEngine.Object.DestroyImmediate(mesh);
                                        mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                                    }
                                    meshes.Add(mesh);
                                    var matrix = character.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                                    var vertices = mesh.vertices;
                                    var normals = mesh.normals;
                                    for (int v = 0; v < vertices.Length; v++) vertices[v] = matrix.MultiplyPoint3x4(vertices[v]);
                                    if (categoryName == "Gloves" && vertices.Length > 0)
                                    {
                                        // Remove the empty torso-sized gap between paired gloves for a readable UI icon.
                                        float mid = (vertices.Min(v => v.x) + vertices.Max(v => v.x)) * 0.5f;
                                        var left = vertices.Where(v => v.x < mid).ToArray();
                                        var right = vertices.Where(v => v.x >= mid).ToArray();
                                        if (left.Length > 0 && right.Length > 0)
                                        {
                                            float gap = right.Min(v => v.x) - left.Max(v => v.x);
                                            if (gap > 0.08f)
                                                for (int v = 0; v < vertices.Length; v++)
                                                    vertices[v].x += (vertices[v].x < mid ? 1 : -1) * (gap - 0.025f) * 0.5f;
                                        }
                                    }
                                    var normalMatrix = matrix.inverse.transpose;
                                    for (int n = 0; n < normals.Length; n++) normals[n] = normalMatrix.MultiplyVector(normals[n]).normalized;
                                    mesh.vertices = vertices;
                                    mesh.normals = normals;
                                    mesh.RecalculateBounds();
                                    var child = new GameObject(renderer.name, typeof(MeshFilter), typeof(MeshRenderer));
                                    child.transform.SetParent(item.transform, false);
                                    child.GetComponent<MeshFilter>().sharedMesh = mesh;
                                    child.GetComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                                }
                            var renderers = item.GetComponentsInChildren<MeshRenderer>();
                            if (renderers.Length == 0) throw new InvalidOperationException("No geometry: " + itemName);
                            var bounds = renderers[0].bounds;
                            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                            var camera = preview.camera;
                            var direction = new Vector3(0.22f, 0.08f, 1).normalized;
                            camera.transform.position = bounds.center + direction * 5;
                            camera.transform.LookAt(bounds.center);
                            camera.orthographic = true;
                            float xExtent = 0, yExtent = 0;
                            foreach (var mesh in item.GetComponentsInChildren<MeshFilter>())
                                foreach (var vertex in mesh.sharedMesh.vertices)
                                {
                                    var offset = vertex - bounds.center;
                                    xExtent = Mathf.Max(xExtent, Mathf.Abs(Vector3.Dot(offset, camera.transform.right)));
                                    yExtent = Mathf.Max(yExtent, Mathf.Abs(Vector3.Dot(offset, camera.transform.up)));
                                }
                            camera.orthographicSize = Mathf.Max(yExtent, xExtent / ((float)Width / Height)) * 1.18f;
                            camera.nearClipPlane = 0.01f;
                            camera.farClipPlane = 20;
                            preview.BeginPreview(new Rect(0, 0, Width, Height), GUIStyle.none);
                            camera.clearFlags = CameraClearFlags.SolidColor;
                            camera.backgroundColor = Color.clear;
                            camera.allowHDR = false;
                            camera.allowMSAA = false;
                            preview.Render();
                            var texture = preview.EndPreview() as RenderTexture;
                            var oldActive = RenderTexture.active;
                            var pixels = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                            try
                            {
                                RenderTexture.active = texture;
                                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                                pixels.Apply();
                                File.WriteAllBytes(categoryFolder + "/" + itemName + ".png", pixels.EncodeToPNG());
                            }
                            finally { RenderTexture.active = oldActive; UnityEngine.Object.DestroyImmediate(pixels); }
                            count++;
                        }
                        finally
                        {
                            UnityEngine.Object.DestroyImmediate(item);
                            foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
                            meshes.Clear();
                        }
                    }
                }
            }
            finally
            {
                preview.Cleanup();
                foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid() && originalScene.isLoaded) SceneManager.SetActiveScene(originalScene);
                AssetDatabase.Refresh();
            }
            foreach (string file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                var importer = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (!importer) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
            Debug.Log($"CLOTHING ICON EXPORT: {count} transparent {Width}x{Height} PNG sprites -> {folder}");
            File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Captures/LatestWardrobeIconExport.txt"), folder);
        }
    }
}
