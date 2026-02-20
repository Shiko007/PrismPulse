using System;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// Immutable 2D grid coordinate. Col = X, Row = Y.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int Col;
        public readonly int Row;

        public GridPosition(int col, int row)
        {
            Col = col;
            Row = row;
        }

        public GridPosition Add(GridPosition other)
        {
            return new GridPosition(Col + other.Col, Row + other.Row);
        }

        public bool Equals(GridPosition other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (Col * 397) ^ Row;
        public override string ToString() => $"({Col}, {Row})";

        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
    }
}
