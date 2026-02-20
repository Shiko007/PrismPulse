using System.Collections.Generic;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// Pure-logic router that determines output directions for a beam entering a tile.
    /// All routing considers the tile's rotation (0–3 clockwise 90° steps).
    /// </summary>
    public static class TileRouter
    {
        /// <summary>
        /// Given a tile and the direction a beam is traveling INTO the tile,
        /// returns the direction(s) the beam exits the tile.
        /// Returns empty if the beam is absorbed or doesn't connect.
        /// </summary>
        public static void GetOutputDirections(
            TileState tile,
            Direction incomingDirection,
            List<Direction> outputBuffer)
        {
            outputBuffer.Clear();

            // The direction the beam enters FROM (relative to the tile)
            Direction entryFace = incomingDirection.Opposite();
            // Convert to tile-local space by undoing rotation
            Direction localEntry = entryFace.RotateCW(-tile.Rotation);

            switch (tile.Type)
            {
                case TileType.Empty:
                    // Beam continues straight through
                    outputBuffer.Add(incomingDirection);
                    break;

                case TileType.Straight:
                    // Only connects on its axis: Up/Down in local space
                    if (localEntry == Direction.Up || localEntry == Direction.Down)
                        outputBuffer.Add(localEntry.Opposite().RotateCW(tile.Rotation));
                    break;

                case TileType.Bend:
                    // Connects Up→Right and Right→Up in local space (90° bend)
                    if (localEntry == Direction.Up)
                        outputBuffer.Add(Direction.Right.RotateCW(tile.Rotation));
                    else if (localEntry == Direction.Right)
                        outputBuffer.Add(Direction.Up.RotateCW(tile.Rotation));
                    break;

                case TileType.Mirror:
                    // 45° mirror: reflects at 45°.
                    // In local space: Up↔Right, Down↔Left
                    if (localEntry == Direction.Up)
                        outputBuffer.Add(Direction.Right.RotateCW(tile.Rotation));
                    else if (localEntry == Direction.Right)
                        outputBuffer.Add(Direction.Up.RotateCW(tile.Rotation));
                    else if (localEntry == Direction.Down)
                        outputBuffer.Add(Direction.Left.RotateCW(tile.Rotation));
                    else if (localEntry == Direction.Left)
                        outputBuffer.Add(Direction.Down.RotateCW(tile.Rotation));
                    break;

                case TileType.Splitter:
                    // T-splitter: entry from bottom, exits left and right in local space
                    if (localEntry == Direction.Down)
                    {
                        outputBuffer.Add(Direction.Left.RotateCW(tile.Rotation));
                        outputBuffer.Add(Direction.Right.RotateCW(tile.Rotation));
                    }
                    else if (localEntry == Direction.Left)
                    {
                        outputBuffer.Add(Direction.Right.RotateCW(tile.Rotation));
                    }
                    else if (localEntry == Direction.Right)
                    {
                        outputBuffer.Add(Direction.Left.RotateCW(tile.Rotation));
                    }
                    break;

                case TileType.Cross:
                    // Beam passes straight through regardless of axis
                    outputBuffer.Add(incomingDirection);
                    break;

                case TileType.Target:
                    // Absorbs beam (no output). Hit is recorded by BeamTracer.
                    break;

                case TileType.Source:
                    // Sources only emit, they don't route incoming beams
                    break;

                case TileType.DarkAbsorber:
                    // Handled by BeamTracer — checks activation color before calling this.
                    // If we get here, the color matched, so pass through.
                    outputBuffer.Add(incomingDirection);
                    break;

                case TileType.Merger:
                    // Merger: two inputs from sides, one output from top in local space.
                    // Color merging is handled by BeamTracer.
                    if (localEntry == Direction.Left || localEntry == Direction.Right)
                        outputBuffer.Add(Direction.Up.RotateCW(tile.Rotation));
                    break;
            }
        }
    }
}
