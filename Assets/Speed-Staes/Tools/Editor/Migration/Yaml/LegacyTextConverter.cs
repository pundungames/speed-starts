using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SpeedStars.Tools.Migration
{
    /// <summary>
    /// Turns every legacy UnityEngine.UI.Text of a Unity YAML file into a TextMeshProUGUI IN PLACE: same document, same fileID,
    /// same GameObject, so every reference to the component (view fields, nested-prefab targets) survives. It works on the text
    /// of the file because loading the prefab first would null view fields whose type already changed to TMP_Text (T-101).
    /// Pure: no Unity API, so EditMode tests feed it YAML snippets. Re-running on its own output changes nothing.
    /// </summary>
    public static class LegacyTextConverter
    {
        // ---- Property mapping, legacy Text -> TextMeshProUGUI (the one readable table; code below follows it) ----
        // m_Text                      -> m_text                      (raw copy, quoting and line breaks kept)
        // m_Color                     -> m_Color + m_fontColor + m_fontColor32 + m_faceColor
        // m_FontData.m_Font           -> m_fontAsset (American Captain SDF) + m_sharedMaterial (its material, or an outline preset)
        // m_FontData.m_FontSize       -> m_fontSize + m_fontSizeBase
        // m_FontData.m_FontStyle      -> m_fontStyle 0 (Normal: the font is already heavy and upright)
        // m_FontData.m_BestFit        -> m_enableAutoSizing
        // m_FontData.m_MinSize/MaxSize-> m_fontSizeMin / m_fontSizeMax
        // m_FontData.m_Alignment (9)  -> m_HorizontalAlignment (1,2,4) + m_VerticalAlignment (0x100,0x200,0x400) + m_textAlignment 65535
        // m_FontData.m_RichText       -> m_isRichText
        // m_FontData.m_HorizontalOverflow (Wrap 0 / Overflow 1) -> m_enableWordWrapping (wrap = on)
        // m_FontData.m_VerticalOverflow (Truncate 0 / Overflow 1) -> m_overflowMode (Truncate 3 only when wrapping, else Overflow 0)
        // m_FontData.m_LineSpacing    -> m_lineSpacing = (x - 1) * 100 (legacy is a factor, TMP a percent)
        // m_RaycastTarget, m_RaycastPadding, m_Maskable, m_Material (reset to none), m_OnCullStateChanged, m_Name -> same name, copied
        // Outline / ThickOutline on the same GameObject -> component removed, look moves into the material preset (OutlineSpec)

        private const string Indent = "  ";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Regex ColorPattern = new Regex(
            @"r: (?<r>[-+0-9.eE]+), g: (?<g>[-+0-9.eE]+), b: (?<b>[-+0-9.eE]+), a: (?<a>[-+0-9.eE]+)", RegexOptions.Compiled);
        private static readonly Regex VectorPattern = new Regex(@"x: (?<x>[-+0-9.eE]+), y: (?<y>[-+0-9.eE]+)", RegexOptions.Compiled);
        private static readonly Regex TargetPattern = new Regex(@"target: \{fileID: (-?\d+), guid: ([0-9a-fA-F]{32})", RegexOptions.Compiled);
        private static readonly Regex PlainRefPattern = new Regex(@"^(\s*)([A-Za-z_][A-Za-z0-9_]*): \{fileID: (-?\d+)\}\s*$", RegexOptions.Compiled);
        private static readonly Regex ComponentPattern = new Regex(@"^  - component: \{fileID: (-?\d+)\}\s*$", RegexOptions.Compiled);

        private sealed class TextPlan
        {
            public UnityYamlDoc Doc;
            public long GoId;
            public bool Owned;
            public readonly List<UnityYamlDoc> Outlines = new List<UnityYamlDoc>();
        }

        private sealed class Plan
        {
            public readonly Dictionary<long, UnityYamlDoc> GameObjects = new Dictionary<long, UnityYamlDoc>();
            public readonly List<TextPlan> Legacy = new List<TextPlan>();
            public readonly List<long> TmpIds = new List<long>();
        }

        /// <summary>Pass 1: which text components the file has and which outline looks its convertible texts need.</summary>
        public static TextAnalysis Analyze(string yaml)
        {
            UnityYamlFile file = UnityYamlFile.Parse(yaml);
            Plan plan = BuildPlan(file);
            var result = new TextAnalysis();
            result.TextIds.AddRange(plan.TmpIds);
            foreach (TextPlan tp in plan.Legacy)
            {
                if (tp.Owned) continue;
                result.LegacyCount++;
                result.TextIds.Add(tp.Doc.FileId);
                if (TryOutline(tp, out OutlineSpec spec) && !result.Outlines.Contains(spec)) result.Outlines.Add(spec);
            }

            return result;
        }

        /// <summary>Pass 2: the converted file text. When nothing needs changing, <see cref="ConversionResult.Text"/> is the input itself.</summary>
        public static ConversionResult Convert(string yaml, TextConversionContext ctx)
        {
            UnityYamlFile file = UnityYamlFile.Parse(yaml);
            Plan plan = BuildPlan(file);
            var res = new ConversionResult();
            var removedIds = new HashSet<long>();
            bool changed = false;

            foreach (TextPlan tp in plan.Legacy)
            {
                if (tp.Owned)
                {
                    changed |= SetInputFieldFont(tp.Doc, ctx, res);
                    res.RemainingLegacy.Add($"{NameOf(plan, tp.GoId)} ({tp.Doc.FileId})");
                    continue;
                }

                // Outline components first: the look decides which material the converted text gets.
                string materialRef = ctx.DefaultMaterialRef;
                if (TryOutline(tp, out OutlineSpec spec)) materialRef = ctx.PresetMaterialRef(spec);
                foreach (UnityYamlDoc outline in tp.Outlines)
                {
                    outline.Removed = true;
                    removedIds.Add(outline.FileId);
                    res.OutlinesRemoved++;
                    if (plan.GameObjects.TryGetValue(tp.GoId, out UnityYamlDoc go)) RemoveComponent(go, outline.FileId);
                }

                ConvertTextDoc(tp.Doc, ctx, materialRef, NameOf(plan, tp.GoId), res);
                res.Converted++;
                changed = true;
            }

            if (removedIds.Count > 0)
            {
                changed = true;
                ClearDanglingRefs(file, removedIds, res);
            }

            foreach (UnityYamlDoc doc in file.Docs)
                if (doc.ClassId == 1001 && !doc.Removed) changed |= TranslateOverrides(doc, ctx, res);

            res.Changed = changed;
            res.Text = changed ? file.Write() : yaml;
            return res;
        }

        /// <summary>
        /// New propertyPath for a nested-prefab override on a legacy Text that became TMP. Null = the property has no TMP
        /// counterpart (the override is dropped); the same string = not a text property.
        /// </summary>
        public static string TranslateOverridePath(string path, out bool alsoSizeBase)
        {
            alsoSizeBase = false;
            if (path == "m_Text") return "m_text";
            if (path == "m_Color" || path.StartsWith("m_Color.", StringComparison.Ordinal)) return "m_fontColor" + path.Substring("m_Color".Length);
            if (!path.StartsWith("m_FontData.", StringComparison.Ordinal)) return path;

            switch (path.Substring("m_FontData.".Length))
            {
                case "m_FontSize": alsoSizeBase = true; return "m_fontSize";
                case "m_BestFit": return "m_enableAutoSizing";
                case "m_MinSize": return "m_fontSizeMin";
                case "m_MaxSize": return "m_fontSizeMax";
                case "m_RichText": return "m_isRichText";
                default: return null; // font, style, alignment, overflow, spacing: one legacy field maps to several TMP fields or none
            }
        }

        // ---------------------------------------------------------------- plan

        private static Plan BuildPlan(UnityYamlFile file)
        {
            var plan = new Plan();
            var owned = new HashSet<long>();
            var outlines = new List<(UnityYamlDoc doc, long goId)>();

            foreach (UnityYamlDoc doc in file.Docs)
            {
                if (doc.Stripped) continue;
                if (doc.ClassId == 1) plan.GameObjects[doc.FileId] = doc;
                if (doc.ClassId != 114) continue;

                long textComponent = doc.GetRef("m_TextComponent");
                long placeholder = doc.GetRef("m_Placeholder");
                if (textComponent != 0) owned.Add(textComponent);
                if (placeholder != 0) owned.Add(placeholder);

                switch (doc.ScriptGuid())
                {
                    case MigrationGuids.LegacyText:
                        plan.Legacy.Add(new TextPlan { Doc = doc, GoId = doc.GetRef("m_GameObject") });
                        break;
                    case MigrationGuids.TextMeshProUgui:
                        plan.TmpIds.Add(doc.FileId);
                        break;
                    case MigrationGuids.Outline:
                    case MigrationGuids.ThickOutline:
                        outlines.Add((doc, doc.GetRef("m_GameObject")));
                        break;
                }
            }

            foreach (TextPlan tp in plan.Legacy)
            {
                tp.Owned = owned.Contains(tp.Doc.FileId);
                if (tp.Owned) continue;
                foreach ((UnityYamlDoc doc, long goId) in outlines)
                    if (goId == tp.GoId) tp.Outlines.Add(doc);
            }

            return plan;
        }

        private static bool TryOutline(TextPlan tp, out OutlineSpec spec)
        {
            spec = default;
            double size = ParseDouble(tp.Doc.GetNested("m_FontSize"), 14);
            if (tp.Doc.GetNested("m_BestFit") == "1") size = ParseDouble(tp.Doc.GetNested("m_MaxSize"), size);

            foreach (UnityYamlDoc outline in tp.Outlines)
            {
                if (outline.Get("m_Enabled") == "0") continue; // a disabled outline draws nothing: removed, no preset

                bool thick = outline.ScriptGuid() == MigrationGuids.ThickOutline;
                if (!TryColor(outline.Get(thick ? "effectColor" : "m_EffectColor"), out double[] c)) continue;

                double distance;
                if (thick) distance = ParseDouble(outline.Get("thickness"), 0);
                else
                {
                    Match v = VectorPattern.Match(outline.Get("m_EffectDistance") ?? string.Empty);
                    distance = v.Success
                        ? Math.Max(Math.Abs(ParseDouble(v.Groups["x"].Value, 0)), Math.Abs(ParseDouble(v.Groups["y"].Value, 0)))
                        : 1;
                }

                if (distance <= 0) continue;
                spec = OutlineSpec.FromLegacy(c[0], c[1], c[2], c[3], distance, size);
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------- text doc

        private static void ConvertTextDoc(UnityYamlDoc doc, TextConversionContext ctx, string materialRef, string goName, ConversionResult res)
        {
            string colorRaw = doc.Get("m_Color") ?? "{r: 1, g: 1, b: 1, a: 1}";
            if (!TryColor(colorRaw, out double[] color)) color = new double[] { 1, 1, 1, 1 };

            string size = doc.GetNested("m_FontSize") ?? "14";
            string minSize = doc.GetNested("m_MinSize") ?? "10";
            string maxSize = doc.GetNested("m_MaxSize") ?? size;
            string bestFit = doc.GetNested("m_BestFit") == "1" ? "1" : "0";
            int anchor = (int)ParseDouble(doc.GetNested("m_Alignment"), 0);
            string richText = doc.GetNested("m_RichText") == "0" ? "0" : "1";
            bool wrap = doc.GetNested("m_HorizontalOverflow") == "0";
            bool verticalTruncate = doc.GetNested("m_VerticalOverflow") == "0";
            double lineSpacing = (ParseDouble(doc.GetNested("m_LineSpacing"), 1) - 1.0) * 100.0;
            uint rgba = Rgba32(color);

            // Split the body (everything after m_Script) into top-level key groups.
            int scriptIndex = doc.Lines.FindIndex(l => l.StartsWith("  m_Script:", StringComparison.Ordinal));
            var groups = new List<List<string>>();
            for (int i = scriptIndex + 1; i < doc.Lines.Count; i++)
            {
                string line = doc.Lines[i];
                bool startsGroup = line.Length > 2 && line.StartsWith(Indent, StringComparison.Ordinal) && line[2] != ' ';
                if (startsGroup || groups.Count == 0) groups.Add(new List<string>());
                groups[groups.Count - 1].Add(line);
            }

            var output = new List<string>();
            for (int i = 0; i < scriptIndex; i++) output.Add(doc.Lines[i]);
            output.Add($"  m_Script: {{fileID: 11500000, guid: {MigrationGuids.TextMeshProUgui}, type: 3}}");

            List<string> textGroup = null;
            foreach (List<string> group in groups)
            {
                string first = group[0];
                if (first.StartsWith("  m_FontData:", StringComparison.Ordinal)) continue;
                if (first.StartsWith("  m_Text:", StringComparison.Ordinal)) { textGroup = group; continue; }
                if (first.StartsWith("  m_Material:", StringComparison.Ordinal) && !first.Trim().EndsWith("{fileID: 0}", StringComparison.Ordinal))
                {
                    res.Warnings.Add($"{goName}: custom m_Material dropped (TMP uses the font material)");
                    output.Add("  m_Material: {fileID: 0}");
                    continue;
                }

                output.AddRange(group);
            }

            if (textGroup == null) textGroup = new List<string> { "  m_Text: " };
            output.Add("  m_text:" + textGroup[0].Substring("  m_Text:".Length));
            for (int i = 1; i < textGroup.Count; i++) output.Add(textGroup[i]);

            output.Add("  m_isRightToLeft: 0");
            output.Add("  m_fontAsset: " + ctx.FontAssetRef);
            output.Add("  m_sharedMaterial: " + materialRef);
            output.Add("  m_fontSharedMaterials: []");
            output.Add("  m_fontMaterial: {fileID: 0}");
            output.Add("  m_fontMaterials: []");
            output.Add("  m_fontColor32:");
            output.Add("    serializedVersion: 2");
            output.Add("    rgba: " + rgba.ToString(Inv));
            output.Add("  m_fontColor: " + colorRaw);
            output.Add("  m_enableVertexGradient: 0");
            output.Add("  m_colorMode: 3");
            output.Add("  m_fontColorGradient:");
            foreach (string corner in new[] { "topLeft", "topRight", "bottomLeft", "bottomRight" })
                output.Add($"    {corner}: {{r: 1, g: 1, b: 1, a: 1}}");
            output.Add("  m_fontColorGradientPreset: {fileID: 0}");
            output.Add("  m_spriteAsset: {fileID: 0}");
            output.Add("  m_tintAllSprites: 0");
            output.Add("  m_StyleSheet: {fileID: 0}");
            output.Add("  m_TextStyleHashCode: -1183493901");
            output.Add("  m_overrideHtmlColors: 0");
            output.Add("  m_faceColor:");
            output.Add("    serializedVersion: 2");
            output.Add("    rgba: " + rgba.ToString(Inv));
            output.Add("  m_fontSize: " + size);
            output.Add("  m_fontSizeBase: " + size);
            output.Add("  m_fontWeight: 400");
            output.Add("  m_enableAutoSizing: " + bestFit);
            output.Add("  m_fontSizeMin: " + minSize);
            output.Add("  m_fontSizeMax: " + maxSize);
            output.Add("  m_fontStyle: 0");
            output.Add("  m_HorizontalAlignment: " + HorizontalOf(anchor).ToString(Inv));
            output.Add("  m_VerticalAlignment: " + VerticalOf(anchor).ToString(Inv));
            output.Add("  m_textAlignment: 65535");
            output.Add("  m_characterSpacing: 0");
            output.Add("  m_wordSpacing: 0");
            output.Add("  m_lineSpacing: " + FormatNumber(lineSpacing));
            output.Add("  m_lineSpacingMax: 0");
            output.Add("  m_paragraphSpacing: 0");
            output.Add("  m_charWidthMaxAdj: 0");
            output.Add("  m_enableWordWrapping: " + (wrap ? "1" : "0"));
            output.Add("  m_wordWrappingRatios: 0.4");
            output.Add("  m_overflowMode: " + (wrap && verticalTruncate ? "3" : "0"));
            output.Add("  m_linkedTextComponent: {fileID: 0}");
            output.Add("  m_enableKerning: 1");
            output.Add("  m_enableExtraPadding: 0");
            output.Add("  checkPaddingRequired: 0");
            output.Add("  m_isRichText: " + richText);
            output.Add("  m_parseCtrlCharacters: 1");
            output.Add("  m_isOrthographic: 0");
            output.Add("  m_isCullingEnabled: 0");
            output.Add("  m_horizontalMapping: 0");
            output.Add("  m_verticalMapping: 0");
            output.Add("  m_uvLineOffset: 0");
            output.Add("  m_geometrySortingOrder: 0");
            output.Add("  m_IsTextObjectScaleStatic: 0");
            output.Add("  m_VertexBufferAutoSizeReduction: 0");
            output.Add("  m_useMaxVisibleDescender: 1");
            output.Add("  m_pageToDisplay: 1");
            output.Add("  m_margin: {x: 0, y: 0, z: 0, w: 0}");
            output.Add("  m_isUsingLegacyAnimationComponent: 0");
            output.Add("  m_isVolumetricText: 0");
            output.Add("  m_hasFontAssetChanged: 0");
            output.Add("  m_baseMaterial: {fileID: 0}");
            output.Add("  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}");

            doc.Lines.Clear();
            doc.Lines.AddRange(output);
        }

        private static int HorizontalOf(int anchor) => new[] { 1, 2, 4 }[Math.Max(0, Math.Min(8, anchor)) % 3];

        private static int VerticalOf(int anchor) => new[] { 0x100, 0x200, 0x400 }[Math.Max(0, Math.Min(8, anchor)) / 3];

        /// <summary>InputField texts stay legacy (CJK input falls back to the device font) but get the American Captain TTF.</summary>
        private static bool SetInputFieldFont(UnityYamlDoc doc, TextConversionContext ctx, ConversionResult res)
        {
            const string prefix = "    m_Font:";
            for (int i = 0; i < doc.Lines.Count; i++)
            {
                if (!doc.Lines[i].StartsWith(prefix, StringComparison.Ordinal)) continue;
                string next = prefix + " " + ctx.TtfFontRef;
                if (doc.Lines[i] == next) return false;
                doc.Lines[i] = next;
                res.InputFieldFonts++;
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------- outline removal, references

        private static void RemoveComponent(UnityYamlDoc go, long componentId)
        {
            go.Lines.RemoveAll(l =>
            {
                Match m = ComponentPattern.Match(l);
                return m.Success && long.Parse(m.Groups[1].Value) == componentId;
            });
        }

        /// <summary>A removed component leaves "key: {fileID: id}" fields behind (e.g. a view's wordOutline): they become none, not a dangling id.</summary>
        private static void ClearDanglingRefs(UnityYamlFile file, HashSet<long> removedIds, ConversionResult res)
        {
            foreach (UnityYamlDoc doc in file.Docs)
            {
                if (doc.Removed) continue;
                for (int i = 0; i < doc.Lines.Count; i++)
                {
                    Match m = PlainRefPattern.Match(doc.Lines[i]);
                    if (!m.Success || !removedIds.Contains(long.Parse(m.Groups[3].Value))) continue;
                    doc.Lines[i] = $"{m.Groups[1].Value}{m.Groups[2].Value}: {{fileID: 0}}";
                    res.DanglingRefsCleared++;
                    res.Warnings.Add($"reference '{m.Groups[2].Value}' to a removed outline component set to none");
                }
            }
        }

        // ---------------------------------------------------------------- nested prefab overrides

        private static bool TranslateOverrides(UnityYamlDoc doc, TextConversionContext ctx, ConversionResult res)
        {
            const string entryStart = "    - target: ";
            var output = new List<string>(doc.Lines.Count);
            bool changed = false;
            int i = 0;
            while (i < doc.Lines.Count)
            {
                string line = doc.Lines[i];
                if (!line.StartsWith(entryStart, StringComparison.Ordinal))
                {
                    output.Add(line);
                    i++;
                    continue;
                }

                int end = i + 1;
                while (end < doc.Lines.Count && doc.Lines[end].StartsWith("      ", StringComparison.Ordinal)) end++;
                List<string> entry = doc.Lines.GetRange(i, end - i);
                i = end;

                Match target = TargetPattern.Match(entry[0]);
                int pathIndex = entry.FindIndex(l => l.StartsWith("      propertyPath: ", StringComparison.Ordinal));
                if (!target.Success || pathIndex < 0 || !ctx.TextTargets.Contains(target.Groups[2].Value + ":" + target.Groups[1].Value))
                {
                    output.AddRange(entry);
                    continue;
                }

                string path = entry[pathIndex].Substring("      propertyPath: ".Length).Trim();
                string translated = TranslateOverridePath(path, out bool alsoSizeBase);
                if (translated == path)
                {
                    output.AddRange(entry);
                    continue;
                }

                changed = true;
                if (translated == null)
                {
                    res.OverridesDropped++;
                    res.Warnings.Add($"override '{path}' on a converted text has no TMP counterpart and was dropped");
                    continue;
                }

                res.OverridesTranslated++;
                entry[pathIndex] = "      propertyPath: " + translated;
                output.AddRange(entry);
                if (alsoSizeBase)
                {
                    var copy = new List<string>(entry);
                    copy[pathIndex] = "      propertyPath: m_fontSizeBase";
                    output.AddRange(copy);
                }
            }

            if (changed)
            {
                doc.Lines.Clear();
                doc.Lines.AddRange(output);
            }

            return changed;
        }

        // ---------------------------------------------------------------- small helpers

        private static string NameOf(Plan plan, long goId)
        {
            if (plan.GameObjects.TryGetValue(goId, out UnityYamlDoc go))
            {
                string name = go.Get("m_Name");
                if (!string.IsNullOrEmpty(name)) return name;
            }

            return "<unnamed>";
        }

        private static bool TryColor(string raw, out double[] rgba)
        {
            rgba = null;
            if (raw == null) return false;
            Match m = ColorPattern.Match(raw);
            if (!m.Success) return false;
            rgba = new[]
            {
                ParseDouble(m.Groups["r"].Value, 1), ParseDouble(m.Groups["g"].Value, 1),
                ParseDouble(m.Groups["b"].Value, 1), ParseDouble(m.Groups["a"].Value, 1)
            };
            return true;
        }

        private static uint Rgba32(double[] c)
        {
            uint Byte(double v) => (uint)Math.Round(Math.Min(1.0, Math.Max(0.0, v)) * 255.0);
            return Byte(c[0]) | (Byte(c[1]) << 8) | (Byte(c[2]) << 16) | (Byte(c[3]) << 24);
        }

        private static double ParseDouble(string s, double fallback) =>
            s != null && double.TryParse(s, NumberStyles.Float, Inv, out double v) ? v : fallback;

        private static string FormatNumber(double v)
        {
            string s = Math.Round(v, 3).ToString("0.###", Inv);
            return s == "-0" ? "0" : s;
        }
    }
}
