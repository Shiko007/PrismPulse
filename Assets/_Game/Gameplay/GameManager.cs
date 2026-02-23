using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;
using PrismPulse.Core.Puzzle;
using PrismPulse.Gameplay.Audio;
using PrismPulse.Gameplay.Effects;
using PrismPulse.Gameplay.Levels;
using PrismPulse.Gameplay.Progress;
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
        [SerializeField] private LevelSelectScreen _levelSelect;
        [SerializeField] private TutorialManager _tutorial;

        [Header("Transition")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _shakeStrength = 0.15f;

        private BoardState _boardState;
        private BeamTracer _beamTracer;
        private BeamResult _beamResult;
        private int _moveCount;
        private bool _puzzleSolved;
        private Image _fadeOverlay;
        private Canvas _fadeCanvas;

        private LevelDefinition[] _levels;
        private int _currentLevelIndex;

        private struct UndoEntry
        {
            public bool IsSwap;
            public GridPosition Pos;
            public int PrevRotation;
            public GridPosition SwapTarget;
        }

        private readonly Stack<UndoEntry> _undoStack = new Stack<UndoEntry>();
        private Dictionary<GridPosition, int> _solution;

        private void Awake()
        {
            _beamTracer = new BeamTracer();
            _beamResult = new BeamResult();
            CreateFadeOverlay();
        }

        private void Start()
        {
            _levels = BuiltInLevels.All;
            _currentLevelIndex = 0;

            if (_hud != null)
            {
                _hud.Initialize();
                _hud.OnUndo = UndoLastMove;
                _hud.OnHint = ShowHint;
            }

            if (_winScreen != null)
            {
                _winScreen.Initialize();
                _winScreen.OnNextLevel = NextLevel;
                _winScreen.OnRestart = RestartLevel;
                _winScreen.OnMainMenu = ShowLevelSelect;
            }

            if (_levelSelect != null)
            {
                _levelSelect.Initialize(_levels);
                _levelSelect.OnLevelSelected = StartLevel;
                _levelSelect.OnBack = ShowMainMenu;
            }

            if (_tutorial != null)
                _tutorial.Initialize();

            if (_mainMenu != null)
            {
                _mainMenu.Initialize();
                _mainMenu.OnPlay = ShowLevelSelect;
                if (_hud != null) _hud.Hide();
                _mainMenu.Show();
            }
            else
            {
                LoadLevel(_currentLevelIndex);
            }
        }

        public void ShowLevelSelect()
        {
            // Hide game elements
            _boardView.ClearBoard();
            _beamRenderer.ClearBeams();
            if (_hud != null) { _hud.Stop(); _hud.Hide(); }
            if (_winScreen != null) _winScreen.Hide();
            if (_mainMenu != null) _mainMenu.Hide();

            if (_levelSelect != null) _levelSelect.Show();
        }

        public void StartLevel(int index)
        {
            StartCoroutine(FadeTransition(() =>
            {
                if (_levelSelect != null) _levelSelect.Hide();
                if (_hud != null) _hud.Show();
                LoadLevel(index);
            }));
        }

        public void ShowMainMenu()
        {
            _boardView.ClearBoard();
            _beamRenderer.ClearBeams();
            if (_hud != null) { _hud.Stop(); _hud.Hide(); }
            if (_winScreen != null) _winScreen.Hide();
            if (_levelSelect != null) _levelSelect.Hide();

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
            _undoStack.Clear();
            _solution = PuzzleSolver.Solve(_boardState);
            if (_hud != null) _hud.SetUndoInteractable(false);

            _beamRenderer.ClearBeams();
            _boardView.ShuffleMode = level.ShuffleMode;
            if (level.ShuffleMode)
            {
                int seed = GetDeterministicHash(level.Id);
                ShuffleBoardState(_boardState, seed);
            }
            _boardView.Initialize(_boardState);
            _boardView.OnTileRotated = HandleTileRotated;
            _boardView.OnTileSwapped = HandleTileSwapped;
            _boardView.OnSpawnComplete = () =>
            {
                TraceAndRender(animateAllBeams: true);
            };
            FitCameraToBoard(5, 5);

            if (_hud != null)
                _hud.SetLevelInfo($"{level.Id}. {level.Name}", level.ParMoves, level.ParTimeSeconds);

            if (_winScreen != null)
                _winScreen.Hide();

            if (SoundManager.Instance != null) SoundManager.Instance.PlayLevelStart();

            if (_tutorial != null && _tutorial.TryShowTutorial(_currentLevelIndex))
            {
                // Pause timer during tutorial, resume when dismissed
                if (_hud != null) _hud.Stop();
                _tutorial.OnTutorialComplete = () =>
                {
                    if (_hud != null) _hud.Resume();
                };
            }
        }

        public void NextLevel()
        {
            if (_currentLevelIndex + 1 < _levels.Length)
            {
                int next = _currentLevelIndex + 1;
                StartCoroutine(FadeTransition(() =>
                {
                    if (_winScreen != null) _winScreen.Hide();
                    LoadLevel(next);
                }));
            }
        }

        public void RestartLevel()
        {
            int current = _currentLevelIndex;
            StartCoroutine(FadeTransition(() =>
            {
                if (_winScreen != null) _winScreen.Hide();
                LoadLevel(current);
            }));
        }

        private void HandleTileRotated(GridPosition pos)
        {
            if (_puzzleSolved) return;

            // Push previous rotation for undo (current rotation is already incremented)
            int currentRotation = _boardState.GetTile(pos).Rotation;
            int prevRotation = (currentRotation + 3) % 4;
            _undoStack.Push(new UndoEntry { IsSwap = false, Pos = pos, PrevRotation = prevRotation });
            if (_hud != null) _hud.SetUndoInteractable(true);

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

                // Save progress
                ProgressManager.SetStars(_levels[_currentLevelIndex].Id, stars);

                Debug.Log($"Puzzle '{_levels[_currentLevelIndex].Name}' solved! " +
                          $"{_moveCount} moves, {time:F1}s, {stars} stars");

                var levelColors = GetLevelColors();
                ParticleEffectFactory.CreatePuzzleSolvedEffect(Vector3.zero, levelColors);

                // Camera shake on solve
                var cam = Camera.main;
                if (cam != null)
                    cam.transform.DOShakePosition(0.3f, _shakeStrength, 10, 90f);

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySolve();
                HapticFeedback.Success();

                if (_winScreen != null)
                    _winScreen.Show(stars, _moveCount, time, hasNext);
            }
        }

        private void HandleTileSwapped(GridPosition from, GridPosition to)
        {
            if (_puzzleSolved) return;

            _boardState.SwapTiles(from, to);
            _undoStack.Push(new UndoEntry { IsSwap = true, Pos = from, SwapTarget = to });
            if (_hud != null) _hud.SetUndoInteractable(true);

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

                Progress.ProgressManager.SetStars(_levels[_currentLevelIndex].Id, stars);

                var levelColors = GetLevelColors();
                Effects.ParticleEffectFactory.CreatePuzzleSolvedEffect(Vector3.zero, levelColors);

                var cam = Camera.main;
                if (cam != null)
                    cam.transform.DOShakePosition(0.3f, _shakeStrength, 10, 90f);

                if (SoundManager.Instance != null) SoundManager.Instance.PlaySolve();
                HapticFeedback.Success();

                if (_winScreen != null)
                    _winScreen.Show(stars, _moveCount, time, hasNext);
            }
        }

        public void UndoLastMove()
        {
            if (_puzzleSolved || _undoStack.Count == 0) return;

            var entry = _undoStack.Pop();

            if (entry.IsSwap)
            {
                // Swap back
                _boardState.SwapTiles(entry.Pos, entry.SwapTarget);
                _boardView.SwapTileViews(entry.Pos, entry.SwapTarget);
            }
            else
            {
                _boardView.SetTileRotation(entry.Pos, entry.PrevRotation);
            }

            _moveCount--;
            if (_hud != null)
            {
                _hud.OnMove(_moveCount);
                _hud.SetUndoInteractable(_undoStack.Count > 0);
            }

            if (SoundManager.Instance != null) SoundManager.Instance.PlayRotate();
            HapticFeedback.LightTap();

            TraceAndRender();
        }

        public void ShowHint()
        {
            if (_puzzleSolved) return;

            // Re-solve from current state so the hint is always optimal
            _solution = PuzzleSolver.Solve(_boardState);
            if (_solution == null) return;

            var hint = PuzzleSolver.GetNextHint(_boardState, _solution);
            if (hint == null) return;

            var tileView = _boardView.GetTileView(hint.Value.pos);
            if (tileView != null)
            {
                tileView.AnimateHintPulse();
                if (SoundManager.Instance != null) SoundManager.Instance.PlayBeamConnect();
                HapticFeedback.MediumTap();
            }
        }

        /// <summary>
        /// Shuffle unlocked tile positions across the entire 5x5 board.
        /// Tiles can land on any non-locked cell (including empty ones).
        /// </summary>
        private void ShuffleBoardState(BoardState board, int seed)
        {
            var rng = new System.Random(seed);

            var allSlots = new List<GridPosition>();
            var tilePositions = new List<GridPosition>();

            for (int row = 0; row < board.Height; row++)
            {
                for (int col = 0; col < board.Width; col++)
                {
                    var pos = new GridPosition(col, row);
                    var tile = board.GetTile(pos);
                    if (tile.Locked) continue;
                    allSlots.Add(pos);
                    if (tile.Type != TileType.Empty)
                        tilePositions.Add(pos);
                }
            }

            if (tilePositions.Count < 2 || allSlots.Count < 2) return;

            for (int i = allSlots.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                if (i != j)
                    board.SwapTiles(allSlots[i], allSlots[j]);
            }
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

        private void FitCameraToBoard(int boardWidth, int boardHeight)
        {
            var cam = Camera.main;
            if (cam == null) return;

            float tileSpacing = 1.15f;
            float boardWorldWidth = boardWidth * tileSpacing;
            float boardWorldHeight = boardHeight * tileSpacing;

            float screenAspect = (float)Screen.width / Screen.height;

            float verticalPadding = 1.8f;
            float horizontalPadding = 1.2f;

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

        private void TraceAndRender(bool animateAllBeams = false)
        {
            _beamTracer.Trace(_boardState, _beamResult);
            _beamRenderer.RenderBeams(_beamResult, animateAllBeams);
            _boardView.UpdateBeamLitState(_beamResult);
        }

        private void CreateFadeOverlay()
        {
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);
            _fadeCanvas = canvasGO.AddComponent<Canvas>();
            _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 100;

            var overlayGO = new GameObject("FadeOverlay");
            overlayGO.transform.SetParent(canvasGO.transform, false);
            var rect = overlayGO.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            _fadeOverlay = overlayGO.AddComponent<Image>();
            _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _fadeOverlay.raycastTarget = false;
        }

        private IEnumerator FadeTransition(System.Action onMidpoint)
        {
            // Fade out (to black)
            _fadeOverlay.raycastTarget = true;
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                _fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            _fadeOverlay.color = new Color(0f, 0f, 0f, 1f);

            onMidpoint?.Invoke();

            // Fade in (from black)
            elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
                _fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _fadeOverlay.raycastTarget = false;
        }
    }
}
