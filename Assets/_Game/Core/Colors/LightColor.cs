using System;

namespace PrismPulse.Core.Colors
{
    /// <summary>
    /// Additive light color model using bit flags.
    /// Mixing colors = bitwise OR.
    /// </summary>
    [Flags]
    public enum LightColor : byte
    {
        None    = 0,
        Red     = 1 << 0,  // 1
        Green   = 1 << 1,  // 2
        Blue    = 1 << 2,  // 4

        Yellow  = Red | Green,      // 3
        Cyan    = Green | Blue,     // 6
        Purple  = Red | Blue,       // 5
        White   = Red | Green | Blue // 7
    }
}
