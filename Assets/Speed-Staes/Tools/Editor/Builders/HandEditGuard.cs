using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpeedStars.Tools.Builders
{
    /// <summary>What a builder does with a prefab / scene file it is asked to produce.</summary>
    public enum WriteDecision
    {
        /// <summary>Produce the file (it is missing, or a full rebuild was forced).</summary>
        Build,
        /// <summary>The file exists and may be hand-edited: leave it as it is.</summary>
        Keep,
    }

    /// <summary>
    /// The only place under Tools/Editor/Builders that writes prefab and scene files (T-102; a test scans for stray raw saves).
    /// The user hand-edits the UI prefabs, Home and Race (commit 4f38e016), so a builder must never replace an existing file by
    /// accident: a full rebuild needs overwrite AND the command-line force flag, and patches only add what is missing (they check
    /// first and never delete and rebuild). Every saved file is converted to TextMeshPro afterwards (T-100), deferred to
    /// <see cref="ConvertPending"/> because the converter edits the YAML text and the scene or prefab must not be loaded then.
    /// Exception, by design: <c>ProjectContextBuilder</c> writes its own raw prefab (composition root, no text, never hand-edited).
    /// </summary>
    public static class HandEditGuard
    {
        /// <summary>Command-line flag (without the dash) that lets overwrite rebuild hand-edited prefabs and scenes.</summary>
        public const string ForceFlag = "forceOverwriteHandEdits";

        private static readonly List<string> Pending = new List<string>();

        public static bool ForceOverwriteHandEdits => Array.IndexOf(Environment.GetCommandLineArgs(), "-" + ForceFlag) >= 0;

        /// <summary>Pure rule: a missing file is built; an existing one only when overwrite is asked for and the force flag is set.</summary>
        public static WriteDecision Decide(bool exists, bool overwriteRequested, bool force)
        {
            if (!exists) return WriteDecision.Build;
            return overwriteRequested && force ? WriteDecision.Build : WriteDecision.Keep;
        }

        /// <summary>Decide() for a file on disk, with the command line for the flag; warns when an overwrite request is turned down.</summary>
        public static WriteDecision Decide(string assetPath, bool overwriteRequested, string source)
        {
            bool exists = File.Exists(SceneBuilderUtil.FullPath(assetPath));
            WriteDecision decision = Decide(exists, overwriteRequested, ForceOverwriteHandEdits);
            if (decision == WriteDecision.Keep && overwriteRequested)
                UnityEngine.Debug.LogWarning($"[{source}] '{assetPath}' exists and is hand-edited: kept. Pass -{ForceFlag} to rebuild it anyway.");
            return decision;
        }

        /// <summary>
        /// Saves a prefab built from scratch. An existing file is only replaced under the force flag (callers decide first with
        /// <see cref="Decide(string,bool,string)"/>; this is the second lock). Returns the asset, or null when the file was kept.
        /// </summary>
        public static GameObject SaveNewPrefab(GameObject root, string path)
        {
            if (File.Exists(SceneBuilderUtil.FullPath(path)) && !ForceOverwriteHandEdits)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(HandEditGuard)}] '{path}' exists and is hand-edited: not replaced (-{ForceFlag} not given).");
                return null;
            }
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || saved == null) throw new InvalidOperationException($"Could not save prefab '{path}'.");
            Pending.Add(path);
            return saved;
        }

        /// <summary>Saves a prefab that was opened with LoadPrefabContents and patched (something missing was added to it).</summary>
        public static void SavePatchedPrefab(GameObject contentsRoot, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(contentsRoot, path, out bool success);
            if (!success) throw new InvalidOperationException($"Could not save prefab '{path}'.");
            Pending.Add(path);
        }

        /// <summary>
        /// Saves a scene a builder produced or patched. Callers rebuild an existing scene only after <see cref="Decide(string,bool,string)"/>
        /// said Build; a patch only adds what is missing to the opened file.
        /// </summary>
        public static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Could not save scene '{path}'.");
            Pending.Add(path);
        }

        /// <summary>Converts everything saved since the last call to TextMeshPro (T-100). Call once the saved files are no longer loaded.</summary>
        public static void ConvertPending()
        {
            if (Pending.Count == 0) return;
            var paths = new List<string>(new HashSet<string>(Pending));
            Pending.Clear();
            SpeedStars.Tools.Migration.TmpMigration.ConvertPaths(paths);
        }
    }
}
