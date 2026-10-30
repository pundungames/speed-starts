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
    /// Batchmode smoke build: -executeMethod SpeedStars.Tools.Build.AndroidBuildSmoke.Run [-buildOut path.apk].
    /// The editor compile never touches `#if UNITY_ANDROID &amp;&amp; !UNITY_EDITOR` code, the scene list or the manifest;
    /// a development APK does. Read-only toward PlayerSettings/ProjectSettings: it only reports them, so the
    /// store gaps (64-bit, IL2CPP, target API) show up without changing the project.
    /// </summary>
    public static class AndroidBuildSmoke
    {
        private const string DefaultRelativeOut = "../pipeline-logs/android/SpeedStars-dev.apk";
        private const string VibratePermission = "android.permission.VIBRATE";
        private const int MaxErrorLines = 20;

        public static void Run()
        {
            int code = 1;
            try
            {
                code = Build();
            }
            catch (Exception e)
            {
                Debug.LogError("ANDROID_BUILD_FAIL exception: " + e);
            }
            // Always exit explicitly: the script runs without -quit.
            EditorApplication.Exit(code);
        }

        private static int Build()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("ANDROID_BUILD_FAIL no enabled scene in EditorBuildSettings");
                return 1;
            }

            string outPath = ResolveOutPath();
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            Debug.Log("ANDROID_SCENES " + string.Join(",", scenes));
            Debug.Log("ANDROID_OUT " + outPath);

            // Reported before the build; reading only (no setter is called anywhere in this file).
            LogSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                double mb = summary.totalSize / (1024.0 * 1024.0);
                Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "ANDROID_BUILD_OK size={0:F1}MB time={1:F0}s", mb, summary.totalTime.TotalSeconds));
                LogManifest();
                return 0;
            }

            Debug.LogError(string.Format("ANDROID_BUILD_FAIL result={0} errors={1} warnings={2}",
                summary.result, summary.totalErrors, summary.totalWarnings));
            var errors = report.steps
                .SelectMany(st => st.messages)
                .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                .Select(m => m.content)
                .Take(MaxErrorLines);
            foreach (string line in errors) Debug.LogError("ANDROID_BUILD_ERROR " + line);
            LogManifest();
            return 1;
        }

        private static string ResolveOutPath()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOut") return Path.GetFullPath(args[i + 1]);
            }
            // Application.dataPath = <project>/Assets, so "../" climbs to the project root and one more leaves it.
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultRelativeOut));
        }

        private static void LogSettings()
        {
            string backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString();
            string arch = PlayerSettings.Android.targetArchitectures.ToString();
            string min = PlayerSettings.Android.minSdkVersion.ToString();
            string target = PlayerSettings.Android.targetSdkVersion.ToString();
            string package = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            Debug.Log($"ANDROID_SETTINGS backend={backend} arch={arch} minSdk={min} target={target} package={package}");
        }

        /// <summary>
        /// The merged manifest location differs by Unity/Gradle version, so search the known roots instead of
        /// hard-coding one path. Any manifest declaring VIBRATE counts; the merged one is preferred in the log.
        /// </summary>
        private static void LogManifest()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
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
                    Debug.LogWarning("ANDROID_MANIFEST search failed in " + root + ": " + e.Message);
                }
            }

            if (found.Count == 0)
            {
                Debug.Log("ANDROID_MANIFEST vibrate=unknown (no AndroidManifest.xml found under Library/Bee/Android/Prj or Temp/gradleOut)");
                return;
            }

            string merged = found.FirstOrDefault(p => p.Replace('\\', '/').IndexOf("merged_manifest", StringComparison.OrdinalIgnoreCase) >= 0);
            string withVibrate = found.FirstOrDefault(p => File.ReadAllText(p).Contains(VibratePermission));
            string shown = merged ?? withVibrate ?? found[0];
            bool vibrate = withVibrate != null;
            Debug.Log($"ANDROID_MANIFEST vibrate={(vibrate ? "true" : "false")} file={shown} scanned={found.Count}");
        }
    }
}
