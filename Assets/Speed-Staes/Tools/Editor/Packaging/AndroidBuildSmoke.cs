using System;
using System.IO;
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
    /// Shared helpers live in <c>BuildSupport</c> (also used by the release build).
    /// </summary>
    public static class AndroidBuildSmoke
    {
        private const string DefaultRelativeOut = "../pipeline-logs/android/SpeedStars-dev.apk";

        public static void Run()
        {
            int code = 1;
            try
            {
                code = Build();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("ANDROID_BUILD_FAIL exception: " + e);
            }
            // Always exit explicitly: the script runs without -quit.
            EditorApplication.Exit(code);
        }

        private static int Build()
        {
            string[] scenes = BuildSupport.EnabledScenes();
            if (scenes.Length == 0)
            {
                UnityEngine.Debug.LogError("ANDROID_BUILD_FAIL no enabled scene in EditorBuildSettings");
                return 1;
            }

            // Application.dataPath = <project>/Assets, so "../" climbs to the project root and one more leaves it.
            string outPath = BuildSupport.ResolveOutPath(DefaultRelativeOut);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            UnityEngine.Debug.Log("ANDROID_SCENES " + string.Join(",", scenes));
            UnityEngine.Debug.Log("ANDROID_OUT " + outPath);

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
                UnityEngine.Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "ANDROID_BUILD_OK size={0:F1}MB time={1:F0}s", mb, summary.totalTime.TotalSeconds));
                BuildSupport.LogManifest();
                return 0;
            }

            UnityEngine.Debug.LogError(string.Format("ANDROID_BUILD_FAIL result={0} errors={1} warnings={2}",
                summary.result, summary.totalErrors, summary.totalWarnings));
            BuildSupport.LogBuildErrors(report, "ANDROID_BUILD_ERROR");
            BuildSupport.LogManifest();
            return 1;
        }

        private static void LogSettings()
        {
            string backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString();
            string arch = PlayerSettings.Android.targetArchitectures.ToString();
            string min = PlayerSettings.Android.minSdkVersion.ToString();
            string target = PlayerSettings.Android.targetSdkVersion.ToString();
            string package = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            UnityEngine.Debug.Log($"ANDROID_SETTINGS backend={backend} arch={arch} minSdk={min} target={target} package={package}");
        }
    }
}
