using System.Linq;
using NUnit.Framework;
using SpeedStars.Tools.Migration;

namespace SpeedStars.Tests.Migration
{
    /// <summary>The pure YAML transform of the legacy Text -> TextMeshProUGUI migration (T-100): YAML text in, YAML text out.</summary>
    public sealed class LegacyTextConverterTests
    {
        private const string Prefab = "b5485d39d186590498637aa4b1e95d1e";

        private const string Header = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n";

        private static readonly string TextDoc =
            "--- !u!114 &639748800371716377\n" +
            "MonoBehaviour:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  m_CorrespondingSourceObject: {fileID: 0}\n" +
            "  m_PrefabInstance: {fileID: 0}\n" +
            "  m_PrefabAsset: {fileID: 0}\n" +
            "  m_GameObject: {fileID: 100}\n" +
            "  m_Enabled: 1\n" +
            "  m_EditorHideFlags: 0\n" +
            "  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}\n" +
            "  m_Name: \n" +
            "  m_EditorClassIdentifier: \n" +
            "  m_Material: {fileID: 0}\n" +
            "  m_Color: {r: 0.024, g: 0.102, b: 0.255, a: 1}\n" +
            "  m_RaycastTarget: 0\n" +
            "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}\n" +
            "  m_Maskable: 1\n" +
            "  m_OnCullStateChanged:\n" +
            "    m_PersistentCalls:\n" +
            "      m_Calls: []\n" +
            "  m_FontData:\n" +
            "    m_Font: {fileID: 12800000, guid: 84d6a383ffca61a41b1823438eec1bdb, type: 3}\n" +
            "    m_FontSize: 183\n" +
            "    m_FontStyle: 1\n" +
            "    m_BestFit: 1\n" +
            "    m_MinSize: 73\n" +
            "    m_MaxSize: 190\n" +
            "    m_Alignment: 5\n" +
            "    m_AlignByGeometry: 0\n" +
            "    m_RichText: 1\n" +
            "    m_HorizontalOverflow: 0\n" +
            "    m_VerticalOverflow: 1\n" +
            "    m_LineSpacing: 1.2\n" +
            "  m_Text: 'Height: 5''9'\n";

        private const string GameObjectWithOutline =
            "--- !u!1 &100\n" +
            "GameObject:\n" +
            "  m_ObjectHideFlags: 0\n" +
            "  serializedVersion: 6\n" +
            "  m_Component:\n" +
            "  - component: {fileID: 101}\n" +
            "  - component: {fileID: 639748800371716377}\n" +
            "  - component: {fileID: 777}\n" +
            "  m_Layer: 5\n" +
            "  m_Name: Word\n";

        private const string OutlineDoc =
            "--- !u!114 &777\n" +
            "MonoBehaviour:\n" +
            "  m_GameObject: {fileID: 100}\n" +
            "  m_Enabled: 1\n" +
            "  m_Script: {fileID: 11500000, guid: e19747de3f5aca642ab2be37e372fb86, type: 3}\n" +
            "  m_EffectColor: {r: 0.9, g: 0.16, b: 0.16, a: 1}\n" +
            "  m_EffectDistance: {x: 6, y: -6}\n" +
            "  m_UseGraphicAlpha: 1\n";

        private static TextConversionContext Context() => new TextConversionContext
        {
            FontAssetRef = "{fileID: 11400000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 2}",
            DefaultMaterialRef = "{fileID: 2100000, guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, type: 2}",
            TtfFontRef = "{fileID: 12800000, guid: cccccccccccccccccccccccccccccccc, type: 3}",
            PresetMaterialRef = spec => "{fileID: 2100000, guid: dddddddddddddddddddddddddddddddd, type: 2}"
        };

