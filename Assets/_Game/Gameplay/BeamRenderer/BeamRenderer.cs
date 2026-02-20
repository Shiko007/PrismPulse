using System.Collections.Generic;
using UnityEngine;
using PrismPulse.Core.Board;

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

        [Header("References")]
        [SerializeField] private BoardView.BoardView _boardView;

        private readonly List<LineRenderer> _activeLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _pool = new List<LineRenderer>();

        /// <summary>
        /// Rebuild all beam visuals from a new BeamResult.
        /// </summary>
        public void RenderBeams(BeamResult result)
        {
            ReturnAllToPool();

            // Group segments into continuous paths by color for cleaner rendering.
            // For now, render each segment as its own line (simple, works well with bloom).
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

                Color beamColor = LightColorMap.ToEmissionColor(segment.Color, 2f);
                line.startColor = beamColor;
                line.endColor = beamColor;
                line.startWidth = _beamWidth;
                line.endWidth = _beamWidth;
            }
        }

        public void ClearBeams()
        {
            ReturnAllToPool();
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
                line.numCapVertices = 4;
                line.numCornerVertices = 4;
            }

            _activeLines.Add(line);
            return line;
        }

        private void ReturnAllToPool()
        {
            foreach (var line in _activeLines)
            {
                line.gameObject.SetActive(false);
                _pool.Add(line);
            }
            _activeLines.Clear();
        }
    }
}
