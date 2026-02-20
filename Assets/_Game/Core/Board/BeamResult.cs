using System.Collections.Generic;
using PrismPulse.Core.Colors;

namespace PrismPulse.Core.Board
{
    /// <summary>
    /// The result of tracing all beams on a board.
    /// Consumed by the visual layer to render beams and check win state.
    /// </summary>
    public class BeamResult
    {
        public readonly List<BeamSegment> Segments = new List<BeamSegment>();
        public readonly Dictionary<GridPosition, LightColor> TargetHits = new Dictionary<GridPosition, LightColor>();

        /// <summary>
        /// True when every Target tile on the board receives its required color.
        /// </summary>
        public bool AllTargetsSatisfied;

        public void Clear()
        {
            Segments.Clear();
            TargetHits.Clear();
            AllTargetsSatisfied = false;
        }
    }
}
