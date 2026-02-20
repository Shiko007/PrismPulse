using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;
using PrismPulse.Core.Puzzle;

namespace PrismPulse.Gameplay.Levels
{
    /// <summary>
    /// Hand-crafted levels with increasing difficulty.
    /// All levels verified solvable with documented solutions.
    ///
    /// Rotation cheat sheet (for beam going in a direction, what rotation connects):
    ///   Straight: rot=0 → Up/Down axis,  rot=1 → Left/Right axis
    ///   Splitter (local: Down→L+R): beam going Down needs rot=2
    ///   Cross: works for any direction, any rotation
    /// </summary>
    public static class BuiltInLevels
    {
        public static LevelDefinition[] All => new[]
        {
            Level01(), Level02(), Level03(), Level04(),
            Level05(), Level06(), Level07(), Level08(),
        };

        // ================================================================
        // Level 1: "First Light" — Tutorial. Rotate one straight.
        //   [Src(R,→)] [Str(|)] [Tgt(R)]
        //   Solution: click Str once (vertical→horizontal). 1 move.
        // ================================================================
        private static LevelDefinition Level01()
        {
            return new LevelDefinition
            {
                Id = "01", Name = "First Light", Width = 3, Height = 1,
                ParMoves = 1, ParTimeSeconds = 10f,
                Tiles = new[]
                {
                    Src(0, 0, LightColor.Red, Direction.Right),
                    Str(1, 0, 0),
                    Tgt(2, 0, LightColor.Red),
                }
            };
        }

        // ================================================================
        // Level 2: "Crossroads" — Two beams cross via a Cross tile.
        //   Row 0:  .         .       Src(G,↓)    .        .
        //   Row 1: Src(R,→)  Str(|)   Cross(L)  Str(|)   Tgt(R)
        //   Row 2:  .         .       Tgt(G)      .        .
        //   Solution: click Str(1,1) and Str(3,1) once each. 2 moves.
        // ================================================================
        private static LevelDefinition Level02()
        {
            return new LevelDefinition
            {
                Id = "02", Name = "Crossroads", Width = 5, Height = 3,
                ParMoves = 2, ParTimeSeconds = 15f,
                Tiles = new[]
                {
                    Src(0, 1, LightColor.Red, Direction.Right),
                    Src(2, 0, LightColor.Green, Direction.Down),
                    Str(1, 1, 0),
                    Locked(2, 1, TileType.Cross),
                    Str(3, 1, 0),
                    Tgt(4, 1, LightColor.Red),
                    Tgt(2, 2, LightColor.Green),
                }
            };
        }

        // ================================================================
        // Level 3: "Color Mix" — Red + Blue = Purple at center target.
        //   [Src(R,→)] [Str(|)] [Tgt(Purple)] [Str(|)] [Src(B,←)]
        //   Solution: click both Str once each. 2 moves.
        // ================================================================
        private static LevelDefinition Level03()
        {
            return new LevelDefinition
            {
                Id = "03", Name = "Color Mix", Width = 5, Height = 1,
                ParMoves = 2, ParTimeSeconds = 20f,
                Tiles = new[]
                {
                    Src(0, 0, LightColor.Red, Direction.Right),
                    Str(1, 0, 0),
                    Tgt(2, 0, LightColor.Purple),
                    Str(3, 0, 0),
                    Src(4, 0, LightColor.Blue, Direction.Left),
                }
            };
        }

        // ================================================================
        // Level 4: "Split Path" — Splitter divides beam to two targets.
        //   Row 0:            Src(G,↓)
        //   Row 1: Tgt(G)  Splitter(L,rot=2)  Tgt(G)
        //
        //   Splitter is locked at rot=2 (accepts from Up, splits Left+Right).
        //   Beam goes Down from (1,0), enters (1,1) splitter, splits to (0,1) and (2,1).
        //   Targets at (0,1) and (2,1) receive directly. Already solved on load!
        //
        //   Add straights that start wrong to make it a puzzle:
        //   Row 0: Str(-)  Src(G,↓)  Str(-)
        //   Row 1: Tgt(G)  Split(L)  Tgt(G)
        //   Wait, that doesn't work either. Let me use a 3x3 with straights before targets.
        //
        //   Row 0:           Src(G,↓)
        //   Row 1: Str(|)  Split(L,r2)  Str(|)
        //   Row 2: Tgt(G)              Tgt(G)
        //
        //   Splitter at (1,1) rot=2: beam Down → splits Left(→goes to 0,1) and Right(→goes to 2,1)
        //   Str(0,1) rot=0 (vertical): beam going Left enters Right face.
        //     Straight rot=0 is Up/Down axis. Entry from Right → localEntry=Right.RotCW(0)=Right.
        //     Straight only connects Up/Down. Right doesn't match → blocks.
        //   So Str(0,1) needs to be rot=1 (horizontal) to let Left beam through? No...
        //     rot=1: localEntry=Right.RotCW(-1)=Up. Straight: Up→Down. output=Down.RotCW(1)=Left.
        //     That sends beam Left, off the board. Not Down to target.
        //
        //   Straights can't redirect. Need empty cells instead, and put puzzle elsewhere.
        //
        //   Simplest fix: put straights on the VERTICAL path between source and splitter.
        //
        //   Row 0:           Src(G,↓)
        //   Row 1:           Str(-)     ← starts horizontal, needs vertical (1 click)
        //   Row 2: Tgt(G)  Split(L,r2)  Tgt(G)
        //
        //   Source at (1,0)↓, Str at (1,1), Splitter at (1,2).
        //   Splitter rot=2: beam Down enters Up face. localEntry=Up.RotCW(-2)=Down. Split→L+R ✓
        //   outputs: Left.RotCW(2)=Right → (2,2). Right.RotCW(2)=Left → (0,2). ✓
        // ================================================================
        private static LevelDefinition Level04()
        {
            return new LevelDefinition
            {
                Id = "04", Name = "Split Path", Width = 3, Height = 3,
                ParMoves = 1, ParTimeSeconds = 15f,
                Tiles = new[]
                {
                    Src(1, 0, LightColor.Green, Direction.Down),
                    Str(1, 1, 1), // starts horizontal, click once → vertical, lets beam through
                    new LevelDefinition.TileDef { Col=1, Row=2, Type=TileType.Splitter, Rotation=2, Locked=true },
                    Tgt(0, 2, LightColor.Green),
                    Tgt(2, 2, LightColor.Green),
                }
            };
        }

