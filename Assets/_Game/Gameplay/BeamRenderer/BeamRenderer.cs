using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PrismPulse.Core.Board;
using PrismPulse.Core.Colors;

namespace PrismPulse.Gameplay.BeamRenderer
{
    /// <summary>
    /// Renders beam segments as glowing lines using LineRenderers.
    /// Creates and pools LineRenderer objects to avoid per-frame allocation.
    /// </summary>
    public class BeamRenderer : MonoBehaviour
    {
        [Header("Beam Visual")]
        [SerializeField] private Material _beamMaterial;
        [SerializeField] private float _beamWidth = 0.08f;
        [SerializeField] private float _beamZOffset = -0.1f;

        [Header("Beam Animation")]
        [SerializeField] private float _beamGrowDuration = 0.2f;
        [SerializeField] private float _beamGrowStagger = 0.02f;

        [Header("References")]
        [SerializeField] private BoardView.BoardView _boardView;

        private readonly List<LineRenderer> _activeLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private readonly List<Tweener> _beamTweens = new List<Tweener>();

        // Track previous segments for diffing — only animate new/changed beams
        private readonly HashSet<(GridPosition from, GridPosition to, LightColor color)> _previousSegmentKeys =
            new HashSet<(GridPosition, GridPosition, LightColor)>();

        private static readonly AnimationCurve BeamWidthCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0.7f));

        /// <summary>
        /// Rebuild all beam visuals from a new BeamResult.
        /// When animateAll is true (e.g. level load), all beams get the grow animation.
        /// Otherwise only new/changed segments animate; unchanged ones appear instantly.
        /// </summary>
        public void RenderBeams(BeamResult result, bool animateAll = false)
        {
            // Build key set for the new result
            var newKeys = new HashSet<(GridPosition from, GridPosition to, LightColor color)>();
            foreach (var seg in result.Segments)
                newKeys.Add((seg.From, seg.To, seg.Color));

            ReturnAllToPool();

            int newSegmentIndex = 0;
            foreach (var segment in result.Segments)
            {
                var line = GetLineRenderer();

                Vector3 from = _boardView.GridToWorldPosition(segment.From);
                Vector3 to = _boardView.GridToWorldPosition(segment.To);
                from.z = _beamZOffset;
                to.z = _beamZOffset;

                line.positionCount = 2;
                line.SetPosition(0, from);
                line.SetPosition(1, to);

                Color beamColor = LightColorMap.ToEmissionColor(segment.Color, 2.5f);
                line.startColor = beamColor;
                line.endColor = beamColor;
                line.widthCurve = BeamWidthCurve;

                var key = (segment.From, segment.To, segment.Color);
                bool isNew = animateAll || !_previousSegmentKeys.Contains(key);

                if (isNew)
                {
                    // Animate beam growing from zero width
                    line.widthMultiplier = 0f;
                    float delay = newSegmentIndex * _beamGrowStagger;
                    var tween = DOTween.To(
                        () => line.widthMultiplier,
                        w => line.widthMultiplier = w,
                        _beamWidth, _beamGrowDuration
                    ).SetEase(Ease.OutQuad).SetDelay(delay);
                    _beamTweens.Add(tween);
                    newSegmentIndex++;
                }
                else
                {
                    // Unchanged segment — show at full width immediately
                    line.widthMultiplier = _beamWidth;
                }
            }

            // Store current keys for next diff
            _previousSegmentKeys.Clear();
            foreach (var key in newKeys)
                _previousSegmentKeys.Add(key);
        }

        public void ClearBeams()
        {
            ReturnAllToPool();
            _previousSegmentKeys.Clear();
        }

        private LineRenderer GetLineRenderer()
        {
            LineRenderer line;

            if (_pool.Count > 0)
            {
                line = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
                line.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Beam");
                go.transform.SetParent(transform);
                line = go.AddComponent<LineRenderer>();
                line.material = _beamMaterial;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.useWorldSpace = true;
                line.numCapVertices = 8;
                line.numCornerVertices = 8;
            }

            _activeLines.Add(line);
            return line;
        }

        private void ReturnAllToPool()
        {
            foreach (var tween in _beamTweens)
                tween.Kill();
            _beamTweens.Clear();

            foreach (var line in _activeLines)
            {
                line.gameObject.SetActive(false);
                _pool.Add(line);
            }
            _activeLines.Clear();
        }
    }
}
