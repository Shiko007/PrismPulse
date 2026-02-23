using System.Collections.Generic;
using UnityEngine;
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

        private readonly Dictionary<GridPosition, TileView> _tileViews = new Dictionary<GridPosition, TileView>();
        private readonly HashSet<GridPosition> _previouslySatisfiedTargets = new HashSet<GridPosition>();
        private BoardState _boardState;
        private Camera _cam;

        public System.Action<GridPosition> OnTileRotated;

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
            if (_boardState == null || _cam == null) return;

            // Detect click/tap using new Input System
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                HandleClick(mouse.position.ReadValue());
                return;
            }

            // Also support touch
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                HandleClick(touchscreen.primaryTouch.position.ReadValue());
            }
        }

        private void HandleClick(Vector2 screenPos)
        {
            var ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var tileView = hit.collider.GetComponent<TileView>();
                if (tileView != null)
                {
                    HandleTileTapped(tileView.GridPosition);
                }
            }
        }

        private void SpawnTiles()
        {
            float spacing = _tileSize + _tileGap;

            // Center the board in world space
            float offsetX = (_boardState.Width - 1) * spacing * 0.5f;
            float offsetY = (_boardState.Height - 1) * spacing * 0.5f;

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
                    tileGO.transform.localScale = Vector3.one * _tileSize;
                    tileGO.name = $"Tile_{col}_{row}_{tile.Type}";

                    var tileView = tileGO.GetComponent<TileView>();
                    tileView.Initialize(gridPos, tile);

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

        private void ClearBoard()
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
