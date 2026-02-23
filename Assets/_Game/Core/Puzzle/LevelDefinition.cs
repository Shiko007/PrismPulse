using System;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Puzzle
{
    /// <summary>
    /// Serializable definition of a single puzzle level.
    /// Pure data — no Unity types. Can be built from JSON, ScriptableObject, or code.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        public string Id;
        public string Name;
        public int Width;
        public int Height;
        public int ParMoves;     // Target number of moves for best rating
        public float ParTimeSeconds; // Target time for best rating
        public bool ShuffleMode; // If true, unlocked tile positions are shuffled on load
        public TileDef[] Tiles;

        [Serializable]
        public struct TileDef
        {
            public int Col;
            public int Row;
            public TileType Type;
            public int Rotation;
            public LightColor Color;       // SourceColor, RequiredColor, or ActivationColor
            public Direction Direction;     // For Source tiles
            public bool Locked;
        }

        /// <summary>
        /// Build a BoardState from this definition.
        /// </summary>
        public BoardState ToBoardState()
        {
            // Board is always 5x5; tiles are placed at their defined positions
            var board = new BoardState(5, 5);

            foreach (var def in Tiles)
            {
                var pos = new GridPosition(def.Col, def.Row);
                TileState tile;

                switch (def.Type)
                {
                    case TileType.Source:
                        tile = TileState.CreateSource(def.Color, def.Direction);
                        break;
                    case TileType.Target:
                        tile = TileState.CreateTarget(def.Color);
                        break;
                    case TileType.DarkAbsorber:
                        tile = TileState.CreateDark(def.Color);
                        break;
                    default:
                        tile = new TileState
                        {
                            Type = def.Type,
                            Rotation = def.Rotation,
                            Locked = def.Locked
                        };
                        break;
                }

                board.SetTile(pos, tile);
            }

            return board;
        }
    }
}
