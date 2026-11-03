using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SpeedStars.Tests.UI.Builders
{
    /// <summary>
    /// T-102: the user hand-edits UI prefabs, Home and Race, so no builder may write a prefab or scene file except through
    /// HandEditGuard. The builder sources are scanned as text (the test assembly cannot reference Assembly-CSharp-Editor);
    /// the keep / build rule is checked through reflection.
    /// </summary>
    public sealed class BuilderGuardTests
    {
        private const string BuildersFolder = "Assets/Speed-Staes/Tools/Editor/Builders";

        /// <summary>The guard itself, plus the one documented exception: the composition-root prefab, generated from config references only.</summary>
        private static readonly string[] Allowed = { "HandEditGuard.cs", "ProjectContextBuilder.cs" };

        private static readonly Regex RawWrite = new Regex(
            @"(?<!HandEditGuard\.)\b(SaveAsPrefabAsset\w*|SavePrefabAsset|SaveScene|SaveOpenScenes|SaveScenes|SaveAsScene\w*)\s*\(", RegexOptions.Compiled);

        [Test]
        public void BuilderSources_AreFound()
        {
            Assert.IsNotEmpty(BuilderFiles().ToArray(), "no builder .cs found under " + BuildersFolder);
        }

        [Test]
        public void NoBuilderWritesAPrefabOrSceneOutsideTheGuard()
        {
            var offenders = BuilderFiles()
                .Where(f => !Allowed.Contains(Path.GetFileName(f)))
                .SelectMany(f => File.ReadAllLines(f)
                    .Select((line, i) => (line, n: i + 1))
                    .Where(x => !x.line.TrimStart().StartsWith("//") && RawWrite.IsMatch(x.line))
                    .Select(x => $"{Path.GetFileName(f)}:{x.n}: {x.line.Trim()}"))
                .ToArray();
            Assert.IsEmpty(offenders, "raw prefab/scene saves outside HandEditGuard (use HandEditGuard.SaveNewPrefab / SavePatchedPrefab / SaveScene):\n"
                + string.Join("\n", offenders));
        }

        [Test]
        public void TheGuard_IsTheOnlyPlaceThatSavesAndItConvertsToTmp()
        {
            string guard = File.ReadAllText(BuilderFiles().Single(f => Path.GetFileName(f) == "HandEditGuard.cs"));
            StringAssert.Contains("ConvertPaths", guard, "saved files must be converted to TextMeshPro");
            StringAssert.Contains("SaveAsPrefabAsset", guard);
            StringAssert.Contains("SaveScene", guard);
        }

        [TestCase(false, false, false, true)]  // missing: always built
        [TestCase(false, true, true, true)]
        [TestCase(true, false, false, false)]  // exists, no overwrite: kept
        [TestCase(true, true, false, false)]   // exists, overwrite but no force flag: kept
        [TestCase(true, false, true, false)]   // force alone does not rebuild
        [TestCase(true, true, true, true)]     // exists, overwrite and force: rebuilt
        public void Decide_BuildsMissingAndKeepsExistingUnlessForced(bool exists, bool overwrite, bool force, bool expectBuild)
        {
            Type guard = FindEditorType("SpeedStars.Tools.Builders.HandEditGuard");
            MethodInfo decide = guard.GetMethod("Decide", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
            Assert.IsNotNull(decide, "HandEditGuard.Decide(bool,bool,bool) missing");
            object result = decide.Invoke(null, new object[] { exists, overwrite, force });
            Assert.AreEqual(expectBuild ? "Build" : "Keep", result.ToString());
        }

        private static string[] BuilderFiles()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", BuildersFolder));
            return Directory.Exists(root) ? Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories) : new string[0];
        }

        private static Type FindEditorType(string fullName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);
            Assert.IsNotNull(type, fullName + " not found (Assembly-CSharp-Editor)");
            return type;
        }
    }
}
