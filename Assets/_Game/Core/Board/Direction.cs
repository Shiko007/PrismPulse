namespace PrismPulse.Core.Board
{
    /// <summary>
    /// Cardinal directions a beam can travel on the grid.
    /// Values are ordered clockwise; rotation math relies on this order.
    /// </summary>
    public enum Direction : byte
    {
        Up    = 0,
        Right = 1,
        Down  = 2,
        Left  = 3
    }

    public static class DirectionExtensions
    {
        /// <summary>
        /// Returns the opposite direction (Up↔Down, Left↔Right).
        /// </summary>
        public static Direction Opposite(this Direction dir)
        {
            return (Direction)(((int)dir + 2) % 4);
        }

        /// <summary>
        /// Rotate a direction clockwise by a number of 90° steps.
        /// </summary>
        public static Direction RotateCW(this Direction dir, int steps)
        {
            return (Direction)(((int)dir + steps % 4 + 4) % 4);
        }

        /// <summary>
        /// Returns the grid offset (col, row) for moving one step in this direction.
        /// Row increases downward, col increases rightward.
        /// </summary>
        public static GridPosition ToOffset(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return new GridPosition(0, -1);
                case Direction.Right: return new GridPosition(1, 0);
                case Direction.Down:  return new GridPosition(0, 1);
                case Direction.Left:  return new GridPosition(-1, 0);
                default:              return new GridPosition(0, 0);
            }
        }

        public static readonly Direction[] All = { Direction.Up, Direction.Right, Direction.Down, Direction.Left };
    }
}
