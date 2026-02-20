using System.Collections.Generic;
using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// Traces all light beams from every Source on the board.
    /// Produces a <see cref="BeamResult"/> consumed by the visual layer.
    /// Pure logic — no Unity dependencies.
    /// </summary>
    public class BeamTracer
    {
        // Reusable buffers to avoid allocations
        private readonly List<Direction> _outputBuffer = new List<Direction>(4);
        private readonly List<GridPosition> _sourceBuffer = new List<GridPosition>(8);
        private readonly List<GridPosition> _targetBuffer = new List<GridPosition>(8);
        private readonly Queue<(GridPosition pos, Direction dir, LightColor color)> _queue =
            new Queue<(GridPosition, Direction, LightColor)>();
        private readonly Dictionary<(GridPosition, Direction), LightColor> _visited =
            new Dictionary<(GridPosition, Direction), LightColor>();

        /// <summary>
        /// Accumulated colors arriving at each cell from each direction.
        /// Used for merger tiles that combine multiple inputs.
        /// </summary>
        private readonly Dictionary<(GridPosition pos, Direction entryFace), LightColor> _arrivals =
            new Dictionary<(GridPosition, Direction), LightColor>();

        /// <summary>
        /// Trace all beams on the given board. Reuses and fills the provided result.
        /// </summary>
        public void Trace(BoardState board, BeamResult result)
        {
            result.Clear();
            _queue.Clear();
            _visited.Clear();
            _arrivals.Clear();

            // Seed the queue with all Source tiles
            board.GetSources(_sourceBuffer);
            foreach (var srcPos in _sourceBuffer)
            {
                ref var src = ref board.GetTile(srcPos);
                var dir = src.SourceDirection.RotateCW(src.Rotation);
                var next = srcPos.Add(dir.ToOffset());
                if (board.InBounds(next))
                {
                    _queue.Enqueue((next, dir, src.SourceColor));
                    result.Segments.Add(new BeamSegment(srcPos, next, src.SourceColor, dir));
                }
            }

            // BFS beam propagation
            while (_queue.Count > 0)
            {
                var (pos, dir, color) = _queue.Dequeue();

                // Prevent infinite loops (beam re-entering same cell in same direction)
                // Allow re-processing if a new color component arrives (needed for Merger)
                var visitKey = (pos, dir);
                if (_visited.TryGetValue(visitKey, out var prevColor) && prevColor.Contains(color))
                    continue;
                _visited[visitKey] = _visited.ContainsKey(visitKey)
                    ? LightColorMath.Mix(prevColor, color)
                    : color;

                ref var tile = ref board.GetTile(pos);

                // Handle DarkAbsorber — blocks unless activation color matches
                if (tile.Type == TileType.DarkAbsorber)
                {
                    if (!color.Contains(tile.ActivationColor))
                        continue; // absorbed
                }

                // Handle Target — record the hit
                if (tile.Type == TileType.Target)
                {
                    if (result.TargetHits.ContainsKey(pos))
                        result.TargetHits[pos] = LightColorMath.Mix(result.TargetHits[pos], color);
                    else
                        result.TargetHits[pos] = color;
                    continue; // targets don't output beams
                }

                // Handle Merger — accumulate colors, output mixed color
                if (tile.Type == TileType.Merger)
                {
                    var entryFace = dir.Opposite();
                    var arrivalKey = (pos, entryFace);
                    if (_arrivals.ContainsKey(arrivalKey))
                        _arrivals[arrivalKey] = LightColorMath.Mix(_arrivals[arrivalKey], color);
                    else
                        _arrivals[arrivalKey] = color;

                    // Compute merged color from all arrivals at this merger
                    LightColor merged = LightColor.None;
                    foreach (var kvp in _arrivals)
                    {
                        if (kvp.Key.pos == pos)
                            merged = LightColorMath.Mix(merged, kvp.Value);
                    }
                    color = merged;
                }

                // Route through tile
                TileRouter.GetOutputDirections(tile, dir, _outputBuffer);

                foreach (var outDir in _outputBuffer)
                {
                    var nextPos = pos.Add(outDir.ToOffset());
                    if (!board.InBounds(nextPos))
                        continue;

                    result.Segments.Add(new BeamSegment(pos, nextPos, color, outDir));
                    _queue.Enqueue((nextPos, outDir, color));
                }
            }

            // Check if all targets are satisfied
            board.GetTargets(_targetBuffer);
            result.AllTargetsSatisfied = _targetBuffer.Count > 0;
            foreach (var tgtPos in _targetBuffer)
            {
                ref var tgt = ref board.GetTile(tgtPos);
                if (!result.TargetHits.TryGetValue(tgtPos, out var hitColor)
                    || !hitColor.Contains(tgt.RequiredColor))
                {
                    result.AllTargetsSatisfied = false;
                    break;
                }
            }
        }
    }
}
