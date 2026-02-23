using System.Collections.Generic;
using PrismPulse.Core.Board;

namespace PrismPulse.Core.Puzzle
{
    /// <summary>
    /// Brute-force solver that finds a rotation assignment satisfying all targets.
    /// Works by trying all 4^N combinations for the N rotatable tiles.
    /// Levels are small (3-8 rotatable tiles) so this is instant.
    /// </summary>
    public static class PuzzleSolver
    {
        /// <summary>
        /// Finds the solved rotation for every rotatable tile.
        /// Returns null if no solution exists.
        /// </summary>
        public static Dictionary<GridPosition, int> Solve(BoardState board)
        {
            var tracer = new BeamTracer();
            var result = new BeamResult();

            // Collect all rotatable tile positions
            var rotatable = new List<GridPosition>();
            for (int row = 0; row < board.Height; row++)
            {
                for (int col = 0; col < board.Width; col++)
                {
                    var pos = new GridPosition(col, row);
                    ref var tile = ref board.GetTile(pos);
                    if (!tile.Locked && tile.Type != TileType.Empty)
                        rotatable.Add(pos);
                }
            }

            if (rotatable.Count == 0)
                return null;

            // Save original rotations
            var originalRotations = new int[rotatable.Count];
            for (int i = 0; i < rotatable.Count; i++)
                originalRotations[i] = board.GetTile(rotatable[i]).Rotation;

            var solution = new Dictionary<GridPosition, int>();
            bool found = SolveRecursive(board, tracer, result, rotatable, originalRotations, 0, solution);

            // Restore original rotations
            for (int i = 0; i < rotatable.Count; i++)
                board.GetTile(rotatable[i]).Rotation = originalRotations[i];

            return found ? solution : null;
        }

        private static bool SolveRecursive(
            BoardState board, BeamTracer tracer, BeamResult result,
            List<GridPosition> rotatable, int[] originalRotations, int index,
            Dictionary<GridPosition, int> solution)
        {
            if (index >= rotatable.Count)
            {
                // Check if all targets are satisfied
                tracer.Trace(board, result);
                return result.AllTargetsSatisfied;
            }

            var pos = rotatable[index];
            int orig = originalRotations[index];

            // Try rotations starting from current, so we prefer solutions
            // that need fewer moves (0 extra, then 1, then 2, then 3)
            for (int offset = 0; offset < 4; offset++)
            {
                int r = (orig + offset) % 4;
                board.GetTile(pos).Rotation = r;
                if (SolveRecursive(board, tracer, result, rotatable, originalRotations, index + 1, solution))
                {
                    solution[pos] = r;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Given the current board state and a known solution, returns the next
        /// tile that needs rotating and how many rotations are needed.
        /// Returns null if already solved or no hint available.
        /// </summary>
        public static (GridPosition pos, int rotationsNeeded)? GetNextHint(
            BoardState board, Dictionary<GridPosition, int> solution)
        {
            if (solution == null) return null;

            foreach (var kvp in solution)
            {
                int current = board.GetTile(kvp.Key).Rotation;
                int target = kvp.Value;
                if (current != target)
                    return (kvp.Key, (target - current + 4) % 4);
            }

            return null; // already solved
        }
    }
}
