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
        private LightColor _storedRequiredColor;
        private bool _isAnimating;
        private MeshRenderer _meshRenderer;
        private MeshRenderer _indicatorRenderer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Material _tileMat;
        private Material _indicatorMat;
        private TextMesh _colorLabel;
        private Tweener _glowTween;

        public GridPosition GridPosition
        {
            get => _gridPosition;
            set => _gridPosition = value;
        }

        public void Initialize(GridPosition pos, TileState state)
        {
            _gridPosition = pos;
            _tileType = state.Type;
            _storedRequiredColor = state.RequiredColor;

            // Auto-find renderers and create material instances (needed for SRP Batcher compat)
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
                _tileMat = _meshRenderer.material; // creates instance

            var indicatorTransform = transform.Find("Indicator");
            if (indicatorTransform != null)
            {
                _indicatorRenderer = indicatorTransform.GetComponent<MeshRenderer>();
                if (_indicatorRenderer != null)
                    _indicatorMat = _indicatorRenderer.material;
            }

            // Color-blind label for Source and Target tiles
            if (LightColorMap.ColorBlindMode &&
                (state.Type == TileType.Source || state.Type == TileType.Target
                 || state.Type == TileType.DarkAbsorber))
            {
                CreateColorLabel(state);
            }

            // Set initial rotation
            transform.localRotation = Quaternion.Euler(0f, 0f, -state.Rotation * 90f);

            UpdateVisual(state);

            // Source tiles: start idle glow pulse
            if (state.Type == TileType.Source && _tileMat != null)
            {
                Color baseColor = _tileMat.GetColor(BaseColorId);
                Color brightColor = baseColor * 1.6f;
                brightColor.a = 1f;
                _glowTween = DOTween.To(
                    () => _tileMat.GetColor(BaseColorId),
                    c => _tileMat.SetColor(BaseColorId, c),
                    brightColor, 1.5f
                ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }

        private void CreateColorLabel(TileState state)
        {
            var labelColor = state.Type == TileType.Source ? state.SourceColor
                : state.Type == TileType.DarkAbsorber ? state.ActivationColor
                : state.RequiredColor;
            string text = LightColorMap.ToLabel(labelColor);
            if (string.IsNullOrEmpty(text)) return;

            var labelGO = new GameObject("ColorLabel");
            labelGO.transform.SetParent(transform, false);
            // Counter-rotate so label stays upright regardless of tile rotation
            labelGO.transform.localPosition = new Vector3(0.3f, -0.3f, -0.1f);
            labelGO.transform.localScale = Vector3.one * 0.08f;

            _colorLabel = labelGO.AddComponent<TextMesh>();
            _colorLabel.text = text;
            _colorLabel.fontSize = 48;
            _colorLabel.anchor = TextAnchor.MiddleCenter;
            _colorLabel.alignment = TextAlignment.Center;
            _colorLabel.color = Color.white;
            _colorLabel.fontStyle = FontStyle.Bold;
        }

        public void UpdateVisual(TileState state)
        {
            if (_meshRenderer == null) return;

            // All colors go through _BaseColor on Unlit shader.
            // HDR values (>1.0) will glow via bloom post-processing.
            Color tileColor;
            Color indicatorColor = Color.white;
            bool showIndicator = true;

            switch (state.Type)
            {
                case TileType.Source:
                    // Source tiles glow with their color (HDR for bloom)
                    tileColor = LightColorMap.ToUnityColor(state.SourceColor) * 1.5f;
                    tileColor.a = 1f;
                    indicatorColor = LightColorMap.ToUnityColor(state.SourceColor) * 4f;
                    break;

                case TileType.Target:
                    tileColor = LightColorMap.ToUnityColor(state.RequiredColor) * 0.25f;
                    tileColor.a = 1f;
                    indicatorColor = LightColorMap.ToUnityColor(state.RequiredColor) * 1.5f;
                    if (_indicatorRenderer != null)
                        _indicatorRenderer.transform.localScale = new Vector3(3f, 0.6f, 1f);
                    break;

                case TileType.DarkAbsorber:
                    tileColor = new Color(0.08f, 0.04f, 0.04f, 1f);
                    indicatorColor = new Color(0.8f, 0.15f, 0.15f) * 1.5f;
                    if (_indicatorRenderer != null)
                        CreateDarkAbsorberIndicator();
                    break;

                case TileType.Empty:
                    tileColor = new Color(0.06f, 0.06f, 0.1f, 1f);
                    showIndicator = false;
                    break;

                case TileType.Straight:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f;
                    break;

                case TileType.Cross:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f;
                    if (_indicatorRenderer != null)
                        CreateCrossIndicator();
                    break;

                case TileType.Bend:
                    tileColor = new Color(0.12f, 0.14f, 0.22f, 1f);
                    indicatorColor = new Color(0.5f, 0.7f, 1f) * 2f;
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
            if (_tileMat != null)
                _tileMat.SetColor(BaseColorId, tileColor);

            // Apply indicator color and visibility
            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.gameObject.SetActive(showIndicator);
                if (showIndicator)
                {
                    if (_indicatorMat != null)
                        _indicatorMat.SetColor(BaseColorId, indicatorColor);

                    // Apply to extra indicator children (Cross +, Bend L, etc.)
                    var parent = _indicatorRenderer.transform.parent;
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        var childRenderer = child.GetComponent<MeshRenderer>();
                        if (childRenderer != null && childRenderer != _indicatorRenderer)
                            childRenderer.material.SetColor(BaseColorId, indicatorColor);
                    }
                }
            }

            // Locked non-source/target tiles: dim slightly
            if (state.Locked && state.Type != TileType.Source && state.Type != TileType.Target)
            {
                if (_tileMat != null)
                    _tileMat.SetColor(BaseColorId, tileColor * 0.5f);
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
        /// Pulse animation to highlight this tile as a hint.
        /// </summary>
        public void AnimateHintPulse()
        {
            if (transform == null) return;
            transform.DOComplete();
            transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 4, 0.3f);

            // Flash the tile bright briefly
            if (_tileMat != null)
            {
                var originalColor = _tileMat.GetColor(BaseColorId);
                _tileMat.SetColor(BaseColorId, Color.white * 2f);
                DOTween.To(
                    () => _tileMat.GetColor(BaseColorId),
                    c => _tileMat.SetColor(BaseColorId, c),
                    originalColor, 0.5f
                ).SetEase(Ease.OutQuad);
            }
        }

        private void OnDestroy()
        {
            _glowTween?.Kill();
        }

        private void LateUpdate()
        {
            // Keep color-blind label upright regardless of tile rotation
            if (_colorLabel != null)
                _colorLabel.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Animate this tile moving to a new world position (for swap).
        /// </summary>
        public void AnimateSwapTo(Vector3 targetLocalPos, float duration = 0.2f, System.Action onComplete = null)
        {
            transform.DOLocalMove(targetLocalPos, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => onComplete?.Invoke());
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
            if (_tileMat == null) return;

            if (lit && _tileType != TileType.Source && _tileType != TileType.Empty)
            {
                // Tint tile with beam color — visible and glows via bloom
                Color beamUnity = LightColorMap.ToUnityColor(beamColor);
                Color tinted = beamUnity * 0.6f;
                tinted.a = 1f;
                _tileMat.SetColor(BaseColorId, tinted);
            }
            else if (_tileType != TileType.Source)
            {
                // Restore default dark tile color
                Color dark = _tileType == TileType.Target
                    ? LightColorMap.ToUnityColor(_storedRequiredColor) * 0.25f
                    : new Color(0.12f, 0.14f, 0.22f, 1f);
                dark.a = 1f;
                _tileMat.SetColor(BaseColorId, dark);
            }
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
