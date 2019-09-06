namespace TennisHighlights.Rallies
{
    /// <summary>
    /// The rally filtering types
    /// </summary>
    public enum RallyFilteringType
    {
        Duration,
        DetectedFrames,
        Both
    }

    /// <summary>
    /// The rally filter info
    /// </summary>
    public class RallyFilterInfo
    {
        /// <summary>
        /// Gets the index of the rally before filtering.
        /// </summary>
        public int RallyBeforeFilterIndex { get; }
        /// <summary>
        /// The duration cluster
        /// </summary>
        public int DurationCluster = -1;
        /// <summary>
        /// The duration score
        /// </summary>
        public double DurationScore;
        /// <summary>
        /// The detected frames cluster
        /// </summary>
        public int DetectedFramesCluster = -1;
        /// <summary>
        /// The undetected frames scoe
        /// </summary>
        public double DetectedFramesScore;

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyFilterInfo"/> class.
        /// </summary>
        /// <param name="rallyOriginalIndex">Index of the rally beforing the filtering.</param>
        public RallyFilterInfo(int rallyBeforeFilterIndex) => RallyBeforeFilterIndex = rallyBeforeFilterIndex;
    }
}
