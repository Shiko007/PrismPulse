using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PrismPulse.Core.Board;
using PrismPulse.Core.Puzzle;
using PrismPulse.Gameplay.Levels;

namespace PrismPulse.Editor
{
    public static class ParCalculator
    {
        [MenuItem("PrismPulse/Calculate Par Moves")]
        public static void CalculateAllPar()
        {
            var levels = BuiltInLevels.All;
            var tracer = new BeamTracer();
            var result = new BeamResult();

            Debug.Log("=== Par Move Calculation ===");

            for (int i = 0; i < levels.Length; i++)
            {
                var level = levels[i];

                // Build the original (solved-position) board
                var originalBoard = level.ToBoardState();

                // Find the solved rotations on the original board
                var solvedRotations = PuzzleSolver.Solve(originalBoard);
                if (solvedRotations == null)
                {
                    Debug.LogWarning($"Level {level.Id} ({level.Name}): NO SOLUTION FOUND");
                    continue;
                }

                // Count rotation moves needed from initial rotations to solved rotations
                int rotationMoves = 0;
                foreach (var kvp in solvedRotations)
                {
                    // Find original rotation from level definition
                    int origRotation = 0;
                    foreach (var def in level.Tiles)
                    {
                        if (def.Col == kvp.Key.Col && def.Row == kvp.Key.Row)
                        {
                            origRotation = def.Rotation;
                            break;
                        }
                    }
                    int needed = (kvp.Value - origRotation + 4) % 4;
                    rotationMoves += needed;
                }

                int swapMoves = 0;

                if (level.ShuffleMode)
                {
                    // Simulate the deterministic shuffle
                    var shuffledBoard = level.ToBoardState();
                    int seed = GetDeterministicHash(level.Id);
                    ShuffleBoardState(shuffledBoard, seed);

                    // Build mapping: for each unlocked position, what tile type+rotation is there
                    // in the shuffled board vs the original board.
                    // We need to find minimum swaps to restore the original arrangement.

                    var unlockedPositions = new List<GridPosition>();
                    for (int row = 0; row < 5; row++)
                    {
                        for (int col = 0; col < 5; col++)
                        {
                            var pos = new GridPosition(col, row);
                            var tile = originalBoard.GetTile(pos);
                            if (!tile.Locked)
                                unlockedPositions.Add(pos);
                        }
                    }

                    // Build permutation: shuffledBoard[pos] should go to where in originalBoard?
                    // For each unlocked position in shuffled board, find which original position
                    // has a matching tile (by type + rotation + color properties).
                    // Since tiles keep their identity during shuffle, we can match by content.

                    // Map: position index -> which position index the tile at that position belongs to
                    int n = unlockedPositions.Count;
                    int[] perm = new int[n];
                    bool[] matched = new bool[n];

                    for (int si = 0; si < n; si++)
                    {
                        var shuffledTile = shuffledBoard.GetTile(unlockedPositions[si]);
                        bool found = false;

                        for (int oi = 0; oi < n; oi++)
                        {
                            if (matched[oi]) continue;
                            var origTile = originalBoard.GetTile(unlockedPositions[oi]);

                            if (TilesMatch(shuffledTile, origTile))
                            {
                                perm[si] = oi;
                                matched[oi] = true;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            // Fallback: identity (shouldn't happen)
                            perm[si] = si;
                        }
                    }

                    // Count minimum swaps = n - number_of_cycles
                    bool[] visited = new bool[n];
                    int cycles = 0;
                    for (int j = 0; j < n; j++)
                    {
                        if (visited[j]) continue;
                        cycles++;
                        int curr = j;
                        while (!visited[curr])
                        {
                            visited[curr] = true;
                            curr = perm[curr];
                        }
                    }
                    swapMoves = n - cycles;
                }

                int totalPar = swapMoves + rotationMoves;
                string changed = totalPar != level.ParMoves ? " <-- CHANGED" : "";
                Debug.Log($"Level {level.Id} ({level.Name}): " +
                          $"swaps={swapMoves}, rotations={rotationMoves}, " +
                          $"total={totalPar}, current par={level.ParMoves}{changed}");
            }

            Debug.Log("=== Done ===");
        }

        private static bool TilesMatch(TileState a, TileState b)
        {
            return a.Type == b.Type
                && a.Rotation == b.Rotation
                && a.SourceColor == b.SourceColor
                && a.RequiredColor == b.RequiredColor
                && a.ActivationColor == b.ActivationColor
                && a.SourceDirection == b.SourceDirection;
        }

        private static int GetDeterministicHash(string s)
        {
            unchecked
            {
                int hash = 5381;
                foreach (char c in s)
                    hash = hash * 33 + c;
                return hash;
            }
        }

        private static void ShuffleBoardState(BoardState board, int seed)
        {
            var rng = new System.Random(seed);

            var allSlots = new List<GridPosition>();

            for (int row = 0; row < board.Height; row++)
            {
                for (int col = 0; col < board.Width; col++)
                {
                    var pos = new GridPosition(col, row);
                    var tile = board.GetTile(pos);
                    if (tile.Locked) continue;
                    allSlots.Add(pos);
                }
            }

            if (allSlots.Count < 2) return;

            for (int i = allSlots.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                if (i != j)
                    board.SwapTiles(allSlots[i], allSlots[j]);
            }
        }
    }
}