        // ================================================================
        // Level 5: "Dark Passage" — Dark absorber blocks wrong color.
        //   [Src(R,→)] [Str(|)] [Dark(R)] [Str(|)] [Tgt(R)]
        //   Dark tile at (2,0) only passes Red. Beam is Red, so it passes.
        //   Solution: click both Str once each. 2 moves.
        // ================================================================
        private static LevelDefinition Level05()
        {
            return new LevelDefinition
            {
                Id = "05", Name = "Dark Passage", Width = 5, Height = 1,
                ParMoves = 2, ParTimeSeconds = 20f,
                Tiles = new[]
                {
                    Src(0, 0, LightColor.Red, Direction.Right),
                    Str(1, 0, 0),
                    new LevelDefinition.TileDef { Col=2, Row=0, Type=TileType.DarkAbsorber,
                        Color=LightColor.Red, Locked=true },
                    Str(3, 0, 0),
                    Tgt(4, 0, LightColor.Red),
                }
            };
        }

        // ================================================================
        // Level 6: "Three Rivers" — Three beams, crossing paths.
        //   7x3 board. No path conflicts.
        //
        //   Row 0: Src(B,→)  Str(|)  Str(|)  Src(G,↓)  .         .        .
        //   Row 1:  .         .       .      Cross(L)   Str(|)   Str(|)   Tgt(R)
        //   Row 2:  .         .      Tgt(B)  Tgt(G)     .         .        .
        //     ↑ Wait, this gets complicated. Use separate rows.
        //
        //   Simplify: 7x1 with two colors merging + 3x1 separate.
        //   Actually just use two crossing paths in a 5x5.
        //
        //   Row 0:  .      Src(B,↓)    .       .       .
        //   Row 1: Src(R,→)  Str(|)  Cross(L) Str(|)  Tgt(R)
        //   Row 2:  .        Str(-)    .       Str(-)   .
        //   Row 3:  .       Tgt(B)    .       Tgt(G)    .
        //     — Green source? Missing. Let me just do 2 paths.
        //
        //   Final: 5x5, Red horizontal + Green vertical through cross.
        //   Blue goes across row 4 independently.
        //
        //   Row 0:  .         .       Src(G,↓)   .         .
        //   Row 1:  .         .       Str(-)     .         .
        //   Row 2: Src(R,→)  Str(|)  Cross(L)   Str(|)   Tgt(R)
        //   Row 3:  .         .       Str(-)     .         .
        //   Row 4: Src(B,→)  Str(|)  Tgt(G)    Str(|)   Tgt(B)
        //
        //   Green: (2,0)↓ → Str(2,1) → Cross(2,2) → Str(2,3) → Tgt(2,4) ✓
        //   Red:   (0,2)→ → Str(1,2) → Cross(2,2) → Str(3,2) → Tgt(4,2) ✓
        //   Blue:  (0,4)→ → Str(1,4) → (2,4)=Tgt(G), blocked? Target absorbs.
        //   Conflict at (2,4) again! Move Green target to (2,4), Blue to row 4 offset.
        //
        //   Just use separate row for Blue:
        //   Row 4: Src(B,→)  Str(|)  Str(|)    Str(|)   Tgt(B)
        //   And move Green target to (2,3) with only 1 straight in between.
        // ================================================================
        private static LevelDefinition Level06()
        {
            return new LevelDefinition
            {
                Id = "06", Name = "Three Rivers", Width = 5, Height = 5,
                ParMoves = 6, ParTimeSeconds = 40f,
                Tiles = new[]
                {
                    // Green vertical: col 2, rows 0→3
                    Src(2, 0, LightColor.Green, Direction.Down),
                    Str(2, 1, 1), // needs vertical
                    Locked(2, 2, TileType.Cross),
                    Tgt(2, 3, LightColor.Green),

                    // Red horizontal: row 2, cols 0→4
                    Src(0, 2, LightColor.Red, Direction.Right),
                    Str(1, 2, 0), // needs horizontal
                    // (2,2) is Cross
                    Str(3, 2, 0), // needs horizontal
                    Tgt(4, 2, LightColor.Red),

                    // Blue horizontal: row 4, cols 0→4 (completely independent)
                    Src(0, 4, LightColor.Blue, Direction.Right),
                    Str(1, 4, 0), // needs horizontal
                    Str(2, 4, 0), // needs horizontal
                    Str(3, 4, 0), // needs horizontal
                    Tgt(4, 4, LightColor.Blue),
                }
            };
        }

