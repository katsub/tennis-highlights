using OpenCvSharp;
using System.Collections.Generic;
using TennisHighlights.Utils.PoseEstimation.Keypoints;

namespace TennisHighlights.ImageProcessing
{
    /// <summary>
    /// The extraction over arguments
    /// </summary>
    public class ExtractionOverArguments
    {
        /// <summary>
        /// Gets the keypoint extractor.
        /// </summary>
        public KeypointExtractor KeypointExtractor { get; }
        /// <summary>
        /// Gets the balls.
        /// </summary>
        public List<Accord.Point> Balls { get; }
        /// <summary>
        /// Gets the players.
        /// </summary>
        public List<ConnectedComponents.Blob> Players { get; }
        /// <summary>
        /// Gets the original mat.
        /// </summary>
        public MatOfByte3 OriginalMat { get; }
        /// <summary>
        /// Gets the keypoint resize mat.
        /// </summary>
        public MatOfByte3 KeypointResizeMat { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractionOverArguments"/> class.
        /// </summary>
        /// <param name="balls">The balls.</param>
        /// <param name="players">The players.</param>
        /// <param name="mat">The mat.</param>
        /// <param name="keypointResizeMat">The keypoint resize mat.</param>
        /// <param name="extractor">The extractor.</param>
        public ExtractionOverArguments(List<Accord.Point> balls, List<ConnectedComponents.Blob> players, MatOfByte3 mat, MatOfByte3 keypointResizeMat, KeypointExtractor extractor)
        {
            Balls = balls;
            Players = players;
            OriginalMat = mat;
            KeypointResizeMat = keypointResizeMat;
            KeypointExtractor = extractor;
        }
    }
}
