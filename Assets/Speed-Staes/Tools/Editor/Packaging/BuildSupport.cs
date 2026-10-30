using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpeedStars.Tools.Build
{
    /// <summary>
    /// Pieces shared by the batchmode Android builds (smoke and release): scene list, argument reading,
    /// build-error report and the manifest search. Kept in one place so the two entry points cannot drift apart.
    /// </summary>
    internal static class BuildSupport
    {
        private const string VibratePermission = "android.permission.VIBRATE";
        private const int MaxErrorLines = 20;

        public static string[] EnabledScenes()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        }

        /// <summary>Value after a command-line flag, or null when the flag is absent.</summary>
        public static string ReadArg(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag) return args[i + 1];
            }
            return null;
        }

        public static string ProjectRoot()
        {
            // Application.dataPath = <project>/Assets.
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        /// <summary>-buildOut wins; otherwise the default path relative to Assets (so "../" climbs to the project root).</summary>
        public static string ResolveOutPath(string defaultRelativeToAssets)
        {
            string given = ReadArg("-buildOut");
            if (given != null) return Path.GetFullPath(given);
            return Path.GetFullPath(Path.Combine(Application.dataPath, defaultRelativeToAssets));
        }

        public static void LogBuildErrors(BuildReport report, string errorPrefix)
        {
            var errors = report.steps
                .SelectMany(st => st.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content)
                .Take(MaxErrorLines);
            foreach (string line in errors) UnityEngine.Debug.LogError(errorPrefix + " " + line);
        }

        /// <summary>
        /// The merged manifest location differs by Unity/Gradle version, so search the known roots instead of
        /// hard-coding one path. Any manifest declaring VIBRATE counts; the merged one is preferred in the log.
        /// </summary>
        public static void LogManifest()
        {
            string projectRoot = ProjectRoot();
            var found = new List<string>();
            foreach (string root in new[] { Path.Combine(projectRoot, "Library", "Bee", "Android", "Prj"), Path.Combine(projectRoot, "Temp", "gradleOut") })
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    found.AddRange(Directory.GetFiles(root, "AndroidManifest.xml", SearchOption.AllDirectories));
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("ANDROID_MANIFEST search failed in " + root + ": " + e.Message);
                }
            }

            if (found.Count == 0)
            {
                UnityEngine.Debug.Log("ANDROID_MANIFEST vibrate=unknown (no AndroidManifest.xml found under Library/Bee/Android/Prj or Temp/gradleOut)");
                return;
            }

            string merged = found.FirstOrDefault(p => p.Replace('\\', '/').IndexOf("merged_manifest", StringComparison.OrdinalIgnoreCase) >= 0);
            string withVibrate = found.FirstOrDefault(p => File.ReadAllText(p).Contains(VibratePermission));
            string shown = merged ?? withVibrate ?? found[0];
            bool vibrate = withVibrate != null;
            UnityEngine.Debug.Log($"ANDROID_MANIFEST vibrate={(vibrate ? "true" : "false")} file={shown} scanned={found.Count}");
        }
    }
}