        [Test]
        public void Text_BecomesTmp_InTheSameDocumentWithTheSameFileId()
        {
            ConversionResult r = LegacyTextConverter.Convert(Header + TextDoc, Context());

            Assert.IsTrue(r.Changed);
            Assert.AreEqual(1, r.Converted);
            StringAssert.Contains("--- !u!114 &639748800371716377\n", r.Text);
            StringAssert.Contains("m_GameObject: {fileID: 100}", r.Text);
            StringAssert.Contains("m_Enabled: 1", r.Text);
            StringAssert.Contains("guid: " + MigrationGuids.TextMeshProUgui, r.Text);
            StringAssert.DoesNotContain(MigrationGuids.LegacyText, r.Text);
            StringAssert.DoesNotContain("m_FontData", r.Text);
        }

        [Test]
        public void Text_PropertiesAreMapped()
        {
            string t = LegacyTextConverter.Convert(Header + TextDoc, Context()).Text;

            StringAssert.Contains("  m_text: 'Height: 5''9'\n", t);
            StringAssert.Contains("  m_Color: {r: 0.024, g: 0.102, b: 0.255, a: 1}\n", t);
            StringAssert.Contains("  m_fontColor: {r: 0.024, g: 0.102, b: 0.255, a: 1}\n", t);
            StringAssert.Contains("  m_RaycastTarget: 0\n", t);
            StringAssert.Contains("  m_fontAsset: {fileID: 11400000, guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 2}\n", t);
            StringAssert.Contains("  m_sharedMaterial: {fileID: 2100000, guid: bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb, type: 2}\n", t);
            StringAssert.Contains("  m_fontSize: 183\n", t);
            StringAssert.Contains("  m_enableAutoSizing: 1\n", t);
            StringAssert.Contains("  m_fontSizeMin: 73\n", t);
            StringAssert.Contains("  m_fontSizeMax: 190\n", t);
            StringAssert.Contains("  m_fontStyle: 0\n", t); // legacy Bold -> Normal
            StringAssert.Contains("  m_HorizontalAlignment: 4\n", t); // anchor 5 = MiddleRight
            StringAssert.Contains("  m_VerticalAlignment: 512\n", t);
            StringAssert.Contains("  m_enableWordWrapping: 1\n", t);
            StringAssert.Contains("  m_lineSpacing: 20\n", t);
        }

        [Test]
        public void Anchors_MapToHorizontalAndVertical()
        {
            (int anchor, string h, string v)[] cases = { (0, "1", "256"), (1, "2", "256"), (3, "1", "512"), (4, "2", "512"), (8, "4", "1024") };
            foreach ((int anchor, string h, string v) in cases)
            {
                string yaml = Header + TextDoc.Replace("m_Alignment: 5", "m_Alignment: " + anchor);
                string t = LegacyTextConverter.Convert(yaml, Context()).Text;
                StringAssert.Contains("  m_HorizontalAlignment: " + h + "\n", t, "anchor " + anchor);
                StringAssert.Contains("  m_VerticalAlignment: " + v + "\n", t, "anchor " + anchor);
            }
        }

        [Test]
        public void MultiLineText_KeepsItsContinuationLines()
        {
            string yaml = Header + TextDoc.Replace("  m_Text: 'Height: 5''9'\n", "  m_Text: \"two\\nlines\"\n");
            StringAssert.Contains("  m_text: \"two\\nlines\"\n", LegacyTextConverter.Convert(yaml, Context()).Text);
        }

        [Test]
        public void Outline_IsRemoved_FromTheDocumentAndTheComponentList()
        {
            string yaml = Header + GameObjectWithOutline + TextDoc + OutlineDoc;
            ConversionResult r = LegacyTextConverter.Convert(yaml, Context());

            Assert.AreEqual(1, r.OutlinesRemoved);
            StringAssert.DoesNotContain("--- !u!114 &777", r.Text);
            StringAssert.DoesNotContain("fileID: 777}", r.Text);
            StringAssert.DoesNotContain(MigrationGuids.Outline, r.Text);
            StringAssert.Contains("  - component: {fileID: 101}\n", r.Text);
            StringAssert.Contains("  - component: {fileID: 639748800371716377}\n", r.Text); // the text keeps its slot
            StringAssert.Contains("dddddddddddddddddddddddddddddddd", r.Text); // outline look -> preset material
        }

