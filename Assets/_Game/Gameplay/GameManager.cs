using System.Collections.Generic;
using UnityEngine;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;
using PrismPulse.Core.Puzzle;
using PrismPulse.Gameplay.Audio;
using PrismPulse.Gameplay.Effects;
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
        [SerializeField] private MainMenu _mainMenu;

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
                _winScreen.OnMainMenu = ShowMainMenu;
            }

            if (_mainMenu != null)
            {
                _mainMenu.Initialize();
                _mainMenu.OnPlay = StartGame;
                if (_hud != null) _hud.Hide();
                _mainMenu.Show();
            }
            else
            {
                // No menu — load first level directly (fallback)
                LoadLevel(_currentLevelIndex);
            }
        }

        public void StartGame()
        {
            if (_mainMenu != null) _mainMenu.Hide();
            if (_hud != null) _hud.Show();
            LoadLevel(_currentLevelIndex);
        }

        public void ShowMainMenu()
        {
            // Hide game elements
            _boardView.ClearBoard();
            _beamRenderer.ClearBeams();
            if (_hud != null) { _hud.Stop(); _hud.Hide(); }
            if (_winScreen != null) _winScreen.Hide();

            _currentLevelIndex = 0;

            if (_mainMenu != null) _mainMenu.Show();
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
            FitCameraToBoard(level.Width, level.Height);

            if (_hud != null)
                _hud.SetLevelInfo($"{level.Id}. {level.Name}", level.ParMoves, level.ParTimeSeconds);

            if (_winScreen != null)
                _winScreen.Hide();

            TraceAndRender();

            if (SoundManager.Instance != null) SoundManager.Instance.PlayLevelStart();
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

                // Celebration particles using level's beam colors
                var levelColors = GetLevelColors();
                ParticleEffectFactory.CreatePuzzleSolvedEffect(Vector3.zero, levelColors);

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySolve();
                HapticFeedback.Success();

                if (_winScreen != null)
                    _winScreen.Show(stars, _moveCount, time, hasNext);
            }
        }

        private void FitCameraToBoard(int boardWidth, int boardHeight)
        {
            var cam = Camera.main;
            if (cam == null) return;

            float tileSpacing = 1.15f; // tileSize + gap
            float boardWorldWidth = boardWidth * tileSpacing;
            float boardWorldHeight = boardHeight * tileSpacing;

            float screenAspect = (float)Screen.width / Screen.height;

            // Account for HUD space at top (~15% of screen) and bottom padding
            float verticalPadding = 1.8f;
            float horizontalPadding = 1.2f;

            // Orthographic size is half the vertical extent
            float sizeForHeight = (boardWorldHeight * 0.5f) + verticalPadding;
            float sizeForWidth = ((boardWorldWidth * 0.5f) + horizontalPadding) / screenAspect;

            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        }

        private Color[] GetLevelColors()
        {
            var colors = new List<Color>(4);
            foreach (var kvp in _beamResult.TargetHits)
            {
                var c = LightColorMap.ToUnityColor(kvp.Value);
                if (!colors.Contains(c))
                    colors.Add(c);
            }
            return colors.ToArray();
        }

        private void TraceAndRender()
        {
            _beamTracer.Trace(_boardState, _beamResult);
            _beamRenderer.RenderBeams(_beamResult);
            _boardView.UpdateBeamLitState(_beamResult);
        }
    }
}
