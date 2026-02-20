using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// The state of a single cell on the board.
    /// This is a mutable struct stored in the board array.
    /// </summary>
    public struct TileState
    {
        /// <summary>What kind of tile occupies this cell.</summary>
        public TileType Type;

        /// <summary>Clockwise rotation in 90° steps (0–3).</summary>
        public int Rotation;

        /// <summary>For Source tiles: the color of beam emitted.</summary>
        public LightColor SourceColor;

        /// <summary>For Source tiles: the direction the beam is emitted.</summary>
        public Direction SourceDirection;

        /// <summary>For Target tiles: the color required to satisfy this target.</summary>
        public LightColor RequiredColor;

        /// <summary>For DarkAbsorber tiles: the color that activates (passes through) this tile.</summary>
        public LightColor ActivationColor;

        /// <summary>Whether the player is allowed to rotate this tile.</summary>
        public bool Locked;

        public static TileState CreateEmpty()
        {
            return new TileState { Type = TileType.Empty };
        }

        public static TileState CreateSource(LightColor color, Direction direction)
        {
            return new TileState
            {
                Type = TileType.Source,
                SourceColor = color,
                SourceDirection = direction,
                Locked = true
            };
        }

        public static TileState CreateTarget(LightColor requiredColor)
        {
            return new TileState
            {
                Type = TileType.Target,
                RequiredColor = requiredColor,
                Locked = true
            };
        }

        public static TileState CreateDark(LightColor activationColor)
        {
            return new TileState
            {
                Type = TileType.DarkAbsorber,
                ActivationColor = activationColor,
                Locked = true
            };
        }
    }
}
