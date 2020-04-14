using OpenCvSharp;
namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The frame player
    /// </summary>
    public class PlayerFrameData
    {
        /// <summary>
        /// Gets the keypoints.
        /// </summary>
        internal float[] Keypoints { get; }
        /// <summary>
        /// Gets the blob.
        /// </summary>
        internal ConnectedComponents.Blob Blob { get; }
        /// <summary>
        /// Gets the top left corner.
        /// </summary>
        internal Accord.Point TopLeftCorner { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerFrameData"/> class.
        /// </summary>
        /// <param name="keypoints">The keypoints.</param>
        /// <param name="blob">The blob.</param>
        /// <param name="topLeftCorner">The top left corner.</param>
        public PlayerFrameData(float[] keypoints, ConnectedComponents.Blob blob, Accord.Point? topLeftCorner = null)
        {
            Keypoints = keypoints;
            Blob = blob;

            if (topLeftCorner.HasValue)
            {
                TopLeftCorner = topLeftCorner.Value;
            }
            else
            {
                TopLeftCorner = new Accord.Point(blob.Left, blob.Top);
            }
        }
    }
}
