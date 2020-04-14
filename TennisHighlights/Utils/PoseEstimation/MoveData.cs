using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.Utils.PoseEstimation
{
    /// <summary>
    /// The move data
    /// </summary>
    public class MoveData
    {
        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        public string PlayerName { get; }
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
        /// <param name="playerName">Name of the player.</param>
        public MoveData(MoveLabel label, int frameId, string playerName)
        {
            Label = label;
            FrameId = frameId;
            PlayerName = playerName;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        public override string ToString() => Label + ", " + PlayerName + ", " + FrameId;
    }
}
