using UnityEngine;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Maps core LightColor values to Unity Color for rendering.
    /// </summary>
    public static class LightColorMap
    {
        // Neon-glow palette — bright, saturated, looks great with bloom
        private static readonly Color NeonRed     = new Color(1.0f, 0.15f, 0.15f, 1f);
        private static readonly Color NeonGreen   = new Color(0.1f, 1.0f, 0.3f, 1f);
        private static readonly Color NeonBlue    = new Color(0.2f, 0.4f, 1.0f, 1f);
        private static readonly Color NeonYellow  = new Color(1.0f, 0.95f, 0.2f, 1f);
        private static readonly Color NeonCyan    = new Color(0.1f, 1.0f, 0.95f, 1f);
        private static readonly Color NeonPurple  = new Color(0.8f, 0.15f, 1.0f, 1f);
        private static readonly Color NeonWhite   = new Color(1.0f, 1.0f, 1.0f, 1f);

        public static Color ToUnityColor(LightColor color)
        {
            switch (color)
            {
                case LightColor.Red:    return NeonRed;
                case LightColor.Green:  return NeonGreen;
                case LightColor.Blue:   return NeonBlue;
                case LightColor.Yellow: return NeonYellow;
                case LightColor.Cyan:   return NeonCyan;
                case LightColor.Purple: return NeonPurple;
                case LightColor.White:  return NeonWhite;
                default:                return Color.gray;
            }
        }

        /// <summary>
        /// HDR emission color for bloom. Multiplied by intensity for glow effect.
        /// </summary>
        public static Color ToEmissionColor(LightColor color, float intensity = 2f)
        {
            return ToUnityColor(color) * intensity;
        }
    }
}
