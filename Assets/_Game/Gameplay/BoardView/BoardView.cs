using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;
using PrismPulse.Gameplay.Audio;
using PrismPulse.Gameplay.Effects;

namespace PrismPulse.Gameplay.BoardView
{
    /// <summary>
    /// Spawns and manages all TileView instances on the board.
    /// Converts grid positions to world positions and handles tile creation.
    /// Handles tap/click input via the new Input System.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private float _tileSize = 1.1f;
        [SerializeField] private float _tileGap = 0.05f;

        [Header("Prefabs")]
        [SerializeField] private TileView _tilePrefab;

        [Header("Visual")]
        [SerializeField] private Material _tileMaterial;

        [Header("Spawn Animation")]
        [SerializeField] private float _spawnDuration = 0.3f;
        [SerializeField] private float _spawnStagger = 0.03f;

        [Header("Drag")]
        [SerializeField] private float _dragThreshold = 0.3f;

        private readonly Dictionary<GridPosition, TileView> _tileViews = new Dictionary<GridPosition, TileView>();
        private readonly HashSet<GridPosition> _previouslySatisfiedTargets = new HashSet<GridPosition>();
        private BoardState _boardState;
        private Camera _cam;
        private bool _isSpawning;
        private bool _shuffleMode;

        // Drag state
        private TileView _draggedTile;
        private Vector3 _dragStartLocalPos;
        private Vector2 _pointerDownScreenPos;
        private bool _isDragging;
        private bool _pointerIsDown;

        public System.Action<GridPosition> OnTileRotated;
        public System.Action<GridPosition, GridPosition> OnTileSwapped;
        public System.Action OnSpawnComplete;

        public bool IsSpawning => _isSpawning;
        public bool ShuffleMode { get => _shuffleMode; set => _shuffleMode = value; }

        public void Initialize(BoardState boardState)
        {
            _boardState = boardState;
            _cam = Camera.main;
            _previouslySatisfiedTargets.Clear();
            ClearBoard();
            SpawnTiles();
        }

        private void Update()
        {
            if (_boardState == null || _cam == null || _isSpawning) return;

            Vector2 screenPos = Vector2.zero;
            bool pressed = false;
            bool held = false;
            bool released = false;

            var mouse = Mouse.current;
            var touchscreen = Touchscreen.current;

            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                pressed = mouse.leftButton.wasPressedThisFrame;
                held = mouse.leftButton.isPressed;
                released = mouse.leftButton.wasReleasedThisFrame;
            }
            else if (touchscreen != null)
            {
                screenPos = touchscreen.primaryTouch.position.ReadValue();
                pressed = touchscreen.primaryTouch.press.wasPressedThisFrame;
                held = touchscreen.primaryTouch.press.isPressed;
                released = touchscreen.primaryTouch.press.wasReleasedThisFrame;
            }