        [Test]
        public void Outline_OnAnObjectWithoutText_IsLeftAlone()
        {
            string yaml = Header + GameObjectWithOutline.Replace("639748800371716377", "555") + OutlineDoc;
            ConversionResult r = LegacyTextConverter.Convert(yaml, Context());

            Assert.IsFalse(r.Changed);
            Assert.AreEqual(yaml, r.Text);
        }

        [Test]
        public void Outline_Preset_DependsOnColourAndWidthAndIsSharedWhenEqual()
        {
            string yaml = Header + GameObjectWithOutline + TextDoc + OutlineDoc;
            TextAnalysis a = LegacyTextConverter.Analyze(yaml);

            Assert.AreEqual(1, a.Outlines.Count);
            OutlineSpec spec = a.Outlines[0];
            Assert.AreEqual(0xE6, spec.R);
            Assert.Greater(spec.Width, 0f);
            Assert.LessOrEqual(spec.Width, OutlineSpec.MaxWidth);
            // 6 px on a 190 best-fit size is a thin outline; the same 6 px on a 30 size is wider.
            OutlineSpec small = OutlineSpec.FromLegacy(0.9, 0.16, 0.16, 1, 6, 30);
            OutlineSpec large = OutlineSpec.FromLegacy(0.9, 0.16, 0.16, 1, 6, 190);
            Assert.Greater(small.Width, large.Width);
            Assert.AreEqual(large, OutlineSpec.FromLegacy(0.9, 0.16, 0.16, 1, 6, 190));
        }

        [Test]
        public void DisabledOutline_IsRemoved_WithoutAPreset()
        {
            string yaml = Header + GameObjectWithOutline + TextDoc + OutlineDoc.Replace("m_Enabled: 1", "m_Enabled: 0");
            ConversionResult r = LegacyTextConverter.Convert(yaml, Context());

            Assert.AreEqual(1, r.OutlinesRemoved);
            StringAssert.DoesNotContain("dddddddddddddddddddddddddddddddd", r.Text);
            StringAssert.Contains("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", r.Text);
        }

        [Test]
        public void ThickOutline_ReferenceFromAView_BecomesNone()
        {
            const string view =
                "--- !u!114 &900\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee, type: 3}\n" +
                "  wordText: {fileID: 639748800371716377}\n  wordOutline: {fileID: 777}\n";
            string thick = OutlineDoc.Replace(MigrationGuids.Outline, MigrationGuids.ThickOutline)
                .Replace("m_EffectColor", "effectColor").Replace("m_EffectDistance: {x: 6, y: -6}", "thickness: 11.9262");
            ConversionResult r = LegacyTextConverter.Convert(Header + GameObjectWithOutline + TextDoc + thick + view, Context());

            StringAssert.Contains("  wordOutline: {fileID: 0}\n", r.Text);
            StringAssert.Contains("  wordText: {fileID: 639748800371716377}\n", r.Text); // the text reference survives
            Assert.AreEqual(1, r.DanglingRefsCleared);
        }

        [Test]
        public void InputFieldTexts_StayLegacy_OnlyTheirFontChanges()
        {
            const string field =
                "--- !u!114 &300\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: d199490a483bc9a4e9d5a08a4e1d18d0, type: 3}\n" +
                "  m_TextComponent: {fileID: 639748800371716377}\n  m_Placeholder: {fileID: 639748800371716377}\n";
            ConversionResult r = LegacyTextConverter.Convert(Header + TextDoc + field, Context());

            Assert.AreEqual(0, r.Converted);
            Assert.AreEqual(1, r.InputFieldFonts);
            StringAssert.Contains(MigrationGuids.LegacyText, r.Text);
            StringAssert.Contains("    m_Font: {fileID: 12800000, guid: cccccccccccccccccccccccccccccccc, type: 3}\n", r.Text);
            StringAssert.Contains("    m_FontSize: 183\n", r.Text);
            StringAssert.DoesNotContain(MigrationGuids.TextMeshProUgui, r.Text);
        }

