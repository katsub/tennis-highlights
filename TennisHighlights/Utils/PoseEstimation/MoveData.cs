using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.Utils.PoseEstimation
{
    /// <summary>
    /// The move data
    /// </summary>
    public class MoveData
    {
        /// <summary>
        /// Gets the speed.
        /// </summary>
        public Accord.Point Speed { get; }
        /// <summary>
        /// Gets the frame identifier.
        /// </summary>
        public int FrameId { get; }
        /// <summary>
        /// Gets the label.
        /// </summary>
        public MoveLabel Label { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveData"/> class.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="frameId">The frame identifier.</param>
        /// <param name="speed">The speed.</param>
        public MoveData(MoveLabel label, int frameId, Accord.Point speed)
        {
            Label = label;
            FrameId = frameId;
            Speed = speed;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => Label + ", " + $"({Speed.X},{Speed.Y}), " + FrameId;
    }
}
