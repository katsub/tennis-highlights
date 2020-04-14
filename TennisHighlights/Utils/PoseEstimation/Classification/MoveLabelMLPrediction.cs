using Microsoft.ML.Data;

namespace TennisHighlights.Utils.PoseEstimation.Classification
{
    /// <summary>
    /// The movel label ML.Net prediction
    /// </summary>
    public class MoveLabelMLPrediction
    {
        [ColumnName("Label")]
        [KeyType(3)]
        public uint Label { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => "Label: " + Label;
    }
}
