using System;
using System.Collections.Generic;
using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// The full state of the puzzle board. Pure data — no Unity types.
    /// </summary>
    public class BoardState
    {
        public readonly int Width;
        public readonly int Height;

        private readonly TileState[] _tiles;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new TileState[width * height];
        }

        public bool InBounds(GridPosition pos)
        {
            return pos.Col >= 0 && pos.Col < Width && pos.Row >= 0 && pos.Row < Height;
        }

        public ref TileState GetTile(GridPosition pos)
        {
            if (!InBounds(pos))
                throw new ArgumentOutOfRangeException(nameof(pos), $"{pos} is out of bounds ({Width}x{Height})");
            return ref _tiles[pos.Row * Width + pos.Col];
        }

        public void SetTile(GridPosition pos, TileState tile)
        {
            GetTile(pos) = tile;
        }

        /// <summary>
        /// Rotate a tile 90° clockwise. Returns false if the tile is locked.
        /// </summary>
        public bool RotateTile(GridPosition pos)
        {
            ref var tile = ref GetTile(pos);
            if (tile.Locked || tile.Type == TileType.Empty)
                return false;
            tile.Rotation = (tile.Rotation + 1) % 4;
            return true;
        }

        /// <summary>
        /// Find all source tile positions on the board.
        /// </summary>
        public void GetSources(List<GridPosition> buffer)
        {
            buffer.Clear();
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (_tiles[row * Width + col].Type == TileType.Source)
                        buffer.Add(new GridPosition(col, row));
                }
            }
        }

        /// <summary>
        /// Find all target tile positions on the board.
        /// </summary>
        public void GetTargets(List<GridPosition> buffer)
        {
            buffer.Clear();
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (_tiles[row * Width + col].Type == TileType.Target)
                        buffer.Add(new GridPosition(col, row));
                }
            }
        }
    }
}
