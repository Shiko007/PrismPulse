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
        /// Temporary test level to verify the visual pipeline.
        /// Will be replaced by LevelData ScriptableObject loading.
        /// </summary>
        private void LoadTestLevel()
        {
            // 5x5 board with a source, some routing tiles, and a target
            var board = new BoardState(5, 5);

            // Source at top-left emitting right (red)
            board.SetTile(new GridPosition(0, 0),
                TileState.CreateSource(LightColor.Red, Direction.Right));

            // Source at bottom-left emitting up (blue)
            board.SetTile(new GridPosition(0, 4),
                TileState.CreateSource(LightColor.Blue, Direction.Up));

            // Bend at (0,2) to redirect blue beam right — needs rotation 1 (CW 90°)
            // Entry from Down face (beam going Up). Local entry = Down.RotCW(-1) = Left.
            // Wait, let's think: beam going Up enters from Down face.
            // rot=3: localEntry = Down.RotCW(-3) = Down.RotCW(1) = Left.
            // Bend: connects Up↔Right in local. Left doesn't connect.
            // rot=0: localEntry = Down. Doesn't connect.
            // Let's use a straight at (0,2) and bend at (0,1)
            // Actually, simplify: blue goes up through empties to a bend.

            // Row 1-3 on col 0: empty (blue beam passes through going up)
            // Bend at (0,1) redirects blue right
            board.SetTile(new GridPosition(0, 3), TileState.CreateEmpty());
            board.SetTile(new GridPosition(0, 2), TileState.CreateEmpty());

            // Bend at (0,1): beam going Up, enters from Down.
            // We want it to exit Right.
            // localEntry = Down.RotCW(-rotation).
            // Need localEntry=Right so bend outputs Up→... no.
            // Bend connects Up↔Right: if entry=Up → exit Right. if entry=Right → exit Up.
            // We need entry=Down to map to localEntry=Up: Down.RotCW(-rot)=Up → rot=2
            // Then output: Right.RotCW(2) = Down. That sends beam down, not right.
            //
            // Use Mirror instead for (0,1): reflects Down↔Left, Up↔Right
            // Mirror: Up→Right, Right→Up, Down→Left, Left→Down (in local space)
            // Beam going Up, entry face = Down.
            // rot=0: localEntry=Down → output Left.RotCW(0) = Left. Not what we want.
            // rot=2: localEntry=Down.RotCW(-2)=Up → output Right.RotCW(2)=Down. No.
            // rot=1: localEntry=Down.RotCW(-1)=Left → output Down.RotCW(1)=Left. No.
            // rot=3: localEntry=Down.RotCW(-3)=Right → output Up.RotCW(3)=Left. No.
            //
            // Let me use a simpler layout.
            // Red source at (0,2) going Right, Blue source at (4,2) going Left.
            // Target at (2,2) needs Purple (Red+Blue merge).

            // Reset to simpler layout
            board = new BoardState(5, 3);

            // Red source on the left
            board.SetTile(new GridPosition(0, 1),
                TileState.CreateSource(LightColor.Red, Direction.Right));

            // Blue source on the right
            board.SetTile(new GridPosition(4, 1),
                TileState.CreateSource(LightColor.Blue, Direction.Left));

            // Straight tiles leading to center
            board.SetTile(new GridPosition(1, 1),
                new TileState { Type = TileType.Straight, Rotation = 1 }); // horizontal

            board.SetTile(new GridPosition(3, 1),
                new TileState { Type = TileType.Straight, Rotation = 1 }); // horizontal

            // Target in the center — needs Purple (Red + Blue)
            board.SetTile(new GridPosition(2, 1),
                TileState.CreateTarget(LightColor.Purple));

            // Fill remaining with empties
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    var pos = new GridPosition(col, row);
                    if (board.GetTile(pos).Type == TileType.Empty
                        && pos != new GridPosition(0, 1)
                        && pos != new GridPosition(4, 1)
                        && pos != new GridPosition(1, 1)
                        && pos != new GridPosition(2, 1)
                        && pos != new GridPosition(3, 1))
                    {
                        board.SetTile(pos, TileState.CreateEmpty());
                    }
                }
            }

            // Second row: a more interesting puzzle path
            // Bend at (1,0) — player needs to rotate to route beam down
            board.SetTile(new GridPosition(1, 0),
                new TileState { Type = TileType.Bend, Rotation = 0 });

            // Green source at top
            board.SetTile(new GridPosition(2, 0),
                TileState.CreateSource(LightColor.Green, Direction.Down));

            // Target at bottom needs Green
            board.SetTile(new GridPosition(2, 2),
                TileState.CreateTarget(LightColor.Green));

            StartPuzzle(board);
        }
    }
}
