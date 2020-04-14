using System.Collections.Generic;

namespace TennisHighlights.Utils.PoseEstimation.Keypoints
{
    /// <summary>
    /// The frame keypoints
    /// </summary>
    public class FrameKeypoints
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Gets the keypoints.
        /// </summary>
        public List<Accord.Point> Keypoints { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameKeypoints"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="keypoints">The keypoints.</param>
        public FrameKeypoints(int id, List<Accord.Point> keypoints)
        {
            Id = id;

            Keypoints = keypoints;
        }
    }
}
