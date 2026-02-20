namespace PrismPulse.Core.Board
{
    /// <summary>
    /// Defines the behavior category of a tile.
    /// Routing logic is handled by <see cref="TileRouter"/>.
    /// </summary>
    public enum TileType : byte
    {
        /// <summary>Empty cell — beam passes through freely in its current direction.</summary>
        Empty,

        /// <summary>Straight-through crystal — passes beam along its axis (0°/180° relative to rotation).</summary>
        Straight,

        /// <summary>90° bend — redirects beam by 90° based on rotation.</summary>
        Bend,

        /// <summary>T-splitter — incoming beam exits from two perpendicular outputs.</summary>
        Splitter,

        /// <summary>Cross — beams pass through both axes without interacting.</summary>
        Cross,

        /// <summary>Color merger — two inputs combine colors into one output.</summary>
        Merger,

        /// <summary>Dark tile — absorbs all light unless the incoming color matches its activation color.</summary>
        DarkAbsorber,

        /// <summary>Light source — emits a beam of a specific color in a specific direction.</summary>
        Source,

        /// <summary>Target — must receive a beam of a required color to be satisfied.</summary>
        Target,

        /// <summary>45° mirror — reflects beam at 45° angle.</summary>
        Mirror
    }
}
