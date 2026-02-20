using NUnit.Framework;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Tests
{
    [TestFixture]
    public class BeamTracerTests
    {
        private BeamTracer _tracer;
        private BeamResult _result;

        [SetUp]
        public void SetUp()
        {
            _tracer = new BeamTracer();
            _result = new BeamResult();
        }

        /// <summary>
        /// Simple 3x1 board: Source → Empty → Target
        /// Source at (0,0) emitting Right, Target at (2,0) requiring Red
        /// </summary>
        [Test]
        public void StraightBeam_SourceToTarget_Satisfies()
        {
            var board = new BoardState(3, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(1, 0), TileState.CreateEmpty());
            board.SetTile(new GridPosition(2, 0), TileState.CreateTarget(LightColor.Red));

            _tracer.Trace(board, _result);

            Assert.IsTrue(_result.AllTargetsSatisfied);
            Assert.AreEqual(LightColor.Red, _result.TargetHits[new GridPosition(2, 0)]);
        }

        /// <summary>
        /// Target requires Blue but receives Red — should NOT be satisfied.
        /// </summary>
        [Test]
        public void WrongColor_DoesNotSatisfyTarget()
        {
            var board = new BoardState(3, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(1, 0), TileState.CreateEmpty());
            board.SetTile(new GridPosition(2, 0), TileState.CreateTarget(LightColor.Blue));

            _tracer.Trace(board, _result);

            Assert.IsFalse(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// Bend tile redirects beam 90°.
        /// Source(0,0)→Right, Bend(1,0) rot=0 redirects Up→Right or Right→Up.
        /// With rotation=0, entry from Left (beam going Right, enters on Left face).
        /// Local entry = Left.RotateCW(-0) = Left. Bend connects Up↔Right only.
        /// So beam from Left side doesn't connect. Let's use a proper layout.
        ///
        /// Layout (3x2):
        /// (0,0) Source emitting Down
        /// (0,1) Bend rot=0 (connects Up→Right)
        /// (1,1) Target requiring Red
        /// </summary>
        [Test]
        public void BendTile_RedirectsBeam()
        {
            var board = new BoardState(2, 2);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Down));
            board.SetTile(new GridPosition(1, 0), TileState.CreateEmpty());

            // Bend at (0,1): beam arrives going Down, enters from Up face.
            // localEntry = Up.RotateCW(0) = Up. Bend: Up → outputs Right.
            // Output = Right.RotateCW(0) = Right.
            var bend = new TileState { Type = TileType.Bend, Rotation = 0 };
            board.SetTile(new GridPosition(0, 1), bend);
            board.SetTile(new GridPosition(1, 1), TileState.CreateTarget(LightColor.Red));

            _tracer.Trace(board, _result);

            Assert.IsTrue(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// DarkAbsorber blocks beam when color doesn't match activation color.
        /// </summary>
        [Test]
        public void DarkAbsorber_BlocksWrongColor()
        {
            var board = new BoardState(3, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(1, 0), TileState.CreateDark(LightColor.Blue)); // needs Blue to pass
            board.SetTile(new GridPosition(2, 0), TileState.CreateTarget(LightColor.Red));

            _tracer.Trace(board, _result);

            Assert.IsFalse(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// DarkAbsorber lets beam through when color matches.
        /// </summary>
        [Test]
        public void DarkAbsorber_PassesMatchingColor()
        {
            var board = new BoardState(3, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(1, 0), TileState.CreateDark(LightColor.Red));
            board.SetTile(new GridPosition(2, 0), TileState.CreateTarget(LightColor.Red));

            _tracer.Trace(board, _result);

            Assert.IsTrue(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// Splitter sends beam in two directions.
        /// Source(1,0)→Down, Splitter(1,1) rot=0 (entry from Down→exits Left+Right)
        /// Targets at (0,1) and (2,1).
        /// </summary>
        [Test]
        public void Splitter_SplitsBeamInTwo()
        {
            var board = new BoardState(3, 2);
            board.SetTile(new GridPosition(0, 0), TileState.CreateEmpty());
            board.SetTile(new GridPosition(1, 0), TileState.CreateSource(LightColor.Green, Direction.Down));
            board.SetTile(new GridPosition(2, 0), TileState.CreateEmpty());

            board.SetTile(new GridPosition(0, 1), TileState.CreateTarget(LightColor.Green));

            // Splitter: beam going Down enters from Up face.
            // localEntry = Up.RotateCW(-0) = Up. Splitter only accepts entry from Down.
            // So we need rotation=2 to map: localEntry = Up.RotateCW(-2) = Down ✓
            var splitter = new TileState { Type = TileType.Splitter, Rotation = 2 };
            board.SetTile(new GridPosition(1, 1), splitter);

            board.SetTile(new GridPosition(2, 1), TileState.CreateTarget(LightColor.Green));

            _tracer.Trace(board, _result);

            Assert.IsTrue(_result.AllTargetsSatisfied);
            Assert.AreEqual(2, _result.TargetHits.Count);
        }

        /// <summary>
        /// Rotating a tile changes beam path.
        /// </summary>
        [Test]
        public void RotateTile_ChangesBeamPath()
        {
            var board = new BoardState(2, 2);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Down));
            board.SetTile(new GridPosition(1, 0), TileState.CreateEmpty());

            var bend = new TileState { Type = TileType.Bend, Rotation = 0 };
            board.SetTile(new GridPosition(0, 1), bend);
            board.SetTile(new GridPosition(1, 1), TileState.CreateTarget(LightColor.Red));

            // Initially the bend should route Down→Right (satisfies target)
            _tracer.Trace(board, _result);
            Assert.IsTrue(_result.AllTargetsSatisfied);

            // Rotate the bend — should break the path
            board.RotateTile(new GridPosition(0, 1));
            _tracer.Trace(board, _result);
            Assert.IsFalse(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// Locked tiles cannot be rotated.
        /// </summary>
        [Test]
        public void LockedTile_CannotRotate()
        {
            var board = new BoardState(1, 1);
            var tile = new TileState { Type = TileType.Bend, Rotation = 0, Locked = true };
            board.SetTile(new GridPosition(0, 0), tile);

            bool rotated = board.RotateTile(new GridPosition(0, 0));
            Assert.IsFalse(rotated);
            Assert.AreEqual(0, board.GetTile(new GridPosition(0, 0)).Rotation);
        }

        /// <summary>
        /// Board with no targets reports AllTargetsSatisfied = false.
        /// </summary>
        [Test]
        public void NoTargets_NotSatisfied()
        {
            var board = new BoardState(2, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Red, Direction.Right));
            board.SetTile(new GridPosition(1, 0), TileState.CreateEmpty());

            _tracer.Trace(board, _result);

            // No targets means nothing to satisfy
            Assert.IsFalse(_result.AllTargetsSatisfied);
        }

        /// <summary>
        /// Cross tile lets beam pass through in any direction.
        /// </summary>
        [Test]
        public void CrossTile_PassesThroughBothAxes()
        {
            var board = new BoardState(3, 1);
            board.SetTile(new GridPosition(0, 0), TileState.CreateSource(LightColor.Blue, Direction.Right));
            var cross = new TileState { Type = TileType.Cross };
            board.SetTile(new GridPosition(1, 0), cross);
            board.SetTile(new GridPosition(2, 0), TileState.CreateTarget(LightColor.Blue));

            _tracer.Trace(board, _result);
            Assert.IsTrue(_result.AllTargetsSatisfied);
        }
    }
}
