using UnityEngine;
using PrismPulse.Core.Board;
using PrismPulse.Core.Puzzle;
using PrismPulse.Gameplay.Levels;
using PrismPulse.Gameplay.UI;

namespace PrismPulse.Gameplay
{
    /// <summary>
    /// Top-level game controller.
    /// Manages level loading, core logic, visual updates, and UI coordination.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardView.BoardView _boardView;
        [SerializeField] private BeamRenderer.BeamRenderer _beamRenderer;
        [SerializeField] private GameHUD _hud;
        [SerializeField] private WinScreen _winScreen;

        private BoardState _boardState;
        private BeamTracer _beamTracer;
        private BeamResult _beamResult;
        private int _moveCount;
        private bool _puzzleSolved;

        private LevelDefinition[] _levels;
        private int _currentLevelIndex;

        private void Awake()
        {
            _beamTracer = new BeamTracer();
            _beamResult = new BeamResult();
        }

        private void Start()
        {
            _levels = BuiltInLevels.All;
            _currentLevelIndex = 0;

            if (_hud != null) _hud.Initialize();

            if (_winScreen != null)
            {
                _winScreen.Initialize();
                _winScreen.OnNextLevel = NextLevel;
                _winScreen.OnRestart = RestartLevel;
            }

            LoadLevel(_currentLevelIndex);
        }

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= _levels.Length) return;

            _currentLevelIndex = index;
            var level = _levels[index];

            _boardState = level.ToBoardState();
            _moveCount = 0;
            _puzzleSolved = false;

            _boardView.Initialize(_boardState);
            _boardView.OnTileRotated = HandleTileRotated;

            if (_hud != null)
                _hud.SetLevelInfo($"{level.Id}. {level.Name}", level.ParMoves, level.ParTimeSeconds);

            if (_winScreen != null)
                _winScreen.Hide();

            TraceAndRender();
        }

        public void NextLevel()
        {
            if (_currentLevelIndex + 1 < _levels.Length)
                LoadLevel(_currentLevelIndex + 1);
        }

        public void RestartLevel()
        {
            LoadLevel(_currentLevelIndex);
        }

        private void HandleTileRotated(GridPosition pos)
        {
            if (_puzzleSolved) return;

            _moveCount++;
            if (_hud != null) _hud.OnMove(_moveCount);

            TraceAndRender();

            if (_beamResult.AllTargetsSatisfied)
            {
                _puzzleSolved = true;
                if (_hud != null) _hud.Stop();

                bool hasNext = _currentLevelIndex + 1 < _levels.Length;
                int stars = _hud != null ? _hud.GetStarRating() : 1;
                float time = _hud != null ? _hud.ElapsedTime : 0f;

                Debug.Log($"Puzzle '{_levels[_currentLevelIndex].Name}' solved! " +
                          $"{_moveCount} moves, {time:F1}s, {stars} stars");

                if (_winScreen != null)
                    _winScreen.Show(stars, _moveCount, time, hasNext);
            }
        }

        private void TraceAndRender()
        {
            _beamTracer.Trace(_boardState, _beamResult);
            _beamRenderer.RenderBeams(_beamResult);
            _boardView.UpdateBeamLitState(_beamResult);
        }
    }
}
