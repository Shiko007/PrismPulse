namespace PrismPulse.Core.Colors
{
    public static class LightColorMath
    {
        /// <summary>
        /// Additively mix two light colors (bitwise OR).
        /// </summary>
        public static LightColor Mix(LightColor a, LightColor b)
        {
            return a | b;
        }

        /// <summary>
        /// Check if a color contains all components of another color.
        /// e.g., White.Contains(Red) == true, Red.Contains(Blue) == false
        /// </summary>
        public static bool Contains(this LightColor self, LightColor component)
        {
            return (self & component) == component;
        }

        /// <summary>
        /// Check if a color is a single primary (Red, Green, or Blue only).
        /// </summary>
        public static bool IsPrimary(this LightColor color)
        {
            return color == LightColor.Red
                || color == LightColor.Green
                || color == LightColor.Blue;
        }

        /// <summary>
        /// Returns the number of primary components in this color.
        /// </summary>
        public static int ComponentCount(this LightColor color)
        {
            int count = 0;
            if ((color & LightColor.Red) != 0) count++;
            if ((color & LightColor.Green) != 0) count++;
            if ((color & LightColor.Blue) != 0) count++;
            return count;
        }
    }
}
