using UnityEngine;
using DG.Tweening;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay.BoardView
{
    /// <summary>
    /// Visual representation of a single tile on the board.
    /// Handles rotation animation and visual state.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _rotateDuration = 0.2f;
        [SerializeField] private Ease _rotateEase = Ease.OutBack;

        private GridPosition _gridPosition;
        private TileType _tileType;
        private bool _isAnimating;
        private MeshRenderer _meshRenderer;
        private MeshRenderer _indicatorRenderer;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propBlock;
        private MaterialPropertyBlock _indicatorPropBlock;

        public GridPosition GridPosition => _gridPosition;

        public void Initialize(GridPosition pos, TileState state)
        {
            _gridPosition = pos;
            _tileType = state.Type;
            _propBlock = new MaterialPropertyBlock();
            _indicatorPropBlock = new MaterialPropertyBlock();

            // Auto-find renderers
            _meshRenderer = GetComponent<MeshRenderer>();

            // Find indicator child renderer
            var indicatorTransform = transform.Find("Indicator");
            if (indicatorTransform != null)
                _indicatorRenderer = indicatorTransform.GetComponent<MeshRenderer>();

            // Set initial rotation
            transform.localRotation = Quaternion.Euler(0f, 0f, -state.Rotation * 90f);

            UpdateVisual(state);
        }

        public void UpdateVisual(TileState state)
        {
            if (_meshRenderer == null) return;

            Color tileColor;
            Color emissionColor = Color.black;
            Color indicatorColor = Color.white;
            bool showIndicator = true;

            switch (state.Type)
            {
                case TileType.Source:
                    tileColor = LightColorMap.ToUnityColor(state.SourceColor) * 0.6f;
                    tileColor.a = 1f;
                    emissionColor = LightColorMap.ToEmissionColor(state.SourceColor, 3f);
                    indicatorColor = LightColorMap.ToEmissionColor(state.SourceColor, 4f);
                    break;

                case TileType.Target:
                    tileColor = LightColorMap.ToUnityColor(state.RequiredColor) * 0.25f;
                    tileColor.a = 1f;
                    indicatorColor = LightColorMap.ToUnityColor(state.RequiredColor) * 0.6f;
                    // Target indicator: show a dot/circle shape (for now, shorter bar)
                    if (_indicatorRenderer != null)
                        _indicatorRenderer.transform.localScale = new Vector3(3f, 0.6f, 1f);
                    break;

                case TileType.DarkAbsorber:
                    tileColor = new Color(0.08f, 0.04f, 0.04f, 1f);
                    indicatorColor = new Color(0.8f, 0.15f, 0.15f) * 1.2f;
                    if (_indicatorRenderer != null)
                        CreateDarkAbsorberIndicator();
                    break;

                case TileType.Empty:
                    tileColor = new Color(0.06f, 0.06f, 0.1f, 1f);
                    showIndicator = false;
                    break;

                case TileType.Straight:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f; // bright blue-white line
                    // Tall thin bar shows the passthrough axis
                    break;

                case TileType.Cross:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f;
                    // Cross: show a + shape by adding a second bar
                    if (_indicatorRenderer != null)
                    {
                        // Make the vertical bar, and we'll add a horizontal bar as second child
                        CreateCrossIndicator();
                    }
                    break;

                case TileType.Bend:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f;
                    // Bend: show an L-shape
                    if (_indicatorRenderer != null)
                        CreateBendIndicator();
                    break;

                case TileType.Splitter:
                    tileColor = new Color(0.14f, 0.12f, 0.22f, 1f);
                    indicatorColor = new Color(0.7f, 0.5f, 1f) * 1.8f;
                    if (_indicatorRenderer != null)
                        CreateSplitterIndicator();
                    break;

                case TileType.Merger:
                    tileColor = new Color(0.14f, 0.12f, 0.22f, 1f);
                    indicatorColor = new Color(1f, 0.6f, 0.3f) * 1.8f;
                    if (_indicatorRenderer != null)
                        CreateMergerIndicator();
                    break;

                case TileType.Mirror:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.9f, 0.9f, 1f) * 2f;
                    if (_indicatorRenderer != null)
                        CreateMirrorIndicator();
                    break;

                default:
                    tileColor = new Color(0.15f, 0.18f, 0.28f, 1f);
                    indicatorColor = new Color(0.4f, 0.5f, 0.8f) * 1.5f;
                    break;
            }

            // Apply tile background color
            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, tileColor);
            _propBlock.SetColor(EmissionColorId, emissionColor);
            _meshRenderer.SetPropertyBlock(_propBlock);

            // Apply indicator color and visibility to all indicator renderers
            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.gameObject.SetActive(showIndicator);
                if (showIndicator)
                {
                    // Apply to primary indicator
                    _indicatorRenderer.GetPropertyBlock(_indicatorPropBlock);
                    _indicatorPropBlock.SetColor(BaseColorId, indicatorColor);
                    _indicatorPropBlock.SetColor(EmissionColorId, indicatorColor);
                    _indicatorRenderer.SetPropertyBlock(_indicatorPropBlock);

                    // Apply to any extra indicator children (Cross +, Bend L, etc.)
                    var parent = _indicatorRenderer.transform.parent;
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        var childRenderer = child.GetComponent<MeshRenderer>();
                        if (childRenderer != null && childRenderer != _indicatorRenderer)
                        {
                            childRenderer.GetPropertyBlock(_indicatorPropBlock);
                            _indicatorPropBlock.SetColor(BaseColorId, indicatorColor);
                            _indicatorPropBlock.SetColor(EmissionColorId, indicatorColor);
                            childRenderer.SetPropertyBlock(_indicatorPropBlock);
                        }
                    }
                }
            }

            // Locked non-source/target tiles: dim slightly
            if (state.Locked && state.Type != TileType.Source && state.Type != TileType.Target)
            {
                _meshRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, tileColor * 0.5f);
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
        /// Quick scale punch on click for tactile feedback.
        /// </summary>
        public void AnimateClick()
        {
            if (transform == null) return;
            transform.DOComplete();
            transform.DOPunchScale(Vector3.one * 0.12f, 0.15f, 6, 0.5f);
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

        private void CreateCrossIndicator()
        {
            // Add a horizontal bar as sibling to the vertical one
            var existing = _indicatorRenderer.transform;
            var horizontalBar = Instantiate(existing.gameObject, existing.parent);
            horizontalBar.name = "IndicatorH";
            horizontalBar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            horizontalBar.transform.localPosition = existing.localPosition;
        }

        private void CreateBendIndicator()
        {
            // L-shape: keep vertical bar but shorten it (top half only),
            // add a horizontal bar (right half only)
            var existing = _indicatorRenderer.transform;
            float z = existing.localPosition.z;

            // Shorten vertical bar and shift up
            existing.localScale = new Vector3(1f, 0.5f, 1f);
            existing.localPosition = new Vector3(0f, 0.17f, z);

            // Add horizontal half-bar shifted right
            var hBar = Instantiate(existing.gameObject, existing.parent);
            hBar.name = "IndicatorH";
            hBar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            hBar.transform.localPosition = new Vector3(0.17f, 0f, z);
            hBar.transform.localScale = new Vector3(1f, 0.5f, 1f);
        }

        private void CreateSplitterIndicator()
        {
            // T-shape: vertical bar (full) + horizontal bar at bottom
            var existing = _indicatorRenderer.transform;
            float z = existing.localPosition.z;

            // Add horizontal bar at the bottom of the vertical bar
            var hBar = Instantiate(existing.gameObject, existing.parent);
            hBar.name = "IndicatorH";
            hBar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            hBar.transform.localPosition = new Vector3(0f, -0.25f, z);
            hBar.transform.localScale = new Vector3(1f, 0.7f, 1f);
        }

        private void CreateMergerIndicator()
        {
            // Inverted T-shape: vertical bar + horizontal bar at top
            var existing = _indicatorRenderer.transform;
            float z = existing.localPosition.z;

            // Add horizontal bar at the top
            var hBar = Instantiate(existing.gameObject, existing.parent);
            hBar.name = "IndicatorH";
            hBar.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            hBar.transform.localPosition = new Vector3(0f, 0.25f, z);
            hBar.transform.localScale = new Vector3(1f, 0.7f, 1f);
        }

        private void CreateMirrorIndicator()
        {
            // Diagonal line (45° rotated bar)
            var existing = _indicatorRenderer.transform;
            existing.localRotation = Quaternion.Euler(0, 0, 45f);
            existing.localScale = new Vector3(1.2f, 0.8f, 1f);
        }

        private void CreateDarkAbsorberIndicator()
        {
            // X-shape: two crossed diagonal bars
            var existing = _indicatorRenderer.transform;
            float z = existing.localPosition.z;

            existing.localRotation = Quaternion.Euler(0, 0, 45f);
            existing.localScale = new Vector3(1f, 0.6f, 1f);

            var bar2 = Instantiate(existing.gameObject, existing.parent);
            bar2.name = "IndicatorX";
            bar2.transform.localRotation = Quaternion.Euler(0, 0, -45f);
            bar2.transform.localPosition = new Vector3(0f, 0f, z);
            bar2.transform.localScale = new Vector3(1f, 0.6f, 1f);
        }
    }
}