        // ================================================================
        // Level 7: "Long Road" — Longer horizontal path, more straights.
        //   7x1 board. Red source, 5 straights (alternating wrong rotations), target.
        //   Solution: rotate straights at positions 1,2,3,4,5. 5 moves.
        // ================================================================
        private static LevelDefinition Level07()
        {
            return new LevelDefinition
            {
                Id = "07", Name = "Long Road", Width = 7, Height = 1,
                ParMoves = 5, ParTimeSeconds = 20f,
                Tiles = new[]
                {
                    Src(0, 0, LightColor.Blue, Direction.Right),
                    Str(1, 0, 0),
                    Str(2, 0, 0),
                    Str(3, 0, 0),
                    Str(4, 0, 0),
                    Str(5, 0, 0),
                    Tgt(6, 0, LightColor.Blue),
                }
            };
        }

        // ================================================================
        // Level 8: "Spectrum" — All three primaries merge to White.
        //   5x3 board. Three sources converge on center target.
        //
        //   Row 0:           Src(G,↓)
        //   Row 1: Src(R,→) Str Str(Cross) Str Src(B,←)
        //   Row 2:           Tgt(White)
        //
        //   Cross at (2,1) lets Red/Blue through horizontally AND Green vertically.
        //   But target needs White = R+G+B. Cross just passes through, doesn't merge.
        //   Green goes Down through cross to (2,2). Red goes Right through cross.
        //   Blue goes Left through cross.
        //   At the Cross, all three beams pass through (2,1). But target is at (2,2).
        //   Only Green reaches (2,2). Red and Blue continue past.
        //
        //   Need a different approach: no Cross. Use empty cells.
        //   Red→ Str → (2,1) empty → continues Right...
        //   All three beams converge at (2,1) which is the target.
        //   Red enters from Left. Blue enters from Right. Green enters from Up.
        //   Target absorbs all. TargetHits mix: R|G|B = White ✓
        //
        //   Row 0:           Src(G,↓)
        //   Row 1: Src(R,→)  Str(|)  Tgt(W)  Str(|)  Src(B,←)
        //
        //   Green goes Down: (2,0)→(2,1) Target. Absorbed. ✓
        //   Red goes Right: (0,1)→(1,1) Str→(2,1) Target. Absorbed. ✓
        //   Blue goes Left: (4,1)→(3,1) Str→(2,1) Target. Absorbed. ✓
        //   All mix at target: R|G|B = White ✓
        //   Solution: rotate both Str once each. 2 moves.
        // ================================================================
        private static LevelDefinition Level08()
        {
            return new LevelDefinition
            {
                Id = "08", Name = "Spectrum", Width = 5, Height = 2,
                ParMoves = 2, ParTimeSeconds = 20f,
                Tiles = new[]
                {
                    Src(2, 0, LightColor.Green, Direction.Down),
                    Src(0, 1, LightColor.Red, Direction.Right),
                    Str(1, 1, 0),
                    Tgt(2, 1, LightColor.White),
                    Str(3, 1, 0),
                    Src(4, 1, LightColor.Blue, Direction.Left),
                }
            };
        }

        // === Helpers ===

        private static LevelDefinition.TileDef Src(int col, int row, LightColor color, Direction dir)
        {
            return new LevelDefinition.TileDef
            { Col = col, Row = row, Type = TileType.Source, Color = color, Direction = dir, Locked = true };
        }

        private static LevelDefinition.TileDef Tgt(int col, int row, LightColor color)
        {
            return new LevelDefinition.TileDef
            { Col = col, Row = row, Type = TileType.Target, Color = color, Locked = true };
        }

        private static LevelDefinition.TileDef Str(int col, int row, int rotation)
        {
            return new LevelDefinition.TileDef
            { Col = col, Row = row, Type = TileType.Straight, Rotation = rotation, Locked = false };
        }

        private static LevelDefinition.TileDef Locked(int col, int row, TileType type)
        {
            return new LevelDefinition.TileDef
            { Col = col, Row = row, Type = type, Locked = true };
        }
    }
}
