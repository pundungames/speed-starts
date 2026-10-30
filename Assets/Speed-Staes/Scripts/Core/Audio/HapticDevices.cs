namespace SpeedStars.Core.Audio
{
    /// <summary>Picks the device for this platform: the phone's vibrator on Android, nothing elsewhere (editor, desktop).</summary>
    public static class HapticDevices
    {
        public static IHapticsDevice CreateDefault()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidVibrator();
#else
            return new NullHapticsDevice();
#endif
        }
    }

    /// <summary>Does nothing; the editor and other platforms have no buzz.</summary>
    public sealed class NullHapticsDevice : IHapticsDevice
    {
        public void Vibrate(int milliseconds, int amplitude)
        {
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android Vibrator through JNI. API 26+ uses VibrationEffect.createOneShot (with a strength); older phones use the plain
    /// vibrate(ms). Any failure turns the device off for the session: a missing buzz must never break a button press.
    /// Needs the VIBRATE permission in the manifest.
    /// </summary>
    internal sealed class AndroidVibrator : IHapticsDevice
    {
        private const int OneShotMinApi = 26;

        // Never called. Unity writes android.permission.VIBRATE into the manifest only when the scripts use Handheld.Vibrate;
        // without the permission the JNI vibrate call fails and the buzz is silently off.
        private static void KeepVibratePermission() => UnityEngine.Handheld.Vibrate();

        private UnityEngine.AndroidJavaObject _vibrator;
        private int _apiLevel;

        public AndroidVibrator()
        {
            try
            {
                using (var player = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity"))
                using (var version = new UnityEngine.AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _apiLevel = version.GetStatic<int>("SDK_INT");
                    UnityEngine.AndroidJavaObject vibrator = activity.Call<UnityEngine.AndroidJavaObject>("getSystemService", "vibrator");
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator")) _vibrator = vibrator;
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }
        }

        public void Vibrate(int milliseconds, int amplitude)
        {
            if (_vibrator == null) return;
            try
            {
                if (_apiLevel >= OneShotMinApi)
                {
                    using (var effects = new UnityEngine.AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<UnityEngine.AndroidJavaObject>("createOneShot", (long)milliseconds, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }
        }
    }
#endif
}