        private static string PrefabInstance(string path, string value) =>
            "--- !u!1001 &1577679499367227738\n" +
            "PrefabInstance:\n" +
            "  m_Modification:\n" +
            "    m_Modifications:\n" +
            "    - target: {fileID: 6898960499942952424, guid: " + Prefab + ", type: 3}\n" +
            "      propertyPath: " + path + "\n" +
            "      value: " + value + "\n" +
            "      objectReference: {fileID: 0}\n" +
            "    - target: {fileID: 7059545578583710866, guid: " + Prefab + ", type: 3}\n" +
            "      propertyPath: m_Pivot.x\n" +
            "      value: 0\n" +
            "      objectReference: {fileID: 0}\n" +
            "    m_RemovedComponents: []\n" +
            "  m_SourcePrefab: {fileID: 100100000, guid: " + Prefab + ", type: 3}\n";

        [Test]
        public void NestedOverride_OnAConvertedText_GetsTheTmpPath()
        {
            TextConversionContext ctx = Context();
            ctx.TextTargets.Add(Prefab + ":6898960499942952424");
            string yaml = Header + PrefabInstance("m_Text", "Hurdle input sensitivity");

            ConversionResult r = LegacyTextConverter.Convert(yaml, ctx);

            Assert.AreEqual(1, r.OverridesTranslated);
            StringAssert.Contains("      propertyPath: m_text\n      value: Hurdle input sensitivity\n", r.Text);
            StringAssert.Contains("      propertyPath: m_Pivot.x\n", r.Text); // other targets untouched
            StringAssert.DoesNotContain("propertyPath: m_Text\n", r.Text);
        }

        [Test]
        public void NestedOverride_OnAnUnknownTarget_IsNotTouched()
        {
            string yaml = Header + PrefabInstance("m_Text", "x");
            ConversionResult r = LegacyTextConverter.Convert(yaml, Context());

            Assert.IsFalse(r.Changed);
            Assert.AreEqual(yaml, r.Text);
        }

        [TestCase("m_Text", "m_text")]
        [TestCase("m_Color.r", "m_fontColor.r")]
        [TestCase("m_FontData.m_FontSize", "m_fontSize")]
        [TestCase("m_FontData.m_BestFit", "m_enableAutoSizing")]
        [TestCase("m_RaycastTarget", "m_RaycastTarget")]
        [TestCase("m_FontData.m_Alignment", null)]
        public void OverridePath_Table(string legacy, string expected)
        {
            Assert.AreEqual(expected, LegacyTextConverter.TranslateOverridePath(legacy, out _));
        }

        [Test]
        public void FontSizeOverride_AlsoSetsTheBaseSize()
        {
            TextConversionContext ctx = Context();
            ctx.TextTargets.Add(Prefab + ":6898960499942952424");
            string t = LegacyTextConverter.Convert(Header + PrefabInstance("m_FontData.m_FontSize", "50"), ctx).Text;

            StringAssert.Contains("propertyPath: m_fontSize\n", t);
            StringAssert.Contains("propertyPath: m_fontSizeBase\n", t);
        }

        [Test]
        public void SecondRun_ChangesNothing()
        {
            TextConversionContext ctx = Context();
            ctx.TextTargets.Add(Prefab + ":6898960499942952424");
            const string field =
                "--- !u!114 &300\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: d199490a483bc9a4e9d5a08a4e1d18d0, type: 3}\n" +
                "  m_TextComponent: {fileID: 5}\n";
            string yaml = Header + GameObjectWithOutline + TextDoc + OutlineDoc + PrefabInstance("m_Text", "x") + field;

            string first = LegacyTextConverter.Convert(yaml, ctx).Text;
            ConversionResult second = LegacyTextConverter.Convert(first, ctx);

            Assert.IsFalse(second.Changed);
            Assert.AreEqual(first, second.Text);
        }

