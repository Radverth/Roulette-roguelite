using UnityEngine;

namespace SinWheel
{
    /// <summary>Weighty tactile feedback on device; silent in editor/CI.</summary>
    public static class Haptics
    {
        public static void Light() => Vibrate(20);
        public static void Heavy() => Vibrate(120);

        private static void Vibrate(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    vibrator?.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception)
            {
                // Devices without a vibrator, or restricted contexts — feedback is optional.
            }
#endif
        }
    }
}
