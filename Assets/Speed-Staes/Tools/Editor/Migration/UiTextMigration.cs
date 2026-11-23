using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.Tools.Migration
{
    public static class UiTextMigration
    {
        private const string TextHeader = "  m_Script: {fileID: 11500000, guid: " + MigrationGuids.TextMeshProUgui + ", type: 3}";
        private static readonly Regex ObjectHeader = new Regex(@"(?m)^--- !u!(?<type>\d+) &(?<id>-?\d+)(?: stripped)?\n");
        private static readonly Regex GuidPattern = new Regex(@"guid: ([a-f0-9]{32})");
        private static readonly Regex FileIdPattern = new Regex(@"fileID: (-?\d+)");
        private static readonly Regex Override = new Regex(@"(?m)(    - target: \{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: \d+\}\n      propertyPath: )(\S+)(\n      value: [^\n]*)");
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Speed Stars/UI Text Migration/Preview all")]
        public static void Preview() => Run(false);

        [MenuItem("Tools/Speed Stars/UI Text Migration/Convert all")]
        public static void ConvertAll() => Run(true);

        private static void Run(bool apply)
        {
            if (apply && (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(index => SceneManager.GetSceneAt(index).isDirty)))
                throw new InvalidOperationException("Stop Play mode and save open scenes or close the prefab stage before converting.");

            var sources = AssetDatabase.GetAllAssetPaths()
                .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal) &&
                    (path.EndsWith(".prefab", StringComparison.Ordinal) || path.EndsWith(".unity", StringComparison.Ordinal)))
                .Where(File.Exists)
                .ToDictionary(path => path, path => File.ReadAllText(path));
            var targets = new HashSet<string>();
            var edits = new Dictionary<string, string>();
            int converted = 0;
            foreach (var source in sources)
            {
                if (!source.Value.Contains(TextHeader)) continue;
                string yaml = source.Value.Replace("\r\n", "\n");
                Match[] documents = ObjectHeader.Matches(yaml).Cast<Match>().ToArray();
                var replacements = new Dictionary<long, string>();
                var outlines = new Dictionary<long, List<(long Id, string Yaml)>>();
                var ids = new HashSet<long>(documents.Select(document => Id(document.Groups["id"].Value)));
                long nextId = 7000000000000000000L;
                for (int index = 0; index < documents.Length; index++)
                {
                    Match header = documents[index];
                    if (header.Groups["type"].Value != "114") continue;
                    string body = Body(yaml, documents, index);
                    if (!body.Contains(TextHeader)) continue;
                    long id = Id(header.Groups["id"].Value);
                    bool stripped = header.Value.EndsWith(" stripped\n", StringComparison.Ordinal);
                    replacements.Add(id, stripped ? body.Replace(MigrationGuids.TextMeshProUgui, MigrationGuids.LegacyText) : ConvertText(body));
                    targets.Add(AssetDatabase.AssetPathToGUID(source.Key) + ":" + id);
                    converted++;

                    if (stripped) continue;
                    Material material = ResolveAsset<Material>(Field(body, "m_sharedMaterial"));
                    if (!material || !material.HasProperty("_OutlineWidth") || !material.HasProperty("_OutlineColor")) continue;
                    float width = material.GetFloat("_OutlineWidth");
                    Color color = material.GetColor("_OutlineColor");
                    if (width <= 0f || color.a <= 0f) continue;
                    long owner = Id(FileIdPattern.Match(Field(body, "m_GameObject")).Groups[1].Value);
                    while (!ids.Add(nextId)) nextId++;
                    if (!outlines.TryGetValue(owner, out var effects)) outlines.Add(owner, effects = new List<(long, string)>());
                    float thickness = width * Number(body, "m_fontSize") * 0.08f;
                    effects.Add((nextId, OutlineYaml(nextId, owner, thickness, color, Field(body, "m_Enabled"))));
                }

                var result = new StringBuilder(yaml.Substring(0, documents[0].Index));
                for (int index = 0; index < documents.Length; index++)
                {
                    Match header = documents[index];
                    long id = Id(header.Groups["id"].Value);
                    result.Append(header.Value);
                    string body = replacements.TryGetValue(id, out string replacement) ? replacement : Body(yaml, documents, index);
                    if (header.Groups["type"].Value == "1" && outlines.TryGetValue(id, out var effects))
                    {
                        int insertAt = body.IndexOf("  m_Component:\n", StringComparison.Ordinal) + "  m_Component:\n".Length;
                        body = body.Insert(insertAt, string.Concat(effects.Select(effect => $"  - component: {{fileID: {effect.Id}}}\n")));
                    }

                    result.Append(body);
                }
                foreach (var effects in outlines.Values)
                    foreach (var effect in effects) result.Append(effect.Yaml);
                edits[source.Key] = result.ToString();
            }

            int overrides = 0;
            foreach (var source in sources)
            {
                string yaml = edits.TryGetValue(source.Key, out string convertedYaml) ? convertedYaml : source.Value.Replace("\r\n", "\n");
                string result = Override.Replace(yaml, match =>
                {
                    if (!targets.Contains(match.Groups[3].Value + ":" + match.Groups[2].Value)) return match.Value;
                    string property = match.Groups[4].Value;
                    string mapped = property switch
                    {
                        "m_text" => "m_Text",
                        "m_fontSize" => "m_FontData.m_FontSize",
                        "m_fontSizeMin" => "m_FontData.m_MinSize",
                        "m_fontSizeMax" => "m_FontData.m_MaxSize",
                        "m_enableAutoSizing" => "m_FontData.m_BestFit",
                        "m_isRichText" => "m_FontData.m_RichText",
                        _ => property
                    };
                    if (mapped == property) return match.Value;
                    overrides++;
                    return match.Groups[1].Value + mapped + match.Groups[5].Value;
                });
                if (result != yaml || convertedYaml != null)
                    edits[source.Key] = source.Value.Contains("\r\n") ? result.Replace("\n", "\r\n") : result;
            }

            if (apply && edits.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var edit in edits) File.WriteAllText(edit.Key, edit.Value, new UTF8Encoding(false));
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            Debug.Log($"UI_TEXT_MIGRATION {(apply ? "applied" : "preview")} components={converted}, assets={edits.Count}, overrides={overrides}\n" +
                string.Join("\n", edits.Keys.OrderBy(path => path)));
        }

        private static string ConvertText(string body)
        {
            string header = body.Substring(0, body.IndexOf("  m_text:", StringComparison.Ordinal))
                .Replace(MigrationGuids.TextMeshProUgui, MigrationGuids.LegacyText);
            string color = Field(body, "m_fontColor");
            if (!string.IsNullOrEmpty(color))
                header = Regex.Replace(header, @"(?m)^  m_Color: [^\n]+", "  m_Color: " + color);

            TMP_FontAsset fontAsset = ResolveAsset<TMP_FontAsset>(Field(body, "m_fontAsset")) ?? TMP_Settings.defaultFontAsset;
            string fontGuid = fontAsset == null ? "" : new SerializedObject(fontAsset).FindProperty("m_SourceFontFileGUID")?.stringValue;
            string fontPath = AssetDatabase.GUIDToAssetPath(fontGuid ?? "");
            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (!font || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font, out fontGuid, out long fontId))
                throw new InvalidOperationException($"No source font for {fontAsset?.name}. Import its TTF or OTF before converting.");

            int horizontal = (int)Number(body, "m_HorizontalAlignment");
            int vertical = (int)Number(body, "m_VerticalAlignment");
            int encoded = (int)Number(body, "m_textAlignment");
            if (encoded > 0 && encoded != 65535)
            {
                horizontal = encoded & 255;
                vertical = encoded & ~255;
            }
            int alignment = (vertical == 256 ? 0 : vertical == 1024 ? 6 : 3) + (horizontal == 1 ? 0 : horizontal == 4 ? 2 : 1);
            int fontSize = Mathf.Max(1, Mathf.RoundToInt(Number(body, "m_fontSize")));
            int bestFit = (int)Number(body, "m_enableAutoSizing");
            int min = Mathf.Clamp(Mathf.RoundToInt(Number(body, "m_fontSizeMin")), 1, fontSize);
            int max = Mathf.Max(min, Mathf.RoundToInt(Number(body, "m_fontSizeMax")));
            string text = Field(body, "m_text");
            text = Regex.Replace(text, @"<size=(\d+)%>", match =>
                $"<size={Mathf.Max(1, Mathf.RoundToInt(fontSize * int.Parse(match.Groups[1].Value, Invariant) / 100f))}>");
            return header +
                "  m_FontData:\n" +
                $"    m_Font: {{fileID: {fontId}, guid: {fontGuid}, type: 3}}\n" +
                $"    m_FontSize: {fontSize}\n" +
                $"    m_FontStyle: {(int)Number(body, "m_fontStyle") & 3}\n" +
                $"    m_BestFit: {bestFit}\n    m_MinSize: {min}\n    m_MaxSize: {max}\n" +
                $"    m_Alignment: {alignment}\n    m_AlignByGeometry: {(horizontal == 32 || vertical == 4096 ? 1 : 0)}\n" +
                $"    m_RichText: {Field(body, "m_isRichText")}\n" +
                $"    m_HorizontalOverflow: {(Field(body, "m_enableWordWrapping") == "1" ? 0 : 1)}\n" +
                $"    m_VerticalOverflow: {(Field(body, "m_overflowMode") == "0" ? 1 : 0)}\n" +
                "    m_LineSpacing: 1\n" +
                (text.Length == 0 ? "  m_Text:\n" : $"  m_Text: {text}\n");
        }

        private static string OutlineYaml(long id, long owner, float thickness, Color color, string enabled)
        {
            string rgba = FormattableString.Invariant($"{{r: {color.r:R}, g: {color.g:R}, b: {color.b:R}, a: {color.a:R}}}");
            return $"--- !u!114 &{id}\nMonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n" +
                $"  m_GameObject: {{fileID: {owner}}}\n  m_Enabled: {enabled}\n  m_EditorHideFlags: 0\n" +
                $"  m_Script: {{fileID: 11500000, guid: {MigrationGuids.ThickOutline}, type: 3}}\n  m_Name:\n  m_EditorClassIdentifier:\n" +
                $"  thickness: {thickness.ToString("R", Invariant)}\n  directions: 16\n  effectColor: {rgba}\n";
        }

        private static string Body(string yaml, Match[] documents, int index)
        {
            int start = documents[index].Index + documents[index].Length;
            int end = index + 1 < documents.Length ? documents[index + 1].Index : yaml.Length;
            return yaml.Substring(start, end - start);
        }

        private static T ResolveAsset<T>(string reference) where T : UnityEngine.Object
        {
            string path = AssetDatabase.GUIDToAssetPath(GuidPattern.Match(reference).Groups[1].Value);
            if (string.IsNullOrEmpty(path)) return null;
            long id = Id(FileIdPattern.Match(reference).Groups[1].Value);
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<T>().FirstOrDefault(asset =>
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long localId) && localId == id);
        }

        private static string Field(string yaml, string name) =>
            Regex.Match(yaml, @"(?m)^  " + name + @": ?([^\n]*(?:\n(?: {3,}[^\n]*))*?)$(?=\n(?! {3,})|\z)").Groups[1].Value;
        private static long Id(string value) => long.TryParse(value, NumberStyles.Integer, Invariant, out long id) ? id : 0;
        private static float Number(string yaml, string name) =>
            float.TryParse(Field(yaml, name), NumberStyles.Float, Invariant, out float number) ? number : 0f;
    }
}
