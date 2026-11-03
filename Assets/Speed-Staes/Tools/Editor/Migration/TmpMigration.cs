using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SpeedStars.Tools.Builders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace SpeedStars.Tools.Migration
{
    /// <summary>
    /// T-100 entry point: TMP Essential Resources, the American Captain SDF font asset (TMP default font), outline material presets
    /// and the in-place legacy Text -> TextMeshProUGUI conversion of our UI prefabs, Home.unity and Race.unity. The YAML work is
    /// <see cref="LegacyTextConverter"/> (pure, tested); this class is the Unity side: files, assets, GUIDs, reserialize.
    /// Idempotent: a second run finds nothing to do and writes no file.
    /// Batchmode: -executeMethod SpeedStars.Tools.Migration.TmpMigration.Run (markers TMP_MIGRATION_OK / TMP_MIGRATION_FAILED).
    /// </summary>
    public static class TmpMigration
    {
        public const string OkMarker = "TMP_MIGRATION_OK";
        public const string FailedMarker = "TMP_MIGRATION_FAILED";

        private const string HomeScene = "Assets/Speed-Staes/Scenes/Menu/Home.unity";
        private const string RaceScene = "Assets/Speed-Staes/Scenes/Game/Race.unity";
        private const string FontName = "AmericanCaptain SDF";

        // ---------------------------------------------------------------- entry points

        /// <summary>Batchmode / scripted entry: everything in one call.</summary>
        public static void Run()
        {
            try
            {
                string report = Execute(null);
                Debug.Log(report);
            }
            catch (Exception e)
            {
                Debug.LogError($"{FailedMarker} {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Interactive package import. In batchmode the import is async and Unity quits first: use instead
        /// Unity -batchmode -quit -importPackage "&lt;project&gt;/Library/PackageCache/com.unity.textmeshpro@3.0.7/Package Resources/TMP Essential Resources.unitypackage".
        /// </summary>
        public static void ImportEssentials()
        {
            try
            {
                EnsureEssentials();
                Debug.Log($"{OkMarker} essentials present at {UiFontPaths.TmpSettings}");
            }
            catch (Exception e)
            {
                Debug.LogError($"{FailedMarker} {e}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Speed Stars/Convert UI Text to TMP (American Captain)")]
        public static void RunFromMenu()
        {
            if (EditorIsBusy(out string why))
            {
                Debug.LogWarning($"TMP migration not started: {why}");
                EditorUtility.DisplayDialog("Convert UI Text to TMP", $"Not started: {why}", "OK");
                return;
            }

            string report;
            try
            {
                report = Execute(null);
                Debug.Log(report);
            }
            catch (Exception e)
            {
                Debug.LogError($"{FailedMarker} {e}");
                report = $"{FailedMarker}\n{e.Message}";
            }

            EditorUtility.DisplayDialog("Convert UI Text to TMP", report, "OK");
        }

        /// <summary>Converts only the given asset paths (builders call it on prefabs / scenes they just created).</summary>
        public static void ConvertPaths(IEnumerable<string> assetPaths)
        {
            Debug.Log(Execute(new HashSet<string>(assetPaths)));
        }

        private static bool EditorIsBusy(out string why)
        {
            why = null;
            if (EditorApplication.isPlayingOrWillChangePlaymode) why = "the Editor is in Play mode.";
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetCurrentPrefabStage().scene.isDirty)
                why = "the open prefab stage has unsaved changes; save or close it first.";
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty) { why = $"the open scene '{scene.name}' has unsaved changes; save it first."; break; }
                }
            }

            return why != null;
        }

        // ---------------------------------------------------------------- the run

        /// <summary>
        /// onlyThese == null converts the whole scope; otherwise only those paths (targets of nested-prefab overrides are still read
        /// from the whole scope). Returns the report (first line is the marker).
        /// </summary>
        private static string Execute(HashSet<string> onlyThese)
        {
            EnsureEssentials();
            TMP_FontAsset font = EnsureFontAsset();
            EnsureSettingsDefault(font);

            // Context: references that only exist once the assets are saved.
            var ctx = new TextConversionContext
            {
                FontAssetRef = ReferenceOf(font, 2),
                DefaultMaterialRef = ReferenceOf(font.material, 2),
                TtfFontRef = ReferenceOf(AssetDatabase.LoadAssetAtPath<Font>(UiFontPaths.AmericanCaptainTtf), 3)
            };

            List<string> scope = ScopePaths();

            // Pass 1: text targets (for nested overrides) and the outline looks that need a preset.
            var texts = new Dictionary<string, string>();
            var specs = new Dictionary<string, OutlineSpec>();
            foreach (string path in scope)
            {
                string text = File.ReadAllText(Full(path));
                texts[path] = text;
                TextAnalysis a = LegacyTextConverter.Analyze(text);

                string guid = path.EndsWith(".prefab", StringComparison.Ordinal) ? GuidFromMeta(path) : null;
                if (guid != null)
                    foreach (long id in a.TextIds) ctx.TextTargets.Add(guid + ":" + id);

                if (onlyThese != null && !onlyThese.Contains(path)) continue;
                foreach (OutlineSpec spec in a.Outlines) specs[spec.Key] = spec;
            }

            var presetRefs = new Dictionary<string, string>();
            int presetsCreated = 0;
            foreach (OutlineSpec spec in specs.Values)
            {
                presetRefs[spec.Key] = EnsurePreset(font, spec, out bool created);
                if (created) presetsCreated++;
            }

            ctx.PresetMaterialRef = spec => presetRefs[spec.Key];

            // Pass 2: convert; files are rewritten only when the converter changed something.
            var touched = new List<string>();
            var report = new StringBuilder();
            int converted = 0, fonts = 0, outlines = 0, overrides = 0, dropped = 0;
            var remaining = new List<string>();
            var warnings = new List<string>();
            foreach (string path in scope)
            {
                if (onlyThese != null && !onlyThese.Contains(path)) continue;
                ConversionResult r = LegacyTextConverter.Convert(texts[path], ctx);
                converted += r.Converted;
                fonts += r.InputFieldFonts;
                outlines += r.OutlinesRemoved;
                overrides += r.OverridesTranslated;
                dropped += r.OverridesDropped;
                foreach (string item in r.RemainingLegacy) remaining.Add($"{path}: {item}");
                foreach (string w in r.Warnings) warnings.Add($"{path}: {w}");

                if (!r.Changed) continue;
                File.WriteAllText(Full(path), r.Text, new UTF8Encoding(false));
                touched.Add(path);
                report.AppendLine($"  {path}: converted={r.Converted} inputFieldFonts={r.InputFieldFonts} outlinesRemoved={r.OutlinesRemoved} overrides={r.OverridesTranslated}");
            }

            if (touched.Count > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (string path in touched) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ForceReserializeAssets(touched);
            }

            AssetDatabase.SaveAssets();

            var head = new StringBuilder();
            head.AppendLine($"{OkMarker} files={touched.Count}/{scope.Count} converted={converted} inputFieldFonts={fonts} outlinesRemoved={outlines} " +
                            $"overridesTranslated={overrides} overridesDropped={dropped} presetsCreated={presetsCreated} presets={presetRefs.Count}");
            head.Append(report);
            head.AppendLine($"Legacy Text left (InputField-owned, TTF font only): {remaining.Count}");
            foreach (string item in remaining) head.AppendLine("  " + item);
            if (warnings.Count > 0)
            {
                head.AppendLine($"Warnings: {warnings.Count}");
                foreach (string w in warnings) head.AppendLine("  " + w);
            }

            if (touched.Count == 0) head.AppendLine("Nothing to change (already converted).");
            return head.ToString();
        }

        // ---------------------------------------------------------------- TMP setup

        private static void EnsureEssentials()
        {
            if (File.Exists(Full(UiFontPaths.TmpSettings))) return;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!File.Exists(Full(UiFontPaths.TmpSettings)))
                throw new InvalidOperationException($"TMP Essential Resources import did not produce '{UiFontPaths.TmpSettings}'. Run TmpMigration.ImportEssentials in its own batchmode run, then Run again.");
        }

        private static TMP_FontAsset EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPaths.AmericanCaptainSdf);
            if (existing != null) return existing;

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(UiFontPaths.AmericanCaptainTtf);
            if (ttf == null) throw new InvalidOperationException($"'{UiFontPaths.AmericanCaptainTtf}' is missing (the supervisor adds the TTF).");

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(ttf, UiFontPaths.SamplingPointSize, UiFontPaths.AtlasPadding,
                GlyphRenderMode.SDFAA, UiFontPaths.AtlasSize, UiFontPaths.AtlasSize, AtlasPopulationMode.Dynamic, true);
            if (font == null) throw new InvalidOperationException("TMP_FontAsset.CreateFontAsset returned null (font data not included?).");

            font.name = FontName;
            AssetDatabase.CreateAsset(font, UiFontPaths.AmericanCaptainSdf);

            // The material and the atlas live inside the font asset file, like TMP's own font asset creator saves them.
            font.material.name = FontName + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                atlas.name = FontName + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
            }

            // Pre-fill the plain Latin set so the first screen does not generate glyphs while it fades in.
            var latin = new StringBuilder();
            for (char c = ' '; c <= '~'; c++) latin.Append(c);
            font.TryAddCharacters(latin.ToString(), out string _);

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UiFontPaths.AmericanCaptainSdf, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiFontPaths.AmericanCaptainSdf);
        }

        /// <summary>The default font of new TMP objects; the fallback list is left alone (CJK decision is open).</summary>
        private static void EnsureSettingsDefault(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(UiFontPaths.TmpSettings);
            if (settings == null) throw new InvalidOperationException($"'{UiFontPaths.TmpSettings}' does not load as TMP_Settings.");

            var so = new SerializedObject(settings);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop.objectReferenceValue == font) return;
            prop.objectReferenceValue = font;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static string EnsurePreset(TMP_FontAsset font, OutlineSpec spec, out bool created)
        {
            string path = $"{UiFontPaths.PresetFolder}/{FontName} - {spec.Key}.mat";
            created = false;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                UiBuildKit.EnsureFolder(UiFontPaths.PresetFolder);
                mat = new Material(font.material) { name = $"{FontName} - {spec.Key}" };
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(spec.R, spec.G, spec.B, spec.A));
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, spec.Width);
                mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
                AssetDatabase.CreateAsset(mat, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                created = true;
            }

            return ReferenceOf(mat, 2);
        }

        // ---------------------------------------------------------------- files and references

        private static List<string> ScopePaths()
        {
            var paths = new List<string>();
            foreach (string file in Directory.GetFiles(Full(UiPrefabPaths.Root), "*.prefab", SearchOption.AllDirectories))
                paths.Add(ToAssetPath(file));
            paths.Sort(StringComparer.Ordinal);
            if (File.Exists(Full(HomeScene))) paths.Add(HomeScene);
            if (File.Exists(Full(RaceScene))) paths.Add(RaceScene);
            return paths;
        }

        private static string ReferenceOf(UnityEngine.Object asset, int type)
        {
            if (asset == null) throw new InvalidOperationException("ReferenceOf: asset is null.");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long fileId))
                throw new InvalidOperationException($"No GUID for '{asset.name}' (not saved as an asset?).");
            return $"{{fileID: {fileId}, guid: {guid}, type: {type}}}";
        }

        private static string GuidFromMeta(string assetPath)
        {
            string meta = Full(assetPath) + ".meta";
            if (!File.Exists(meta)) return null;
            Match m = Regex.Match(File.ReadAllText(meta), @"^guid: ([0-9a-f]{32})", RegexOptions.Multiline);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Full(string assetPath) => UiBuildKit.FullPath(assetPath);

        private static string ToAssetPath(string fullPath)
        {
            string root = Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? string.Empty);
            string relative = Path.GetFullPath(fullPath).Substring(root.Length).TrimStart('\\', '/');
            return relative.Replace('\\', '/');
        }
    }
}