        [Test]
        public void Analyze_ListsTextsAndSkipsInputFieldOwned()
        {
            const string field =
                "--- !u!114 &300\nMonoBehaviour:\n  m_Script: {fileID: 11500000, guid: d199490a483bc9a4e9d5a08a4e1d18d0, type: 3}\n" +
                "  m_TextComponent: {fileID: 639748800371716377}\n";

            Assert.AreEqual(1, LegacyTextConverter.Analyze(Header + TextDoc).LegacyCount);
            TextAnalysis owned = LegacyTextConverter.Analyze(Header + TextDoc + field);
            Assert.AreEqual(0, owned.LegacyCount);
            Assert.IsFalse(owned.TextIds.Contains(639748800371716377));

            string converted = LegacyTextConverter.Convert(Header + TextDoc, Context()).Text;
            Assert.IsTrue(LegacyTextConverter.Analyze(converted).TextIds.Contains(639748800371716377)); // TMP texts stay targets
        }

        [Test]
        public void CrLfFiles_KeepTheirLineEndings_AndLinesWeDoNotTouchAreByteEqual()
        {
            string yaml = (Header + GameObjectWithOutline + TextDoc + OutlineDoc).Replace("\n", "\r\n");
            string t = LegacyTextConverter.Convert(yaml, Context()).Text;

            Assert.IsFalse(t.Replace("\r\n", string.Empty).Contains("\n"));
            StringAssert.Contains("--- !u!1 &100\r\nGameObject:\r\n", t);
            Assert.AreEqual(t, LegacyTextConverter.Convert(t, Context()).Text);
        }

        [Test]
        public void Output_HasNoDuplicateTopLevelKeys()
        {
            string t = LegacyTextConverter.Convert(Header + TextDoc, Context()).Text;
            string[] keys = t.Split('\n').Where(l => l.StartsWith("  ") && l.Length > 2 && l[2] != ' ' && l.Contains(":"))
                .Select(l => l.Substring(0, l.IndexOf(':'))).ToArray();
            Assert.AreEqual(keys.Length, keys.Distinct().Count());
        }
        [Test]
        public void StrippedProxy_OfAConvertedText_SwitchesScript_AndKeepsItsId()
        {
            TextConversionContext ctx = Context();
            ctx.TextTargets.Add(Prefab + ":6898960499942952424");
            const string proxy =
                "--- !u!114 &361633941944031407 stripped
MonoBehaviour:
" +
                "  m_CorrespondingSourceObject: {fileID: 6898960499942952424, guid: " + Prefab + ", type: 3}
" +
                "  m_PrefabInstance: {fileID: 6537749064677676359}
  m_PrefabAsset: {fileID: 0}
" +
                "  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
" +
                "  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
";
            const string view =
                "--- !u!114 &900
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee, type: 3}
" +
                "  label: {fileID: 361633941944031407}
";
            string yaml = Header + proxy + view;

            ConversionResult r = LegacyTextConverter.Convert(yaml, ctx);

            Assert.AreEqual(1, r.ProxiesSwitched);
            StringAssert.Contains("guid: " + MigrationGuids.TextMeshProUgui + ", type: 3}", r.Text);
            StringAssert.DoesNotContain(MigrationGuids.LegacyText, r.Text);
            StringAssert.Contains("--- !u!114 &361633941944031407 stripped
", r.Text);
            StringAssert.Contains("  label: {fileID: 361633941944031407}
", r.Text);
            Assert.IsFalse(LegacyTextConverter.Convert(r.Text, ctx).Changed);

            // A proxy of a text that is not a target (InputField-owned) stays legacy.
            Assert.IsFalse(LegacyTextConverter.Convert(yaml, Context()).Changed);
        }
    }
}
