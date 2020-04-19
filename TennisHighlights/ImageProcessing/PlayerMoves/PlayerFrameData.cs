namespace TennisHighlights.ImageProcessing.PlayerMoves
{
    /// <summary>
    /// The frame player
    /// </summary>
    public class PlayerFrameData
    {
        /// <summary>
        /// Gets the keypoints. Keypoint coordinates in the source image can be found by mulitplying their value by Scale then adding the TopLeftCorner coordinates
        /// </summary>
        internal float[] Keypoints { get; }
        /// <summary>
        /// Gets the top left corner.
        /// </summary>
        internal Accord.Point TopLeftCorner { get; }
        /// <summary>
        /// Gets the scale.
        /// </summary>
        internal float Scale { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerFrameData"/> class.
        /// </summary>
        /// <param name="keypoints">The keypoints.</param>
        /// <param name="topLeftCorner">The top left corner.</param>
        /// <param name="scale">The scale.</param>
        public PlayerFrameData(float[] keypoints, Accord.Point topLeftCorner, float scale)
        {
            Keypoints = keypoints;
            TopLeftCorner = topLeftCorner;
            Scale = scale;
         }
    }
}
