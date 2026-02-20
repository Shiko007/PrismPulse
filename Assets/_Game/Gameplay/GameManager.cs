using UnityEngine;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Top-level game controller.
    /// Connects core logic (BoardState, BeamTracer) to the visual layer (BoardView, BeamRenderer).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView.BoardView _boardView;
        [SerializeField] private BeamRenderer.BeamRenderer _beamRenderer;

        [Header("Events")]
        public System.Action OnPuzzleSolved;
        public System.Action<int> OnMovePerformed;

        private BoardState _boardState;
        private BeamTracer _beamTracer;
        private BeamResult _beamResult;
        private int _moveCount;

        private void Awake()
        {
            _beamTracer = new BeamTracer();
            _beamResult = new BeamResult();
        }

        private void Start()
        {
            LoadTestLevel();
        }

        /// <summary>
        /// Initialize the game with a board state.
        /// Called by level loader or daily puzzle service.
        /// </summary>
        public void StartPuzzle(BoardState boardState)
        {
            _boardState = boardState;
            _moveCount = 0;

            _boardView.Initialize(_boardState);
            _boardView.OnTileRotated = HandleTileRotated;

            TraceAndRender();
        }

        private void HandleTileRotated(GridPosition pos)
        {
            _moveCount++;
            OnMovePerformed?.Invoke(_moveCount);

            TraceAndRender();

            if (_beamResult.AllTargetsSatisfied)
            {
                Debug.Log($"Puzzle solved in {_moveCount} moves!");
                OnPuzzleSolved?.Invoke();
            }
        }

        private void TraceAndRender()
        {
            _beamTracer.Trace(_boardState, _beamResult);
            _beamRenderer.RenderBeams(_beamResult);
            _boardView.UpdateBeamLitState(_beamResult);
        }

        /// <summary>
        /// Test level — two independent paths, no conflicts.
        ///
        ///   Col:  0           1           2           3           4
        /// Row 0:  .           .        Src(G,↓)       .           .
        /// Row 1: Src(R,→)   Str(v)*    Str(v)*      Cross       Tgt(R)
        /// Row 2:  .           .        Tgt(G)         .           .
        ///
        /// * = rotatable (starts vertical, needs 1 click to become horizontal)
        ///
        /// Solution (2 clicks):
        ///  1. Click Str(1,1) → vertical→horizontal → Red flows Right through to Cross→Tgt(R) ✓
        ///  2. Click Str(2,1) → vertical→horizontal → Red also flows through
        ///     BUT Green also needs (2,1) vertical to reach Tgt(G)...
        ///
        /// Revised: use Cross at (2,1) so Green passes down AND Red passes right.
        ///
        ///   Col:  0           1           2           3           4
        /// Row 0:  .           .        Src(G,↓)       .           .
        /// Row 1: Src(R,→)   Str(v)*    Cross(L)    Str(v)*     Tgt(R)
        /// Row 2:  .           .        Tgt(G)         .           .
        ///
        /// Solution (2 clicks):
        ///  1. Click Str(1,1) → horizontal → Red passes to Cross
        ///  2. Click Str(3,1) → horizontal → Red passes Cross→Str→Tgt(R) ✓
        ///  Green auto-flows: Src(G)↓ → Cross ↓ → Tgt(G) ✓
        /// </summary>
        private void LoadTestLevel()
        {
            var board = new BoardState(5, 3);

            // --- Sources (locked) ---
            board.SetTile(new GridPosition(0, 1),
                TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(2, 0),
                TileState.CreateSource(LightColor.Green, Direction.Down));

            // --- Cross at center (locked) — lets Red through horizontally, Green through vertically ---
            board.SetTile(new GridPosition(2, 1),
                new TileState { Type = TileType.Cross, Locked = true });

            // --- Rotatable Straights — start VERTICAL (blocking Red), player rotates to HORIZONTAL ---
            board.SetTile(new GridPosition(1, 1),
                new TileState { Type = TileType.Straight, Rotation = 0 }); // click once → rot=1 (horizontal)
            board.SetTile(new GridPosition(3, 1),
                new TileState { Type = TileType.Straight, Rotation = 0 }); // click once → rot=1 (horizontal)

            // --- Targets (locked) ---
            board.SetTile(new GridPosition(4, 1),
                TileState.CreateTarget(LightColor.Red));
            board.SetTile(new GridPosition(2, 2),
                TileState.CreateTarget(LightColor.Green));

            StartPuzzle(board);
        }
    }
}
