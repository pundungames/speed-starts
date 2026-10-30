using UnityEditor;
using UnityEngine;

namespace SpeedStars.Tools.Build
{
    /// <summary>
    /// Store-build settings applied only while ReleaseBuild runs, then rolled back. The project's own
    /// ProjectSettings stay on the daily Mono/ARMv7 setup. Passwords are NOT stored here: they come from the
    /// SPEEDSTARS_KEYSTORE_PASS / SPEEDSTARS_KEY_PASS environment variables.
    /// </summary>
    [CreateAssetMenu(fileName = "ReleaseBuildConfig", menuName = "Speed Stars/Release Build Config")]
    public sealed class ReleaseBuildConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/Speed-Staes/Data/Release/ReleaseBuildConfig.asset";

        [Tooltip("Android application id for the store build. Empty = keep the project's current id (final id still to be decided).")]
        [SerializeField] private string packageId = "";

        [Tooltip("Store-visible version name (PlayerSettings.bundleVersion). Empty = keep the project's value.")]
        [SerializeField] private string versionName = "1.0";

        [Tooltip("Play Store version code; must grow with every upload. 0 or less = keep the project's value.")]
        [SerializeField] private int versionCode = 1;

        [Tooltip("CPU architectures in the build. Play requires ARM64; ARMv7 keeps older 32-bit phones.")]
        [SerializeField] private AndroidArchitecture architectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

        [Tooltip("Use IL2CPP instead of Mono. Required for ARM64; link.xml keeps Zenject under stripping.")]
        [SerializeField] private bool il2cpp = true;

        [Tooltip("true = .aab (what Play uploads), false = .apk.")]
        [SerializeField] private bool buildAppBundle = true;

        [Tooltip("Target API level number (e.g. 34). 0 = highest installed (Automatic).")]
        [SerializeField] private int targetSdk = 0;

        [Tooltip("Keystore file; relative paths resolve from the project root. Empty or missing = debug signing (not uploadable).")]
        [SerializeField] private string keystorePath = "";

        [Tooltip("Key alias inside the keystore. Empty = unsigned fallback.")]
        [SerializeField] private string keyAlias = "";

        public string PackageId => packageId;
        public string VersionName => versionName;
        public int VersionCode => versionCode;
        public AndroidArchitecture Architectures => architectures;
        public bool Il2cpp => il2cpp;
        public bool BuildAppBundle => buildAppBundle;
        public int TargetSdk => targetSdk;
        public string KeystorePath => keystorePath;
        public string KeyAlias => keyAlias;
    }
}
