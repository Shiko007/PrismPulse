using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PrismPulse.Gameplay.Audio
{
    /// <summary>
    /// iOS Taptic Engine haptic feedback.
    /// Falls back to no-op on non-iOS platforms.
    /// </summary>
    public static class HapticFeedback
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void _HapticImpactLight();
        [DllImport("__Internal")] private static extern void _HapticImpactMedium();
        [DllImport("__Internal")] private static extern void _HapticNotificationSuccess();
#endif

        public static void LightTap()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _HapticImpactLight();
#endif
        }

        public static void MediumTap()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _HapticImpactMedium();
#endif
        }

        public static void Success()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _HapticNotificationSuccess();
#endif
        }
    }
}
