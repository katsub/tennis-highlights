using System.Collections.Generic;

namespace TennisHighlights.Utils.PoseEstimation.Keypoints
{
    /// <summary>
    /// The move label
    /// </summary>
    public enum MoveLabel
    {
        Backhand = 0,
        Forehand = 1,
        Service = 2
    }

    /// <summary>
    /// The move sample
    /// </summary>
    public class MoveSample
    {
        /// <summary>
        /// Gets the move label.
        /// </summary>
        public int MoveLabel { get; }
        /// <summary>
        /// Gets the sub label.
        /// </summary>
        public string SubLabel { get; }
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string FileName { get; }
        /// <summary>
        /// Gets the frame keypoints.
        /// </summary>
        public List<FrameKeypoints> FrameKeypoints { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveSample"/> class.
        /// </summary>
        /// <param name="move">The move.</param>
        /// <param name="subMove">The sub move.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="frameKeypoints">The frame keypoints.</param>
        public MoveSample(int move, string subMove, string fileName, List<FrameKeypoints> frameKeypoints)
        {
            MoveLabel = move;
            SubLabel = subMove;
            FileName = fileName;
            FrameKeypoints = frameKeypoints;
        }
    }
}
