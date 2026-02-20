using UnityEngine;
using DG.Tweening;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay.BoardView
{
    /// <summary>
    /// Visual representation of a single tile on the board.
    /// Handles tap-to-rotate input and rotation animation.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private GameObject _directionIndicator;

        [Header("Animation")]
        [SerializeField] private float _rotateDuration = 0.2f;
        [SerializeField] private Ease _rotateEase = Ease.OutBack;

        private GridPosition _gridPosition;
        private TileType _tileType;
        private bool _isAnimating;

        // Events
        public System.Action<GridPosition> OnTapped;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propBlock;

        public GridPosition GridPosition => _gridPosition;

        public void Initialize(GridPosition pos, TileState state)
        {
            _gridPosition = pos;
            _tileType = state.Type;
            _propBlock = new MaterialPropertyBlock();

            // Set initial rotation
            transform.localRotation = Quaternion.Euler(0f, 0f, -state.Rotation * 90f);

            UpdateVisual(state);
        }

        public void UpdateVisual(TileState state)
        {
            if (_meshRenderer == null) return;

            Color tileColor;
            Color emissionColor = Color.black;

            switch (state.Type)
            {
                case TileType.Source:
                    tileColor = LightColorMap.ToUnityColor(state.SourceColor);
                    emissionColor = LightColorMap.ToEmissionColor(state.SourceColor, 3f);
                    break;
                case TileType.Target:
                    tileColor = LightColorMap.ToUnityColor(state.RequiredColor) * 0.4f;
                    tileColor.a = 1f;
                    break;
                case TileType.DarkAbsorber:
                    tileColor = new Color(0.05f, 0.05f, 0.08f, 1f);
                    break;
                case TileType.Empty:
                    tileColor = new Color(0.12f, 0.12f, 0.18f, 0.3f);
                    break;
                default:
                    // Crystal tiles — translucent glass look
                    tileColor = new Color(0.25f, 0.3f, 0.4f, 0.8f);
                    emissionColor = new Color(0.1f, 0.15f, 0.25f) * 0.5f;
                    break;
            }

            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, tileColor);
            _propBlock.SetColor(EmissionColorId, emissionColor);
            _meshRenderer.SetPropertyBlock(_propBlock);

            // Show direction indicator for source tiles
            if (_directionIndicator != null)
                _directionIndicator.SetActive(state.Type == TileType.Source);

            // Visual lock indicator — locked tiles are slightly darker
            if (state.Locked && state.Type != TileType.Source && state.Type != TileType.Target)
            {
                _propBlock.SetColor(BaseColorId, tileColor * 0.6f);
                _meshRenderer.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>
        /// Animate rotation by 90° clockwise. Called by BoardView after core logic confirms rotation.
        /// </summary>
        public void AnimateRotation(int newRotation)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            float targetZ = -newRotation * 90f;

            transform.DOLocalRotate(new Vector3(0f, 0f, targetZ), _rotateDuration)
                .SetEase(_rotateEase)
                .OnComplete(() => _isAnimating = false);
        }

        /// <summary>
        /// Highlight tile when beam passes through it.
        /// </summary>
        public void SetBeamLit(bool lit, LightColor beamColor = LightColor.None)
        {
            if (_meshRenderer == null || _propBlock == null) return;

            _meshRenderer.GetPropertyBlock(_propBlock);

            if (lit && _tileType != TileType.Source && _tileType != TileType.Empty)
            {
                Color emission = LightColorMap.ToEmissionColor(beamColor, 1.5f);
                _propBlock.SetColor(EmissionColorId, emission);
            }
            else if (_tileType != TileType.Source)
            {
                _propBlock.SetColor(EmissionColorId, Color.black);
            }

            _meshRenderer.SetPropertyBlock(_propBlock);
        }

        private void OnMouseDown()
        {
            if (_isAnimating) return;
            OnTapped?.Invoke(_gridPosition);
        }
    }
}
