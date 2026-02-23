using UnityEngine;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Maps core LightColor values to Unity Color for rendering.
    /// Supports a color-blind accessible palette.
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

        // Color-blind friendly palette (Okabe-Ito inspired, high contrast)
        private static readonly Color CbRed       = new Color(0.9f, 0.35f, 0.0f, 1f);   // orange-red
        private static readonly Color CbGreen     = new Color(0.0f, 0.6f, 0.5f, 1f);    // teal
        private static readonly Color CbBlue      = new Color(0.35f, 0.7f, 0.9f, 1f);   // sky blue
        private static readonly Color CbYellow    = new Color(0.95f, 0.9f, 0.25f, 1f);   // bright yellow
        private static readonly Color CbCyan      = new Color(0.0f, 0.45f, 0.7f, 1f);   // blue
        private static readonly Color CbPurple    = new Color(0.8f, 0.47f, 0.65f, 1f);  // pink
        private static readonly Color CbWhite     = new Color(1.0f, 1.0f, 1.0f, 1f);

        private static bool? _colorBlindMode;

        public static bool ColorBlindMode
        {
            get
            {
                if (_colorBlindMode == null)
                    _colorBlindMode = PlayerPrefs.GetInt("colorblind_mode", 0) == 1;
                return _colorBlindMode.Value;
            }
            set
            {
                _colorBlindMode = value;
                PlayerPrefs.SetInt("colorblind_mode", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static Color ToUnityColor(LightColor color)
        {
            if (ColorBlindMode)
            {
                switch (color)
                {
                    case LightColor.Red:    return CbRed;
                    case LightColor.Green:  return CbGreen;
                    case LightColor.Blue:   return CbBlue;
                    case LightColor.Yellow: return CbYellow;
                    case LightColor.Cyan:   return CbCyan;
                    case LightColor.Purple: return CbPurple;
                    case LightColor.White:  return CbWhite;
                    default:                return Color.gray;
                }
            }

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
        /// Short label for color-blind mode overlays.
        /// </summary>
        public static string ToLabel(LightColor color)
        {
            switch (color)
            {
                case LightColor.Red:    return "R";
                case LightColor.Green:  return "G";
                case LightColor.Blue:   return "B";
                case LightColor.Yellow: return "Y";
                case LightColor.Cyan:   return "C";
                case LightColor.Purple: return "P";
                case LightColor.White:  return "W";
                default:                return "";
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