            if (pressed) HandlePointerDown(screenPos);
            else if (held && _pointerIsDown) HandlePointerDrag(screenPos);
            else if (released && _pointerIsDown) HandlePointerUp(screenPos);
        }

        private void HandlePointerDown(Vector2 screenPos)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _pointerIsDown = true;
            _pointerDownScreenPos = screenPos;
            _isDragging = false;

            var ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var tileView = hit.collider.GetComponent<TileView>();
                if (tileView != null)
                {
                    var tile = _boardState.GetTile(tileView.GridPosition);
                    // Only allow dragging unlocked, non-empty tiles in shuffle mode
                    if (_shuffleMode && !tile.Locked && tile.Type != TileType.Empty)
                    {
                        _draggedTile = tileView;
                        _dragStartLocalPos = tileView.transform.localPosition;
                    }
                }
            }
        }

        private void HandlePointerDrag(Vector2 screenPos)
        {
            if (_draggedTile == null) return;

            Vector3 worldPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_cam.transform.position.z)));
            Vector3 localPos = transform.InverseTransformPoint(worldPos);

            float dist = Vector2.Distance(screenPos, _pointerDownScreenPos);
            if (!_isDragging && dist > _dragThreshold * Screen.dpi / 2.5f)
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                _draggedTile.transform.localPosition = new Vector3(localPos.x, localPos.y, -1f);
            }
        }

        private void HandlePointerUp(Vector2 screenPos)
        {
            _pointerIsDown = false;

            if (_isDragging && _draggedTile != null)
            {
                // Find the grid cell closest to the drop position
                GridPosition dropPos = WorldToGridPosition(_draggedTile.transform.localPosition);
                GridPosition fromPos = _draggedTile.GridPosition;

                if (_boardState.InBounds(dropPos) && !dropPos.Equals(fromPos))
                {
                    var targetTile = _boardState.GetTile(dropPos);
                    if (!targetTile.Locked)
                    {
                        // Valid swap (with tile or empty space) — perform it
                        PerformVisualSwap(fromPos, dropPos);
                        _draggedTile = null;
                        _isDragging = false;
                        return;
                    }
                }

                // Invalid drop — snap back
                _draggedTile.transform.DOLocalMove(_dragStartLocalPos, 0.15f).SetEase(Ease.OutQuad);
                _draggedTile = null;
                _isDragging = false;
            }
            else if (_draggedTile != null || !_isDragging)
            {
                // Short press — treat as tap (rotate)
                if (_draggedTile != null)
                    _draggedTile.transform.localPosition = _dragStartLocalPos;

                var ray = _cam.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out var hit, 100f))
                {
                    var tileView = hit.collider.GetComponent<TileView>();
                    if (tileView != null)
                        HandleTileTapped(tileView.GridPosition);
                }
                _draggedTile = null;
                _isDragging = false;
            }
        }

        private void PerformVisualSwap(GridPosition fromPos, GridPosition toPos)
        {
            var fromView = _tileViews.ContainsKey(fromPos) ? _tileViews[fromPos] : null;
            var toView = _tileViews.ContainsKey(toPos) ? _tileViews[toPos] : null;

            Vector3 fromLocalPos = GridToLocalPosition(fromPos);
            Vector3 toLocalPos = GridToLocalPosition(toPos);

            // Reset Z on dragged tile and animate to target
            if (fromView != null)
            {
                fromView.transform.localPosition = new Vector3(
                    fromView.transform.localPosition.x,
                    fromView.transform.localPosition.y, 0f);
                fromView.AnimateSwapTo(toLocalPos);
                fromView.GridPosition = toPos;
            }

            // Animate displaced tile to dragged tile's original position
            if (toView != null)
            {
                toView.AnimateSwapTo(fromLocalPos);
                toView.GridPosition = fromPos;
            }

            // Update dictionary
            if (fromView != null) _tileViews[toPos] = fromView;
            else _tileViews.Remove(toPos);

            if (toView != null) _tileViews[fromPos] = toView;
            else _tileViews.Remove(fromPos);

            if (SoundManager.Instance != null) SoundManager.Instance.PlayRotate();
            HapticFeedback.LightTap();

            OnTileSwapped?.Invoke(fromPos, toPos);
        }

        private GridPosition WorldToGridPosition(Vector3 localPos)
        {
            float spacing = _tileSize + _tileGap;
            float offsetX = (_boardState.Width - 1) * spacing * 0.5f;
            float offsetY = (_boardState.Height - 1) * spacing * 0.5f;

            int col = Mathf.RoundToInt((localPos.x + offsetX) / spacing);
            int row = Mathf.RoundToInt((_boardState.Height - 1) - (localPos.y + offsetY) / spacing);

            col = Mathf.Clamp(col, 0, _boardState.Width - 1);
            row = Mathf.Clamp(row, 0, _boardState.Height - 1);

            return new GridPosition(col, row);
        }

        private Vector3 GridToLocalPosition(GridPosition pos)
        {
            float spacing = _tileSize + _tileGap;
            float offsetX = (_boardState.Width - 1) * spacing * 0.5f;
            float offsetY = (_boardState.Height - 1) * spacing * 0.5f;
            float worldX = pos.Col * spacing - offsetX;
            float worldY = (_boardState.Height - 1 - pos.Row) * spacing - offsetY;
            return new Vector3(worldX, worldY, 0f);
        }

        private void SpawnTiles()
        {
            _isSpawning = true;
            float spacing = _tileSize + _tileGap;

            // Center the board in world space
            float offsetX = (_boardState.Width - 1) * spacing * 0.5f;
            float offsetY = (_boardState.Height - 1) * spacing * 0.5f;

            int totalTiles = _boardState.Height * _boardState.Width;
            int lastIndex = totalTiles - 1;

            for (int row = 0; row < _boardState.Height; row++)
            {
                for (int col = 0; col < _boardState.Width; col++)
                {
                    var gridPos = new GridPosition(col, row);
                    var tile = _boardState.GetTile(gridPos);

                    // World position: X = col, Y = inverted row (so row 0 is at top)
                    float worldX = col * spacing - offsetX;
                    float worldY = (_boardState.Height - 1 - row) * spacing - offsetY;

                    var tileGO = Instantiate(_tilePrefab, transform);
                    tileGO.gameObject.SetActive(true);
                    tileGO.transform.localPosition = new Vector3(worldX, worldY, 0f);
                    tileGO.transform.localScale = Vector3.zero;
                    tileGO.name = $"Tile_{col}_{row}_{tile.Type}";

                    var tileView = tileGO.GetComponent<TileView>();
                    tileView.Initialize(gridPos, tile);

                    // Staggered spawn animation: cascade top-left to bottom-right
                    int tileIndex = row * _boardState.Width + col;
                    float delay = tileIndex * _spawnStagger;
                    float targetScale = _tileSize;
                    var tween = tileGO.transform.DOScale(targetScale, _spawnDuration)
                        .SetEase(Ease.OutBack)
                        .SetDelay(delay);

                    // Fire callback when the last tile finishes animating
                    if (tileIndex == lastIndex)
                    {
                        tween.OnComplete(() =>
                        {
                            _isSpawning = false;
                            OnSpawnComplete?.Invoke();
                        });
                    }

                    _tileViews[gridPos] = tileView;
                }
            }
        }

        private void HandleTileTapped(GridPosition pos)
        {
            if (_boardState.RotateTile(pos))
            {
                var view = _tileViews[pos];
                ref var tile = ref _boardState.GetTile(pos);
                view.AnimateClick();
                view.AnimateRotation(tile.Rotation);
                view.UpdateVisual(tile);

                if (SoundManager.Instance != null) SoundManager.Instance.PlayRotate();
                HapticFeedback.LightTap();

                OnTileRotated?.Invoke(pos);
            }
        }

        /// <summary>
        /// Update tile lit states from beam result.
        /// </summary>
        public void UpdateBeamLitState(BeamResult result)
        {
            // Reset all tiles
            foreach (var kvp in _tileViews)
                kvp.Value.SetBeamLit(false);

            // Light up tiles that have beams passing through
            var litTiles = new Dictionary<GridPosition, LightColor>();
            foreach (var seg in result.Segments)
            {
                if (litTiles.ContainsKey(seg.To))
                    litTiles[seg.To] = LightColorMath.Mix(litTiles[seg.To], seg.Color);
                else
                    litTiles[seg.To] = seg.Color;
            }

            foreach (var kvp in litTiles)
            {
                if (_tileViews.TryGetValue(kvp.Key, out var view))
                    view.SetBeamLit(true, kvp.Value);
            }

            // Highlight satisfied targets and spawn sparkles on newly satisfied ones
            foreach (var kvp in result.TargetHits)
            {
                if (_tileViews.TryGetValue(kvp.Key, out var view))
                {
                    view.SetBeamLit(true, kvp.Value);

                    // Check if target is actually satisfied (required color matches)
                    var tile = _boardState.GetTile(kvp.Key);
                    if (tile.Type == TileType.Target && kvp.Value == tile.RequiredColor
                        && !_previouslySatisfiedTargets.Contains(kvp.Key))
                    {
                        _previouslySatisfiedTargets.Add(kvp.Key);
                        var worldPos = GridToWorldPosition(kvp.Key);
                        var color = LightColorMap.ToEmissionColor(kvp.Value, 2f);
                        ParticleEffectFactory.CreateTargetSatisfiedEffect(worldPos, color);

                        if (SoundManager.Instance != null) SoundManager.Instance.PlayBeamConnect();
                        HapticFeedback.MediumTap();
                    }
                }
            }

            // Remove targets that are no longer satisfied
            _previouslySatisfiedTargets.RemoveWhere(pos =>
                !result.TargetHits.ContainsKey(pos) ||
                result.TargetHits[pos] != _boardState.GetTile(pos).RequiredColor);
        }

        public Vector3 GridToWorldPosition(GridPosition pos)
        {
            float spacing = _tileSize + _tileGap;
            float offsetX = (_boardState.Width - 1) * spacing * 0.5f;
            float offsetY = (_boardState.Height - 1) * spacing * 0.5f;
            float worldX = pos.Col * spacing - offsetX;
            float worldY = (_boardState.Height - 1 - pos.Row) * spacing - offsetY;
            return transform.TransformPoint(new Vector3(worldX, worldY, 0f));
        }

        public TileView GetTileView(GridPosition pos)
        {
            return _tileViews.TryGetValue(pos, out var view) ? view : null;
        }

        /// <summary>
        /// Set a tile's rotation directly (for undo). Updates visual and animation.
        /// </summary>
        public void SetTileRotation(GridPosition pos, int rotation)
        {
            ref var tile = ref _boardState.GetTile(pos);
            tile.Rotation = rotation;

            if (_tileViews.TryGetValue(pos, out var view))
            {
                view.AnimateRotation(rotation);
                view.UpdateVisual(tile);
            }
        }

        /// <summary>
        /// Swap two TileView positions visually (for undo). Animates both tiles.
        /// </summary>
        public void SwapTileViews(GridPosition posA, GridPosition posB)
        {
            var viewA = _tileViews.ContainsKey(posA) ? _tileViews[posA] : null;
            var viewB = _tileViews.ContainsKey(posB) ? _tileViews[posB] : null;

            Vector3 localA = GridToLocalPosition(posA);
            Vector3 localB = GridToLocalPosition(posB);

            // viewA goes to posB's world position and vice versa
            if (viewA != null)
            {
                viewA.AnimateSwapTo(localB);
                viewA.GridPosition = posB;
            }
            if (viewB != null)
            {
                viewB.AnimateSwapTo(localA);
                viewB.GridPosition = posA;
            }

            // Swap dictionary entries
            if (viewA != null) _tileViews[posB] = viewA;
            else _tileViews.Remove(posB);

            if (viewB != null) _tileViews[posA] = viewB;
            else _tileViews.Remove(posA);
        }

        public void ClearBoard()
        {
            foreach (var kvp in _tileViews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.transform.DOKill();
                    Destroy(kvp.Value.gameObject);
                }
            }
            _tileViews.Clear();
        }
    }
}
