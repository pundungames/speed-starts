using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SpeedStars.Tools.Build
{
    /// <summary>
    /// Store build: -executeMethod SpeedStars.Tools.Build.ReleaseBuild.Run [-buildOut path] [-config assetPath].
    /// ProjectSettings are shared with the team, so the store settings (IL2CPP, ARM64, package, version, signing)
    /// are applied only for the duration of the build and every touched value is put back in `finally`.
    /// </summary>
    public static class ReleaseBuild
    {
        public const string KeystorePassVar = "SPEEDSTARS_KEYSTORE_PASS";
        public const string KeyPassVar = "SPEEDSTARS_KEY_PASS";
        private const string DefaultRelativeOutBase = "../pipeline-logs/android/SpeedStars-release";

        /// <summary>Everything this build writes to PlayerSettings, captured before the first write.</summary>
        private sealed class SettingsSnapshot
        {
            private readonly ScriptingImplementation backend;
            private readonly AndroidArchitecture arch;
            private readonly bool appBundle;
            private readonly string packageId;
            private readonly string bundleVersion;
            private readonly int versionCode;
            private readonly AndroidSdkVersions targetSdk;
            private readonly bool useCustomKeystore;
            private readonly string keystoreName;
            private readonly string keystorePass;
            private readonly string keyaliasName;
            private readonly string keyaliasPass;

            public SettingsSnapshot()
            {
                backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
                arch = PlayerSettings.Android.targetArchitectures;
                appBundle = EditorUserBuildSettings.buildAppBundle;
                packageId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
                bundleVersion = PlayerSettings.bundleVersion;
                versionCode = PlayerSettings.Android.bundleVersionCode;
                targetSdk = PlayerSettings.Android.targetSdkVersion;
                useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
                keystoreName = PlayerSettings.Android.keystoreName;
                keystorePass = PlayerSettings.Android.keystorePass;
                keyaliasName = PlayerSettings.Android.keyaliasName;
                keyaliasPass = PlayerSettings.Android.keyaliasPass;
            }

            public void Restore()
            {
                // Each write is isolated so one failure cannot leave the rest of the project changed.
                Safe(() => PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, backend), "scriptingBackend");
                Safe(() => PlayerSettings.Android.targetArchitectures = arch, "targetArchitectures");
                Safe(() => EditorUserBuildSettings.buildAppBundle = appBundle, "buildAppBundle");
                Safe(() => PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, packageId), "applicationIdentifier");
                Safe(() => PlayerSettings.bundleVersion = bundleVersion, "bundleVersion");
                Safe(() => PlayerSettings.Android.bundleVersionCode = versionCode, "bundleVersionCode");
                Safe(() => PlayerSettings.Android.targetSdkVersion = targetSdk, "targetSdkVersion");
                Safe(() => PlayerSettings.Android.keystoreName = keystoreName, "keystoreName");
                Safe(() => PlayerSettings.Android.keystorePass = keystorePass, "keystorePass");
                Safe(() => PlayerSettings.Android.keyaliasName = keyaliasName, "keyaliasName");
                Safe(() => PlayerSettings.Android.keyaliasPass = keyaliasPass, "keyaliasPass");
                Safe(() => PlayerSettings.Android.useCustomKeystore = useCustomKeystore, "useCustomKeystore");
            }

            private static void Safe(Action write, string name)
            {
                try { write(); }
                catch (Exception e) { UnityEngine.Debug.LogError("ANDROID_RELEASE_RESTORE_FAIL " + name + ": " + e.Message); }
            }
        }

        public static void Run()
        {
            int code = 1;
            try
            {
                code = Build();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("ANDROID_RELEASE_FAIL exception: " + e);
            }
            // Always exit explicitly: the script runs without -quit.
            EditorApplication.Exit(code);
        }

        private static int Build()
        {
            string[] scenes = BuildSupport.EnabledScenes();
            if (scenes.Length == 0)
            {
                UnityEngine.Debug.LogError("ANDROID_RELEASE_FAIL no enabled scene in EditorBuildSettings");
                return 1;
            }

            ReleaseBuildConfig config = LoadConfig(out bool createdInMemory);
            string defaultOut = DefaultRelativeOutBase + (config.BuildAppBundle ? ".aab" : ".apk");
            string outPath = BuildSupport.ResolveOutPath(defaultOut);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            UnityEngine.Debug.Log("ANDROID_SCENES " + string.Join(",", scenes));
            UnityEngine.Debug.Log("ANDROID_OUT " + outPath);
            if (createdInMemory)
                UnityEngine.Debug.LogWarning("ANDROID_RELEASE_CONFIG asset not found; using in-memory defaults (create " + ReleaseBuildConfig.AssetPath + ")");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            var snapshot = new SettingsSnapshot();
            try
            {
                ApplyConfig(config);
                LogSettings();

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None, // store build: Development stays off
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                if (summary.result == BuildResult.Succeeded)
                {
                    double mb = summary.totalSize / (1024.0 * 1024.0);
                    UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture,
                        "ANDROID_RELEASE_OK size={0:F1}MB time={1:F0}s", mb, summary.totalTime.TotalSeconds));
                    BuildSupport.LogManifest();
                    return 0;
                }

                UnityEngine.Debug.LogError(string.Format("ANDROID_RELEASE_FAIL result={0} errors={1} warnings={2}",
                    summary.result, summary.totalErrors, summary.totalWarnings));
                BuildSupport.LogBuildErrors(report, "ANDROID_RELEASE_ERROR");
                BuildSupport.LogManifest();
                return 1;
            }
            finally
            {
                snapshot.Restore();
                if (createdInMemory) UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static ReleaseBuildConfig LoadConfig(out bool createdInMemory)
        {
            string path = BuildSupport.ReadArg("-config") ?? ReleaseBuildConfig.AssetPath;
            var asset = AssetDatabase.LoadAssetAtPath<ReleaseBuildConfig>(path);
            createdInMemory = asset == null;
            return asset != null ? asset : ScriptableObject.CreateInstance<ReleaseBuildConfig>();
        }

        private static void ApplyConfig(ReleaseBuildConfig c)
        {
            if (c.Il2cpp)
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            if (c.Architectures != AndroidArchitecture.None)
                PlayerSettings.Android.targetArchitectures = c.Architectures;
            EditorUserBuildSettings.buildAppBundle = c.BuildAppBundle;

            if (!string.IsNullOrWhiteSpace(c.PackageId))
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, c.PackageId.Trim());
            if (!string.IsNullOrWhiteSpace(c.VersionName))
                PlayerSettings.bundleVersion = c.VersionName.Trim();
            if (c.VersionCode > 0)
                PlayerSettings.Android.bundleVersionCode = c.VersionCode;
            // The enum values are the API level numbers (0 = Automatic = highest installed).
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)Mathf.Max(0, c.TargetSdk);

            ApplySigning(c);
        }

        /// <summary>
        /// Passwords are read from the environment only and never logged. Anything missing falls back to the
        /// debug key, which Play rejects, so the fallback is announced loudly.
        /// </summary>
        private static void ApplySigning(ReleaseBuildConfig c)
        {
            string keystore = string.IsNullOrWhiteSpace(c.KeystorePath)
                ? null
                : Path.GetFullPath(Path.IsPathRooted(c.KeystorePath) ? c.KeystorePath : Path.Combine(BuildSupport.ProjectRoot(), c.KeystorePath));
            string storePass = Environment.GetEnvironmentVariable(KeystorePassVar);
            string keyPass = Environment.GetEnvironmentVariable(KeyPassVar);

            string missing = null;
            if (keystore == null) missing = "keystorePath empty";
            else if (!File.Exists(keystore)) missing = "keystore file not found";
            else if (string.IsNullOrWhiteSpace(c.KeyAlias)) missing = "keyAlias empty";
            else if (string.IsNullOrEmpty(storePass)) missing = KeystorePassVar + " not set";
            else if (string.IsNullOrEmpty(keyPass)) missing = KeyPassVar + " not set";

            if (missing != null)
            {
                PlayerSettings.Android.useCustomKeystore = false;
                UnityEngine.Debug.LogWarning("ANDROID_RELEASE_UNSIGNED debug signing (" + missing + "); not uploadable to Google Play");
                return;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = c.KeyAlias.Trim();
            PlayerSettings.Android.keyaliasPass = keyPass;
        }

        private static void LogSettings()
        {
            string backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString();
            string arch = PlayerSettings.Android.targetArchitectures.ToString();
            string target = PlayerSettings.Android.targetSdkVersion.ToString();
            string package = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            string signed = PlayerSettings.Android.useCustomKeystore ? "release-key" : "debug-key";
            UnityEngine.Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "ANDROID_RELEASE_SETTINGS backend={0} arch={1} target={2} package={3} version={4} code={5} aab={6} signing={7}",
                backend, arch, target, package, PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode,
                EditorUserBuildSettings.buildAppBundle, signed));
        }
    }
}
