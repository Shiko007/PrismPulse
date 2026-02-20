using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// A single segment of a light beam between two adjacent cells.
    /// Used by the renderer to draw beam lines.
    /// </summary>
    public readonly struct BeamSegment
    {
        public readonly GridPosition From;
        public readonly GridPosition To;
        public readonly LightColor Color;
        public readonly Direction Direction;

        public BeamSegment(GridPosition from, GridPosition to, LightColor color, Direction direction)
        {
            From = from;
            To = to;
            Color = color;
            Direction = direction;
        }
    }
}
