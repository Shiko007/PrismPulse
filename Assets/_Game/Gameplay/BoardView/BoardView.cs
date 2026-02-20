using System.Collections.Generic;
using UnityEngine;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay.BoardView
{
    /// <summary>
    /// Spawns and manages all TileView instances on the board.
    /// Converts grid positions to world positions and handles tile creation.
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
        private BoardState _boardState;

        public System.Action<GridPosition> OnTileRotated;

        public void Initialize(BoardState boardState)
        {
            _boardState = boardState;
            ClearBoard();
            SpawnTiles();
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
                    tileGO.transform.localPosition = new Vector3(worldX, worldY, 0f);
                    tileGO.transform.localScale = Vector3.one * _tileSize;
                    tileGO.name = $"Tile_{col}_{row}_{tile.Type}";

                    var tileView = tileGO.GetComponent<TileView>();
                    tileView.Initialize(gridPos, tile);
                    tileView.OnTapped = HandleTileTapped;

                    _tileViews[gridPos] = tileView;
                }
            }
        }

        private void HandleTileTapped(GridPosition pos)
        {
            if (_boardState.RotateTile(pos))
            {
                ref var tile = ref _boardState.GetTile(pos);
                _tileViews[pos].AnimateRotation(tile.Rotation);
                _tileViews[pos].UpdateVisual(tile);
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

            // Highlight satisfied targets
            foreach (var kvp in result.TargetHits)
            {
                if (_tileViews.TryGetValue(kvp.Key, out var view))
                    view.SetBeamLit(true, kvp.Value);
            }
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
                    Destroy(kvp.Value.gameObject);
            }
            _tileViews.Clear();
        }
    }
}
